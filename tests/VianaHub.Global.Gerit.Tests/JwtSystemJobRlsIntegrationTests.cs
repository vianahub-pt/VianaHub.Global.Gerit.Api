using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Job;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Interceptors;
using VianaHub.Global.Gerit.Infra.Data.Repository.Billing;
using VianaHub.Global.Gerit.Infra.Data.Repository.Identity;
using VianaHub.Global.Gerit.Infra.Data.Security;
using VianaHub.Global.Gerit.Infra.Job.Jobs.Security;
using VianaHub.Global.Gerit.Infra.Job.Services;
using Xunit.Abstractions;

namespace VianaHub.Global.Gerit.Tests;

public sealed class SqlServerRlsFactAttribute : FactAttribute
{
    public SqlServerRlsFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")))
            Skip = "SQL Server/RLS integration requires GERIT_RLS_TEST_CONNECTIONSTRING; no SQL evidence was executed.";
    }
}

[Trait("Category", "SqlServerIntegration")]
public class JwtSystemJobRlsIntegrationTests(ITestOutputHelper output)
{
    [SqlServerRlsFact]
    public async Task RotationJob_DiscoversTenantsUnderRealRlsAndUsesOnlyTenantScopedKeyConnections()
    {
        await using var fixture = new SqlFixture(output);
        var baseline = await fixture.InspectPoolAsync();
        AssertClear(baseline);
        Assert.True(await fixture.HasEnabledTenantPredicateAsync(), "The test database must have its real Tenants FILTER predicate enabled.");

        using (var ordinary = fixture.Services.CreateScope())
        {
            Assert.Null(fixture.Http.HttpContext);
            Assert.Null(ordinary.ServiceProvider.GetRequiredService<IRequestTenantContext>().TenantId);
            var visible = await ordinary.ServiceProvider.GetRequiredService<ITenantDataRepository>().GetAllAsync(default);
            Assert.Empty(visible.Select(tenant => tenant.Id));
        }

        using (var job = fixture.Services.CreateScope())
        using (JobContext.EnterBackgroundJobScope(nameof(JwtKeyRotationJob), Guid.NewGuid().ToString("N")))
        {
            var discovery = job.ServiceProvider.GetRequiredService<IJwtSystemJobTenantDiscovery>();
            var rotation = new JwtKeyRotationJob(fixture.Services.GetRequiredService<IServiceScopeFactory>(), discovery,
                NullLogger<JwtKeyRotationJob>.Instance, Mock.Of<ILocalizationService>());
            await rotation.Execute();
        }

        var active = fixture.Evidence.Discovered.Where(t => t.IsActive && !t.IsDeleted).Select(t => t.Id).Order().ToArray();
        Assert.Contains(1, active);
        Assert.Equal(1, fixture.Evidence.DiscoveryCalls);
        Assert.Equal(active, fixture.Evidence.Calls.Where(c => c.Operation == "GetActive").Select(c => c.TenantId).Order().ToArray());
        Assert.Equal(active, fixture.Evidence.Calls.Where(c => c.Operation == "RotateReadOnly").Select(c => c.TenantId).Order().ToArray());
        Assert.All(fixture.Evidence.Calls, call =>
        {
            Assert.Equal(call.TenantId, call.Session.TenantId);
            Assert.Null(call.Session.IsSuperAdmin);
            Assert.Equal(baseline.Spid, call.Session.Spid);
        });
        Assert.Contains(fixture.Evidence.Calls, call => call.TenantId == 1 && call.Operation == "GetActive" && call.SigningKeyPresent);
        Assert.All(fixture.Evidence.Contexts, context => Assert.Null(context.TenantId));
        var after = await fixture.InspectPoolAsync();
        AssertClear(after);
        Assert.Equal(baseline.Spid, after.Spid);
        output.WriteLine($"RLS real: active tenants={active.Length}; tenant 1 processed; pool SPID={after.Spid}; TenantId=null; IsSuperAdmin=null.");
    }

    [SqlServerRlsFact]
    public async Task AdministrativeEnumeration_SqlFailureAfterGrantClearsSamePooledSession()
    {
        // master é somente leitura neste teste: a ausência de dbo.Tenants força erro 208 após o grant.
        await using var fixture = new SqlFixture(output, useMaster: true);
        var before = await fixture.InspectPoolAsync();
        AssertClear(before);
        using var scope = fixture.Services.CreateScope();
        using var job = JobContext.EnterBackgroundJobScope(nameof(JwtKeyRotationJob), Guid.NewGuid().ToString("N"));
        var discovery = scope.ServiceProvider.GetRequiredService<SqlJwtSystemJobTenantDiscovery>();

        var failure = await Assert.ThrowsAsync<SqlException>(() => discovery.GetAllAsync(default));

        Assert.Equal(208, failure.Number);
        var after = await fixture.InspectPoolAsync();
        AssertClear(after);
        Assert.Equal(before.Spid, after.Spid);
        output.WriteLine($"SQL failure 208: pool SPID={after.Spid}; TenantId=null; IsSuperAdmin=null after finally.");
    }

    [SqlServerRlsFact]
    public async Task AdministrativeEnumeration_CancellationDoesNotLeakPrivilegeToPool()
    {
        await using var fixture = new SqlFixture(output);
        AssertClear(await fixture.InspectPoolAsync());
        using var scope = fixture.Services.CreateScope();
        using var job = JobContext.EnterBackgroundJobScope(nameof(JwtKeyRotationJob), Guid.NewGuid().ToString("N"));
        var discovery = scope.ServiceProvider.GetRequiredService<SqlJwtSystemJobTenantDiscovery>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => discovery.GetAllAsync(cancellation.Token));

        var after = await fixture.InspectPoolAsync();
        AssertClear(after);
        output.WriteLine($"Cancellation: pool SPID={after.Spid}; TenantId=null; IsSuperAdmin=null.");
    }

    [Fact]
    public async Task AdministrativeEnumeration_RejectsOrdinaryScopeBeforeOpeningSqlConnection()
    {
        await using var fixture = new SqlFixture(output, requireConnection: false);
        using var scope = fixture.Services.CreateScope();
        var discovery = scope.ServiceProvider.GetRequiredService<SqlJwtSystemJobTenantDiscovery>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => discovery.GetAllAsync(default));
    }

    [Fact]
    public async Task AdministrativeEnumeration_RejectsHttpEvenWhenJobNameIsPresent()
    {
        await using var fixture = new SqlFixture(output, requireConnection: false);
        using var scope = fixture.Services.CreateScope();
        using var job = JobContext.EnterBackgroundJobScope(nameof(JwtKeyRotationJob), Guid.NewGuid().ToString("N"));
        fixture.Http.HttpContext = new DefaultHttpContext();
        try
        {
            var discovery = scope.ServiceProvider.GetRequiredService<SqlJwtSystemJobTenantDiscovery>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => discovery.GetAllAsync(default));
        }
        finally
        {
            fixture.Http.HttpContext = null;
        }
    }

    private static void AssertClear(SessionSnapshot session)
    {
        Assert.Null(session.TenantId);
        Assert.Null(session.IsSuperAdmin);
    }

    private sealed record SessionSnapshot(int Spid, int? TenantId, int? IsSuperAdmin);
    private sealed record KeyCall(string Operation, int TenantId, SessionSnapshot Session, bool SigningKeyPresent);

    private sealed class SqlEvidence
    {
        public int DiscoveryCalls { get; set; }
        public IReadOnlyList<SystemJobTenant> Discovered { get; set; } = [];
        public List<KeyCall> Calls { get; } = [];
        public List<IRequestTenantContext> Contexts { get; } = [];
    }

    private sealed class SqlFixture : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly ITestOutputHelper _output;
        private readonly bool _requireConnection;
        public HttpContextAccessor Http { get; } = new();
        public SqlEvidence Evidence { get; } = new();
        public ServiceProvider Services { get; }

        public SqlFixture(ITestOutputHelper output, bool useMaster = false, bool requireConnection = true)
        {
            _output = output;
            _requireConnection = requireConnection;
            var configured = requireConnection
                ? Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING")
                : "Server=unused.invalid;Database=unused;Integrated Security=true;Connect Timeout=1";
            Assert.False(string.IsNullOrWhiteSpace(configured), "Set GERIT_RLS_TEST_CONNECTIONSTRING to execute SQL Server/RLS evidence.");
            var builder = new SqlConnectionStringBuilder(configured)
            {
                Encrypt = true,
                ApplicationName = "Gerit.JwtRlsTest." + Guid.NewGuid().ToString("N"),
                MaxPoolSize = 1,
                MinPoolSize = 0,
                Pooling = true,
                MultipleActiveResultSets = false,
                ConnectRetryCount = 0
            };
            if (useMaster) builder.InitialCatalog = "master";
            _connectionString = builder.ConnectionString;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IHttpContextAccessor>(Http);
            services.AddSingleton<IJwtSystemJobContext, JwtSystemJobContext>();
            services.AddSingleton(Evidence);
            services.AddScoped<IRequestTenantContext, RequestTenantContext>();
            services.AddScoped<TenantSessionConnectionInterceptor>();
            services.AddScoped<TenantSessionCommandInterceptor>();
            services.AddDbContext<GeritDbContext>((provider, options) => options.UseSqlServer(_connectionString)
                .AddInterceptors(provider.GetRequiredService<TenantSessionConnectionInterceptor>(), provider.GetRequiredService<TenantSessionCommandInterceptor>()));
            services.AddScoped<ITenantDataRepository, TenantDataRepository>();
            services.AddScoped<IJwtKeyDataRepository, JwtKeyDataRepository>();
            services.AddScoped<SqlJwtSystemJobTenantDiscovery>();
            services.AddScoped<IJwtSystemJobTenantDiscovery>(provider => new RecordingDiscovery(
                provider.GetRequiredService<SqlJwtSystemJobTenantDiscovery>(), this));
            services.AddScoped<IJwtKeyDomainService>(provider => new ReadOnlyKeyDomain(
                provider.GetRequiredService<GeritDbContext>(), provider.GetRequiredService<IJwtKeyDataRepository>(),
                provider.GetRequiredService<IRequestTenantContext>(), Evidence, output));
            Services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        public async Task<SessionSnapshot> InspectPoolAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            try
            {
                return await ReadSessionAsync(connection);
            }
            finally
            {
                await SqlRlsSessionContext.ResetAsync(connection, CancellationToken.None);
            }
        }

        public async Task<bool> HasEnabledTenantPredicateAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sys.security_predicates p JOIN sys.security_policies s ON p.object_id = s.object_id WHERE p.target_object_id = OBJECT_ID(N'dbo.Tenants') AND s.is_enabled = 1 AND p.predicate_type_desc = N'FILTER' AND p.predicate_definition LIKE N'%fn_TenantAccessPredicate%'";
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            Http.HttpContext = null;
            if (_requireConnection)
            {
                using var connection = new SqlConnection(_connectionString);
                SqlConnection.ClearPool(connection);
            }
        }

        private sealed class RecordingDiscovery(SqlJwtSystemJobTenantDiscovery inner, SqlFixture fixture) : IJwtSystemJobTenantDiscovery
        {
            public async Task<IReadOnlyList<SystemJobTenant>> GetAllAsync(CancellationToken ct)
            {
                fixture.Evidence.DiscoveryCalls++;
                var tenants = await inner.GetAllAsync(ct);
                fixture.Evidence.Discovered = tenants;
                var session = await fixture.InspectPoolAsync();
                AssertClear(session);
                fixture._output.WriteLine($"Administrative enumeration complete: tenants={tenants.Count}; SPID={session.Spid}; TenantId=null; IsSuperAdmin=null before tenant operations.");
                return tenants;
            }
        }
    }

    private static async Task<SessionSnapshot> ReadSessionAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT @@SPID, TRY_CONVERT(int, SESSION_CONTEXT(N'TenantId')), TRY_CONVERT(int, SESSION_CONTEXT(N'IsSuperAdmin'))";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return new SessionSnapshot(reader.GetInt16(0), reader.IsDBNull(1) ? null : reader.GetInt32(1), reader.IsDBNull(2) ? null : reader.GetInt32(2));
    }

    private sealed class ReadOnlyKeyDomain : IJwtKeyDomainService
    {
        private readonly GeritDbContext _db;
        private readonly IJwtKeyDataRepository _repository;
        private readonly IRequestTenantContext _tenant;
        private readonly SqlEvidence _evidence;
        private readonly ITestOutputHelper _output;

        public ReadOnlyKeyDomain(GeritDbContext db, IJwtKeyDataRepository repository, IRequestTenantContext tenant, SqlEvidence evidence, ITestOutputHelper output)
        {
            _db = db;
            _repository = repository;
            _tenant = tenant;
            _evidence = evidence;
            _output = output;
            evidence.Contexts.Add(tenant);
        }

        public async Task<JwtKeysEntity?> GetActiveKeyAsync(int tenantId, CancellationToken ct)
        {
            Assert.Equal(tenantId, _tenant.TenantId);
            await _db.Database.OpenConnectionAsync(ct);
            try
            {
                var key = await _repository.GetActiveKeyAsync(tenantId, ct);
                var session = await ReadSessionAsync(_db.Database.GetDbConnection());
                Assert.Equal(tenantId, session.TenantId);
                Assert.Null(session.IsSuperAdmin);
                if (key is not null) Assert.Equal(tenantId, key.TenantId);
                _evidence.Calls.Add(new KeyCall("GetActive", tenantId, session, key is not null));
                _output.WriteLine($"Tenant {tenantId} signing-key query: found={key is not null}; SPID={session.Spid}; SQL TenantId={session.TenantId}; IsSuperAdmin=null.");
                return key;
            }
            finally
            {
                await _db.Database.CloseConnectionAsync();
            }
        }

        public async Task<int> RotateKeysAsync(CancellationToken ct)
        {
            Assert.NotNull(_tenant.TenantId);
            await _db.Database.OpenConnectionAsync(ct);
            try
            {
                // A descoberta e as queries são reais; a fixture não altera chaves do banco compartilhado.
                var candidates = (await _repository.GetKeysEligibleForRotationAsync(ct)).ToList();
                var session = await ReadSessionAsync(_db.Database.GetDbConnection());
                Assert.Equal(_tenant.TenantId, session.TenantId);
                Assert.Null(session.IsSuperAdmin);
                Assert.True(candidates.All(key => key.TenantId == _tenant.TenantId), "Rotation query returned a key from another tenant.");
                _evidence.Calls.Add(new KeyCall("RotateReadOnly", _tenant.TenantId.Value, session, false));
                _output.WriteLine($"Tenant {_tenant.TenantId} rotation query: candidates={candidates.Count}; read-only; SQL TenantId={session.TenantId}; IsSuperAdmin=null.");
                return 0;
            }
            finally
            {
                await _db.Database.CloseConnectionAsync();
            }
        }

        public Task<JwtKeysEntity?> EnsureKeyExistsAsync(int tenantId, int createdBy, CancellationToken ct)
        {
            Assert.Equal(tenantId, _tenant.TenantId);
            return _repository.GetActiveKeyAsync(tenantId, ct);
        }

        private static Task<T> NoWrites<T>() => throw new InvalidOperationException("This SQL integration fixture is read-only.");
        public Task<JwtKeysEntity?> GetByIdAsync(int id, CancellationToken ct) => NoWrites<JwtKeysEntity?>();
        public Task<JwtKeysEntity?> GetByKeyIdAsync(Guid keyId, CancellationToken ct) => NoWrites<JwtKeysEntity?>();
        public Task<IEnumerable<JwtKeysEntity>> GetAllAsync(CancellationToken ct) => NoWrites<IEnumerable<JwtKeysEntity>>();
        public Task<IEnumerable<JwtKeysEntity>> GetByTenantAsync(int tenantId, CancellationToken ct) => NoWrites<IEnumerable<JwtKeysEntity>>();
        public Task<JwtKeysEntity?> CreateAsync(JwtKeysEntity entity, CancellationToken ct) => NoWrites<JwtKeysEntity?>();
        public Task<bool> ActivateAsync(JwtKeysEntity key, CancellationToken ct) => NoWrites<bool>();
        public Task<bool> DeactivateAsync(JwtKeysEntity key, CancellationToken ct) => NoWrites<bool>();
        public Task<bool> RevokeAsync(int id, string reason, int modifiedBy, CancellationToken ct) => NoWrites<bool>();
        public Task<bool> DeleteAsync(JwtKeysEntity key, CancellationToken ct) => NoWrites<bool>();
        public Task<bool> UpdateRotationPolicyAsync(int id, int rotationPolicyDays, int overlapPeriodDays, int modifiedBy, CancellationToken ct) => NoWrites<bool>();
        public Task<(string PublicKey, string PrivateKeyEncrypted)> GenerateKeyPairAsync(string? algorithm, int keySize, CancellationToken ct) => NoWrites<(string, string)>();
        public Task<string> DecryptPrivateKeyAsync(string encryptedPrivateKey, CancellationToken ct) => NoWrites<string>();
        public Task<int> CleanupExpiredKeysAsync(int retentionDays, CancellationToken ct) => NoWrites<int>();
    }
}
