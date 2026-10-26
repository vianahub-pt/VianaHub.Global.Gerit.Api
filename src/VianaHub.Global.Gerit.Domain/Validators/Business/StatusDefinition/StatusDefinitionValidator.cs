using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.StatusDefinition;

public class StatusDefinitionValidator : BaseEntityValidator<StatusDefinitionEntity>
{

    public StatusDefinitionValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(StatusDefinitionEntity entity)
    {
        var validator = new CreateStatusDefinitionValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(StatusDefinitionEntity entity)
    {
        var validator = new UpdateStatusDefinitionValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(StatusDefinitionEntity entity)
    {
        var validator = new ActivateStatusDefinitionValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(StatusDefinitionEntity entity)
    {
        var validator = new DeactivateStatusDefinitionValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(StatusDefinitionEntity entity)
    {
        var validator = new DeleteStatusDefinitionValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(StatusDefinitionEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
