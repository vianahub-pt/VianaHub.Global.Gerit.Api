using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.ClientFiscalData;

public class ActivateClientFiscalDataValidator : AbstractValidator<ClientFiscalDataEntity>
{
    public ActivateClientFiscalDataValidator(ILocalizationService localization)
    {
        RuleFor(x => x.IsDeleted)
            .Equal(false).WithMessage(localization.GetMessage("ClientFiscalData_Deleted"));

        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0).WithMessage(localization.GetMessage("ModifiedBy_Required"));
    }
}
