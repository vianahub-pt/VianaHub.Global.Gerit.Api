using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;
using VianaHub.Global.Gerit.Application.Services.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Services.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Validators.Business.PartyType;
using VianaHub.Global.Gerit.Infra.Data.Context;
using VianaHub.Global.Gerit.Infra.Data.Mappings.Business;
using VianaHub.Global.Gerit.Infra.Data.Repository.Business;

namespace VianaHub.Global.Gerit.Tests;

[Trait("Category", "SqlServerIntegration")]
public class PartyTypeIdSqlIntegrationTests
{
    [SqlServerRlsFact]
    public async Task ApplicationReturnsDatabaseGeneratedIdsAndPersistsTranslationForeignKeys()
    {
        await using var fixture = new SqlFixture();
        await fixture.InitializeAsync();
        await using var context = fixture.CreateContext();
        var notify = new Notify();
        var app = CreateApplication(context, notify);

        var first = await app.CreateAsync(Request("Dynamic One"), CancellationToken.None);
        var second = await app.CreateAsync(Request("Dynamic Two"), CancellationToken.None);

        Assert.InRange(first, (byte)3, byte.MaxValue);
        Assert.True(second > first);
        Assert.False(notify.HasNotify());
        // O retorno deve ser o ID materializado pelo EF, não apenas um valor calculado.
        Assert.Equal(first, context.PartyTypes.Local.Single(p => p.Code == "DYNAMIC_ONE").Id);
        Assert.Equal(second, context.PartyTypes.Local.Single(p => p.Code == "DYNAMIC_TWO").Id);
        await using var verification = fixture.CreateContext();
        await AssertGraphAsync(verification, first, "DYNAMIC_ONE");
        await AssertGraphAsync(verification, second, "DYNAMIC_TWO");
        var seeds = await verification.PartyTypes.Where(p => p.Id <= 2).OrderBy(p => p.Id).ToListAsync();
        Assert.Equal(new[] { "INDIVIDUAL", "ORGANIZATION" }, seeds.Select(p => p.Code));
        Assert.Equal(new byte[] { 1, 2 }, seeds.Select(p => p.Id));
        var key = verification.Model.FindEntityType(typeof(PartyTypeEntity))!.FindPrimaryKey()!.Properties.Single();
        Assert.Equal(typeof(byte), key.ClrType);
        Assert.Equal(typeof(PartyTypeEntity), key.PropertyInfo!.DeclaringType);
        Assert.Equal(ValueGenerated.OnAdd, key.ValueGenerated);
        Assert.Equal(1, await verification.Database.SqlQuery<int>(
            $"SELECT COLUMNPROPERTY(OBJECT_ID(N'dbo.PartyTypes'), N'Id', 'IsIdentity') AS [Value]").SingleAsync());
    }

    [SqlServerRlsFact]
    public async Task ConcurrentCreatesPersistDistinctDatabaseGeneratedIdsAndBothGraphs()
    {
        await using var fixture = new SqlFixture();
        await fixture.InitializeAsync();
        await using var firstContext = fixture.CreateContext();
        await using var secondContext = fixture.CreateContext();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Entity("CONCURRENT_FIRST");
        var second = Entity("CONCURRENT_SECOND");
        async Task<bool> CreateAsync(GeritDbContext context, PartyTypeEntity entity)
        {
            await start.Task;
            return await new PartyTypeDataRepository(context).AddAsync(entity, CancellationToken.None);
        }
        var firstTask = CreateAsync(firstContext, first);
        var secondTask = CreateAsync(secondContext, second);
        start.SetResult();

        Assert.All(await Task.WhenAll(firstTask, secondTask), success => Assert.True(success));
        Assert.NotEqual(first.Id, second.Id);
        Assert.InRange(first.Id, (byte)3, byte.MaxValue);
        Assert.InRange(second.Id, (byte)3, byte.MaxValue);
        await using var verification = fixture.CreateContext();
        await AssertGraphAsync(verification, first.Id, first.Code!);
        await AssertGraphAsync(verification, second.Id, second.Code!);
    }

    [SqlServerRlsFact]
    public async Task FailedGraphInsertRollsBackParentAndTranslationsWithoutAssumingGaplessIdentity()
    {
        await using var fixture = new SqlFixture();
        await fixture.InitializeAsync();
        await using var context = fixture.CreateContext();
        var invalid = Entity("ROLLBACK");
        invalid.Translations.Add(new PartyTypeTranslationsEntity(0, "en-US", "Duplicate language", null));

        await Assert.ThrowsAsync<DbUpdateException>(() => new PartyTypeDataRepository(context).AddAsync(invalid, CancellationToken.None));

        Assert.Null(context.Database.CurrentTransaction);
        await using var verification = fixture.CreateContext();
        Assert.False(await verification.PartyTypes.AnyAsync(p => p.Code == "ROLLBACK"));
        Assert.False(await verification.Set<PartyTypeTranslationsEntity>().AnyAsync());
        var valid = Entity("AFTER_ROLLBACK");
        Assert.True(await new PartyTypeDataRepository(verification).AddAsync(valid, CancellationToken.None));
        Assert.InRange(valid.Id, (byte)3, byte.MaxValue);
        await AssertGraphAsync(verification, valid.Id, valid.Code!);
    }

    private static PartyTypeEntity Entity(string code)
    {
        var entity = new PartyTypeEntity(code, 1);
        entity.Translations.Add(new PartyTypeTranslationsEntity(0, "en-US", code, null));
        entity.Translations.Add(new PartyTypeTranslationsEntity(0, "pt-PT", $"PT {code}", null));
        return entity;
    }

    private static CreatePartyTypeRequest Request(string name) => new()
    {
        Translations = [new() { LanguageCode = "en-US", Name = name }, new() { LanguageCode = "pt-PT", Name = $"PT {name}" }]
    };

    private static PartyTypeAppService CreateApplication(GeritDbContext context, Notify notify)
    {
        var localization = new Mock<ILocalizationService>();
        localization.Setup(l => l.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        localization.Setup(l => l.GetMessage(It.IsAny<string>(), It.IsAny<object?[]>()))
            .Returns((string key, object?[] _) => key);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(u => u.GetUserId()).Returns(1);
        var repository = new PartyTypeDataRepository(context);
        var domain = new PartyTypeDomainService(repository, new PartyTypeValidator(localization.Object), notify);
        return new PartyTypeAppService(repository, domain, Mock.Of<IMapper>(), notify, localization.Object, currentUser.Object,
            NullLogger<PartyTypeAppService>.Instance);
    }

    private static async Task AssertGraphAsync(GeritDbContext context, byte id, string code)
    {
        var entity = await context.PartyTypes.Include(p => p.Translations).SingleAsync(p => p.Id == id);
        Assert.Equal(id, entity.Id);
        Assert.Equal(code, entity.Code);
        Assert.Equal(2, entity.Translations.Count);
        Assert.All(entity.Translations, t => Assert.Equal(id, t.PartyTypeId));
        Assert.Equal(new[] { "en-US", "pt-PT" }, entity.Translations.Select(t => t.LanguageCode).OrderBy(l => l));
        Assert.DoesNotContain(entity.Translations, t => t.PartyTypeId == 0);
    }

    private sealed class SchemaContext(DbContextOptions<SchemaContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new PartyTypeMapping());
            modelBuilder.ApplyConfiguration(new PartyTypeTranslationsMapping());
            modelBuilder.Entity<PartyTypeEntity>().HasQueryFilter(p => !p.IsDeleted);
        }
    }

    private sealed class SqlFixture : IAsyncDisposable
    {
        private readonly string _database = $"Gerit_PartyTypeIdTest_{Guid.NewGuid():N}";
        private readonly SchemaContext _schema;
        public string ConnectionString { get; }

        public SqlFixture()
        {
            // Base efémera exclusiva deste teste; nunca usa o catálogo da aplicação.
            var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("GERIT_RLS_TEST_CONNECTIONSTRING"))
            {
                Encrypt = true,
                InitialCatalog = _database,
                Pooling = false
            };
            ConnectionString = builder.ConnectionString;
            _schema = new SchemaContext(new DbContextOptionsBuilder<SchemaContext>().UseSqlServer(ConnectionString).Options);
        }

        public async Task InitializeAsync()
        {
            await _schema.Database.EnsureCreatedAsync();
            // Seeds somente na base isolada; a criação positiva usa o repository real.
            await _schema.Database.ExecuteSqlRawAsync("""
                SET IDENTITY_INSERT dbo.PartyTypes ON;
                INSERT INTO dbo.PartyTypes (Id, Code, CreatedBy)
                VALUES (1, N'INDIVIDUAL', 1), (2, N'ORGANIZATION', 1);
                SET IDENTITY_INSERT dbo.PartyTypes OFF;
                """);
        }

        public GeritDbContext CreateContext() => new(
            new DbContextOptionsBuilder<GeritDbContext>()
                .UseSqlServer(ConnectionString, sql => sql.CommandTimeout(10))
                .UseModel(_schema.Model).Options);

        public async ValueTask DisposeAsync()
        {
            // Validar o catálogo antes de eliminar exclusivamente a base gerada pelo teste.
            Assert.StartsWith("Gerit_PartyTypeIdTest_", _database);
            Assert.Equal(_database, new SqlConnectionStringBuilder(ConnectionString).InitialCatalog);
            await _schema.DisposeAsync();
            // EnsureDeleted limpa todos os pools SQL e interfere nos testes RLS de reutilização de sessão.
            // Com Pooling=false, basta remover a base própria por conexão administrativa independente.
            var builder = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master", Encrypt = true };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                IF @database NOT LIKE N'Gerit[_]PartyTypeIdTest[_]%'
                    THROW 51000, 'Unexpected integration test database.', 1;
                IF DB_ID(@database) IS NOT NULL
                BEGIN
                    DECLARE @sql nvarchar(max) = N'DROP DATABASE ' + QUOTENAME(@database);
                    EXEC sys.sp_executesql @sql;
                END;
                """;
            command.Parameters.Add("@database", System.Data.SqlDbType.NVarChar, 128).Value = _database;
            await command.ExecuteNonQueryAsync();
        }
    }
}
