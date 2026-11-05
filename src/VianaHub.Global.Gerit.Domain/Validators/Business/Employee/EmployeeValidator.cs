using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.Employee;

public class EmployeeValidator : BaseEntityValidator<EmployeeEntity>
{

    public EmployeeValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(EmployeeEntity entity)
    {
        var validator = new CreateEmployeeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(EmployeeEntity entity)
    {
        var validator = new UpdateEmployeeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(EmployeeEntity entity)
    {
        var validator = new ActivateEmployeeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(EmployeeEntity entity)
    {
        var validator = new DeactivateEmployeeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(EmployeeEntity entity)
    {
        var validator = new DeleteEmployeeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(EmployeeEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
