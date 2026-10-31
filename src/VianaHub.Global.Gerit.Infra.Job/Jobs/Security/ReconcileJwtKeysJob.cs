using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Infra.Job.Interfaces;

namespace VianaHub.Global.Gerit.Infra.Job.Jobs.Security;

/// <summary>
/// Job de reconciliação de chaves JWT para aplicações legadas.
/// Garante que cada tenant ativo tenha pelo menos uma chave JWT válida para assinatura.
/// Executa uma única vez na inicialização ou sob demanda.
/// </summary>
public class ReconcileJwtKeysJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IJwtSystemJobTenantDiscovery _tenantDiscovery;
    private readonly ILogger<ReconcileJwtKeysJob> _logger;

    /// <summary>
    /// ID do sistema para operações automatizadas
    /// </summary>
    private static readonly int SystemUserId = 0;

    public ReconcileJwtKeysJob(
        IServiceScopeFactory scopeFactory,
        IJwtSystemJobTenantDiscovery tenantDiscovery,
        ILogger<ReconcileJwtKeysJob> logger)
    {
        _scopeFactory = scopeFactory;
        _tenantDiscovery = tenantDiscovery;
        _logger = logger;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("?? [ReconcileJwtKeysJob] Iniciando reconciliação de chaves JWT para aplicações legadas");

        var startTime = DateTime.UtcNow;
        var totalProcessed = 0;
        var totalCreated = 0;
        var totalSkipped = 0;
        var totalErrors = 0;

        // 1. Buscar todos os tenants ativos
        var tenants = await _tenantDiscovery.GetAllAsync(cancellationToken);
        var activeTenants = tenants.Where(t => t.IsActive && !t.IsDeleted).ToList();

        _logger.LogInformation("?? [ReconcileJwtKeysJob] Encontrados {TenantCount} tenants ativos para processar", activeTenants.Count);

        // 2. Processar tenants ativos
        foreach (var tenant in activeTenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await ProcessTenantAsync(tenant.Id, cancellationToken);

            totalProcessed += result.Processed;
            totalCreated += result.Created;
            totalSkipped += result.Skipped;
            totalErrors += result.Errors;
        }

        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "? [ReconcileJwtKeysJob] Reconciliação concluída com sucesso. " +
            "Processados: {Processed}, Criados: {Created}, Pulados: {Skipped}, Erros: {Errors}, Duração: {Duration}s",
            totalProcessed, totalCreated, totalSkipped, totalErrors, duration.TotalSeconds);
    }

    /// <summary>
    /// Processa somente o tenant recebido em uma sessão isolada.
    /// </summary>
    private async Task<ProcessingTenantResult> ProcessTenantAsync(int tenantId, CancellationToken cancellationToken)
    {
        var result = new ProcessingTenantResult { Processed = 1 };
        using var scope = _scopeFactory.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<IRequestTenantContext>();
        var previousTenantId = tenantContext.TenantId;
        tenantContext.SetTenantId(tenantId);
        try
        {
            var jwtKeyRepo = scope.ServiceProvider.GetRequiredService<IJwtKeyDataRepository>();
            var jwtKeyDomain = scope.ServiceProvider.GetRequiredService<IJwtKeyDomainService>();
            // Verificar se já existe chave ativa
            var existingKey = await jwtKeyRepo.GetActiveKeyAsync(tenantId, cancellationToken);

            if (existingKey != null)
            {
                _logger.LogInformation("[ReconcileJwtKeysJob] Tenant {TenantId} processed. SigningKeyId={SigningKeyId}, Created={Created}",
                    tenantId, existingKey.KeyId, false);
                _logger.LogDebug(
                    "[ReconcileJwtKeysJob] Tenant {TenantId}: chave JWT ativa já existe (KeyId: {KeyId}), pulando",
                    tenantId, existingKey.KeyId);
                result.Skipped++;
                return result;
            }

            // Criar chave JWT automaticamente
            _logger.LogInformation(
                "[ReconcileJwtKeysJob] Tenant {TenantId}: nenhuma chave JWT encontrada, criando automaticamente",
                tenantId);

            var newKey = await jwtKeyDomain.EnsureKeyExistsAsync(tenantId, SystemUserId, cancellationToken);

            if (newKey != null)
            {
                _logger.LogInformation("[ReconcileJwtKeysJob] Tenant {TenantId} processed. SigningKeyId={SigningKeyId}, Created={Created}",
                    tenantId, newKey.KeyId, true);
                _logger.LogInformation(
                    "[ReconcileJwtKeysJob] Tenant {TenantId}: chave JWT criada com sucesso (Id: {KeyId})", tenantId, newKey.KeyId);
                result.Created++;
            }
            else
            {
                _logger.LogWarning(
                    "[ReconcileJwtKeysJob] Tenant {TenantId}: falha ao criar chave JWT (resultado nulo)",
                    tenantId);
                result.Errors++;
            }
        }
        finally
        {
            if (previousTenantId.HasValue)
                tenantContext.SetTenantId(previousTenantId.Value);
            else
                tenantContext.Clear();
        }

        return result;
    }

    /// <summary>
    /// Resultado do processamento de aplicações
    /// </summary>
    private class ProcessingTenantResult
    {
        public int Processed { get; set; }
        public int Created { get; set; }
        public int Skipped { get; set; }
        public int Errors { get; set; }
    }
}

