using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.DocumentType;

public class DocumentTypeValidator : BaseEntityValidator<DocumentTypeEntity>
{

    public DocumentTypeValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(DocumentTypeEntity entity)
    {
        var validator = new CreateDocumentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(DocumentTypeEntity entity)
    {
        var validator = new UpdateDocumentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(DocumentTypeEntity entity)
    {
        var validator = new ActivateDocumentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(DocumentTypeEntity entity)
    {
        var validator = new DeactivateDocumentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(DocumentTypeEntity entity)
    {
        var validator = new DeleteDocumentTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(DocumentTypeEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
