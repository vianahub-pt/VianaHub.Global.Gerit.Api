using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Action;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Application.Services.Identity;

public class ActionAppService : IActionAppService
{
    private readonly IActionDataRepository _repo;
    private readonly IActionDomainService _domain;
    private readonly ICurrentUserService _currentUser;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ILogger<ActionAppService> _logger;
    private readonly IFileValidationService _fileValidation;

    public ActionAppService(
        IActionDataRepository repo,
        IActionDomainService domain,
        INotify notify,
        ICurrentUserService currentUser,
        ILocalizationService localization,
        ILogger<ActionAppService> logger,
        IFileValidationService fileValidation)
    {
        _repo = repo;
        _domain = domain;
        _notify = notify;
        _currentUser = currentUser;
        _localization = localization;
        _logger = logger;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<ActionResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return ActionMapper.ToResponses(entities);
    }
    public async Task<ActionDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Action.GetById.ResourceNotFound"), 410);
            return null;
        }
        return ActionMapper.ToDetail(entity);
    }
    public async Task<ListPageResponse<ActionResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return ActionMapper.ToPaged(paged);
    }


    public async Task<int> CreateAsync(CreateActionRequest request, CancellationToken ct)
    {
        var normalizedName = request.Name?.Trim() ?? string.Empty;
        var exists = await _repo.ExistsByNameAsync(normalizedName, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Action.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new ActionEntity(normalizedName, request.Description ?? string.Empty, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }
    public async Task<bool> UpdateAsync(int id, UpdateActionRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Action.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.Description ?? string.Empty, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }
    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Action.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Action.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Action.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }
    public async Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct)
    {
        // Valida arquivo usando serviço centralizado
        if (!_fileValidation.ValidateFile(file))
            return false;

        // Lê itens do CSV
        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.Action.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }


    private List<BulkUploadActionItem>? ReadCsvFile(IFormFile file)
    {
        try
        {
            // Cria StreamReader com encoding UTF-8 forçado
            using var reader = file.OpenReadStream().CreateUtf8StreamReader();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";", // CSV usa ponto e vírgula como delimitador
                HeaderValidated = args =>
                {
                    if (args.InvalidHeaders is { Length: > 0 })
                        throw new InvalidDataException("CSV header is invalid.");
                },
                TrimOptions = TrimOptions.Trim,
                BadDataFound = _ => throw new InvalidDataException("CSV row is malformed."),
                MissingFieldFound = _ => throw new InvalidDataException("CSV field is missing.")
            };

            using var csv = new CsvReader(reader, config);
            var records = new List<BulkUploadActionItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read())
            {
                rowCount++;
                if (rowCount > maxRows)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.MaxRows", maxRows), 400);
                    return null;
                }

                var record = csv.GetRecord<BulkUploadActionItem>();
                if (record is null) continue;

                record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8().Trim();
                record.Description = record.Description?.SanitizeCsvInput().NormalizeUtf8();

                if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                    throw new InvalidDataException(_localization.GetMessage("Application.Service.Action.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 1));

                if (!string.IsNullOrEmpty(record.Description) && !record.Description.IsSafeCsvValue())
                    throw new InvalidDataException(_localization.GetMessage("Application.Service.Action.ReadCsvFile.Description.IsSafeCsvValue", rowCount + 1));

                records.Add(record);
            }

            return records;
        }
        catch (CsvHelperException ex)
        {
            _logger.LogWarning("Erro ao ler CSV de Actions | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning("CSV inválido de Actions | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogError("Erro de IO ao ler CSV de Actions | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.Exception"), 400);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Erro de formato ao ler CSV de Actions | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.Exception"), 400);
            return null;
        }
    }
    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadActionItem> items, CancellationToken ct)
    {
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entities = new List<ActionEntity>(items.Count);

        foreach (var item in items)
        {
            // Valida campos obrigatórios
            if (!ValidateBulkItem(item))
            {
                return false;
            }

            item.Name = item.Name!.Trim();
            if (!seenNames.Add(item.Name))
            {
                _notify.Add(_localization.GetMessage("Application.Service.Action.Create.ResourceAlreadyExists"), 409);
                return false;
            }

            // Verifica duplicidade
            var exists = await _repo.ExistsByNameAsync(item.Name, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Action.ProcessBulkItems.ExistsByName", item.Name), 409);
                return false;
            }

            entities.Add(new ActionEntity(item.Name, item.Description ?? string.Empty, _currentUser.GetUserId()));
        }

        return await _domain.CreateManyAsync(entities, ct);
    }
    private bool ValidateBulkItem(BulkUploadActionItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Action.ValidateBulkItem.Name", item.Name), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Description))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Action.ValidateBulkItem.Description", item.Name), 400);
            return false;
        }

        return true;
    }
}
