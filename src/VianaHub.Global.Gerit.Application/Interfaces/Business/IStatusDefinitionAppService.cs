using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.StatusDefinition;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.StatusDefinition;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface do serviço de aplicação para Definições de Status.
/// </summary>
public interface IStatusDefinitionAppService
{
    Task<IEnumerable<StatusDefinitionResponse>> GetAllAsync(CancellationToken ct);
    Task<StatusDefinitionDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<StatusDefinitionResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateStatusDefinitionRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateStatusDefinitionRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    // Translation sub-resource
    Task<int> CreateTranslationAsync(int id, CreateStatusDefinitionTranslationRequest request, CancellationToken ct);
    Task<bool> UpdateTranslationAsync(int id, int translationId, UpdateStatusDefinitionTranslationRequest request, CancellationToken ct);
    Task<bool> DeleteTranslationAsync(int id, int translationId, CancellationToken ct);
}
