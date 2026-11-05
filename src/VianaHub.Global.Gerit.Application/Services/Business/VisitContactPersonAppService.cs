using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitContactPersons;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitContactPersons;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

/// <summary>
/// Serviço de aplicação para VisitContact
/// </summary>
public class VisitContactPersonAppService : IVisitContactPersonAppService
{
    private readonly IVisitContactDataRepository _repo;
    private readonly IVisitContactDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;
    private readonly ILogger<VisitContactPersonAppService> _logger;

    public VisitContactPersonAppService(
        IVisitContactDataRepository repo,
        IVisitContactDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation,
        ILogger<VisitContactPersonAppService> logger)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
        _logger = logger;
    }

    public async Task<IEnumerable<VisitContactPersonResponse>> GetAllAsync(int visitId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(visitId, ct);
        return _mapper.Map<IEnumerable<VisitContactPersonResponse>>(entities);
    }

    public async Task<VisitContactPersonDetailResponse?> GetByIdAsync(int visitId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitContactPersonDetailResponse>(entity);
    }

    public async Task<ListPageResponse<VisitContactPersonResponse>> GetPagedAsync(int visitId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(visitId, filter, ct);
        return _mapper.Map<ListPageResponse<VisitContactPersonResponse>>(paged);
    }

    public async Task<int> CreateAsync(int visitId, CreateVisitContactPersonRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var userId = _currentUser.GetUserId();

        var exists = await _repo.ExistsByVisitAndEmailAsync(visitId, request.Email, null, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new VisitContactPersonsEntity(
            tenantId,
            visitId,
            request.Name,
            request.Email,
            request.Phone,
            request.JobTitle,
            request.Department,
            request.CellPhoneNumber,
            request.IsCellPhoneWhatsapp,
            request.IsPrimary,
            userId
        );

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int visitId, int id, UpdateVisitContactPersonRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.Update.ResourceNotFound"), 410);
            return false;
        }

        var exists = await _repo.ExistsByVisitAndEmailAsync(entity.VisitId, request.Email, id, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.Update.ResourceAlreadyExists"), 409);
            return false;
        }

        entity.Update(request.Name, request.Email, request.Phone, request.JobTitle, request.Department, request.CellPhoneNumber, request.IsCellPhoneWhatsapp, request.IsPrimary, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int visitId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(visitId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(int visitId, IFormFile? file, CancellationToken ct)
    {
        if (!_fileValidation.ValidateFile(file))
            return false;

        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(visitId, items, ct);
    }

    private List<BulkUploadVisitContactPersonItem>? ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadVisitContactPersonItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadVisitContactPersonItem>();
                    if (record != null)
                    {
                        record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8();
                        record.Email = record.Email?.SanitizeCsvInput().NormalizeUtf8();
                        record.Phone = record.Phone?.SanitizeCsvInput().NormalizeUtf8();

                        if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Email) && !record.Email.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ReadCsvFile.Email.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Phone) && !record.Phone.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ReadCsvFile.Phone.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    _logger.LogWarning("Erro ao processar linha {RowNumber} do CSV de VisitContactPersons | Exception: {ExceptionDetails}", rowCount + 2, LogValueSanitizer.ExceptionDetails(ex));
                    _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao ler arquivo CSV de VisitContactPersons: {Message} | Exception: {ExceptionDetails}", LogValueSanitizer.Sanitize(ex.Message), LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(int visitId, List<BulkUploadVisitContactPersonItem> items, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var userId = _currentUser.GetUserId();
        var hasErrors = false;

        foreach (var item in items)
        {
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            var exists = await _repo.ExistsByVisitAndEmailAsync(item.VisitId, item.Email, null, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ProcessBulkItems.ExistsByVisitAndEmail", item.Email, item.VisitId), 400);
                hasErrors = true;
                continue;
            }

            var entity = new VisitContactPersonsEntity(
                tenantId,
                item.VisitId,
                item.Name,
                item.Email,
                item.Phone,
                null, // JobTitle
                null, // Department
                null, // CellPhoneNumber
                false, // IsCellPhoneWhatsapp
                item.IsPrimary,
                userId
            );

            var created = await _domain.CreateAsync(entity, ct);
            if (!created)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ProcessBulkItems.FailedToCreate", item.Name), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadVisitContactPersonItem item)
    {
        if (item.VisitId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ValidateBulkItem.VisitTeamId"), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ValidateBulkItem.Name"), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Email))
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitContactPerson.ValidateBulkItem.Email"), 400);
            return false;
        }

        return true;
    }
}
