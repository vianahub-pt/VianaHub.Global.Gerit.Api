using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.ClientFiscalData;

public class CreateClientFiscalDataValidator : AbstractValidator<ClientFiscalDataEntity>
{
    public CreateClientFiscalDataValidator(ILocalizationService localization)
    {
        Include(new ClientFiscalDataValidator(localization));

        RuleFor(x => x.TenantId)
            .GreaterThan(0).WithMessage(localization.GetMessage("TenantId_Required"));

        RuleFor(x => x.ClientId)
            .GreaterThan(0).WithMessage(localization.GetMessage("ClientId_Required"));

        RuleFor(x => x.CreatedBy)
            .GreaterThan(0).WithMessage(localization.GetMessage("CreatedBy_Required"));
    }
}
