using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.StatusDomain;

public class StatusDomainValidator : BaseEntityValidator<StatusDomainEntity>
{

    public StatusDomainValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(StatusDomainEntity entity)
    {
        var validator = new CreateStatusDomainValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(StatusDomainEntity entity)
    {
        var validator = new UpdateStatusDomainValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(StatusDomainEntity entity)
    {
        var validator = new ActivateStatusDomainValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(StatusDomainEntity entity)
    {
        var validator = new DeactivateStatusDomainValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(StatusDomainEntity entity)
    {
        var validator = new DeleteStatusDomainValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(StatusDomainEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
