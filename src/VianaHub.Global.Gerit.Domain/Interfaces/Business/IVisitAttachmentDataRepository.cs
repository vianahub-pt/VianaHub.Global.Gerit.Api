using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Pagination;

namespace VianaHub.Global.Gerit.Domain.Interfaces.Business;

public interface IVisitAttachmentDataRepository
{
    Task<VisitAttachmentsEntity?> GetByIdAsync(int visitId, int id, CancellationToken ct);
    Task<VisitAttachmentsEntity?> GetByPublicIdAsync(Guid publicId, CancellationToken ct);
    Task<IEnumerable<VisitAttachmentsEntity>> GetAllAsync(int visitId, CancellationToken ct);
    Task<IEnumerable<VisitAttachmentsEntity>> GetByVisitIdAsync(int visitId, CancellationToken ct);
    Task<VisitAttachmentsEntity?> GetPrimaryByVisitIdAsync(int visitId, CancellationToken ct);
    Task<ListPage<VisitAttachmentsEntity>> GetPagedAsync(int visitId, PagedFilter request, CancellationToken ct);
    Task<bool> ExistsByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByS3KeyAsync(int tenantId, string? s3Key, CancellationToken ct);
    Task<bool> HasPrimaryAttachmentAsync(int visitId, CancellationToken ct);
    Task<bool> AddAsync(VisitAttachmentsEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(VisitAttachmentsEntity entity, CancellationToken ct);
    Task<long> GetTotalSizeByVisitIdAsync(int visitId, CancellationToken ct);
    Task<int> GetCountByVisitIdAsync(int visitId, CancellationToken ct);
}
