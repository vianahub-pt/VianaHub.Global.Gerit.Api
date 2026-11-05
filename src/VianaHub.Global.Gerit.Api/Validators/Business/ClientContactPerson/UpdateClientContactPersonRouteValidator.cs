using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientContact;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.ClientContact;

/// <summary>
/// Validador de rota para atualiza��o de ClientContact
/// </summary>
public class UpdateClientContactPersonRouteValidator : AbstractValidator<UpdateClientContactPersonRequest>
{
    public UpdateClientContactPersonRouteValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Api.Validator.ClientContactPerson.Update.Name"))
            .MaximumLength(150)
            .WithMessage(localization.GetMessage("Api.Validator.ClientContactPerson.Update.Name.MaximumLength", 150));

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Api.Validator.ClientContactPerson.Update.Email"))
            .MaximumLength(255)
            .WithMessage(localization.GetMessage("Api.Validator.ClientContactPerson.Update.Email.MaximumLength", 255))
            .EmailAddress()
            .WithMessage(localization.GetMessage("Api.Validator.ClientContactPerson.Update.Email.Invalid"));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(50)
            .WithMessage(localization.GetMessage("Api.Validator.ClientContactPerson.Update.Phone.MaximumLength", 50))
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.CellPhoneNumber).MaximumLength(50)
            .WithMessage(localization.GetMessage("Domain.ClientContactPerson.CellPhoneNumberMaxLength", 50))
            .When(x => !string.IsNullOrWhiteSpace(x.CellPhoneNumber));
        RuleFor(x => x.CellPhoneNumber).NotEmpty()
            .WithMessage(localization.GetMessage("Domain.ClientContactPerson.CellPhoneNumberRequiredForWhatsapp"))
            .When(x => x.IsCellPhoneWhatsapp || x.IsWhatsapp);
        RuleFor(x => x.JobTitle).MaximumLength(100)
            .WithMessage(localization.GetMessage("Domain.ClientContactPerson.JobTitleMaxLength", 100));
        RuleFor(x => x.Department).MaximumLength(100)
            .WithMessage(localization.GetMessage("Domain.ClientContactPerson.DepartmentMaxLength", 100));
    }
}
