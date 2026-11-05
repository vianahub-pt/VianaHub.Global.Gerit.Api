using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Billing.TenantContactPerson;

/// <summary>
/// Validador para exclusão de TenantContact
/// </summary>
public class DeleteTenantContactPersonValidator : AbstractValidator<TenantContactPersonsEntity>
{
    public DeleteTenantContactPersonValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.TenantContactPerson.IdRequired"));

    }
}
