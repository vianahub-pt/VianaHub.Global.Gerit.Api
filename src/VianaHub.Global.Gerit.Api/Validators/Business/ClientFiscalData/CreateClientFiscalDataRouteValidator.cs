using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.ClientFiscalData;

public class CreateClientFiscalDataRouteValidator : AbstractValidator<CreateClientFiscalDataRequest>
{
    public CreateClientFiscalDataRouteValidator(ILocalizationService localization)
    {
        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage(localization.GetMessage("TaxNumber_Required"))
            .MaximumLength(20).WithMessage(localization.GetMessage("TaxNumber_MaxLength"));

        RuleFor(x => x.FiscalCountry).NotEmpty().WithMessage(localization.GetMessage("FiscalCountry_Required")).Length(2).WithMessage(localization.GetMessage("FiscalCountry_Length"));
        RuleFor(x => x.VatNumber).MaximumLength(20).WithMessage(localization.GetMessage("VatNumber_MaxLength")).NotEmpty().WithMessage(localization.GetMessage("VatNumber_Required")).When(x => x.IsVatRegistered);
        RuleFor(x => x.FiscalEmail).EmailAddress().WithMessage(localization.GetMessage("FiscalEmail_Invalid")).MaximumLength(255).WithMessage(localization.GetMessage("FiscalEmail_MaxLength")).When(x => !string.IsNullOrWhiteSpace(x.FiscalEmail));
        RuleFor(x => x.IBAN).MaximumLength(34).WithMessage(localization.GetMessage("IBAN_MaxLength"));
    }
}
