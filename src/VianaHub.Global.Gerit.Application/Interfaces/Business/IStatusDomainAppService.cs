using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDomain;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDomain;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface do serviço de aplicação para Domínios de Status.
/// </summary>
public interface IStatusDomainAppService
{
    Task<IEnumerable<StatusDomainResponse>> GetAllAsync(CancellationToken ct);
    Task<StatusDomainDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<StatusDomainResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateStatusDomainRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateStatusDomainRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    // Translation sub-resource
    Task<int> CreateTranslationAsync(int id, CreateStatusDomainTranslationRequest request, CancellationToken ct);
    Task<bool> UpdateTranslationAsync(int id, int translationId, UpdateStatusDomainTranslationRequest request, CancellationToken ct);
    Task<bool> DeleteTranslationAsync(int id, int translationId, CancellationToken ct);
}
