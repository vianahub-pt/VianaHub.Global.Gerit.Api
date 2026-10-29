using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Identity.Action;

public class ActivateActionValidator : AbstractValidator<ActionEntity>
{
    public ActivateActionValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.ActionEntity.IdRequired"));

        RuleFor(x => x.IsDeleted)
            .Must(x => !x)
            .WithMessage(localization.GetMessage("Domain.ActionEntity.CannotActivateDeleted"));
    }
}
