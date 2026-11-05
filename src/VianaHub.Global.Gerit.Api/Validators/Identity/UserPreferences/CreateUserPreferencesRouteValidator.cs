using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.UserPreferences;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Identity.UserPreferences;

public class CreateUserPreferencesRouteValidator : AbstractValidator<CreateUserPreferencesRequest>
{
    private readonly ILocalizationService _localization;

    public CreateUserPreferencesRouteValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Appearance)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.Appearance"))
            .MaximumLength(10).WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.Appearance.MaximumLength", 10))
            .Must(x => x is "light" or "dark").WithMessage(_localization.GetMessage("Domain.UserPreferences.AppearanceInvalid"));

        RuleFor(x => x.Locale)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.Locale"))
            .MaximumLength(10).WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.Locale.MaximumLength", 10));

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.Timezone"))
            .MaximumLength(100).WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.Timezone.MaximumLength", 100));

        RuleFor(x => x.DateFormat)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.DateFormat"))
            .MaximumLength(20).WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.DateFormat.MaximumLength", 20));

        RuleFor(x => x.TimeFormat)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.TimeFormat"))
            .MaximumLength(10).WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.TimeFormat.MaximumLength", 10))
            .Must(x => x is "24h" or "12h").WithMessage(_localization.GetMessage("Domain.UserPreferences.TimeFormatInvalid"));

        RuleFor(x => x.CurrencyCode).NotEmpty().WithMessage(_localization.GetMessage("Domain.UserPreferences.CurrencyCodeRequired"))
            .MaximumLength(3).WithMessage(_localization.GetMessage("Domain.UserPreferences.CurrencyCodeMaxLength"));
        RuleFor(x => x.DayEnd).NotEmpty().WithMessage(_localization.GetMessage("Domain.UserPreferences.DayEndFormat"))
            .Matches(@"^([0-1][0-9]|2[0-3]):[0-5][0-9]$").WithMessage(_localization.GetMessage("Domain.UserPreferences.DayEndFormat"));

        RuleFor(x => x.DayStart)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.DayStart"))
            .Matches(@"^([0-1][0-9]|2[0-3]):[0-5][0-9]$")
            .WithMessage(_localization.GetMessage("Api.Validator.UserPreferences.Create.DayStart.Format"));
    }
}
