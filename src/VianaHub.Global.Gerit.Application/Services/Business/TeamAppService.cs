using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Team;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Team;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class TeamAppService : ITeamAppService
{
    private readonly ITeamDataRepository _repo;
    private readonly ITeamDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<TeamAppService> _logger;
    private readonly IFileValidationService _fileValidation;

    public TeamAppService(
        ITeamDataRepository repo,
        ITeamDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        ILogger<TeamAppService> logger,
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

    public async Task<IEnumerable<TeamResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<TeamResponse>>(entities);
    }

    public async Task<TeamDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Team.Update.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<TeamDetailResponse>(entity);
    }

    public async Task<ListPageResponse<TeamResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<TeamResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateTeamRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByNameAsync(tenantId, request.Name, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Team.Create.ResourceAlreadyExists"), 400);
            return 0;
        }

        var entity = new TeamEntity(tenantId, request.Name, request.Description, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateTeamRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Team.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.Name, request.Description, _currentUser.GetUserId());

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Team.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Team.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Team.Delete.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Team.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadTeamItem>? ReadCsvFile(IFormFile file)
    {
        try
        {
            // Cria StreamReader com encoding UTF-8 for�ado
            using var reader = file.OpenReadStream().CreateUtf8StreamReader();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";", // CSV usa ponto e v�rgula como delimitador
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null // Ignora linhas mal formatadas ao inv�s de lan�ar exce��o
            };

            using var csv = new CsvReader(reader, config);
            var records = new List<BulkUploadTeamItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadTeamItem>();
                    if (record != null)
                    {
                        // Sanitiza e normaliza campos
                        record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8();
                        record.Description = record.Description?.SanitizeCsvInput().NormalizeUtf8();

                        // Valida se os campos n�o cont�m conte�do perigoso
                        if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Team.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Description) && !record.Description.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Team.ReadCsvFile.Description.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    // Log linha com erro mas continua processamento
                    _logger.LogWarning(ex, "Erro ao processar linha {RowNumber} do CSV de Teams", rowCount + 2);
                    _notify.Add(_localization.GetMessage("Application.Service.Team.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Team.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ler arquivo CSV de Teams: {Message}", ex.Message);
            _notify.Add(_localization.GetMessage("Application.Service.Team.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadTeamItem> items, CancellationToken ct)
    {
        var hasErrors = false;
        var tenantId = _currentUser.GetTenantId();

        foreach (var item in items)
        {
            // Valida campos obrigat�rios
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            // Verifica duplicidade
            var exists = await _repo.ExistsByNameAsync(tenantId, item.Name, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Team.ProcessBulkItems.ExistsByName", item.Name), 400);
                hasErrors = true;
                continue;
            }

            // Cria a entidade
            var entity = new TeamEntity(tenantId, item.Name, item.Description, _currentUser.GetUserId());

            // Tenta criar no dom�nio
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Team.ProcessBulkItems.FailedToCreate", item.Name), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadTeamItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Team.ValidateBulkItem.Name", item.Name), 400);
            return false;
        }

        return true;
    }
}
