using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Infra.Data.Security;

public sealed class SqlJwtSystemJobTenantDiscovery(
    GeritDbContext database,
    IHttpContextAccessor httpContextAccessor,
    IJwtSystemJobContext jobContext,
    ILogger<SqlJwtSystemJobTenantDiscovery> logger) : IJwtSystemJobTenantDiscovery
{
    public async Task<IReadOnlyList<SystemJobTenant>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (httpContextAccessor.HttpContext is not null || !jobContext.IsAuthorized)
            throw new InvalidOperationException("JWT tenant discovery requires a JWT system-job scope without HTTP context.");

        // Conexão exclusiva, sem compartilhar privilégio com DbContexts tenant-scoped.
        await using var connection = new SqlConnection(database.Database.GetConnectionString());
        var tenants = new List<SystemJobTenant>();
        try
        {
            await connection.OpenAsync(cancellationToken);
            await SqlRlsSessionContext.ResetAsync(connection, cancellationToken);
            await using (var grant = connection.CreateCommand())
            {
                grant.CommandText = "EXEC sp_set_session_context @key=N'IsSuperAdmin', @value=1;";
                await grant.ExecuteNonQueryAsync(cancellationToken);
            }
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, IsActive, IsDeleted FROM dbo.Tenants WHERE IsDeleted = 0 ORDER BY Id;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                tenants.Add(new SystemJobTenant(reader.GetInt32(0), reader.GetBoolean(1), reader.GetBoolean(2)));
        }
        finally
        {
            // Cleanup não cancelável: nenhum privilégio pode retornar ao pool, mesmo em erro/cancelamento.
            try
            {
                if (connection.State == ConnectionState.Open)
                    await SqlRlsSessionContext.ResetAsync(connection, CancellationToken.None);
                else
                    SqlConnection.ClearPool(connection);
            }
            catch
            {
                SqlConnection.ClearPool(connection);
                throw;
            }
        }

        logger.LogInformation("[JWT-JOB-RLS] Tenant discovery completed. Count={Count}; admin and tenant session context cleared", tenants.Count);
        return tenants;
    }
}
