using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Billing.TenantAddress;

/// <summary>
/// Validador para exclusão de TenantAddress
/// </summary>
public class DeleteTenantAddressValidator : AbstractValidator<TenantAddressesEntity>
{
    public DeleteTenantAddressValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.TenantAddress.IdRequired"));

    }
}
