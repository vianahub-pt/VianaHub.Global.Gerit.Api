using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IDocumentTypeDataRepository
{
    Task<IEnumerable<DocumentTypeEntity>> GetAllAsync(CancellationToken ct);
    Task<DocumentTypeEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPage<DocumentTypeEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct);
    Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct);

    Task<bool> AddAsync(DocumentTypeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(DocumentTypeEntity entity, CancellationToken ct);
}
