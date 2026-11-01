using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.EmployeeContact;

/// <summary>
/// Validator para ativa��o de EmployeeContact
/// </summary>
public class ActivateEmployeeContactPersonValidator : AbstractValidator<EmployeeContactPersonsEntity>
{
    private readonly ILocalizationService _localization;

    public ActivateEmployeeContactPersonValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(_localization.GetMessage("Domain.EmployeeContactPerson.IdRequired"));

        RuleFor(x => x.IsDeleted)
            .Equal(false)
            .WithMessage(_localization.GetMessage("Domain.EmployeeContactPerson.CannotActivateDeleted"));

        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0)
            .WithMessage(_localization.GetMessage("Domain.EmployeeContactPerson.ModifiedByRequired"));
    }
}
