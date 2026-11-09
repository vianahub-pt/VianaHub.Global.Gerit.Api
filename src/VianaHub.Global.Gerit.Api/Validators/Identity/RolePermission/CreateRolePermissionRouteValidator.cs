using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.RolePermission;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Identity.RolePermission;

public sealed class CreateRolePermissionRouteValidator : AbstractValidator<CreateRolePermissionRequest>
{
    public CreateRolePermissionRouteValidator(ILocalizationService localization)
    {
        RuleFor(x => x.RoleId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.RolePermission.RoleIdRequired"));

        RuleFor(x => x.ResourceId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.RolePermission.ResourceIdRequired"));

        RuleFor(x => x.ActionId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.RolePermission.ActionIdRequired"));
    }
}
