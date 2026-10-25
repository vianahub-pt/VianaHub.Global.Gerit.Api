using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Vehicle;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Vehicle;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class VehicleAppService : IVehicleAppService
{
    private readonly IVehicleDataRepository _repo;
    private readonly IVehicleDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public VehicleAppService(
        IVehicleDataRepository repo,
        IVehicleDomainService domain,
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

    public async Task<IEnumerable<VehicleResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<VehicleResponse>>(entities);
    }

    public async Task<VehicleDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<VehicleDetailResponse>(entity);
    }

    public async Task<ListPageResponse<VehicleResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<VehicleResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateVehicleRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var exists = await _repo.ExistsByPlateAsync(tenantId, request.Plate, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.Create.ResourceAlreadyExists"), 400);
            return 0;
        }

        var entity = new VehicleEntity(tenantId, request.StatusDefinitionId, request.StatusDomainId, request.Plate, request.Brand, request.Model, request.Year, request.Color, request.FuelType, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateVehicleRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.StatusDefinitionId, request.StatusDomainId, request.Plate, request.Brand, request.Model, request.Year, request.Color, request.FuelType, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.Delete.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadVehicleItem>? ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadVehicleItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadVehicleItem>();
                    if (record != null)
                    {
                        // Sanitiza e normaliza campos
                        record.Plate = record.Plate?.SanitizeCsvInput().NormalizeUtf8();
                        record.Brand = record.Brand?.SanitizeCsvInput().NormalizeUtf8();
                        record.Model = record.Model?.SanitizeCsvInput().NormalizeUtf8();
                        record.Color = record.Color?.SanitizeCsvInput().NormalizeUtf8();
                        record.FuelType = record.FuelType?.SanitizeCsvInput().NormalizeUtf8();

                        // Valida se os campos n�o cont�m conte�do perigoso
                        if (!string.IsNullOrEmpty(record.Plate) && !record.Plate.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ReadCsvFile.Plate.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Brand) && !record.Brand.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ReadCsvFile.Brand.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Model) && !record.Model.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ReadCsvFile.Model.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    // Log linha com erro mas continua processamento
                    _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadVehicleItem> items, CancellationToken ct)
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
            var exists = await _repo.ExistsByPlateAsync(tenantId, item.Plate, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ProcessBulkItems.ExistsByPlate", item.Plate), 400);
                hasErrors = true;
                continue;
            }

            // Cria a entidade
            var entity = new VehicleEntity(tenantId, item.StatusDefinitionId, item.StatusDomainId, item.Plate, item.Brand, item.Model, item.Year, item.Color, item.FuelType, _currentUser.GetUserId());

            // Tenta criar no dom�nio
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ProcessBulkItems.FailedToCreate", item.Plate), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadVehicleItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Plate))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ValidateBulkItem.Plate", item.Plate), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Brand))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ValidateBulkItem.Brand", item.Plate), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Model))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Vehicle.ValidateBulkItem.Model", item.Plate), 400);
            return false;
        }

        return true;
    }
}
