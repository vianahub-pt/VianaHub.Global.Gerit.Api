using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Resource;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Identity.Resource;

public class UpdateResourceRouteValidator : AbstractValidator<UpdateResourceRequest>
{
    private readonly ILocalizationService _localization;

    public UpdateResourceRouteValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Description)
            .MaximumLength(255).WithMessage(_localization.GetMessage("Api.Validator.Resource.Update.Description.MaximumLength", 255));
    }
}
