using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.Vehicle;

public class VehicleValidator : BaseEntityValidator<VehicleEntity>
{

    public VehicleValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(VehicleEntity entity)
    {
        var validator = new CreateVehicleValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(VehicleEntity entity)
    {
        var validator = new UpdateVehicleValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(VehicleEntity entity)
    {
        var validator = new ActivateVehicleValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(VehicleEntity entity)
    {
        var validator = new DeactivateVehicleValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(VehicleEntity entity)
    {
        var validator = new DeleteVehicleValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(VehicleEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
