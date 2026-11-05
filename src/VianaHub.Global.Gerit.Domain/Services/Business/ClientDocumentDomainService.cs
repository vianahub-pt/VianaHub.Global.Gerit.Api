using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Exceptions;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Domain.Services.Business;

public class ClientDocumentDomainService : IClientDocumentDomainService
{
    private readonly IClientDocumentDataRepository _repo;
    private readonly IEntityDomainValidator<ClientDocumentsEntity> _validator;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;

    public ClientDocumentDomainService(IClientDocumentDataRepository repo, IEntityDomainValidator<ClientDocumentsEntity> validator, INotify notify, ILocalizationService localization)
    {
        _repo = repo;
        _validator = validator;
        _notify = notify;
        _localization = localization;
    }

    public Task<bool> CreateAsync(ClientDocumentsEntity entity, CancellationToken ct) => PersistAsync(entity, _validator.ValidateForCreateAsync, _repo.CreateAsync, ct);
    public async Task<bool> CreateManyAsync(IReadOnlyCollection<ClientDocumentsEntity> entities, CancellationToken ct)
    {
        foreach (var entity in entities)
        {
            var result = await _validator.ValidateForCreateAsync(entity);
            if (!result.IsValid)
            {
                foreach (var error in result.Errors) _notify.Add(error.ErrorMessage, 400);
                return false;
            }
        }

        try
        {
            return await _repo.CreateManyAsync(entities, ct);
        }
        catch (ClientDocumentConflictException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.DuplicateExisting"), 409);
            return false;
        }
    }
    public Task<bool> UpdateAsync(ClientDocumentsEntity entity, CancellationToken ct) => PersistAsync(entity, _validator.ValidateForUpdateAsync, _repo.UpdateAsync, ct);
    public Task<bool> ActivateAsync(ClientDocumentsEntity entity, CancellationToken ct) => PersistAsync(entity, _validator.ValidateForActivateAsync, _repo.UpdateAsync, ct);
    public Task<bool> DeactivateAsync(ClientDocumentsEntity entity, CancellationToken ct) => PersistAsync(entity, _validator.ValidateForDeactivateAsync, _repo.UpdateAsync, ct);
    public Task<bool> DeleteAsync(ClientDocumentsEntity entity, CancellationToken ct) => PersistAsync(entity, _validator.ValidateForDeleteAsync, _repo.UpdateAsync, ct);

    private async Task<bool> PersistAsync(ClientDocumentsEntity entity, Func<ClientDocumentsEntity, Task<FluentValidation.Results.ValidationResult>> validate, Func<ClientDocumentsEntity, CancellationToken, Task<bool>> persist, CancellationToken ct)
    {
        var result = await validate(entity);
        if (!result.IsValid)
        {
            foreach (var error in result.Errors) _notify.Add(error.ErrorMessage, 400);
            return false;
        }
        try
        {
            return await persist(entity, ct);
        }
        catch (ClientDocumentConflictException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.DuplicateDocument"), 409);
            return false;
        }
    }
}
