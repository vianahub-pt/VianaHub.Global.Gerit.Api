using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.AddressType;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.AddressType;

public class CreateAddressTypeRouteValidator : AbstractValidator<CreateAddressTypeRequest>
{
    public CreateAddressTypeRouteValidator(ILocalizationService localization)
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
            translation.RuleFor(t => t.Name).NotEmpty().WithMessage(localization.GetMessage("Application.Catalog.TranslationNameInvalid", 200)).MaximumLength(200)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationNameInvalid", 200));
            translation.RuleFor(t => t.Description).MaximumLength(500)
                .WithMessage(localization.GetMessage("Application.Catalog.TranslationDescriptionInvalid", 500));
        }).When(x => x.Translations != null && x.Translations.All(t => t != null));

    }
}
