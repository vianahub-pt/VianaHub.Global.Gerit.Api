using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Role;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Identity.Role;

public class UpdateRoleRouteValidator : AbstractValidator<UpdateRoleRequest>
{
    private readonly ILocalizationService _localization;

    public UpdateRoleRouteValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage(_localization.GetMessage("Api.Validator.Role.Update.Description.MaximumLength", 255));
    }
}
