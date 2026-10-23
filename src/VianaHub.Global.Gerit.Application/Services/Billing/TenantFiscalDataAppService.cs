using AutoMapper;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Billing.TenantFiscalData;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.TenantFiscalData;
using VianaHub.Global.Gerit.Application.Interfaces.Billing;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Billing;

public class TenantFiscalDataAppService : ITenantFiscalDataAppService
{
    private readonly ITenantFiscalDataDataRepository _repo;
    private readonly ITenantFiscalDataDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;

    public TenantFiscalDataAppService(
        ITenantFiscalDataDataRepository repo,
        ITenantFiscalDataDomainService domain,
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

    public async Task<IEnumerable<TenantFiscalDataResponse>> GetByTenantIdAsync(int tenantId, CancellationToken ct)
    {
        var entities = await _repo.GetByTenantIdAsync(tenantId, ct);
        return _mapper.Map<IEnumerable<TenantFiscalDataResponse>>(entities);
    }

    public async Task<TenantFiscalDataDetailResponse> GetByIdAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<TenantFiscalDataDetailResponse>(entity);
    }

    public async Task<ListPageResponse<TenantFiscalDataResponse>> GetPagedAsync(int tenantId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(tenantId, filter, ct);
        return _mapper.Map<ListPageResponse<TenantFiscalDataResponse>>(paged);
    }

    public async Task<int> CreateAsync(int tenantId, CreateTenantFiscalDataRequest request, CancellationToken ct)
    {
        var effectiveTenantId = tenantId > 0 ? tenantId : _currentUser.GetTenantId();

        // Unique index: apenas 1 registo ativo por tenant
        if (await _repo.ExistsActiveByTenantAsync(effectiveTenantId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Create.ActiveAlreadyExists"), 409);
            return 0;
        }

        // Unique index: TaxNumber único por Tenant+FiscalCountry
        if (!string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            var fiscalCountry = request.FiscalCountry ?? "PT";
            var taxNumberExists = await _repo.ExistsByTaxNumberAsync(effectiveTenantId, fiscalCountry, request.TaxNumber, ct);
            if (taxNumberExists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Create.TaxNumberAlreadyExists"), 409);
                return 0;
            }
        }

        var entity = new TenantFiscalDataEntity(
            effectiveTenantId,
            request.TaxNumber,
            request.VatNumber,
            request.IBAN,
            request.FiscalEmail,
            request.FiscalCountry ?? "PT",
            request.IsVatRegistered,
            _currentUser.GetUserId()
        );

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int tenantId, int id, UpdateTenantFiscalDataRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Update.ResourceNotFound"), 410);
            return false;
        }

        // Validação multi-tenant: o registo deve pertencer ao tenant da rota
        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Update.ResourceNotFound"), 410);
            return false;
        }

        // Unique index: TaxNumber único por Tenant+FiscalCountry (excluindo o próprio registo)
        if (!string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            var fiscalCountry = request.FiscalCountry ?? entity.FiscalCountry ?? "PT";
            var taxNumberExists = await _repo.ExistsByTaxNumberAsync(tenantId, fiscalCountry, request.TaxNumber, ct);
            if (taxNumberExists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Update.TaxNumberAlreadyExists"), 409);
                return false;
            }
        }

        entity.UpdateFiscalData(
            request.TaxNumber,
            request.VatNumber,
            request.IBAN,
            request.FiscalEmail,
            request.FiscalCountry ?? entity.FiscalCountry ?? "PT",
            request.IsVatRegistered,
            _currentUser.GetUserId()
        );

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Activate.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int tenantId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(tenantId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Delete.ResourceNotFound"), 410);
            return false;
        }

        if (entity.TenantId != tenantId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.TenantFiscalData.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }
}
