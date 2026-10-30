using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Infra.Job.Jobs.Cleanup;
using VianaHub.Global.Gerit.Infra.Job.Jobs.Security;

namespace VianaHub.Global.Gerit.Tests;

public class JwtKeyJobTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rotation_ProcessesActiveTenantsOnceWithSeparateTenantContexts(bool keyExists)
    {
        using var fixture = new JobFixture(keyExists, previousTenantId: 77);

        await fixture.Rotation().Execute();

        Assert.Equal(new int?[] { 1, 2 }, fixture.TenantsFor("GetActive"));
        Assert.Equal(new int?[] { 1, 2 }, fixture.TenantsFor("Rotate"));
        Assert.Equal(keyExists ? Array.Empty<int?>() : new int?[] { 1, 2 }, fixture.TenantsFor("Ensure"));
        Assert.Equal(2, fixture.Contexts.Count);
        Assert.NotSame(fixture.Contexts[0], fixture.Contexts[1]);
        Assert.All(fixture.Contexts, context => Assert.Equal(77, context.TenantId));
        fixture.TenantRepo.Verify(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rotation_ClearsTenantContextsAfterSuccess()
    {
        using var fixture = new JobFixture();

        await fixture.Rotation().Execute();

        Assert.Equal(2, fixture.Contexts.Count);
        Assert.All(fixture.Contexts, context => Assert.Null(context.TenantId));
    }

    [Fact]
    public async Task Rotation_PropagatesRotationFailureAndClearsContext()
    {
        using var fixture = new JobFixture { FailingOperation = "Rotate" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Rotation().Execute());

        Assert.Null(Assert.Single(fixture.Contexts).TenantId);
    }

    [Fact]
    public async Task Reconcile_QueriesOnlyEachActiveTenantOnce()
    {
        using var fixture = new JobFixture();

        await fixture.Reconcile().Execute();

        Assert.Equal(new int?[] { 1, 2 }, fixture.TenantsFor("RepositoryGetActive"));
        Assert.Equal(new int?[] { 1, 2 }, fixture.TenantsFor("Ensure"));
        Assert.Equal(2, fixture.Contexts.Count);
        Assert.All(fixture.Contexts, context => Assert.Null(context.TenantId));
        fixture.TenantRepo.Verify(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reconcile_SkipsExistingKeyAndRestoresContext()
    {
        using var fixture = new JobFixture(keyExists: true, previousTenantId: 77);

        await fixture.Reconcile().Execute();

        Assert.Equal(new int?[] { 1, 2 }, fixture.TenantsFor("RepositoryGetActive"));
        Assert.Empty(fixture.TenantsFor("Ensure"));
        Assert.All(fixture.Contexts, context => Assert.Equal(77, context.TenantId));
    }

    [Fact]
    public async Task Reconcile_PropagatesFailureAndClearsContext()
    {
        using var fixture = new JobFixture { FailingOperation = "Ensure" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Reconcile().Execute());

        Assert.Null(Assert.Single(fixture.Contexts).TenantId);
    }

    [Fact]
    public async Task Cleanup_KeepsRetentionAndIncludesInactiveTenantsInSeparateScopes()
    {
        using var fixture = new JobFixture();

        await fixture.Cleanup().Execute();

        Assert.Equal(new int?[] { 1, 2, 3 }, fixture.TenantsFor("Cleanup"));
        Assert.Equal(new[] { 90, 90, 90 }, fixture.RetentionDays);
        Assert.Equal(3, fixture.Contexts.Count);
        Assert.All(fixture.Contexts, context => Assert.Null(context.TenantId));
    }

    [Fact]
    public async Task Cleanup_PropagatesFailureAndRestoresContext()
    {
        using var fixture = new JobFixture(previousTenantId: 77) { FailingOperation = "Cleanup" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Cleanup().Execute());

        Assert.Equal(77, Assert.Single(fixture.Contexts).TenantId);
    }

    private sealed class JobFixture : IDisposable
    {
        private readonly ServiceProvider _services;
        public Mock<ITenantDataRepository> TenantRepo { get; } = new();
        public List<RecordingTenantContext> Contexts { get; } = new();
        public List<int> RetentionDays { get; } = new();
        public string? FailingOperation { get; init; }
        private readonly List<(string Operation, int? TenantId)> _calls = new();

        public JobFixture(bool keyExists = false, int? previousTenantId = null)
        {
            var tenants = new[] { Tenant(1), Tenant(2), Tenant(3), Tenant(4) };
            tenants[2].Deactivate(0);
            tenants[3].Delete(0);
            TenantRepo.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tenants);
            var services = new ServiceCollection();
            services.AddScoped<IRequestTenantContext>(_ =>
            {
                var context = new RecordingTenantContext(previousTenantId);
                Contexts.Add(context);
                return context;
            });
            services.AddScoped<IJwtKeyDomainService>(provider =>
            {
                var context = provider.GetRequiredService<IRequestTenantContext>();
                var domain = new Mock<IJwtKeyDomainService>();
                domain.Setup(service => service.GetActiveKeyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns((int tenantId, CancellationToken _) =>
                    {
                        Record("GetActive", context, tenantId);
                        return Task.FromResult<JwtKeysEntity?>(keyExists ? Key(tenantId) : null);
                    });
                domain.Setup(service => service.EnsureKeyExistsAsync(It.IsAny<int>(), 0, It.IsAny<CancellationToken>()))
                    .Returns((int tenantId, int _, CancellationToken _) =>
                    {
                        Record("Ensure", context, tenantId);
                        return Task.FromResult<JwtKeysEntity?>(Key(tenantId));
                    });
                domain.Setup(service => service.RotateKeysAsync(It.IsAny<CancellationToken>()))
                    .Returns(() =>
                    {
                        Record("Rotate", context);
                        return Task.FromResult(1);
                    });
                domain.Setup(service => service.CleanupExpiredKeysAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns((int days, CancellationToken _) =>
                    {
                        Record("Cleanup", context);
                        RetentionDays.Add(days);
                        return Task.FromResult(1);
                    });
                return domain.Object;
            });
            services.AddScoped<IJwtKeyDataRepository>(provider =>
            {
                var context = provider.GetRequiredService<IRequestTenantContext>();
                var repository = new Mock<IJwtKeyDataRepository>();
                repository.Setup(repo => repo.GetActiveKeyAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns((int tenantId, CancellationToken _) =>
                    {
                        Record("RepositoryGetActive", context, tenantId);
                        return Task.FromResult<JwtKeysEntity?>(keyExists ? Key(tenantId) : null);
                    });
                return repository.Object;
            });
            _services = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        public int?[] TenantsFor(string operation) => _calls.Where(call => call.Operation == operation).Select(call => call.TenantId).ToArray();

        public JwtKeyRotationJob Rotation() => new(_services.GetRequiredService<IServiceScopeFactory>(), TenantRepo.Object,
            NullLogger<JwtKeyRotationJob>.Instance, Mock.Of<ILocalizationService>());

        public ReconcileJwtKeysJob Reconcile() => new(_services.GetRequiredService<IServiceScopeFactory>(), TenantRepo.Object,
            NullLogger<ReconcileJwtKeysJob>.Instance);

        public CleanupExpiredJwtKeysJob Cleanup() => new(_services.GetRequiredService<IServiceScopeFactory>(), TenantRepo.Object,
            NullLogger<CleanupExpiredJwtKeysJob>.Instance);

        private void Record(string operation, IRequestTenantContext context, int? expectedTenantId = null)
        {
            Assert.NotNull(context.TenantId);
            if (expectedTenantId.HasValue) Assert.Equal(expectedTenantId, context.TenantId);
            _calls.Add((operation, context.TenantId));
            if (FailingOperation == operation) throw new InvalidOperationException("Simulated job operation failure");
        }

        public void Dispose() => _services.Dispose();

        private static TenantEntity Tenant(int id)
        {
            var tenant = new TenantEntity(1, 1, "Tenant", null, null, null, null, 0);
            typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(tenant, id);
            return tenant;
        }

        private static JwtKeysEntity Key(int tenantId) => new(tenantId, "public", "encrypted", 0);
    }

    private sealed class RecordingTenantContext(int? tenantId) : IRequestTenantContext
    {
        public int? TenantId { get; private set; } = tenantId;
        public void SetTenantId(int value) => TenantId = value;
        public void Clear() => TenantId = null;
    }
}
