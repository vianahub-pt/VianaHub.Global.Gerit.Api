using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Resource;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Identity.Resource;

public class CreateResourceRouteValidator : AbstractValidator<CreateResourceRequest>
{
    private readonly ILocalizationService _localization;

    public CreateResourceRouteValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.Resource.Create.Name"))
            .MaximumLength(100).WithMessage(_localization.GetMessage("Api.Validator.Resource.Create.Name.MaximumLength", 100));

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.Resource.Create.Description.Required"))
            .MaximumLength(255).WithMessage(_localization.GetMessage("Api.Validator.Resource.Create.Description.MaximumLength", 255));
    }
}
