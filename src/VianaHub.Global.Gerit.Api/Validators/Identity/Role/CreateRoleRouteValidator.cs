using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Role;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Identity.Role;

public class CreateRoleRouteValidator : AbstractValidator<CreateRoleRequest>
{
    private readonly ILocalizationService _localization;

    public CreateRoleRouteValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.Role.Create.Name"))
            .MaximumLength(100).WithMessage(_localization.GetMessage("Api.Validator.Role.Create.Name.MaximumLength", 100));

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.Role.Create.Description"))
            .MaximumLength(255).WithMessage(_localization.GetMessage("Api.Validator.Role.Create.Description.MaximumLength", 255));
    }
}
