using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Identity.UserPreferences;

public class ActivateUserPreferencesValidator : AbstractValidator<UserPreferencesEntity>
{
    public ActivateUserPreferencesValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.UserPreferences.InvalidId"));

        RuleFor(x => x.IsDeleted)
            .Equal(false)
            .WithMessage(localization.GetMessage("Domain.UserPreferences.CannotActivateDeleted"));

    }
}
