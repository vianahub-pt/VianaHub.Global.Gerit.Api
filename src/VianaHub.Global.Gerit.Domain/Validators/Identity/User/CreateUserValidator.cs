using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Identity.User;

public class CreateUserValidator : AbstractValidator<UserEntity>
{
    public CreateUserValidator(ILocalizationService localization)
    {
        RuleFor(x => x.TenantId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.User.TenantIdRequired"));

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.User.NameRequired"))
            .MaximumLength(150)
            .WithMessage(localization.GetMessage("Domain.User.NameMaxLength", 150));

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.User.EmailRequired"))
            .MaximumLength(256)
            .WithMessage(localization.GetMessage("Domain.User.EmailMaxLength", 256))
            .EmailAddress()
            .WithMessage(localization.GetMessage("Domain.User.EmailInvalid"));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50)
            .WithMessage(localization.GetMessage("Domain.User.PhoneNumberMaxLength", 50));

        RuleFor(x => x.UrlImage)
            .MaximumLength(500).WithMessage(localization.GetMessage("Domain.User.UrlImageMaxLength", 500))
            .Must(UserProfileRules.IsValidImageUrl).WithMessage(localization.GetMessage("Domain.User.UrlImageInvalid"));

        RuleFor(x => x.PasswordHash)
            .NotNull()
            .WithMessage(localization.GetMessage("Domain.User.PasswordHashRequired"))
            .MinimumLength(60)
            .WithMessage(localization.GetMessage("Domain.User.PasswordHashInvalid"))
            .MaximumLength(500)
            .WithMessage(localization.GetMessage("Domain.User.PasswordHashInvalid"));
    }
}
