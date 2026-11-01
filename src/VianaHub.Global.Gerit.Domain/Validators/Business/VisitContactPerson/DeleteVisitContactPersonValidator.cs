using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.VisitContactPersons;

/// <summary>
/// Validador para dele��o de VisitContact
/// </summary>
public class DeleteVisitContactValidator : AbstractValidator<VisitContactPersonsEntity>
{
    public DeleteVisitContactValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.VisitContactPerson.IdRequired"));

        RuleFor(x => x.ModifiedBy)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.VisitContactPerson.ModifiedByRequired"));

    }
}
