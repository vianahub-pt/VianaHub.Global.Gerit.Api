using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Billing.TenantFiscalData;

/// <summary>
/// Validador para exclusão de TenantFiscalData
/// </summary>
public class DeleteTenantFiscalDataValidator : AbstractValidator<TenantFiscalDataEntity>
{
    public DeleteTenantFiscalDataValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.TenantFiscalData.IdRequired"));

    }
}
