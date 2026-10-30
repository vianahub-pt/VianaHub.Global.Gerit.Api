using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Infra.Job.Interfaces;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Infra.Job.Jobs.Security;

/// <summary>
/// Job de rotação automática de chaves JWT
/// A agenda e o fuso horário são definidos exclusivamente pelo Hangfire.
/// </summary>
public class JwtKeyRotationJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITenantDataRepository _tenantRepo;
    private readonly ILogger<JwtKeyRotationJob> _logger;
    private readonly ILocalizationService _localization;

    /// <summary>
    /// ID do sistema para operações automatizadas
    /// </summary>
    private static readonly int SystemUserId = 0;

    public JwtKeyRotationJob(
        IServiceScopeFactory scopeFactory,
        ITenantDataRepository tenantRepo,
        ILogger<JwtKeyRotationJob> logger,
        ILocalizationService localization)
    {
        _scopeFactory = scopeFactory;
        _tenantRepo = tenantRepo;
        _logger = logger;
        _localization = localization;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[JwtKeyRotationJob] " + _localization.GetMessage("Job.SyncDefinitions.Starting"));

        try
        {
            // 0. Garantir que cada tenant ativo possua uma chave ativa (reconciliação mínima)
            var tenants = await _tenantRepo.GetAllAsync(cancellationToken);
            var activeTenants = tenants.Where(t => t.IsActive && !t.IsDeleted).ToList();

            _logger.LogInformation("[JwtKeyRotationJob] " + _localization.GetMessage("Job.SyncDefinitions.Starting") + " Verificando existência de chaves para {Count} tenants ativos", activeTenants.Count);

            var createdCount = 0;
            var rotatedCount = 0;
            foreach (var tenant in activeTenants)
            {
                if (cancellationToken.IsCancellationRequested) break;

                using var scope = _scopeFactory.CreateScope();
                var tenantContext = scope.ServiceProvider.GetRequiredService<IRequestTenantContext>();
                var previousTenantId = tenantContext.TenantId;
                tenantContext.SetTenantId(tenant.Id);
                try
                {
                    var jwtKeyService = scope.ServiceProvider.GetRequiredService<IJwtKeyDomainService>();
                    try
                    {
                        var existing = await jwtKeyService.GetActiveKeyAsync(tenant.Id, cancellationToken);
                        if (existing != null)
                        {
                            _logger.LogDebug("[JwtKeyRotationJob] Tenant {TenantId} já possui chave ativa (KeyId={KeyId})", tenant.Id, existing.KeyId);
                        }
                        else
                        {
                            var newKey = await jwtKeyService.EnsureKeyExistsAsync(tenant.Id, SystemUserId, cancellationToken);
                            if (newKey != null)
                            {
                                createdCount++;
                                _logger.LogInformation("[JwtKeyRotationJob] Tenant {TenantId}: chave JWT criada (KeyId={KeyId})", tenant.Id, newKey.KeyId);
                            }
                            else
                            {
                                _logger.LogError("[JwtKeyRotationJob] Tenant {TenantId}: falha ao criar chave JWT", tenant.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[JwtKeyRotationJob] Erro ao garantir chave para Tenant {TenantId}", tenant.Id);
                    }

                    // A consulta de rotação usa a mesma sessão SQL isolada pelo tenant.
                    rotatedCount += await jwtKeyService.RotateKeysAsync(cancellationToken);
                }
                finally
                {
                    if (previousTenantId.HasValue)
                        tenantContext.SetTenantId(previousTenantId.Value);
                    else
                        tenantContext.Clear();
                }
            }

            if (createdCount > 0)
            {
                _logger.LogInformation("[JwtKeyRotationJob] Reconciliação de chaves finalizada. Chaves criadas: {CreatedCount}", createdCount);
            }

            if (rotatedCount > 0)
            {
                _logger.LogInformation(_localization.GetMessage("Job.SyncDefinitions.Completed") + ": Rotação concluída com sucesso. Chaves rotacionadas: {RotatedCount}", rotatedCount);
            }
            else
            {
                _logger.LogInformation("[JwtKeyRotationJob] " + _localization.GetMessage("Job.SyncDefinitions.Completed") + " Nenhuma chave elegível para rotação encontrada");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JwtKeyRotationJob] " + _localization.GetMessage("Job.SyncDefinitions.Error"));
            throw;
        }
    }
}
