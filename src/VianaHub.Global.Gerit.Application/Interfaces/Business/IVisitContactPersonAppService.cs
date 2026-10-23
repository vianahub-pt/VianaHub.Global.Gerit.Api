using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitContactPersons;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitContactPersons;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

/// <summary>
/// Interface do serviço de aplicação para VisitContact
/// </summary>
public interface IVisitContactPersonAppService
{
    Task<IEnumerable<VisitContactPersonResponse>> GetAllAsync(int visitId, CancellationToken ct);
    Task<VisitContactPersonDetailResponse> GetByIdAsync(int visitId, int id, CancellationToken ct);
    Task<ListPageResponse<VisitContactPersonResponse>> GetPagedAsync(int visitId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int visitId, CreateVisitContactPersonRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int visitId, int id, UpdateVisitContactPersonRequest request, CancellationToken ct);
    Task<bool> ActivateAsync(int visitId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int visitId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int visitId, int id, CancellationToken ct);
    Task<bool> BulkUploadAsync(int visitId, IFormFile file, CancellationToken ct);
}
