using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.Plan;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Billing.Plan;

public class CreatePlanRouteValidator : AbstractValidator<CreatePlanRequest>
{
    private readonly ILocalizationService _localization;

    public CreatePlanRouteValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Translations)
            .Must(translations => !string.IsNullOrEmpty(VianaHub.Global.Gerit.Domain.Helpers.CatalogCodeGenerator.FromCanonicalTranslation(translations, t => t.LanguageCode, t => t.Name)))
            .WithMessage(localization.GetMessage("Application.Catalog.CanonicalEnglishNameRequired"));
        RuleFor(x => x.Translations)
            .Must(translations => translations != null && translations.All(t => t != null) && translations.Select(t => t.LanguageCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() == translations.Count)
            .WithMessage(localization.GetMessage("Application.Catalog.TranslationLanguageInvalid"));
        RuleForEach(x => x.Translations).ChildRules(translation =>
        {
            translation.RuleFor(t => t.LanguageCode).NotEmpty().WithMessage(localization.GetMessage("Application.Catalog.TranslationLanguageInvalid")).MaximumLength(10)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationLanguageInvalid"));
            translation.RuleFor(t => t.Name).NotEmpty().WithMessage(localization.GetMessage("Application.Catalog.TranslationNameInvalid", 100)).MaximumLength(100)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationNameInvalid", 100));
            translation.RuleFor(t => t.Description).MaximumLength(500)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationDescriptionInvalid", 500));
        }).When(x => x.Translations != null && x.Translations.All(t => t != null));
        _localization = localization;



        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.Plan.Create.Currency"))
            .MaximumLength(3).WithMessage(_localization.GetMessage("Api.Validator.Plan.Create.Currency.MaximumLength", 3));

        RuleFor(x => x.MaxUsers)
            .GreaterThan(0).WithMessage(_localization.GetMessage("Api.Validator.Plan.Create.MaxUsers"));

        RuleFor(x => x.MaxPhotosPerVisit)
            .GreaterThan(0).WithMessage(_localization.GetMessage("Api.Validator.Plan.Create.MaxPhotosPerVisit"));

        When(x => x.PricePerHour.HasValue, () =>
        {
            RuleFor(x => x.PricePerHour)
                .GreaterThanOrEqualTo(0).WithMessage(_localization.GetMessage("Api.Validator.Plan.Create.PricePerHour"));
        });

        When(x => x.PricePerDay.HasValue, () =>
        {
            RuleFor(x => x.PricePerDay)
                .GreaterThanOrEqualTo(0).WithMessage(_localization.GetMessage("Api.Validator.Plan.Create.PricePerDay"));
        });

        When(x => x.PricePerMonth.HasValue, () =>
        {
            RuleFor(x => x.PricePerMonth)
                .GreaterThanOrEqualTo(0).WithMessage(_localization.GetMessage("Api.Validator.Plan.Create.PricePerMonth"));
        });

        When(x => x.PricePerYear.HasValue, () =>
        {
            RuleFor(x => x.PricePerYear)
                .GreaterThanOrEqualTo(0).WithMessage(_localization.GetMessage("Api.Validator.Plan.Create.PricePerYear"));
        });
    }
}
