using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.Equipment;

public class EquipmentValidator : BaseEntityValidator<EquipmentEntity>
{

    public EquipmentValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(EquipmentEntity entity)
    {
        var validator = new CreateEquipmentValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(EquipmentEntity entity)
    {
        var validator = new UpdateEquipmentValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(EquipmentEntity entity)
    {
        var validator = new ActivateEquipmentValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(EquipmentEntity entity)
    {
        var validator = new DeactivateEquipmentValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(EquipmentEntity entity)
    {
        var validator = new DeleteEquipmentValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(EquipmentEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
