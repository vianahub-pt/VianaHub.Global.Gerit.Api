using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IDocumentTypeDomainService
{
    Task<DocumentTypeEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<DocumentTypeEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<DocumentTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(DocumentTypeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(DocumentTypeEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(DocumentTypeEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(DocumentTypeEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(DocumentTypeEntity entity, CancellationToken ct);
}
