using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Function;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamFunction;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class VisitTeamFunctionAppService : IVisitTeamFunctionAppService
{
    private readonly IVisitTeamFunctionDataRepository _repo;
    private readonly IVisitTeamFunctionDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<VisitTeamFunctionAppService> _logger;
    private readonly IFileValidationService _fileValidation;

    public VisitTeamFunctionAppService(
        IVisitTeamFunctionDataRepository repo,
        IVisitTeamFunctionDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        ILogger<VisitTeamFunctionAppService> logger,
        IFileValidationService fileValidation)
    {
        _repo = repo;
        _domain = domain;
        this._mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _logger = logger;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<VisitTeamFunctionResponse>> GetAllAsync(int visitTeamId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<VisitTeamFunctionResponse>>(entities);
    }

    public async Task<VisitTeamFunctionDetailResponse?> GetByIdAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Function.Update.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitTeamFunctionDetailResponse>(entity);
    }

    public async Task<ListPageResponse<VisitTeamFunctionResponse>> GetPagedAsync(int visitTeamId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<VisitTeamFunctionResponse>>(paged);
    }

    public async Task<int> CreateAsync(int visitTeamId, CreateVisitTeamFunctionRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByNameAsync(tenantId, request.Name, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Function.Create.ResourceAlreadyExists"), 400);
            return 0;
        }

        var entity = new VisitTeamFunctionsEntity(tenantId, request.Name, request.Description, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int visitTeamId, int id, UpdateVisitTeamFunctionRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Function.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.Name, request.Description, _currentUser.GetUserId());

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Function.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Function.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Function.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(int visitTeamId, IFormFile? file, CancellationToken ct)
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
            _notify.Add(_localization.GetMessage("Application.Service.Function.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadVisitTeamFunctionItem>? ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadVisitTeamFunctionItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadVisitTeamFunctionItem>();
                    if (record != null)
                    {
                        // Sanitiza e normaliza campos
                        record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8();
                        record.Description = record.Description?.SanitizeCsvInput().NormalizeUtf8();

                        // Valida se os campos não contêm conteúdo perigoso
                        if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Function.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Description) && !record.Description.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Function.ReadCsvFile.Description.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    // Log linha com erro mas continua processamento
                    _logger.LogWarning("Erro ao processar linha {RowNumber} do CSV de Functions | Exception: {ExceptionDetails}", rowCount + 2, LogValueSanitizer.ExceptionDetails(ex));
                    _notify.Add(_localization.GetMessage("Application.Service.Function.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Function.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao ler arquivo CSV de Functions: {Message} | Exception: {ExceptionDetails}", LogValueSanitizer.Sanitize(ex.Message), LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Function.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadVisitTeamFunctionItem> items, CancellationToken ct)
    {
        var hasErrors = false;
        var tenantId = _currentUser.GetTenantId();

        foreach (var item in items)
        {
            // Valida campos obrigatórios
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            // Verifica duplicidade
            var exists = await _repo.ExistsByNameAsync(tenantId, item.Name, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Function.ProcessBulkItems.ExistsByName", item.Name), 400);
                hasErrors = true;
                continue;
            }

            // Cria a entidade
            var entity = new VisitTeamFunctionsEntity(tenantId, item.Name, item.Description, _currentUser.GetUserId());

            // Tenta criar no domínio
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Function.ProcessBulkItems.FailedToCreate", item.Name), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadVisitTeamFunctionItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Function.ValidateBulkItem.Name", item.Name), 400);
            return false;
        }

        return true;
    }
}
