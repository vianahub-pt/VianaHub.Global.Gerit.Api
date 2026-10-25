using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IFileTypeDomainService
{
    Task<FileTypeEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<IEnumerable<FileTypeEntity>> GetAllAsync(CancellationToken ct);
    Task<ListPage<FileTypeEntity>> GetPagedAsync(PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(FileTypeEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(FileTypeEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(FileTypeEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(FileTypeEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(FileTypeEntity entity, CancellationToken ct);
}
