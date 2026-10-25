using AutoMapper;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitAttachment;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAttachment;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class VisitAttachmentAppService : IVisitAttachmentAppService
{
    private readonly IVisitAttachmentDataRepository _repo;
    private readonly IVisitAttachmentDomainService _domain;
    private readonly IFileTypeDataRepository _fileTypeRepo;
    private readonly IVisitDataRepository _visitRepo;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<VisitAttachmentAppService> _logger;

    public VisitAttachmentAppService(
        IVisitAttachmentDataRepository repo,
        IVisitAttachmentDomainService domain,
        IFileTypeDataRepository fileTypeRepo,
        IVisitDataRepository visitRepo,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        ILogger<VisitAttachmentAppService> logger)
    {
        _repo = repo;
        _domain = domain;
        _fileTypeRepo = fileTypeRepo;
        _visitRepo = visitRepo;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<VisitAttachmentDetailResponse?> GetByIdAsync(int visitId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitAttachmentDetailResponse>(entity);
    }

    public async Task<VisitAttachmentResponse?> GetByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        var entity = await _repo.GetByPublicIdAsync(publicId, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.GetByPublicId.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitAttachmentResponse>(entity);
    }

    public async Task<VisitAttachmentDetailResponse?> GetDetailByIdAsync(int visitId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitAttachmentDetailResponse>(entity);
    }

    public async Task<VisitAttachmentDetailResponse?> GetDetailByPublicIdAsync(Guid publicId, CancellationToken ct)
    {
        var entity = await _repo.GetByPublicIdAsync(publicId, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.GetByPublicId.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitAttachmentDetailResponse>(entity);
    }

    public async Task<IEnumerable<VisitAttachmentResponse>> GetAllAsync(int visitId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(visitId, ct);
        return _mapper.Map<IEnumerable<VisitAttachmentResponse>>(entities);
    }

    public async Task<IEnumerable<VisitAttachmentResponse>> GetByVisitIdAsync(int visitId, CancellationToken ct)
    {
        var entities = await _repo.GetByVisitIdAsync(visitId, ct);
        return _mapper.Map<IEnumerable<VisitAttachmentResponse>>(entities);
    }

    public async Task<VisitAttachmentResponse?> GetPrimaryByVisitIdAsync(int visitId, CancellationToken ct)
    {
        var entity = await _repo.GetPrimaryByVisitIdAsync(visitId, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.GetPrimary.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitAttachmentResponse>(entity);
    }

    public async Task<ListPageResponse<VisitAttachmentResponse>> GetPagedAsync(int visitId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(visitId, filter, ct);
        return _mapper.Map<ListPageResponse<VisitAttachmentResponse>>(paged);
    }

    public async Task<int> CreateAsync(int visitId, CreateVisitAttachmentRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();

        if (!await _visitRepo.ExistsByIdAsync(visitId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.Create.VisitNotFound"), 400);
            return 0;
        }

        if (!await _fileTypeRepo.ExistsByIdAsync(request.FileTypeId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.Create.FileTypeNotFound"), 400);
            return 0;
        }

        if (await _repo.ExistsByS3KeyAsync(tenantId, request.S3Key, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.Create.S3KeyAlreadyExists"), 409);
            return 0;
        }

        if (request.IsPrimary)
        {
            await RemoveAllPrimaryFlagsAsync(visitId, ct);
        }

        var entity = new VisitAttachmentsEntity(tenantId, request.FileTypeId, visitId,
            request.S3Key, request.FileName, request.FileSizeBytes,
            request.DisplayOrder, request.IsPrimary, _currentUser.GetUserId());

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int visitId, int id, UpdateVisitAttachmentRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.Update.ResourceNotFound"), 410);
            return false;
        }

        if (request.IsPrimary && !entity.IsPrimary)
        {
            await RemoveAllPrimaryFlagsAsync(entity.VisitId, ct);
        }

        entity.Update(request.FileName, request.DisplayOrder, request.IsPrimary, _currentUser.GetUserId());

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> SetAsPrimaryAsync(int visitId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.SetAsPrimary.ResourceNotFound"), 410);
            return false;
        }

        await RemoveAllPrimaryFlagsAsync(entity.VisitId, ct);

        entity.SetAsPrimary(_currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int visitId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int visitId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int visitId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitAttachment.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    private async Task RemoveAllPrimaryFlagsAsync(int visitId, CancellationToken ct)
    {
        var userId = _currentUser.GetUserId();
        var attachments = await _repo.GetByVisitIdAsync(visitId, ct);

        foreach (var attachment in attachments.Where(a => a.IsPrimary))
        {
            attachment.RemovePrimary(userId);
            await _domain.UpdateAsync(attachment, ct);
        }
    }
}
