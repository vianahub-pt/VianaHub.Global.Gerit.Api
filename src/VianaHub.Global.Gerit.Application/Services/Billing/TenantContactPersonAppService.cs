using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantContactPerson;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantContactPerson;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Billing;

public class TenantContactPersonAppService : ITenantContactPersonAppService
{
    private readonly ITenantContactPersonDataRepository _repo;
    private readonly ITenantContactPersonDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;

    public TenantContactPersonAppService(
        ITenantContactPersonDataRepository repo,
        ITenantContactPersonDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
    }

    public async Task<IEnumerable<TenantContactPersonResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<TenantContactPersonResponse>>(entities);
    }

    public async Task<IEnumerable<TenantContactPersonResponse>> GetActiveAsync(CancellationToken ct)
    {
        var entities = await _repo.GetActiveAsync(ct);
        return _mapper.Map<IEnumerable<TenantContactPersonResponse>>(entities);
    }

    public async Task<TenantContactPersonDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<TenantContactPersonDetailResponse>(entity);
    }

    public async Task<IEnumerable<TenantContactPersonResponse>> GetByTenantIdAsync(int tenantId, CancellationToken ct)
    {
        var entities = await _repo.GetByTenantIdAsync(tenantId, ct);
        return _mapper.Map<IEnumerable<TenantContactPersonResponse>>(entities);
    }

    public async Task<TenantContactPersonResponse> GetPrimaryByTenantIdAsync(int tenantId, CancellationToken ct)
    {
        var entity = await _repo.GetPrimaryByTenantIdAsync(tenantId, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.GetPrimaryByTenantId.ResourceNotFound"), 404);
            return null;
        }
        return _mapper.Map<TenantContactPersonResponse>(entity);
    }

    public async Task<ListPageResponse<TenantContactPersonResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<TenantContactPersonResponse>>(paged);
    }

    public async Task<int> CreateAsync(int tenantId, CreateTenantContactPersonRequest request, CancellationToken ct)
    {
        // tenantId da rota é o tenant efetivo; fallback para o do utilizador autenticado se a rota não trouxer
        var effectiveTenantId = tenantId > 0 ? tenantId : _currentUser.GetTenantId();

        if (request.IsPrimary && await _repo.ExistsPrimaryContactAsync(effectiveTenantId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.Create.PrimaryAlreadyExists"), 409);
            return 0;
        }

        var entity = new TenantContactPersonsEntity(
            effectiveTenantId,
            request.Name,
            request.Email,
            request.Phone,
            request.JobTitle,
            request.Department,
            request.CellPhoneNumber,
            request.IsCellPhoneWhatsapp,
            request.IsPrimary,
            _currentUser.GetUserId()
        );

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int tenantId, int id, UpdateTenantContactPersonRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.Update.ResourceNotFound"), 410);
            return false;
        }

        // Validação multi-tenant: o contacto deve pertencer ao tenant da rota
        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(
            request.Name,
            request.Email,
            request.Phone,
            request.JobTitle,
            request.Department,
            request.CellPhoneNumber,
            request.IsCellPhoneWhatsapp,
            _currentUser.GetUserId()
        );

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> SetAsPrimaryAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.SetAsPrimary.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.SetAsPrimary.ResourceNotFound"), 410);
            return false;
        }

        var currentPrimary = await _repo.GetPrimaryByTenantIdAsync(entity.TenantId, ct);
        if (currentPrimary != null && currentPrimary.Id != id)
        {
            currentPrimary.RemoveAsPrimary();
            await _repo.UpdateAsync(currentPrimary, ct);
        }

        entity.SetAsPrimary();
        return await _repo.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.Activate.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.Delete.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantContactPerson.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }
}
