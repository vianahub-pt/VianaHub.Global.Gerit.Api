using FluentValidation;
using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.ClientFiscalData;

public class ClientFiscalDataValidator : AbstractValidator<ClientFiscalDataEntity>, IEntityDomainValidator<ClientFiscalDataEntity>
{
    public ClientFiscalDataValidator()
    {
        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("TaxNumber_Required")
            .MaximumLength(20).WithMessage("TaxNumber_MaxLength");

        RuleFor(x => x.VatNumber)
            .MaximumLength(20).WithMessage("VatNumber_MaxLength")
            .NotEmpty().WithMessage("VatNumber_Required")
            .When(x => x.IsVatRegistered);

        RuleFor(x => x.FiscalCountry)
            .NotEmpty().WithMessage("FiscalCountry_Required")
            .Length(2).WithMessage("FiscalCountry_Length");

        RuleFor(x => x.IBAN)
            .MaximumLength(34).WithMessage("IBAN_MaxLength");

        RuleFor(x => x.FiscalEmail)
            .MaximumLength(255).WithMessage("FiscalEmail_MaxLength")
            .EmailAddress().WithMessage("FiscalEmail_Invalid")
            .When(x => !string.IsNullOrEmpty(x.FiscalEmail));
    }

    public async Task<ValidationResult> ValidateForCreateAsync(ClientFiscalDataEntity entity)
        => await new CreateClientFiscalDataValidator().ValidateAsync(entity);

    public async Task<ValidationResult> ValidateForUpdateAsync(ClientFiscalDataEntity entity)
        => await new UpdateClientFiscalDataValidator().ValidateAsync(entity);

    public async Task<ValidationResult> ValidateForActivateAsync(ClientFiscalDataEntity entity)
        => await new ActivateClientFiscalDataValidator().ValidateAsync(entity);

    public async Task<ValidationResult> ValidateForDeactivateAsync(ClientFiscalDataEntity entity)
        => await new DeactivateClientFiscalDataValidator().ValidateAsync(entity);

    public async Task<ValidationResult> ValidateForDeleteAsync(ClientFiscalDataEntity entity)
        => await new DeleteClientFiscalDataValidator().ValidateAsync(entity);

    public Task<ValidationResult> ValidateForRevokeAsync(ClientFiscalDataEntity entity)
        => ValidateForDeleteAsync(entity);
}
