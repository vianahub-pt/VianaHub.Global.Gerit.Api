using VianaHub.Global.Gerit.Domain.Helpers;
using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.StatusDefinition;

/// <summary>
/// Validador de rota para criação de StatusDefinition.
/// </summary>
public class CreateStatusDefinitionRouteValidator : AbstractValidator<CreateStatusDefinitionRequest>
{
    public CreateStatusDefinitionRouteValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Translations)
            .Must(translations => translations != null && translations.All(t => t != null) && !string.IsNullOrEmpty(CatalogCodeGenerator.FromCanonicalTranslation(translations, t => t.LanguageCode, t => t.Name)))
            .WithMessage(localization.GetMessage("Application.Catalog.CanonicalEnglishNameRequired"));
        RuleFor(x => x.Translations)
            .Must(translations => translations == null || translations.Where(t => t != null).Select(t => t.LanguageCode).Distinct(StringComparer.OrdinalIgnoreCase).Count() == translations.Count)
            .WithMessage(localization.GetMessage("Application.Catalog.TranslationLanguagesUnique"));
        RuleForEach(x => x.Translations).ChildRules(translation =>
        {
            translation.RuleFor(t => t.LanguageCode).NotEmpty()
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationLanguageInvalid"))
                .MaximumLength(10)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationLanguageInvalid"));
            translation.RuleFor(t => t.Name).NotEmpty()
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationNameInvalid", 200))
                .MaximumLength(200)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationNameInvalid", 200));
            translation.RuleFor(t => t.Description).MaximumLength(500)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationDescriptionInvalid", 500));
        }).When(x => x.Translations != null && x.Translations.All(t => t != null));
        RuleFor(x => x.StatusDomainId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Api.Validator.StatusDefinition.Create.StatusDomainId"));


        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0)
            .WithMessage(localization.GetMessage("Api.Validator.StatusDefinition.Create.DisplayOrder"));
    }
}
