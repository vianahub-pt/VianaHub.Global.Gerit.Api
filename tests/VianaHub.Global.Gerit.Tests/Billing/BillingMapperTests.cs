using System.Globalization;
using System.Text.Json;
using VianaHub.Global.Gerit.Application.Mappings.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;
using Xunit;

namespace VianaHub.Global.Gerit.Tests.Billing;

public class BillingMapperTests
{
    [Theory]
    [InlineData("pt-PT", "Plano PT", "pt-PT")]
    [InlineData("pt-BR", "Plano PT", "pt-PT")]
    [InlineData("en-US", "Plan EN", "en-US")]
    [InlineData("es-ES", "Plano PT", "pt-PT")]
    public void PlanMapper_MapsRequestedTranslationOrPortugueseFallback(string culture, string expectedName, string expectedLanguage)
    {
        var plan = CreatePlan();
        plan.AddTranslation(new SubscriptionPlanTranslationsEntity(0, "pt-PT", "Plano PT", "Descrição PT"));
        plan.AddTranslation(new SubscriptionPlanTranslationsEntity(0, "en-US", "Plan EN", "Description EN"));

        var detail = PlanMapper.ToDetail(plan, culture);
        var summary = PlanMapper.ToResponse(plan, culture);

        Assert.Equal(expectedName, detail.Name);
        Assert.Equal(expectedName, summary.Name);
        Assert.Equal(expectedLanguage, detail.LanguageCode);
        Assert.Equal("Plano PT" == expectedName ? "Descrição PT" : "Description EN", detail.Description);
        Assert.Equal(expectedLanguage, summary.LanguageCode);
        Assert.Equal(expectedName, JsonDocument.Parse(JsonSerializer.Serialize(detail)).RootElement.GetProperty("Name").GetString());
    }

    [Fact]
    public void PlanMapper_PreservesEmptyTranslationAndReturnsNullWhenNoTranslationExists()
    {
        var plan = CreatePlan();
        plan.AddTranslation(new SubscriptionPlanTranslationsEntity(0, "en-US", string.Empty, string.Empty));
        Assert.Equal(string.Empty, PlanMapper.ToDetail(plan, "en-US").Name);
        Assert.Equal("en-US", PlanMapper.ToDetail(plan, "en-US").LanguageCode);

        var untranslated = PlanMapper.ToDetail(CreatePlan(), "es-ES");
        Assert.Null(untranslated.Name);
        Assert.Null(untranslated.Description);
        Assert.Null(untranslated.LanguageCode);
    }

    [Fact]
    public void PlanMapper_PagedPreservesEnvelopeAndMapsItems()
    {
        var plan = CreatePlan();
        plan.AddTranslation(new SubscriptionPlanTranslationsEntity(0, "pt-PT", "Plano", null));
        var page = PlanMapper.ToPaged(new ListPage<SubscriptionPlanEntity>(new[] { plan }, 2, 5, 6, 2), "pt-PT");
        Assert.Equal((2, 5, 6, 2), (page.PageNumber, page.PageSize, page.TotalItems, page.TotalPages));
        Assert.Equal("Plano", Assert.Single(page.Items).Name);
        Assert.Empty(PlanMapper.ToResponses(Array.Empty<SubscriptionPlanEntity>(), "pt-PT"));
    }

    [Fact]
    public void SubscriptionMapper_UsesControlledInstantForTrialAndDaysRemaining()
    {
        var now = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new SubscriptionEntity(3, 4, 5, 6, 10m, "month", "EUR", now.AddDays(-1), now.AddDays(4), now.AddDays(-2), now.AddDays(1), null, 7);
        var detail = SubscriptionMapper.ToDetail(subscription, "en-US", now);
        Assert.True(detail!.IsTrial);
        Assert.Equal(4, detail.DaysRemaining);
        Assert.Equal(now.AddDays(-1), detail.CurrentPeriodStart);
        Assert.Equal(now.AddDays(4), detail.CurrentPeriodEnd);
        Assert.Equal(string.Empty, detail.SubscriptionPlanName);
        Assert.Contains("\"IsTrial\":true", JsonSerializer.Serialize(detail));
    }

    [Fact]
    public void SubscriptionMapper_MapsDetailAndSummaryWithNullablePlanAndControlledTime()
    {
        var now = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new SubscriptionEntity(3, 4, 5, 6, 10m, "month", "EUR", now.AddDays(-10), now.AddDays(10), now.AddDays(-2), now.AddDays(3), null, 7);
        var detail = SubscriptionMapper.ToDetail(subscription, "pt-PT", now);
        var summary = SubscriptionMapper.ToResponse(subscription, "pt-PT");
        Assert.Equal(10m, detail!.AgreedAmount);
        Assert.Equal("EUR", detail.CurrencyCode);
        Assert.Equal(string.Empty, detail.SubscriptionPlanName);
        Assert.Equal(subscription.Id, summary.Id);
        Assert.Equal(string.Empty, summary.SubscriptionPlanName);
    }

    [Fact]
    public void SubscriptionMapper_ExpiredTrialAndPeriodHaveNoPositiveTimeRemaining()
    {
        var now = new DateTime(2026, 11, 22, 12, 0, 0, DateTimeKind.Utc);
        var subscription = new SubscriptionEntity(3, 4, 5, 6, 10m, "month", "EUR", now.AddDays(-10), now.AddDays(-1), now.AddDays(-3), now.AddDays(-1), null, 7);
        var detail = SubscriptionMapper.ToDetail(subscription, "pt-PT", now);
        Assert.False(detail!.IsTrial);
        Assert.Equal(0, detail.DaysRemaining);
    }

    private static SubscriptionPlanEntity CreatePlan() => new("PLAN", 1m, 2m, 3m, 4m, "EUR", 10, 20, 7);
}
