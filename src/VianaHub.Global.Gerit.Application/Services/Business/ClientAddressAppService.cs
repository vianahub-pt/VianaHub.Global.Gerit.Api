using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientAddress;
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
/// Servi�o de aplica��o para ClientAddress
/// </summary>
public class ClientAddressAppService : IClientAddressAppService
{
    public int TenantId { get; set; }
    public int UserId { get; set; }
    private readonly IClientAddressDataRepository _repo;
    private readonly IClientAddressDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public ClientAddressAppService(
        IClientAddressDataRepository repo,
        IClientRepository clientRepository,
        IClientAddressDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation)
    {
        TenantId = currentUser.GetTenantId();
        UserId = currentUser.GetUserId();
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<ClientAddressResponse>> GetAllAsync(int clientId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(clientId, ct);
        return _mapper.Map<IEnumerable<ClientAddressResponse>>(entities);
    }

    public async Task<ClientAddressDetailResponse> GetByIdAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<ClientAddressDetailResponse>(entity);
    }

    public async Task<ListPageResponse<ClientAddressResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(clientId, filter, ct);
        return _mapper.Map<ListPageResponse<ClientAddressResponse>>(paged);
    }

    public async Task<int> CreateAsync(int clientId, CreateClientAddressRequest request, CancellationToken ct)
    {
        var exists = await _repo.ExistsByClientIdAsync(clientId, request.CountryCode, request.Street, request.StreetNumber, request.Neighborhood, request.City, request.District, request.PostalCode, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var clientAddress = new ClientAddressesEntity(
            TenantId,
            clientId,
            request.AddressTypeId,
            request.CountryCode,
            request.Street,
            request.Neighborhood,
            request.City,
            request.District,
            request.PostalCode,
            request.StreetNumber,
            request.Complement,
            request.Latitude,
            request.Longitude,
            request.Note,
            request.IsPrimary,
            UserId);

        var success = await _domain.CreateAsync(clientAddress, ct);
        return success ? clientAddress.Id : 0;
    }

    public async Task<bool> UpdateAsync(int clientId, int id, UpdateClientAddressRequest request, CancellationToken ct)
    {
        var clientAddress = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientAddress == null || clientAddress.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Update.ResourceNotFound"), 410);
            return false;
        }

        clientAddress.Update(request.AddressTypeId, request.CountryCode, request.Street, request.StreetNumber, request.Complement, request.Neighborhood, request.City, request.District, request.PostalCode, request.Latitude, request.Longitude, request.Note, request.IsPrimary, UserId);

        return await _domain.UpdateAsync(clientAddress, ct);
    }

    public async Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct)
    {
        var clientAddress = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientAddress == null || clientAddress.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Activate.ResourceNotFound"), 410);
            return false;
        }

        clientAddress.Activate(UserId);
        return await _domain.UpdateAsync(clientAddress, ct);
    }

    public async Task<bool> DeactivateAsync(int clientId, int id, CancellationToken ct)
    {
        var clientAddress = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientAddress == null || clientAddress.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        clientAddress.Deactivate(UserId);
        return await _domain.UpdateAsync(clientAddress, ct);
    }

    public async Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct)
    {
        var clientAddress = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientAddress == null || clientAddress.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        clientAddress.Delete(UserId);
        return await _domain.UpdateAsync(clientAddress, ct);
    }

    public async Task<bool> BulkUploadAsync(int clientId, IFormFile file, CancellationToken ct)
    {
        if (!_fileValidation.ValidateFile(file))
            return false;

        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.BulkUpload.EmptyFile"), 400);
            return false;
        }

        return await ProcessBulkItemsAsync(clientId, items, ct);
    }

    private List<BulkUploadClientAddressItem> ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadClientAddressItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadClientAddressItem>();
                    if (record != null)
                    {
                        record.Street = record.Street?.SanitizeCsvInput().NormalizeUtf8();
                        record.City = record.City?.SanitizeCsvInput().NormalizeUtf8();
                        record.PostalCode = record.PostalCode?.SanitizeCsvInput().NormalizeUtf8();
                        record.District = record.District?.SanitizeCsvInput().NormalizeUtf8();
                        record.Neighborhood = record.Neighborhood?.SanitizeCsvInput().NormalizeUtf8();
                        record.CountryCode = record.CountryCode?.SanitizeCsvInput().NormalizeUtf8();
                        record.StreetNumber = record.StreetNumber?.SanitizeCsvInput().NormalizeUtf8();
                        record.Complement = record.Complement?.SanitizeCsvInput().NormalizeUtf8();
                        record.Note = record.Note?.SanitizeCsvInput().NormalizeUtf8();

                        if (!string.IsNullOrEmpty(record.Street) && !record.Street.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ReadCsvFile.Street.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.City) && !record.City.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ReadCsvFile.City.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.PostalCode) && !record.PostalCode.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ReadCsvFile.PostalCode.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(int clientId, List<BulkUploadClientAddressItem> items, CancellationToken ct)
    {
        var hasErrors = false;
        var tenantId = _currentUser.GetTenantId();

        foreach (var item in items)
        {
            if (!ValidateBulkItem(clientId, item))
            {
                hasErrors = true;
                continue;
            }

            var exists = await _repo.ExistsByClientAndAddressTypeAsync(clientId, item.AddressTypeId, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ProcessBulkItems.ExistsByClientAndType", clientId, item.AddressTypeId), 400);
                hasErrors = true;
                continue;
            }

            var entity = new ClientAddressesEntity(
                tenantId,
                clientId,
                item.AddressTypeId,
                item.CountryCode,
                item.Street,
                item.Neighborhood,
                item.City,
                item.District,
                item.PostalCode,
                item.StreetNumber,
                item.Complement,
                item.Latitude,
                item.Longitude,
                item.Note,
                item.IsPrimary,
                _currentUser.GetUserId());

            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ProcessBulkItems.FailedToCreate", item.Street), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(int clientId, BulkUploadClientAddressItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Street))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ValidateBulkItem.Street", clientId), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.City))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ValidateBulkItem.City", clientId), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.PostalCode))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ValidateBulkItem.PostalCode", clientId), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.CountryCode))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ValidateBulkItem.CountryCode", clientId), 400);
            return false;
        }

        if (clientId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ValidateBulkItem.ClientId", clientId), 400);
            return false;
        }

        if (item.AddressTypeId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ValidateBulkItem.AddressTypeId", clientId), 400);
            return false;
        }

        return true;
    }
}

