using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.ClientFiscalData;

public class DeleteClientFiscalDataValidator : AbstractValidator<ClientFiscalDataEntity>
{
    public DeleteClientFiscalDataValidator()
    {
        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0).WithMessage("ModifiedBy_Required");
    }
}
