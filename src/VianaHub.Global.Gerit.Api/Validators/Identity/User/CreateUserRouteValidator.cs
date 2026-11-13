using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Validators.Identity.User;

namespace VianaHub.Global.Gerit.Api.Validators.Identity.User;

public class CreateUserRouteValidator : AbstractValidator<CreateUserRequest>
{
    private readonly ILocalizationService _localization;

    public CreateUserRouteValidator(ILocalizationService localization)
    {
        _localization = localization;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.User.Create.Name"))
            .MaximumLength(150).WithMessage(_localization.GetMessage("Api.Validator.User.Create.Name.MaximumLength", 150));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.User.Create.Email"))
            .MaximumLength(256).WithMessage(_localization.GetMessage("Api.Validator.User.Create.Email.MaximumLength", 256))
            .EmailAddress().WithMessage(_localization.GetMessage("Api.Validator.User.Create.Email.Invalid"));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50).WithMessage(_localization.GetMessage("Domain.User.PhoneNumberMaxLength", 50));
        RuleFor(x => x.UrlImage)
            .MaximumLength(500).WithMessage(_localization.GetMessage("Domain.User.UrlImageMaxLength", 500))
            .Must(UserProfileRules.IsValidImageUrl).WithMessage(_localization.GetMessage("Domain.User.UrlImageInvalid"));

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.User.Create.Password"))
            .MinimumLength(8).WithMessage(_localization.GetMessage("Api.Validator.User.Create.Password.MinimumLength", 8))
            .MaximumLength(100).WithMessage(_localization.GetMessage("Api.Validator.User.Create.Password.MaximumLength", 100))
            .Matches(@"[A-Z]").WithMessage(_localization.GetMessage("Api.Validator.User.Create.Password.RequiresUpperCase"))
            .Matches(@"[a-z]").WithMessage(_localization.GetMessage("Api.Validator.User.Create.Password.RequiresLowerCase"))
            .Matches(@"[0-9]").WithMessage(_localization.GetMessage("Api.Validator.User.Create.Password.RequiresDigit"))
            .Matches(@"[^a-zA-Z0-9]").WithMessage(_localization.GetMessage("Api.Validator.User.Create.Password.RequiresSpecialCharacter"));

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(_localization.GetMessage("Api.Validator.User.Create.ConfirmPassword"))
            .Equal(x => x.Password).WithMessage(_localization.GetMessage("Api.Validator.User.Create.ConfirmPassword.Equal"));
    }
}
