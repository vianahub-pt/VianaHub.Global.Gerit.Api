using FluentValidation.Results;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Domain.Validators.Business.FileType;

public class FileTypeValidator : BaseEntityValidator<FileTypeEntity>
{

    public FileTypeValidator(ILocalizationService localization) : base(localization)
    {
    }

    public override async Task<ValidationResult> ValidateForCreateAsync(FileTypeEntity entity)
    {
        var validator = new CreateFileTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForUpdateAsync(FileTypeEntity entity)
    {
        var validator = new UpdateFileTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForActivateAsync(FileTypeEntity entity)
    {
        var validator = new ActivateFileTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeactivateAsync(FileTypeEntity entity)
    {
        var validator = new DeactivateFileTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override async Task<ValidationResult> ValidateForDeleteAsync(FileTypeEntity entity)
    {
        var validator = new DeleteFileTypeValidator(_localization);
        return await validator.ValidateAsync(entity);
    }

    public override Task<ValidationResult> ValidateForRevokeAsync(FileTypeEntity entity)
    {
        return Task.FromResult(new ValidationResult());
    }
}
