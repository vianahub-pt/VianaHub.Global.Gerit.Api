using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantAddress;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Billing;

public class TenantAddressesAppService : ITenantAddressesAppService
{
    private readonly ITenantAddressesDataRepository _repo;
    private readonly ITenantAddressesDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;

    public TenantAddressesAppService(
        ITenantAddressesDataRepository repo,
        ITenantAddressesDomainService domain,
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

    public async Task<IEnumerable<TenantAddressResponse>> GetByTenantIdAsync(int tenantId, CancellationToken ct)
    {
        var entities = await _repo.GetByTenantIdAsync(tenantId, ct);
        return _mapper.Map<IEnumerable<TenantAddressResponse>>(entities);
    }

    public async Task<TenantAddressDetailResponse?> GetByIdAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<TenantAddressDetailResponse>(entity);
    }

    public async Task<ListPageResponse<TenantAddressResponse>> GetPagedAsync(int tenantId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(tenantId, filter, ct);
        return _mapper.Map<ListPageResponse<TenantAddressResponse>>(paged);
    }

    public async Task<int> CreateAsync(int tenantId, CreateTenantAddressRequest request, CancellationToken ct)
    {
        var effectiveTenantId = tenantId > 0 ? tenantId : _currentUser.GetTenantId();

        // UX_TenantAddresses_Primary: apenas 1 primário por tenant
        if (request.IsPrimary && await _repo.ExistsPrimaryByTenantAsync(effectiveTenantId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.Create.PrimaryAlreadyExists"), 409);
            return 0;
        }

        var entity = new TenantAddressesEntity(
            effectiveTenantId,
            request.AddressTypeId,
            request.CountryCode ?? "PT",
            request.Street,
            request.Neighborhood,
            request.City,
            request.District,
            request.PostalCode,
            request.StreetNumber,
            request.Complement,
            request.Latitude,
            request.Longitude,
            request.Note,
            request.IsPrimary,
            _currentUser.GetUserId()
        );

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int tenantId, int id, UpdateTenantAddressRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.Update.ResourceNotFound"), 410);
            return false;
        }

        // Validação multi-tenant: o registo deve pertencer ao tenant da rota
        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(
            request.AddressTypeId,
            request.CountryCode ?? entity.CountryCode ?? "PT",
            request.Street,
            request.Neighborhood,
            request.City,
            request.District,
            request.PostalCode,
            request.StreetNumber,
            request.Complement,
            request.Latitude,
            request.Longitude,
            request.Note,
            _currentUser.GetUserId()
        );

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.Activate.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate();
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate();
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.Delete.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantAddress.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete();
        return await _domain.DeleteAsync(entity, ct);
    }
}
