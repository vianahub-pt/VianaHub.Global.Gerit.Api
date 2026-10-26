using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.PartyType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.PartyType;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface do serviço de aplicação para Tipos de Party (Pessoa Física / Jurídica).
/// </summary>
public interface IPartyTypeAppService
{
    Task<IEnumerable<PartyTypeResponse>> GetAllAsync(CancellationToken ct);
    Task<PartyTypeDetailResponse?> GetByIdAsync(byte id, CancellationToken ct);
    Task<ListPageResponse<PartyTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct);
    Task<byte> CreateAsync(CreatePartyTypeRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(byte id, UpdatePartyTypeRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(byte id, CancellationToken ct);
    Task<bool> DeactivateAsync(byte id, CancellationToken ct);
    Task<bool> DeleteAsync(byte id, CancellationToken ct);

    // Translation sub-resource
    Task<int> CreateTranslationAsync(byte id, CreatePartyTypeTranslationRequest request, CancellationToken ct);
    Task<bool> UpdateTranslationAsync(byte id, int translationId, UpdatePartyTypeTranslationRequest request, CancellationToken ct);
    Task<bool> DeleteTranslationAsync(byte id, int translationId, CancellationToken ct);
}
