using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamVehicles;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicle;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamVehicles;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class VisitTeamVehicleAppService : IVisitTeamVehiclesAppService
{
    private readonly IVisitTeamVehicleDataRepository _repo;
    private readonly IVisitTeamVehicleDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public VisitTeamVehicleAppService(
        IVisitTeamVehicleDataRepository repo,
        IVisitTeamVehicleDomainService domain,
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

    public async Task<IEnumerable<VisitTeamVehicleResponse>> GetAllAsync(int visitTeamId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<VisitTeamVehicleResponse>>(entities);
    }

    public async Task<VisitTeamVehicleDetailResponse> GetByIdAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitTeamVehicleDetailResponse>(entity);
    }

    public async Task<ListPageResponse<VisitTeamVehicleResponse>> GetPagedAsync(int visitTeamId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<VisitTeamVehicleResponse>>(paged);
    }

    public async Task<int> CreateAsync(int visitTeamId, CreateVisitTeamVehicleRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByIdAsync(tenantId, visitTeamId, request.VehicleId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.Create.ResourceAlreadyExists", visitTeamId, request.VehicleId), 409);
            return 0;
        }

        var entity = new VisitTeamVehicleEntity(tenantId, visitTeamId, request.VehicleId, _currentUser.GetUserId());
        entity.Update(visitTeamId, request.VehicleId, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int visitTeamId, int id, UpdateVisitTeamVehicleRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(visitTeamId, request.VehicleId, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(int visitTeamId, IFormFile file, CancellationToken ct)
    {
        if (!_fileValidation.ValidateFile(file))
            return false;

        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(visitTeamId, items, ct);
    }

    private List<BulkUploadVisitTeamVehicleItem> ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadVisitTeamVehicleItem>();

            csv.Read();
            csv.ReadHeader();

            if (csv.HeaderRecord != null && csv.HeaderRecord.Length == 1 && csv.HeaderRecord[0].Contains(','))
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.ReadCsvFile.InvalidDelimiter"), 400);
                return null;
            }

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadVisitTeamVehicleItem>();
                    if (record != null)
                    {
                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(int visitTeamId, List<BulkUploadVisitTeamVehicleItem> items, CancellationToken ct)
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

            var exists = await _repo.ExistsByIdAsync(tenantId, visitTeamId, item.VehicleId, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.ProcessBulkItems.ExistsByVisitAndVehicle", visitTeamId, item.VehicleId), 400);
                hasErrors = true;
                continue;
            }

            var entity = new VisitTeamVehicleEntity(tenantId, visitTeamId, item.VehicleId, _currentUser.GetUserId());
            entity.Update(visitTeamId, item.VehicleId, _currentUser.GetUserId());

            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.ProcessBulkItems.FailedToCreate", visitTeamId, item.VehicleId), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadVisitTeamVehicleItem item)
    {
        if (item.VehicleId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamVehicle.ValidateBulkItem.VehicleId", item.VehicleId), 400);
            return false;
        }

        return true;
    }
}
