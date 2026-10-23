using AutoMapper;
using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeFiscalData;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeFiscalData;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class EmployeeFiscalDataAppService : IEmployeeFiscalDataAppService
{
    private readonly IEmployeeFiscalDataDataRepository _repo;
    private readonly IEmployeeFiscalDataDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;

    public EmployeeFiscalDataAppService(
        IEmployeeFiscalDataDataRepository repo,
        IEmployeeFiscalDataDomainService domain,
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

    public async Task<IEnumerable<EmployeeFiscalDataResponse>> GetAllAsync(int employeeId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(employeeId, ct);

        return _mapper.Map<IEnumerable<EmployeeFiscalDataResponse>>(entities);
    }

    public async Task<EmployeeFiscalDataDetailResponse> GetByIdAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeFiscalData.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<EmployeeFiscalDataDetailResponse>(entity);
    }

    public async Task<ListPageResponse<EmployeeFiscalDataResponse>> GetPagedAsync(int employeeId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(employeeId, filter, ct);
        return _mapper.Map<ListPageResponse<EmployeeFiscalDataResponse>>(paged);
    }

    public async Task<bool> ExistsByIdAsync(int employeeId, CancellationToken ct = default)
    {
        return await _repo.ExistsByIdAsync(employeeId, ct);
    }

    public async Task<bool> ExistsByTaxNumberAsync(int employeeId, string taxNumber, CancellationToken ct = default)
    {
        return await _repo.ExistsByTaxNumberAsync(employeeId, taxNumber, ct);
    }

    public async Task<int> CreateAsync(int employeeId, CreateEmployeeFiscalDataRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByIdAsync(employeeId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeFiscalData.Create.EmployeeAlreadyHasFiscalData"), 409);
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            var taxNumberExists = await _repo.ExistsByTaxNumberAsync(employeeId, request.TaxNumber, ct);
            if (taxNumberExists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeFiscalData.Create.TaxNumberAlreadyExists"), 409);
                return 0;
            }
        }

        var entity = new EmployeeFiscalDataEntity(tenantId, employeeId, request.TaxNumber, request.VatNumber, request.FiscalCountry, request.IsVatRegistered, request.IBAN, request.FiscalEmail, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int employeeId, int id, UpdateEmployeeFiscalDataRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeFiscalData.Update.ResourceNotFound"), 410);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            var taxNumberExists = await _repo.ExistsByTaxNumberAsync(employeeId, request.TaxNumber, ct);
            if (taxNumberExists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeFiscalData.Update.TaxNumberAlreadyExists"), 409);
                return false;
            }
        }

        entity.Update(request.TaxNumber, request.VatNumber, request.FiscalCountry, request.IsVatRegistered, request.IBAN, request.FiscalEmail, _currentUser.GetUserId());

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeFiscalData.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeFiscalData.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeFiscalData.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(int employeeId, IFormFile file, CancellationToken ct)
    {
        _notify.Add(_localization.GetMessage("Application.Service.EmployeeFiscalData.BulkUpload.NotImplemented"), 501);
        return false;
    }
}
