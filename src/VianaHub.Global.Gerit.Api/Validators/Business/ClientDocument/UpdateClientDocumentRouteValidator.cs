using FluentValidation;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientDocument;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Validators.Business.ClientDocument;

public class UpdateClientDocumentRouteValidator : AbstractValidator<UpdateClientDocumentRequest>
{
    public UpdateClientDocumentRouteValidator(ILocalizationService localization)
    {
        RuleFor(x => x.DocumentTypeId).GreaterThan(0).WithMessage(localization.GetMessage("Domain.ClientDocument.DocumentTypeIdRequired"));
        RuleFor(x => x.DocumentNumber).NotEmpty().WithMessage(localization.GetMessage("Domain.ClientDocument.DocumentNumberRequired")).MaximumLength(100).WithMessage(localization.GetMessage("Domain.ClientDocument.DocumentNumberMaxLength", 100));
        RuleFor(x => x.IssuingCountryCode).NotEmpty().WithMessage(localization.GetMessage("Domain.ClientDocument.IssuingCountryCodeRequired")).Length(2).WithMessage(localization.GetMessage("Domain.ClientDocument.IssuingCountryCodeLength"));
        RuleFor(x => x.ExpiresAt).GreaterThanOrEqualTo(x => x.IssuedAt).When(x => x.IssuedAt.HasValue && x.ExpiresAt.HasValue).WithMessage(localization.GetMessage("Domain.ClientDocument.ExpiresAtMustBeAfterIssuedAt"));
    }
}
