using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Infra.Job.Interfaces;

namespace VianaHub.Global.Gerit.Infra.Job.Jobs.Cleanup;

/// <summary>
/// Job de limpeza de chaves JWT expiradas
/// Executado semanalmente aos domingos às 04:00 UTC
/// </summary>
public class CleanupExpiredJwtKeysJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IJwtSystemJobTenantDiscovery _tenantDiscovery;
    private readonly ILogger<CleanupExpiredJwtKeysJob> _logger;
    private const int RetentionDays = 90; // Período de retenção padrão

    public CleanupExpiredJwtKeysJob(
        IServiceScopeFactory scopeFactory,
        IJwtSystemJobTenantDiscovery tenantDiscovery,
        ILogger<CleanupExpiredJwtKeysJob> logger)
    {
        _scopeFactory = scopeFactory;
        _tenantDiscovery = tenantDiscovery;
        _logger = logger;
    }

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("?? [CleanupExpiredJwtKeysJob] Iniciando limpeza de chaves JWT expiradas");

        try
        {
            var deletedCount = 0;
            var tenants = await _tenantDiscovery.GetAllAsync(cancellationToken);
            _logger.LogInformation("[CleanupExpiredJwtKeysJob] Tenants to process: {Count}", tenants.Count(t => !t.IsDeleted));
            foreach (var tenant in tenants.Where(t => !t.IsDeleted))
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var scope = _scopeFactory.CreateScope();
                var tenantContext = scope.ServiceProvider.GetRequiredService<IRequestTenantContext>();
                var previousTenantId = tenantContext.TenantId;
                tenantContext.SetTenantId(tenant.Id);
                try
                {
                    var jwtKeyService = scope.ServiceProvider.GetRequiredService<IJwtKeyDomainService>();
                    var tenantDeletedCount = await jwtKeyService.CleanupExpiredKeysAsync(RetentionDays, cancellationToken);
                    deletedCount += tenantDeletedCount;
                    _logger.LogInformation("[CleanupExpiredJwtKeysJob] Tenant {TenantId} processed. DeletedCount={DeletedCount}",
                        tenant.Id, tenantDeletedCount);
                }
                finally
                {
                    if (previousTenantId.HasValue)
                        tenantContext.SetTenantId(previousTenantId.Value);
                    else
                        tenantContext.Clear();
                }
            }

            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "? [CleanupExpiredJwtKeysJob] Limpeza concluída com sucesso. Chaves removidas: {DeletedCount}",
                    deletedCount);
            }
            else
            {
                _logger.LogInformation(
                    "?? [CleanupExpiredJwtKeysJob] Nenhuma chave expirada encontrada para limpeza");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? [CleanupExpiredJwtKeysJob] Erro durante limpeza de chaves JWT");
            throw;
        }
    }
}
