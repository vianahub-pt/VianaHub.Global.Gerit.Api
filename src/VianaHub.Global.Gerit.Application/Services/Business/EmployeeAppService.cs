using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Employee;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Employee;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class EmployeeAppService : IEmployeeAppService
{
    private readonly IEmployeeDataRepository _repo;
    private readonly IEmployeeDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public EmployeeAppService(
        IEmployeeDataRepository repo,
        IEmployeeDomainService domain,
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

    public async Task<IEnumerable<EmployeeResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<EmployeeResponse>>(entities);
    }

    public async Task<EmployeeDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Employee.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<EmployeeDetailResponse>(entity);
    }

    public async Task<ListPageResponse<EmployeeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<EmployeeResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateEmployeeRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var exists = await _repo.ExistsByEmailAsync(tenantId, request.Email, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Employee.Create.ResourceAlreadyExists"), 409);
                return 0;
            }
        }

        var entity = new EmployeeEntity(tenantId, request.StatusDefinitionId, request.StatusDomainId, request.Name,
            request.PhoneNumber, request.CellPhoneNumber, request.IsCellPhoneWhatsapp,
            request.Email, request.ImageUrl, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Employee.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.StatusDefinitionId, request.StatusDomainId, request.Name,
            request.PhoneNumber, request.CellPhoneNumber, request.IsCellPhoneWhatsapp,
            request.Email, request.ImageUrl, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Employee.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Employee.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Employee.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(IFormFile file, CancellationToken ct)
    {
        if (!_fileValidation.ValidateFile(file))
            return false;

        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.Employee.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadEmployeeItem> ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadEmployeeItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadEmployeeItem>();
                    if (record != null)
                    {
                        record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8();
                        record.Email = record.Email?.SanitizeCsvInput().NormalizeUtf8();
                        record.PhoneNumber = record.PhoneNumber?.SanitizeCsvInput().NormalizeUtf8();
                        record.CellPhoneNumber = record.CellPhoneNumber?.SanitizeCsvInput().NormalizeUtf8();
                        record.ImageUrl = record.ImageUrl?.SanitizeCsvInput().NormalizeUtf8();

                        if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Employee.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Email) && !record.Email.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Employee.ReadCsvFile.Email.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.Employee.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Employee.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Employee.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadEmployeeItem> items, CancellationToken ct)
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

            if (!string.IsNullOrWhiteSpace(item.Email))
            {
                var exists = await _repo.ExistsByEmailAsync(tenantId, item.Email, ct);
                if (exists)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.Employee.ProcessBulkItems.ExistsByEmail", item.Email), 400);
                    hasErrors = true;
                    continue;
                }
            }

            var entity = new EmployeeEntity(tenantId, 0, 0, item.Name,
                item.PhoneNumber, item.CellPhoneNumber, item.IsCellPhoneWhatsapp,
                item.Email, item.ImageUrl, _currentUser.GetUserId());

            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Employee.ProcessBulkItems.FailedToCreate", item.Name), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadEmployeeItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Employee.ValidateBulkItem.Name", item.Name), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Email))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Employee.ValidateBulkItem.Email", item.Name), 400);
            return false;
        }

        return true;
    }
}
