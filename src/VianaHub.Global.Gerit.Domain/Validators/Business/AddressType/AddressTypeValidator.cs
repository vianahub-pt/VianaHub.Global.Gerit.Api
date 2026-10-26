using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.AddressType;

public class AddressTypeValidator : BaseEntityValidator<AddressTypeEntity>
{

    public AddressTypeValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(AddressTypeEntity entity)
    {
        var validator = new CreateAddressTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(AddressTypeEntity entity)
    {
        var validator = new UpdateAddressTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(AddressTypeEntity entity)
    {
        var validator = new ActivateAddressTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(AddressTypeEntity entity)
    {
        var validator = new DeactivateAddressTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(AddressTypeEntity entity)
    {
        var validator = new DeleteAddressTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(AddressTypeEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
