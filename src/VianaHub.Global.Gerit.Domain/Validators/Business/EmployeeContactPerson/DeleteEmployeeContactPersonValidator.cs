using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.EmployeeContact;

/// <summary>
/// Validator para exclus�o de EmployeeContact
/// </summary>
public class DeleteEmployeeContactPersonValidator : AbstractValidator<EmployeeContactPersonsEntity>
{
    private readonly ILocalizationService _localization;

    public DeleteEmployeeContactPersonValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(_localization.GetMessage("Domain.EmployeeContactPerson.IdRequired"));

        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0)
            .WithMessage(_localization.GetMessage("Domain.EmployeeContactPerson.ModifiedByRequired"));
    }
}
