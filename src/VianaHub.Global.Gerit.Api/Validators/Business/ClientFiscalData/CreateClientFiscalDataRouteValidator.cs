using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientFiscalData;

namespace VianaHub.Global.Gerit.Api.Validators.Business.ClientFiscalData;

public class CreateClientFiscalDataRouteValidator : AbstractValidator<CreateClientFiscalDataRequest>
{
    public CreateClientFiscalDataRouteValidator()
    {
        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("TaxNumber_Required")
            .MaximumLength(20).WithMessage("TaxNumber_MaxLength");

        RuleFor(x => x.FiscalCountry).NotEmpty().Length(2);
        RuleFor(x => x.VatNumber).MaximumLength(20).NotEmpty().When(x => x.IsVatRegistered);
        RuleFor(x => x.FiscalEmail).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.FiscalEmail));
        RuleFor(x => x.IBAN).MaximumLength(34);
    }
}
