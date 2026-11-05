using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.EquipmentType;

public class EquipmentTypeValidator : BaseEntityValidator<EquipmentTypeEntity>
{

    public EquipmentTypeValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(EquipmentTypeEntity entity)
    {
        var validator = new CreateEquipmentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(EquipmentTypeEntity entity)
    {
        var validator = new UpdateEquipmentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(EquipmentTypeEntity entity)
    {
        var validator = new ActivateEquipmentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(EquipmentTypeEntity entity)
    {
        var validator = new DeactivateEquipmentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(EquipmentTypeEntity entity)
    {
        var validator = new DeleteEquipmentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(EquipmentTypeEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
