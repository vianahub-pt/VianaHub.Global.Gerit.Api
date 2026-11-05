using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientAddress;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientAddress;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Mappings.Business;
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
    private readonly IClientRepository _clientRepository;
    private readonly IAddressTypeDataRepository _addressTypeRepository;
    private readonly IClientAddressDomainService _domain;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public ClientAddressAppService(
        IClientAddressDataRepository repo,
        IClientRepository clientRepository,
        IAddressTypeDataRepository addressTypeRepository,
        IClientAddressDomainService domain,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation)
    {
        TenantId = currentUser.GetTenantId();
        UserId = currentUser.GetUserId();
        _repo = repo;
        _clientRepository = clientRepository;
        _addressTypeRepository = addressTypeRepository;
        _domain = domain;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<ClientAddressResponse>> GetAllAsync(int clientId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(clientId, ct);
        return ClientAddressMapper.ToResponses(entities);
    }

    public async Task<ClientAddressDetailResponse?> GetByIdAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.GetById.ResourceNotFound"), 410);
            return null;
        }
        return ClientAddressMapper.ToLocalizedDetail(entity);
    }

    public async Task<ListPageResponse<ClientAddressResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(clientId, filter, ct);
        return ClientAddressMapper.ToPaged(paged);
    }

    public async Task<int> CreateAsync(int clientId, CreateClientAddressRequest request, CancellationToken ct)
    {
        if (!await HasActiveClientAsync(clientId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.GetById.ResourceNotFound"), 410);
            return 0;
        }
        if (!await _addressTypeRepository.ExistsByIdAsync(request.AddressTypeId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.AddressType.GetById.ResourceNotFound"), 410);
            return 0;
        }

        var exists = await _repo.ExistsByIdentityAsync(clientId, null, request.CountryCode, request.Street, request.StreetNumber, request.Neighborhood, request.City, request.District, request.PostalCode, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var clientAddress = new ClientAddressesEntity(
            TenantId,
            clientId,
            request.AddressTypeId,
            NormalizeCountryCode(request.CountryCode) ?? string.Empty,
            request.Street ?? string.Empty,
            request.StreetNumber,
            request.Complement,
            request.Neighborhood ?? string.Empty,
            request.City ?? string.Empty,
            request.District,
            request.PostalCode ?? string.Empty,
            request.Latitude,
            request.Longitude,
            request.Note,
            request.IsPrimary,
            UserId);

        if (request.IsPrimary && await _repo.ExistsPrimaryAsync(clientId, null, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

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

        if (!await HasActiveClientAsync(clientId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Update.ResourceNotFound"), 410);
            return false;
        }
        if (!await _addressTypeRepository.ExistsByIdAsync(request.AddressTypeId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.AddressType.GetById.ResourceNotFound"), 410);
            return false;
        }

        if (await _repo.ExistsByIdentityAsync(clientId, id, request.CountryCode, request.Street, request.StreetNumber, request.Neighborhood, request.City, request.District, request.PostalCode, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Update.ResourceAlreadyExists"), 409);
            return false;
        }

        if (request.IsPrimary && await _repo.ExistsPrimaryAsync(clientId, id, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Update.ResourceAlreadyExists"), 409);
            return false;
        }

        clientAddress.Update(request.AddressTypeId, NormalizeCountryCode(request.CountryCode) ?? string.Empty, request.Street ?? string.Empty, request.StreetNumber, request.Complement, request.Neighborhood ?? string.Empty, request.City ?? string.Empty, request.District, request.PostalCode ?? string.Empty, request.Latitude, request.Longitude, request.Note, request.IsPrimary, UserId);

        return await _domain.UpdateAsync(clientAddress, ct);
    }

    public async Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct)
    {
        if (!await HasActiveClientAsync(clientId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Activate.ResourceNotFound"), 410);
            return false;
        }
        var clientAddress = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientAddress == null || clientAddress.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Activate.ResourceNotFound"), 410);
            return false;
        }

        clientAddress.Activate(UserId);
        return await _domain.ActivateAsync(clientAddress, ct);
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
        return await _domain.DeactivateAsync(clientAddress, ct);
    }

    public async Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct)
    {
        var clientAddress = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientAddress == null || clientAddress.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Delete.ResourceNotFound"), 410);
            return false;
        }

        clientAddress.Delete(UserId);
        return await _domain.DeleteAsync(clientAddress, ct);
    }

    public async Task<bool> BulkUploadAsync(int clientId, IFormFile? file, CancellationToken ct)
    {
        if (!await HasActiveClientAsync(clientId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.GetById.ResourceNotFound"), 410);
            return false;
        }

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

    private List<BulkUploadClientAddressItem>? ReadCsvFile(IFormFile file)
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

            while (csv.Read())
            {
                if (rowCount >= maxRows)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ReadCsvFile.MaxRows", maxRows), 400);
                    return null;
                }
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
                        record.CountryCode = record.CountryCode?.SanitizeCsvInput().NormalizeUtf8().Trim().ToUpperInvariant();
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
        var tenantId = _currentUser.GetTenantId();
        var entities = new List<ClientAddressesEntity>();
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var primaryCount = 0;

        foreach (var item in items)
        {
            if (!ValidateBulkItem(clientId, item))
                continue;

            if (!await _addressTypeRepository.ExistsByIdAsync(item.AddressTypeId, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Service.AddressType.GetById.ResourceNotFound"), 410);
                continue;
            }

            var identity = string.Join("|", item.CountryCode, item.Street, item.StreetNumber, item.Neighborhood, item.City, item.District, item.PostalCode);
            if (!identities.Add(identity))
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Create.ResourceAlreadyExists"), 409);
                continue;
            }

            if (await _repo.ExistsByIdentityAsync(clientId, null, item.CountryCode, item.Street, item.StreetNumber, item.Neighborhood, item.City, item.District, item.PostalCode, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.ProcessBulkItems.ExistsByClientAndType", clientId, item.AddressTypeId), 400);
                continue;
            }

            if (item.IsPrimary)
                primaryCount++;

            entities.Add(new ClientAddressesEntity(
                tenantId,
                clientId,
                item.AddressTypeId,
                item.CountryCode ?? string.Empty,
                item.Street ?? string.Empty,
                item.StreetNumber,
                item.Complement,
                item.Neighborhood ?? string.Empty,
                item.City ?? string.Empty,
                item.District,
                item.PostalCode ?? string.Empty,
                item.Latitude,
                item.Longitude,
                item.Note,
                item.IsPrimary,
                _currentUser.GetUserId()));
        }

        if (_notify.HasNotify() || entities.Count != items.Count)
            return false;

        if (primaryCount > 1 || (primaryCount == 1 && await _repo.ExistsPrimaryAsync(clientId, null, ct)))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientAddress.Create.ResourceAlreadyExists"), 409);
            return false;
        }

        return await _domain.CreateManyAsync(entities, ct);
    }

    private async Task<bool> HasActiveClientAsync(int clientId, CancellationToken ct)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, ct);
        return client is not null && client.IsActive && !client.IsDeleted;
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

    private static string? NormalizeCountryCode(string? value) => string.IsNullOrWhiteSpace(value) ? value : value.Trim().ToUpperInvariant();
}

