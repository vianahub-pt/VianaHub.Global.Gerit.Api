using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

/// <summary>
/// Interface do serviço de domínio para VisitContact
/// </summary>
public interface IVisitContactDomainService
{
    Task<VisitContactPersonsEntity?> GetByIdAsync(int visitId, int id, CancellationToken ct);
    Task<IEnumerable<VisitContactPersonsEntity>> GetAllAsync(int visitId, CancellationToken ct);
    Task<ListPage<VisitContactPersonsEntity>> GetPagedAsync(int visitId, PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);

    Task<bool> CreateAsync(VisitContactPersonsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitContactPersonsEntity entity, CancellationToken ct);
    Task<bool> ActivateAsync(VisitContactPersonsEntity entity, CancellationToken ct);
    Task<bool> DeactivateAsync(VisitContactPersonsEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(VisitContactPersonsEntity entity, CancellationToken ct);
}
