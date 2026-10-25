using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeams;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeam;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class VisitTeamAppService : IVisitTeamsAppService
{
    private readonly IVisitTeamDataRepository _repo;
    private readonly IVisitTeamDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public VisitTeamAppService(
        IVisitTeamDataRepository repo,
        IVisitTeamDomainService domain,
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

    public async Task<IEnumerable<VisitTeamResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<VisitTeamResponse>>(entities);
    }

    public async Task<VisitTeamDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitTeamDetailResponse>(entity);
    }

    public async Task<ListPageResponse<VisitTeamResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<VisitTeamResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateVisitTeamRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByIdAsync(tenantId, request.VisitId, request.TeamId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new VisitTeamEntity(tenantId, request.VisitId, request.TeamId, DateTime.UtcNow, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateVisitTeamRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.VisitId, request.TeamId, DateTime.UtcNow, null, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct)
    {
        if (!_fileValidation.ValidateFile(file))
            return false;

        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadVisitTeamItem>? ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadVisitTeamItem>();

            csv.Read();
            csv.ReadHeader();

            // If header was read as a single column that contains commas, the delimiter is likely incorrect
            if (csv.HeaderRecord != null && csv.HeaderRecord.Length == 1 && csv.HeaderRecord[0].Contains(','))
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.ReadCsvFile.InvalidDelimiter"), 400);
                return null;
            }

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadVisitTeamItem>();
                    if (record != null)
                    {
                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadVisitTeamItem> items, CancellationToken ct)
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

            var exists = await _repo.ExistsByIdAsync(tenantId, item.VisitId, item.TeamId, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.ProcessBulkItems.ExistsByVisitAndTeam", item.VisitId, item.TeamId), 400);
                hasErrors = true;
                continue;
            }

            var entity = new VisitTeamEntity(tenantId, item.VisitId, item.TeamId, DateTime.UtcNow, _currentUser.GetUserId());

            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.ProcessBulkItems.FailedToCreate", item.VisitId, item.TeamId), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadVisitTeamItem item)
    {
        if (item.VisitId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.ValidateBulkItem.VisitTeamId", item.VisitId), 400);
            return false;
        }

        if (item.TeamId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeam.ValidateBulkItem.TeamId", item.TeamId), 400);
            return false;
        }

        return true;
    }
}
