using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeAddress;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class EmployeeAddressAppService : IEmployeeAddressAppService
{
    private readonly IEmployeeAddressDataRepository _repo;
    private readonly IEmployeeAddressDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public EmployeeAddressAppService(
        IEmployeeAddressDataRepository repo,
        IEmployeeAddressDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<EmployeeAddressResponse>> GetAllAsync(int employeeId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(employeeId, ct);
        return _mapper.Map<IEnumerable<EmployeeAddressResponse>>(entities);
    }

    public async Task<EmployeeAddressDetailResponse> GetByIdAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<EmployeeAddressDetailResponse>(entity);
    }

    public async Task<ListPageResponse<EmployeeAddressResponse>> GetPagedAsync(int employeeId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(employeeId, filter, ct);
        return _mapper.Map<ListPageResponse<EmployeeAddressResponse>>(paged);
    }

    public async Task<int> CreateAsync(int employeeId, CreateEmployeeAddressRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByEmployeeAndAddressAsync(tenantId, employeeId, request.Street, request.City, request.PostalCode, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new EmployeeAddressesEntity(
            tenantId,
            employeeId,
            request.AddressTypeId,
            request.CountryCode,
            request.Street,
            request.StreetNumber,
            request.Complement,
            request.Neighborhood,
            request.City,
            request.District,
            request.PostalCode,
            request.Latitude,
            request.Longitude,
            request.Note,
            request.IsPrimary,
            _currentUser.GetUserId());

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int employeeId, int id, UpdateEmployeeAddressRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.UpdateAddressInfo(
            request.AddressTypeId,
            request.CountryCode,
            request.Street,
            request.StreetNumber,
            request.Complement,
            request.Neighborhood,
            request.City,
            request.District,
            request.PostalCode,
            request.Latitude,
            request.Longitude,
            request.Note,
            _currentUser.GetUserId());

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(int employeeId, IFormFile file, CancellationToken ct)
    {
        if (!_fileValidation.ValidateFile(file))
            return false;

        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(employeeId, items, ct);
    }

    private List<BulkUploadEmployeeAddressItem> ReadCsvFile(IFormFile file)
    {
        try
        {
            using var reader = file.OpenReadStream().CreateUtf8StreamReader();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";",
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null
            };

            using var csv = new CsvReader(reader, config);
            var records = new List<BulkUploadEmployeeAddressItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadEmployeeAddressItem>();
                    if (record != null)
                    {
                        record.Street = record.Street?.SanitizeCsvInput().NormalizeUtf8();
                        record.City = record.City?.SanitizeCsvInput().NormalizeUtf8();
                        record.PostalCode = record.PostalCode?.SanitizeCsvInput().NormalizeUtf8();
                        record.District = record.District?.SanitizeCsvInput().NormalizeUtf8();
                        record.CountryCode = record.CountryCode?.SanitizeCsvInput().NormalizeUtf8();

                        if (!string.IsNullOrEmpty(record.Street) && !record.Street.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ReadCsvFile.Street.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.City) && !record.City.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ReadCsvFile.City.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.PostalCode) && !record.PostalCode.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ReadCsvFile.PostalCode.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(int employeeId, List<BulkUploadEmployeeAddressItem> items, CancellationToken ct)
    {
        var hasErrors = false;
        var tenantId = _currentUser.GetTenantId();

        foreach (var item in items)
        {
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            var exists = await _repo.ExistsByEmployeeAndAddressAsync(tenantId, item.EmployeeId, item.Street, item.City, item.PostalCode, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ProcessBulkItems.ExistsByAddress", item.Street), 400);
                hasErrors = true;
                continue;
            }

            var entity = new EmployeeAddressesEntity(
                tenantId,
                item.EmployeeId,
                item.AddressTypeId,
                item.CountryCode,
                item.Street,
                item.StreetNumber,
                item.Complement,
                item.Neighborhood,
                item.City,
                item.District,
                item.PostalCode,
                item.Latitude,
                item.Longitude,
                item.Note,
                item.IsPrimary,
                _currentUser.GetUserId());

            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ProcessBulkItems.FailedToCreate", item.Street), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadEmployeeAddressItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Street))
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ValidateBulkItem.Street", item.EmployeeId), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.City))
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ValidateBulkItem.City", item.EmployeeId), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.PostalCode))
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ValidateBulkItem.PostalCode", item.EmployeeId), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.CountryCode))
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ValidateBulkItem.CountryCode", item.EmployeeId), 400);
            return false;
        }

        if (item.EmployeeId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeAddress.ValidateBulkItem.EmployeeId", item.EmployeeId), 400);
            return false;
        }

        return true;
    }
}
