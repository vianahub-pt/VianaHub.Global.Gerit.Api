using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.FileType;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.FileType;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class FileTypeAppService : IFileTypeAppService
{
    private readonly IFileTypeDataRepository _repo;
    private readonly IFileTypeDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;
    private readonly ILogger<FileTypeAppService> _logger;

    public FileTypeAppService(
        IFileTypeDataRepository repo,
        IFileTypeDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation,
        ILogger<FileTypeAppService> logger)
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

    public async Task<IEnumerable<FileTypeResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<FileTypeResponse>>(entities);
    }

    public async Task<FileTypeDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.FileType.GetById.ResourceNotFound"), 410);
            return null;
        }

        return _mapper.Map<FileTypeDetailResponse>(entity);
    }

    public async Task<ListPageResponse<FileTypeResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<FileTypeResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateFileTypeRequest request, CancellationToken ct)
    {
        var exists = await _repo.ExistsByMimeTypeAsync(request.MimeType, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.FileType.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new FileTypeEntity(request.MimeType, request.Extension, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateFileTypeRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.FileType.Update.ResourceNotFound"), 410);
            return false;
        }

        var exists = await _repo.ExistsByMimeTypeAsync(request.MimeType, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.FileType.Update.ResourceAlreadyExists"), 409);
            return false;
        }

        entity.Update(request.MimeType, request.Extension, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.FileType.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.FileType.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.FileType.Delete.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.FileType.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadFileTypeItem> ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadFileTypeItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadFileTypeItem>();
                    if (record != null)
                    {
                        record.MimeType = record.MimeType?.SanitizeCsvInput().NormalizeUtf8();
                        record.Extension = record.Extension?.SanitizeCsvInput().NormalizeUtf8();

                        if (!string.IsNullOrEmpty(record.MimeType) && !record.MimeType.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.FileType.ReadCsvFile.MimeType.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Extension) && !record.Extension.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.FileType.ReadCsvFile.Extension.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    _logger.LogWarning(ex, "Erro ao processar linha {RowNumber} do CSV de FileTypes", rowCount + 2);
                    _notify.Add(_localization.GetMessage("Application.Service.FileType.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.FileType.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ler arquivo CSV de FileTypes: {Message}", ex.Message);
            _notify.Add(_localization.GetMessage("Application.Service.FileType.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadFileTypeItem> items, CancellationToken ct)
    {
        var hasErrors = false;

        foreach (var item in items)
        {
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            var exists = await _repo.ExistsByMimeTypeAsync(item.MimeType, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.FileType.ProcessBulkItems.ExistsByMimeType", item.MimeType), 400);
                hasErrors = true;
                continue;
            }

            var entity = new FileTypeEntity(item.MimeType, item.Extension, _currentUser.GetUserId());
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.FileType.ProcessBulkItems.FailedToCreate", item.MimeType), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadFileTypeItem item)
    {
        if (string.IsNullOrWhiteSpace(item.MimeType))
        {
            _notify.Add(_localization.GetMessage("Application.Service.FileType.ValidateBulkItem.MimeType"), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Extension))
        {
            _notify.Add(_localization.GetMessage("Application.Service.FileType.ValidateBulkItem.Extension"), 400);
            return false;
        }

        return true;
    }
}
