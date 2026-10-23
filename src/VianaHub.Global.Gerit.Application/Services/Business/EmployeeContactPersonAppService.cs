using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeContact;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeContact;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class EmployeeContactPersonAppService : IEmployeeContactPersonAppService
{
    private readonly IEmployeeContactPersonDataRepository _repo;
    private readonly IEmployeeContactPersonDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public EmployeeContactPersonAppService(
        IEmployeeContactPersonDataRepository repo,
        IEmployeeContactPersonDomainService domain,
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

    public async Task<IEnumerable<EmployeeContactPersonResponse>> GetAllAsync(int employeeId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(employeeId, ct);
        return _mapper.Map<IEnumerable<EmployeeContactPersonResponse>>(entities);
    }

    public async Task<EmployeeContactPersonDetailResponse> GetByIdAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<EmployeeContactPersonDetailResponse>(entity);
    }

    public async Task<ListPageResponse<EmployeeContactPersonResponse>> GetPagedAsync(int employeeId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(employeeId, filter, ct);
        return _mapper.Map<ListPageResponse<EmployeeContactPersonResponse>>(paged);
    }

    public async Task<int> CreateAsync(int employeeId, CreateEmployeeContactPersonRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByEmailAsync(tenantId, employeeId, request.Email, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new EmployeeContactPersonsEntity(
            tenantId,
            employeeId,
            request.Name,
            request.Email,
            request.Phone,
            request.JobTitle,
            request.Department,
            request.CellPhoneNumber,
            request.IsCellPhoneWhatsapp,
            request.IsPrimary,
            _currentUser.GetUserId());

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int employeeId, int id, UpdateEmployeeContactPersonRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.Update.ResourceNotFound"), 410);
            return false;
        }

        var tenantId = _currentUser.GetTenantId();
        var emailExists = await _repo.ExistsByEmailForUpdateAsync(tenantId, entity.EmployeeId, request.Email, id, ct);
        if (emailExists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.Update.EmailAlreadyExists"), 409);
            return false;
        }

        entity.UpdateContactInfo(request.Name, request.Email, request.Phone, request.JobTitle, request.Department, request.CellPhoneNumber, request.IsCellPhoneWhatsapp, _currentUser.GetUserId());

        if (request.IsPrimary && !entity.IsPrimary)
        {
            entity.SetAsPrimary(_currentUser.GetUserId());
        }
        else if (!request.IsPrimary && entity.IsPrimary)
        {
            entity.RemoveAsPrimary(_currentUser.GetUserId());
        }

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.Delete.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(employeeId, items, ct);
    }

    private List<BulkUploadEmployeeContactPersonItem> ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadEmployeeContactPersonItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadEmployeeContactPersonItem>();
                    if (record != null)
                    {
                        record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8();
                        record.Email = record.Email?.SanitizeCsvInput().NormalizeUtf8();
                        record.Phone = record.Phone?.SanitizeCsvInput().NormalizeUtf8();

                        if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Email) && !record.Email.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ReadCsvFile.Email.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Phone) && !record.Phone.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ReadCsvFile.Phone.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(int employeeId, List<BulkUploadEmployeeContactPersonItem> items, CancellationToken ct)
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

            var exists = await _repo.ExistsByEmailAsync(tenantId, item.EmployeeId, item.Email, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ProcessBulkItems.ExistsByEmail", item.Email), 400);
                hasErrors = true;
                continue;
            }

            var entity = new EmployeeContactPersonsEntity(
                tenantId,
                item.EmployeeId,
                item.Name,
                item.Email,
                item.Phone,
                null, // JobTitle
                null, // Department
                null, // CellPhoneNumber
                false, // IsCellPhoneWhatsapp
                item.IsPrimary,
                _currentUser.GetUserId());

            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ProcessBulkItems.FailedToCreate", item.Name), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadEmployeeContactPersonItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ValidateBulkItem.Name", item.Name), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Email))
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ValidateBulkItem.Email", item.Name), 400);
            return false;
        }

        if (item.EmployeeId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeContactPerson.ValidateBulkItem.EmployeeId", item.Name), 400);
            return false;
        }

        return true;
    }
}
