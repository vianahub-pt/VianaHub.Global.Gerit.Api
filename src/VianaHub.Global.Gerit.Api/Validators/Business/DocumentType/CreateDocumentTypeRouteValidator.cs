using VianaHub.Global.Gerit.Domain.Helpers;
using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.DocumentType;

/// <summary>
/// Validador de rota para criação de DocumentType.
/// </summary>
public class CreateDocumentTypeRouteValidator : AbstractValidator<CreateDocumentTypeRequest>
{
    public CreateDocumentTypeRouteValidator(ILocalizationService localization)
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
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationNameInvalid", 100))
                .MaximumLength(100)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationNameInvalid", 100));
            translation.RuleFor(t => t.Description).MaximumLength(300)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationDescriptionInvalid", 300));
        }).When(x => x.Translations != null && x.Translations.All(t => t != null));
    }
}
