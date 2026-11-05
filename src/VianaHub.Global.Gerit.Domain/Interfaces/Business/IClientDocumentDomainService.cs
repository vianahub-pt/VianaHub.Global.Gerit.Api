using VianaHub.Global.Gerit.Domain.Entities.Business;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IClientDocumentDomainService
{
    Task<bool> CreateAsync(ClientDocumentsEntity entity, CancellationToken ct);
    Task<bool> CreateManyAsync(IReadOnlyCollection<ClientDocumentsEntity> entities, CancellationToken ct);
    Task<bool> UpdateAsync(ClientDocumentsEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(ClientDocumentsEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(ClientDocumentsEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(ClientDocumentsEntity entity, CancellationToken ct);
}
