using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Visit;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Visit;
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
/// Servi�o de aplica��o para Visit
/// </summary>
public class VisitAppService : IVisitAppService
{
    private readonly IVisitDataRepository _repo;
    private readonly IVisitDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;
    private readonly ILogger<VisitAppService> _logger;

    public VisitAppService(
        IVisitDataRepository repo,
        IVisitDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation,
        ILogger<VisitAppService> logger)
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

    public async Task<IEnumerable<VisitResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<VisitResponse>>(entities);
    }

    public async Task<VisitDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitDetailResponse>(entity);
    }

    public async Task<ListPageResponse<VisitResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<VisitResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateVisitRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var userId = _currentUser.GetUserId();

        // Validar unicidade: T�tulo �nico por Tenant
        var exists = await _repo.ExistsByTenantAndTitleAsync(tenantId, request.Title, null, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new VisitEntity(
            tenantId,
            request.ClientId,
            request.StatusDefinitionId,
            request.StatusDomainId,
            request.CurrencyCode,
            request.Title,
            request.Description,
            request.StartDateTime,
            request.EstimatedValue,
            userId
        );

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateVisitRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.Update.ResourceNotFound"), 410);
            return false;
        }

        var tenantId = _currentUser.GetTenantId();

        // Validar unicidade: T�tulo �nico por Tenant (excluindo o pr�prio registro)
        var exists = await _repo.ExistsByTenantAndTitleAsync(tenantId, request.Title, id, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.Create.ResourceAlreadyExists"), 409);
            return false;
        }

        entity.UpdateDetails(
            request.ClientId,
            request.StatusDefinitionId,
            request.StatusDomainId,
            request.CurrencyCode,
            request.Title,
            request.Description,
            request.StartDateTime,
            request.EndDateTime,
            request.EstimatedValue,
            request.RealValue,
            _currentUser.GetUserId()
        );

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Visit.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Visit.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct)
    {
        // Valida arquivo usando servi�o centralizado
        if (!_fileValidation.ValidateFile(file))
            return false;

        // L� itens do CSV
        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadVisitItem>? ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadVisitItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadVisitItem>();
                    if (record != null)
                    {
                        record.Title = record.Title?.SanitizeCsvInput().NormalizeUtf8();
                        record.Description = record.Description?.SanitizeCsvInput().NormalizeUtf8();

                        if (!string.IsNullOrEmpty(record.Title) && !record.Title.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Visit.ReadCsvFile.Title.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Description) && !record.Description.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Visit.ReadCsvFile.Description.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    _logger.LogWarning("Erro ao processar linha {RowNumber} do CSV de Visits | Exception: {ExceptionDetails}", rowCount + 2, LogValueSanitizer.ExceptionDetails(ex));
                    _notify.Add(_localization.GetMessage("Application.Service.Visit.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Visit.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao ler arquivo CSV de Visits: {Message} | Exception: {ExceptionDetails}", LogValueSanitizer.Sanitize(ex.Message), LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Visit.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadVisitItem> items, CancellationToken ct)
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

            var exists = await _repo.ExistsByTenantAndTitleAsync(tenantId, item.Title, null, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Visit.ProcessBulkItems.ExistsByTitle", item.Title), 400);
                hasErrors = true;
                continue;
            }

            var entity = new VisitEntity(
                tenantId,
                item.ClientId,
                item.StatusDefinitionId,
                item.StatusDomainId,
                item.CurrencyCode,
                item.Title,
                item.Description,
                item.StartDateTime,
                item.EstimatedValue,
                userId
            );

            var created = await _domain.CreateAsync(entity, ct);
            if (!created)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Visit.ProcessBulkItems.FailedToCreate", item.Title), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadVisitItem item)
    {
        if (item.ClientId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.ValidateBulkItem.ClientId"), 400);
            return false;
        }

        if (item.StatusDefinitionId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.ValidateBulkItem.StatusDefinitionId"), 400);
            return false;
        }

        if (item.StatusDomainId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.ValidateBulkItem.StatusDomainId"), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Title))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.ValidateBulkItem.Title"), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Description))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.ValidateBulkItem.Description"), 400);
            return false;
        }

        if (item.StartDateTime == default)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.ValidateBulkItem.StartDateTime"), 400);
            return false;
        }

        if (item.EstimatedValue < 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Visit.ValidateBulkItem.EstimatedValue"), 400);
            return false;
        }

        return true;
    }
}

