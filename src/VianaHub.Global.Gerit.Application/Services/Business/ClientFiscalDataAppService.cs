using AutoMapper;
using Microsoft.AspNetCore.Http;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientFiscalData;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class ClientFiscalDataAppService : IClientFiscalDataAppService
{
    private readonly IClientFiscalDataDataRepository _repo;
    private readonly IClientRepository _clientRepository;
    private readonly IClientFiscalDataDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public ClientFiscalDataAppService(
        IClientFiscalDataDataRepository repo,
        IClientRepository clientRepository,
        IClientFiscalDataDomainService domain,
        IMapper mapper,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation)
    {
        _repo = repo;
        _clientRepository = clientRepository;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<ClientFiscalDataResponse>> GetAllAsync(int clientId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(clientId, ct);

        return _mapper.Map<IEnumerable<ClientFiscalDataResponse>>(entities);
    }

    public async Task<ClientFiscalDataDetailResponse?> GetByIdAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<ClientFiscalDataDetailResponse>(entity);
    }

    public async Task<ListPageResponse<ClientFiscalDataResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(clientId, filter, ct);
        return _mapper.Map<ListPageResponse<ClientFiscalDataResponse>>(paged);
    }

    public async Task<bool> ExistsByIdAsync(int clientId, CancellationToken ct = default)
    {
        return await _repo.ExistsByIdAsync(clientId, ct);
    }

    public async Task<int> CreateAsync(int clientId, CreateClientFiscalDataRequest request, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        if (!await HasActiveClientAsync(clientId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.GetById.ResourceNotFound"), 410);
            return 0;
        }
        var exists = await _repo.ExistsByIdAsync(clientId, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.Create.ClientAlreadyHasFiscalData"), 409);
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            var taxNumberExists = await _repo.ExistsByTaxIdentityAsync(tenantId, request.FiscalCountry?.Trim() ?? string.Empty, request.TaxNumber.Trim(), null, ct);
            if (taxNumberExists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.Create.TaxNumberAlreadyExists"), 409);
                return 0;
            }
        }

        var entity = new ClientFiscalDataEntity(tenantId, clientId, request.TaxNumber, request.VatNumber, request.FiscalCountry, request.IsVatRegistered, request.IBAN, request.FiscalEmail, _currentUser.GetUserId());
        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int clientId, int id, UpdateClientFiscalDataRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.Update.ResourceNotFound"), 410);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            var taxNumberExists = await _repo.ExistsByTaxIdentityAsync(_currentUser.GetTenantId(), request.FiscalCountry?.Trim() ?? string.Empty, request.TaxNumber.Trim(), id, ct);
            if (taxNumberExists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.Update.TaxNumberAlreadyExists"), 409);
                return false;
            }
        }

        entity.Update(request.TaxNumber, request.VatNumber, request.FiscalCountry, request.IsVatRegistered, request.IBAN, request.FiscalEmail, _currentUser.GetUserId());

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.Activate.ResourceNotFound"), 410);
            return false;
        }

        entity.Activate(_currentUser.GetUserId());
        return await _domain.ActivateAsync(entity, ct);
    }

    public async Task<bool> DeactivateAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        entity.Deactivate(_currentUser.GetUserId());
        return await _domain.DeactivateAsync(entity, ct);
    }

    public async Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(int clientId, IFormFile? file, CancellationToken ct)
    {
        if (!await HasActiveClientAsync(clientId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.GetById.ResourceNotFound"), 410);
            return false;
        }
        if (!_fileValidation.ValidateFile(file)) return false;
        using var reader = file!.OpenReadStream().CreateUtf8StreamReader();
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true, Delimiter = ";", MissingFieldFound = null, HeaderValidated = null, TrimOptions = TrimOptions.Trim, BadDataFound = null });
        var items = new List<BulkUploadClientFiscalDataItem>();
        csv.Read();
        csv.ReadHeader();
        var maxRows = DomainExtensions.GetMaxCsvRows();
        var row = 0;
        while (csv.Read())
        {
            if (++row > maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.BulkUpload.MaxRows", maxRows), 400);
                return false;
            }
            try
            {
                var item = csv.GetRecord<BulkUploadClientFiscalDataItem>();
                if (item == null) continue;
                item.TaxNumber = item.TaxNumber?.SanitizeCsvInput().NormalizeUtf8();
                item.VatNumber = item.VatNumber?.SanitizeCsvInput().NormalizeUtf8();
                item.FiscalCountry = item.FiscalCountry?.SanitizeCsvInput().NormalizeUtf8();
                item.IBAN = item.IBAN?.SanitizeCsvInput().NormalizeUtf8();
                item.FiscalEmail = item.FiscalEmail?.SanitizeCsvInput().NormalizeUtf8();
                items.Add(item);
            }
            catch (CsvHelperException)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.BulkUpload.MalformedRow", row + 1), 400);
                return false;
            }
        }
        if (items.Count == 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientFiscalData.BulkUpload.EmptyFile"), 400);
            return false;
        }
        var ok = true;
        foreach (var item in items)
        {
            var request = new CreateClientFiscalDataRequest { TaxNumber = item.TaxNumber, VatNumber = item.VatNumber, FiscalCountry = item.FiscalCountry, IsVatRegistered = item.IsVatRegistered, IBAN = item.IBAN, FiscalEmail = item.FiscalEmail };
            if (await CreateAsync(clientId, request, ct) == 0) ok = false;
        }
        return ok;
    }

    private async Task<bool> HasActiveClientAsync(int clientId, CancellationToken ct)
    {
        var client = await _clientRepository.GetByIdAsync(clientId, ct);
        return client is not null && client.IsActive && !client.IsDeleted;
    }
}
