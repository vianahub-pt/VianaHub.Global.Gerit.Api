using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Action;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Identity.Action;

public class UpdateActionRouteValidator : AbstractValidator<UpdateActionRequest>
{
    private readonly ILocalizationService _localization;

    public UpdateActionRouteValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage(_localization.GetMessage("Api.Validator.Action.Update.Description.MaximumLength", 255));
    }
}
