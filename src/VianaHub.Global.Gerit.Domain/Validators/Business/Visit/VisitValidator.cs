using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.Visit;

public class VisitValidator : BaseEntityValidator<VisitEntity>
{

    public VisitValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(VisitEntity entity)
    {
        var validator = new CreateVisitValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(VisitEntity entity)
    {
        var validator = new UpdateVisitValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(VisitEntity entity)
    {
        var validator = new ActivateVisitValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(VisitEntity entity)
    {
        var validator = new DeactivateVisitValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(VisitEntity entity)
    {
        var validator = new DeleteVisitValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(VisitEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
