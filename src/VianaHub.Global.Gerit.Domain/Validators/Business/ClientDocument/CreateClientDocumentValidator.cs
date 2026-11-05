using FluentValidation;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.ClientDocument;

/// <summary>
/// Validador para criação de ClientDocument
/// </summary>
public class CreateClientDocumentValidator : AbstractValidator<ClientDocumentsEntity>
{
    public CreateClientDocumentValidator(ILocalizationService localization)
    {
        RuleFor(x => x.TenantId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.ClientDocument.TenantIdRequired"));

        RuleFor(x => x.ClientId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.ClientDocument.ClientIdRequired"));

        RuleFor(x => x.DocumentTypeId)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.ClientDocument.DocumentTypeIdRequired"));

        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.ClientDocument.DocumentNumberRequired"))
            .MaximumLength(100)
            .WithMessage(localization.GetMessage("Domain.ClientDocument.DocumentNumberMaxLength", 100));

        RuleFor(x => x.IssuingCountryCode)
            .NotEmpty()
            .WithMessage(localization.GetMessage("Domain.ClientDocument.IssuingCountryCodeRequired"))
            .Length(2)
            .WithMessage(localization.GetMessage("Domain.ClientDocument.IssuingCountryCodeLength"));

        // CK_ClientDocuments_Dates: ExpiresAt must be equal to or after IssuedAt when both are present
        RuleFor(x => x.ExpiresAt)
            .GreaterThanOrEqualTo(x => x.IssuedAt)
            .WithMessage(localization.GetMessage("Domain.ClientDocument.ExpiresAtMustBeAfterIssuedAt"))
            .When(x => x.IssuedAt.HasValue && x.ExpiresAt.HasValue);

        RuleFor(x => x.CreatedBy)
            .GreaterThan(0)
            .WithMessage(localization.GetMessage("Domain.ClientDocument.CreatedByRequired"));
    }
}
