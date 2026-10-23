using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.EmployeeTeams;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.EmployeeTeams;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class EmployeeTeamAppService : IEmployeeTeamsAppService
{
    private readonly IEmployeeTeamDataRepository _repo;
    private readonly IEmployeeTeamDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public EmployeeTeamAppService(
        IEmployeeTeamDataRepository repo,
        IEmployeeTeamDomainService domain,
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

    public async Task<IEnumerable<EmployeeTeamResponse>> GetAllAsync(int employeeId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(employeeId, ct);
        return _mapper.Map<IEnumerable<EmployeeTeamResponse>>(entities);
    }

    public async Task<EmployeeTeamDetailResponse> GetByIdAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<EmployeeTeamDetailResponse>(entity);
    }

    public async Task<ListPageResponse<EmployeeTeamResponse>> GetPagedAsync(int employeeId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(employeeId, filter, ct);
        return _mapper.Map<ListPageResponse<EmployeeTeamResponse>>(paged);
    }

    public async Task<int> CreateAsync(int employeeId, CreateEmployeeTeamRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByTeamAndMemberAsync(tenantId, request.TeamId, employeeId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var entity = new EmployeeTeamEntity(tenantId, request.TeamId, employeeId, false, DateTime.UtcNow, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int employeeId, int id, UpdateEmployeeTeamRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.TeamId, employeeId, false, DateTime.UtcNow, null, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int employeeId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(employeeId, id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.Delete.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(employeeId, items, ct);
    }

    private List<BulkUploadEmployeeTeamItem> ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadEmployeeTeamItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadEmployeeTeamItem>();
                    if (record != null)
                    {
                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(int employeeId, List<BulkUploadEmployeeTeamItem> items, CancellationToken ct)
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

            var exists = await _repo.ExistsByTeamAndMemberAsync(tenantId, item.TeamId, item.EmployeeId, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.ProcessBulkItems.ExistsByTeamAndMember", item.TeamId, item.EmployeeId), 400);
                hasErrors = true;
                continue;
            }

            var entity = new EmployeeTeamEntity(tenantId, item.TeamId, item.EmployeeId, false, DateTime.UtcNow, _currentUser.GetUserId());

            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.ProcessBulkItems.FailedToCreate", item.TeamId, item.EmployeeId), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadEmployeeTeamItem item)
    {
        if (item.TeamId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.ValidateBulkItem.TeamId", item.TeamId), 400);
            return false;
        }

        if (item.EmployeeId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.EmployeeTeam.ValidateBulkItem.EmployeeId", item.EmployeeId), 400);
            return false;
        }

        return true;
    }
}
