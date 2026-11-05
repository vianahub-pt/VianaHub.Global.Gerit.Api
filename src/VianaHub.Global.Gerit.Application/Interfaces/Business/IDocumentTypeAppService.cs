using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.DocumentType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.DocumentType;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface do serviço de aplicação para Tipos de Documento.
/// </summary>
public interface IDocumentTypeAppService
{
    Task<IEnumerable<DocumentTypeResponse>> GetAllAsync(CancellationToken ct);
    Task<DocumentTypeDetailResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<ListPageResponse<DocumentTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(CreateDocumentTypeRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int id, UpdateDocumentTypeRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int id, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    // Translation sub-resource
    Task<int> CreateTranslationAsync(int id, CreateDocumentTypeTranslationRequest request, CancellationToken ct);
    Task<bool> UpdateTranslationAsync(int id, int translationId, UpdateDocumentTypeTranslationRequest request, CancellationToken ct);
    Task<bool> DeleteTranslationAsync(int id, int translationId, CancellationToken ct);
}
