using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitAttachment;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAttachment;

namespace VianaHub.Global.Gerit.Application.Interfaces.Business;

public interface IVisitAttachmentAppService
{
    Task<VisitAttachmentDetailResponse?> GetByIdAsync(int visitId, int id, CancellationToken ct);
    Task<VisitAttachmentResponse?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
    Task<VisitAttachmentDetailResponse?> GetDetailByIdAsync(int visitId, int id, CancellationToken ct);
    Task<VisitAttachmentDetailResponse?> GetDetailByPublicIdAsync(Guid publicId, CancellationToken ct);
    Task<IEnumerable<VisitAttachmentResponse>> GetAllAsync(int visitId, CancellationToken ct);
    Task<IEnumerable<VisitAttachmentResponse>> GetByVisitIdAsync(int visitId, CancellationToken ct);
    Task<VisitAttachmentResponse?> GetPrimaryByVisitIdAsync(int visitId, CancellationToken ct);
    Task<ListPageResponse<VisitAttachmentResponse>> GetPagedAsync(int visitId, PagedFilterRequest request, CancellationToken ct);
    Task<int> CreateAsync(int visitId, CreateVisitAttachmentRequest request, CancellationToken ct);
    Task<bool> UpdateAsync(int visitId, int id, UpdateVisitAttachmentRequest request, CancellationToken ct);
    Task<bool> SetAsPrimaryAsync(int visitId, int id, CancellationToken ct);
    Task<bool> ActivateAsync(int visitId, int id, CancellationToken ct);
    Task<bool> DeactivateAsync(int visitId, int id, CancellationToken ct);
    Task<bool> DeleteAsync(int visitId, int id, CancellationToken ct);
}
