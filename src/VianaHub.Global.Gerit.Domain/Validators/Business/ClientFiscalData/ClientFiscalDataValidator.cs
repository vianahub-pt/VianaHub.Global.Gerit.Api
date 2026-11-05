using FluentValidation;
using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.ClientFiscalData;

public class ClientFiscalDataValidator : AbstractValidator<ClientFiscalDataEntity>, IEntityDomainValidator<ClientFiscalDataEntity>
{
    private readonly ILocalizationService _localization;

    public ClientFiscalDataValidator(ILocalizationService localization)
    {
        _localization = localization;
        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage(localization.GetMessage("TaxNumber_Required"))
            .MaximumLength(20).WithMessage(localization.GetMessage("TaxNumber_MaxLength"));

        RuleFor(x => x.VatNumber)
            .MaximumLength(20).WithMessage(localization.GetMessage("VatNumber_MaxLength"))
            .NotEmpty().WithMessage(localization.GetMessage("VatNumber_Required"))
            .When(x => x.IsVatRegistered);

        RuleFor(x => x.FiscalCountry)
            .NotEmpty().WithMessage(localization.GetMessage("FiscalCountry_Required"))
            .Length(2).WithMessage(localization.GetMessage("FiscalCountry_Length"));

        RuleFor(x => x.IBAN)
            .MaximumLength(34).WithMessage(localization.GetMessage("IBAN_MaxLength"));

        RuleFor(x => x.FiscalEmail)
            .MaximumLength(255).WithMessage(localization.GetMessage("FiscalEmail_MaxLength"))
            .EmailAddress().WithMessage(localization.GetMessage("FiscalEmail_Invalid"))
            .When(x => !string.IsNullOrEmpty(x.FiscalEmail));
    }

    public async Task<ValidationResult> ValidateForCreateAsync(ClientFiscalDataEntity entity)
        => await new CreateClientFiscalDataValidator(_localization).ValidateAsync(entity);

    public async Task<ValidationResult> ValidateForUpdateAsync(ClientFiscalDataEntity entity)
        => await new UpdateClientFiscalDataValidator(_localization).ValidateAsync(entity);

    public async Task<ValidationResult> ValidateForActivateAsync(ClientFiscalDataEntity entity)
        => await new ActivateClientFiscalDataValidator(_localization).ValidateAsync(entity);

    public async Task<ValidationResult> ValidateForDeactivateAsync(ClientFiscalDataEntity entity)
        => await new DeactivateClientFiscalDataValidator(_localization).ValidateAsync(entity);

    public async Task<ValidationResult> ValidateForDeleteAsync(ClientFiscalDataEntity entity)
        => await new DeleteClientFiscalDataValidator(_localization).ValidateAsync(entity);

    public Task<ValidationResult> ValidateForRevokeAsync(ClientFiscalDataEntity entity)
        => ValidateForDeleteAsync(entity);
}
