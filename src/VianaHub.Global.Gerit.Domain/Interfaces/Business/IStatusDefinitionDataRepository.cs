using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IStatusDefinitionDataRepository
{
    Task<IEnumerable<StatusDefinitionEntity>> GetAllAsync(CancellationToken ct);
    Task<StatusDefinitionEntity?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPage<StatusDefinitionEntity>> GetPagedAsync(PagedFilter filter, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByCodeAndDomainAsync(int statusDomainId, string? code, CancellationToken ct);
    Task<bool> ExistsByTranslationAndDomainAsync(int statusDomainId, string? languageCode, string? name, CancellationToken ct);

    Task<bool> AddAsync(StatusDefinitionEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(StatusDefinitionEntity entity, CancellationToken ct);
}
