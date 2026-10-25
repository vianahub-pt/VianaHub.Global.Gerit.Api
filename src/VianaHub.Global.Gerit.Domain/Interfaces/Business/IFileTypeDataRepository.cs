using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IFileTypeDataRepository
{
    Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct);
    Task<IEnumerable<FileTypeEntity>> GetAllAsync(CancellationToken ct);
    Task<FileTypeEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPage<FileTypeEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByMimeTypeAsync(string? mimeType, CancellationToken ct);

    Task<bool> AddAsync(FileTypeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(FileTypeEntity entity, CancellationToken ct);
}
