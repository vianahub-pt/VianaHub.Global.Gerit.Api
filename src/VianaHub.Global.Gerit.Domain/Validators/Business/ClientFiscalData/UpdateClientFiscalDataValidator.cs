using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.ClientFiscalData;

public class UpdateClientFiscalDataValidator : AbstractValidator<ClientFiscalDataEntity>
{
    public UpdateClientFiscalDataValidator(ILocalizationService localization)
    {
        Include(new ClientFiscalDataValidator(localization));

        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0).WithMessage(localization.GetMessage("ModifiedBy_Required"));
    }
}
