using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Identity.UserPreferences;

public class UserPreferencesValidator : BaseEntityValidator<UserPreferencesEntity>
{

    public UserPreferencesValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override Task<ValidationResult> ValidateForCreateAsync(UserPreferencesEntity entity)
    {
        var validator = new CreateUserPreferencesValidator(_localization);
        return validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForUpdateAsync(UserPreferencesEntity entity)
    {
        var validator = new UpdateUserPreferencesValidator(_localization);
        return validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForActivateAsync(UserPreferencesEntity entity)
    {
        var validator = new ActivateUserPreferencesValidator(_localization);
        return validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForDeactivateAsync(UserPreferencesEntity entity)
    {
        var validator = new DeactivateUserPreferencesValidator(_localization);
        return validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForDeleteAsync(UserPreferencesEntity entity)
    {
        var validator = new DeleteUserPreferencesValidator(_localization);
        return validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(UserPreferencesEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
