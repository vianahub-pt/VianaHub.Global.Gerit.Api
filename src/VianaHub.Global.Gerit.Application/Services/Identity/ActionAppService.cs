using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Action;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Action;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
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
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localization;
    private readonly ILogger<ActionAppService> _logger;
    private readonly IFileValidationService _fileValidation;

    public ActionAppService(
        IActionDataRepository repo,
        IActionDomainService domain,
        INotify notify,
        IMapper mapper,
        ICurrentUserService currentUser,
        ILocalizationService localization,
        ILogger<ActionAppService> logger,
        IFileValidationService fileValidation)
    {
        _repo = repo;
        _domain = domain;
        _notify = notify;
        _mapper = mapper;
        _currentUser = currentUser;
        _localization = localization;
        _logger = logger;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<ActionResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<ActionResponse>>(entities);
    }
    public async Task<ActionDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Action.Update.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<ActionDetailResponse?>(entity);
    }
    public async Task<ListPageResponse<ActionResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<ActionResponse>>(paged);
    }


    public async Task<int> CreateAsync(CreateActionRequest request, CancellationToken ct)
    {
        var exists = await _repo.ExistsByNameAsync(request.Name, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Action.Create.ResourceAlreadyExists"), 400);
            return 0;
        }

        var entity = new ActionEntity(request.Code ?? request.Name ?? "ACT", request.Name, request.Description, _currentUser.GetUserId());
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

        entity.Update(request.Code ?? entity.Code ?? request.Name ?? "ACT", request.Name, request.Description, _currentUser.GetUserId());
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
    public async Task<bool> BulkUploadAsync(IFormFile file, CancellationToken ct)
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


    private List<BulkUploadActionItem> ReadCsvFile(IFormFile file)
    {
        try
        {
            // Cria StreamReader com encoding UTF-8 forçado
            using var reader = file.OpenReadStream().CreateUtf8StreamReader();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";", // CSV usa ponto e vírgula como delimitador
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null // Ignora linhas mal formatadas ao invés de lançar exceção
            };

            using var csv = new CsvReader(reader, config);
            var records = new List<BulkUploadActionItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadActionItem>();
                    if (record != null)
                    {
                        // Sanitiza e normaliza campos
                        record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8();
                        record.Description = record.Description?.SanitizeCsvInput().NormalizeUtf8();

                        // Valida se os campos não contêm conteúdo perigoso
                        if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Description) && !record.Description.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.Description.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    // Log linha com erro mas continua processamento
                    _logger.LogWarning(ex, "Erro ao processar linha {RowNumber} do CSV de Actions", rowCount + 2);
                    _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ler arquivo CSV de Actions: {Message}", ex.Message);
            _notify.Add(_localization.GetMessage("Application.Service.Action.ReadCsvFile.Exception"), 400);
            return null;
        }
    }
    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadActionItem> items, CancellationToken ct)
    {
        var hasErrors = false;

        foreach (var item in items)
        {
            // Valida campos obrigatórios
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            // Verifica duplicidade
            var exists = await _repo.ExistsByNameAsync(item.Name, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Action.ProcessBulkItems.ExistsByName", item.Name), 400);
                hasErrors = true;
                continue;
            }

            // Cria a entidade
            var entity = new ActionEntity(item.Code ?? item.Name ?? "ACT", item.Name, item.Description, _currentUser.GetUserId());

            // Tenta criar no domínio
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Action.ProcessBulkItems.FailedToCreate", item.Name), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
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
