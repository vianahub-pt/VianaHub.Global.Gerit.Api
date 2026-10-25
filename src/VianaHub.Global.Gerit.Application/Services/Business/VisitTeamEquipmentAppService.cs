using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitTeamEquipments;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitTeamEquipments;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class VisitTeamEquipmentAppService : IVisitTeamEquipmentsAppService
{
    private readonly IVisitTeamEquipmentDataRepository _repo;
    private readonly IVisitTeamEquipmentDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public VisitTeamEquipmentAppService(
        IVisitTeamEquipmentDataRepository repo,
        IVisitTeamEquipmentDomainService domain,
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

    public async Task<IEnumerable<VisitTeamEquipmentResponse>> GetAllAsync(int visitTeamId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<VisitTeamEquipmentResponse>>(entities);
    }

    public async Task<VisitTeamEquipmentDetailResponse?> GetByIdAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VisitTeamEquipmentDetailResponse>(entity);
    }

    public async Task<ListPageResponse<VisitTeamEquipmentResponse>> GetPagedAsync(int visitTeamId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<VisitTeamEquipmentResponse>>(paged);
    }

    public async Task<int> CreateAsync(int visitTeamId, CreateVisitTeamEquipmentRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByIdAsync(tenantId, visitTeamId, request.EquipmentId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.Create.ResourceAlreadyExists", visitTeamId, request.EquipmentId), 409);
            return 0;
        }

        var entity = new VisitTeamEquipmentEntity(tenantId, visitTeamId, request.EquipmentId, _currentUser.GetUserId());
        entity.Update(visitTeamId, request.EquipmentId, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int visitTeamId, int id, UpdateVisitTeamEquipmentRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(visitTeamId, request.EquipmentId, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int visitTeamId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.VisitTeamId != visitTeamId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(int visitTeamId, IFormFile? file, CancellationToken ct)
    {
        if (!_fileValidation.ValidateFile(file))
            return false;

        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(visitTeamId, items, ct);
    }

    private List<BulkUploadVisitTeamEquipmentItem>? ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadVisitTeamEquipmentItem>();

            csv.Read();
            csv.ReadHeader();

            if (csv.HeaderRecord != null && csv.HeaderRecord.Length == 1 && csv.HeaderRecord[0].Contains(','))
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.ReadCsvFile.InvalidDelimiter"), 400);
                return null;
            }

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadVisitTeamEquipmentItem>();
                    if (record != null)
                    {
                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(int visitTeamId, List<BulkUploadVisitTeamEquipmentItem> items, CancellationToken ct)
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

            var exists = await _repo.ExistsByIdAsync(tenantId, visitTeamId, item.EquipmentId, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.ProcessBulkItems.ExistsByVisitAndEquipment", visitTeamId, item.EquipmentId), 400);
                hasErrors = true;
                continue;
            }

            var entity = new VisitTeamEquipmentEntity(tenantId, visitTeamId, item.EquipmentId, _currentUser.GetUserId());
            entity.Update(visitTeamId, item.EquipmentId, _currentUser.GetUserId());

            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.ProcessBulkItems.FailedToCreate", visitTeamId, item.EquipmentId), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadVisitTeamEquipmentItem item)
    {
        if (item.EquipmentId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.VisitTeamEquipment.ValidateBulkItem.EquipmentId", item.EquipmentId), 400);
            return false;
        }

        return true;
    }
}
