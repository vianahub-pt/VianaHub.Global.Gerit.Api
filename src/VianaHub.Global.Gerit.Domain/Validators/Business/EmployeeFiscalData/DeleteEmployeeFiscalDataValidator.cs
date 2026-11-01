using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.EmployeeFiscalData;

public class DeleteEmployeeFiscalDataValidator : AbstractValidator<EmployeeFiscalDataEntity>
{
    public DeleteEmployeeFiscalDataValidator()
    {
        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0).WithMessage("ModifiedBy_Required");
    }
}
