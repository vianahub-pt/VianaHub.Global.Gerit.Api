using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDefinition;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Application.Mappings.Business;

public class StatusDefinitionMappingProfileTests
{
    private readonly IMapper _mapper;

    public StatusDefinitionMappingProfileTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<StatusDefinitionMappingProfile>());
        var provider = services.BuildServiceProvider();
        _mapper = provider.GetRequiredService<IMapper>();
    }

    /// <summary>
    /// Cria uma StatusDefinitionEntity com dados base (Id, Tenant, StatusDomain, traduções).
    /// </summary>
    private static StatusDefinitionEntity CreateEntityWithTranslations(
        int tenantId, string tenantName,
        string statusDomainCode, string statusDomainPtName, string statusDomainEnName,
        string definitionCode, string definitionPtName, string definitionEnName)
    {
        // Domain
        var domain = new StatusDomainEntity(statusDomainCode, 1);
        // Adiciona traduções ao domínio (via reflection pois a coleção é ICollection com setter privado)
        ((List<StatusDomainTranslationsEntity>)domain.Translations)
            .Add(new StatusDomainTranslationsEntity(0, "pt-PT", statusDomainPtName, null));
        ((List<StatusDomainTranslationsEntity>)domain.Translations)
            .Add(new StatusDomainTranslationsEntity(0, "en-US", statusDomainEnName, null));
        typeof(StatusDomainEntity)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(domain, 10);

        // Tenant
        var tenant = new TenantEntity(1, 1, tenantName, "tenant@test.com", null, null, null, 1);
        typeof(TenantEntity)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(tenant, tenantId);

        // Definition
        var entity = new StatusDefinitionEntity(tenantId, 10, definitionCode, 1, false, 1);
        // Adiciona traduções à definição (via reflection)
        ((List<StatusDefinitionTranslationsEntity>)entity.Translations)
            .Add(new StatusDefinitionTranslationsEntity(tenantId, 10, 1, "pt-PT", definitionPtName, null));
        ((List<StatusDefinitionTranslationsEntity>)entity.Translations)
            .Add(new StatusDefinitionTranslationsEntity(tenantId, 10, 1, "en-US", definitionEnName, null));

        // Seta navigation properties e Id via reflection
        var entityType = typeof(StatusDefinitionEntity);
        entityType.GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(entity, 1);
        entityType.GetProperty("Tenant", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(entity, tenant);
        entityType.GetProperty("StatusDomain", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(entity, domain);

        return entity;
    }

    [Fact(DisplayName = "StatusDefinitionMapping — traduções resolvidas para pt-PT")]
    [Trait("Application", "Mapping")]
    public void Map_ShouldResolveFromPtPtTranslation()
    {
        var entity = CreateEntityWithTranslations(
            tenantId: 101, tenantName: "Empresa XPTO",
            statusDomainCode: "VISIT", statusDomainPtName: "Estado Visita PT", statusDomainEnName: "Visit Status EN",
            definitionCode: "PENDING", definitionPtName: "Pendente PT", definitionEnName: "Pending EN");

        var currentCulture = CultureInfo.CurrentCulture;
        var currentUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-PT");
            CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");

            var result = _mapper.Map<StatusDefinitionResponse>(entity);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(10, result.StatusDomainId);
            Assert.Equal("Pendente PT", result.Name);
            Assert.Equal("pt-PT", result.LanguageCode);
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUiCulture;
        }
    }

    [Fact(DisplayName = "StatusDefinitionMapping — traduções resolvidas para en-US")]
    [Trait("Application", "Mapping")]
    public void Map_ShouldResolveFromEnUsTranslation()
    {
        var entity = CreateEntityWithTranslations(
            tenantId: 202, tenantName: "Company ACME",
            statusDomainCode: "VISIT", statusDomainPtName: "Estado Visita PT", statusDomainEnName: "Visit Status EN",
            definitionCode: "PENDING", definitionPtName: "Pendente PT", definitionEnName: "Pending EN");

        var currentCulture = CultureInfo.CurrentCulture;
        var currentUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var result = _mapper.Map<StatusDefinitionResponse>(entity);

            Assert.NotNull(result);
            Assert.Equal("Pending EN", result.Name);
            Assert.Equal("en-US", result.LanguageCode);
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUiCulture;
        }
    }

    [Fact(DisplayName = "StatusDefinitionMapping — fallback para pt-PT quando idioma não encontrado")]
    [Trait("Application", "Mapping")]
    public void Map_ShouldFallbackToPtPt_WhenLanguageNotFound()
    {
        var entity = CreateEntityWithTranslations(
            tenantId: 303, tenantName: "Fallback Ltd",
            statusDomainCode: "VISIT", statusDomainPtName: "Estado Visita PT", statusDomainEnName: "Visit Status EN",
            definitionCode: "ACTIVE", definitionPtName: "Ativo PT", definitionEnName: "Active EN");

        var currentCulture = CultureInfo.CurrentCulture;
        var currentUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");

            var result = _mapper.Map<StatusDefinitionResponse>(entity);

            Assert.NotNull(result);
            Assert.Equal("Ativo PT", result.Name);
            Assert.Equal("pt-PT", result.LanguageCode); // fallback para pt-PT
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
            CultureInfo.CurrentUICulture = currentUiCulture;
        }
    }

    [Fact(DisplayName = "StatusDefinitionMapping — valores nulos quando sem traduções")]
    [Trait("Application", "Mapping")]
    public void Map_ShouldReturnNullValues_WhenNoTranslationsExist()
    {
        var tenant = new TenantEntity(1, 1, "Tenant Sem Traduções", "t@t.com", null, null, null, 1);
        typeof(TenantEntity)
            .GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(tenant, 999);

        var entity = new StatusDefinitionEntity(999, 0, "NOTRANS", 1, false, 1);
        var entityType = typeof(StatusDefinitionEntity);
        entityType.GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(entity, 5);
        entityType.GetProperty("Tenant", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(entity, tenant);

        var result = _mapper.Map<StatusDefinitionResponse>(entity);

        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Null(result.Name);
        Assert.Null(result.LanguageCode);
    }
}
