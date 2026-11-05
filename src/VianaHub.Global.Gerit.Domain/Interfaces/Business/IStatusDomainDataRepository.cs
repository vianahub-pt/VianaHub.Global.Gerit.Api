using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IStatusDomainDataRepository
{
    Task<bool> ExistsByTranslationAsync(string? languageCode, string? name, CancellationToken ct);

    Task<IEnumerable<StatusDomainEntity>> GetAllAsync(CancellationToken ct);
    Task<StatusDomainEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPage<StatusDomainEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByCodeAsync(string? code, CancellationToken ct);

    Task<bool> AddAsync(StatusDomainEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(StatusDomainEntity entity, CancellationToken ct);
}
