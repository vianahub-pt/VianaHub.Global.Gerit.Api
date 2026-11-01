using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.EmployeeAddress;

/// <summary>
/// Validador para exclus�o de EmployeeAddress
/// </summary>
public class DeleteEmployeeAddressValidator : AbstractValidator<EmployeeAddressesEntity>
{
    private readonly ILocalizationService _localization;

    public DeleteEmployeeAddressValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(_localization.GetMessage("Domain.EmployeeAddress.IdRequired"));

        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0)
            .WithMessage(_localization.GetMessage("Domain.EmployeeAddress.ModifiedByRequired"));

    }
}
