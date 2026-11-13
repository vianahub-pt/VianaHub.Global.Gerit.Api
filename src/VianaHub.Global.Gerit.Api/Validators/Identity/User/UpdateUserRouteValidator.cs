using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Validators.Identity.User;

namespace VianaHub.Global.Gerit.Api.Validators.Identity.User;

public class UpdateUserRouteValidator : AbstractValidator<UpdateUserRequest>
{
    private readonly ILocalizationService _localization;

    public UpdateUserRouteValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_localization.GetMessage("Domain.User.NameRequired"))
            .MaximumLength(150).WithMessage(_localization.GetMessage("Api.Validator.User.Update.Name.MaximumLength", 150));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_localization.GetMessage("Domain.User.EmailRequired"))
            .MaximumLength(256).WithMessage(_localization.GetMessage("Domain.User.EmailMaxLength", 256))
            .EmailAddress().WithMessage(_localization.GetMessage("Domain.User.EmailInvalid"))
            .When(x => x.EmailProvided);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage(_localization.GetMessage("Api.Validator.User.Update.PhoneNumber.MaximumLength", 50));

        RuleFor(x => x.UrlImage)
            .MaximumLength(500).WithMessage(_localization.GetMessage("Domain.User.UrlImageMaxLength", 500))
            .Must(UserProfileRules.IsValidImageUrl).WithMessage(_localization.GetMessage("Domain.User.UrlImageInvalid"));
    }
}
