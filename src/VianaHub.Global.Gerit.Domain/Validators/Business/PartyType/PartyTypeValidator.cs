using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.PartyType;

public class PartyTypeValidator : BaseEntityValidator<PartyTypeEntity>
{

    public PartyTypeValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(PartyTypeEntity entity)
    {
        var validator = new CreatePartyTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(PartyTypeEntity entity)
    {
        var validator = new UpdatePartyTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(PartyTypeEntity entity)
    {
        var validator = new ActivatePartyTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(PartyTypeEntity entity)
    {
        var validator = new DeactivatePartyTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(PartyTypeEntity entity)
    {
        var validator = new DeletePartyTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(PartyTypeEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
