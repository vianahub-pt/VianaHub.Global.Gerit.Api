using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.Client;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.Client;
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

public class ClientAppService : IClientAppService
{
    private int UserId { get; set; }
    private int TenantId { get; set; }
    private readonly IClientRepository _repo;
    private readonly IClientDomainService _domain;
    private readonly IPartyTypeDataRepository? _partyTypes;
    private readonly IAcquisitionSourceTypeDataRepository? _acquisitionSources;
    private readonly IStatusDomainDataRepository? _statusDomains;
    private readonly IStatusDefinitionDataRepository? _statusDefinitions;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;
    private readonly ILogger<ClientAppService> _logger;


    public ClientAppService(
        IClientRepository repo,
        IClientDomainService domain,
        INotify notify,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IFileValidationService fileValidation,
        ILogger<ClientAppService> logger,
        IPartyTypeDataRepository? partyTypes = null,
        IAcquisitionSourceTypeDataRepository? acquisitionSources = null,
        IStatusDomainDataRepository? statusDomains = null,
        IStatusDefinitionDataRepository? statusDefinitions = null)
    {
        _repo = repo;
        _domain = domain;
        _partyTypes = partyTypes;
        _acquisitionSources = acquisitionSources;
        _statusDomains = statusDomains;
        _statusDefinitions = statusDefinitions;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
        _logger = logger;
        UserId = _currentUser.GetUserId();
        TenantId = _currentUser.GetTenantId();
    }

    public async Task<IEnumerable<ClientResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return ClientMapper.ToResponses(entities);
    }

    public async Task<ClientDetailResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Client.GetById.ResourceNotFound"), 410);
            return null;
        }
        return ClientMapper.ToLocalizedDetail(entity);
    }

    public async Task<ListPageResponse<ClientResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return ClientMapper.ToPaged(paged);
    }

    public async Task<int> CreateAsync(CreateClientRequest request, CancellationToken ct)
    {
        // Valida consistência PartyTypeId vs campos obrigatórios via INotify
        if (!ValidatePartyTypeConsistency(request, isCreate: true))
            return 0;

        if (!await ValidateDependenciesAsync(request.PartyTypeId, request.AcquisitionSourceTypeId, request.StatusDomainId, request.StatusDefinitionId, ct))
            return 0;

        var client = new ClientEntity(
            TenantId,
            request.PartyTypeId,
            request.AcquisitionSourceTypeId,
            request.UrlImage,
            request.Note,
            request.Name,
            request.PhoneNumber,
            request.CellPhoneNumber,
            request.IsCellPhoneWhatsapp,
            request.Email,
            request.WebsiteUrl,
            request.BirthDate,
            request.Gender,
            request.Nationality,
            request.CompanyRegistrationNumber,
            request.EconomicActivityCode,
            request.NumberOfEmployees,
            request.StatusDefinitionId,
            request.StatusDomainId,
            UserId);

        var success = await _domain.CreateAsync(client, ct);
        return success ? client.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateClientRequest request, CancellationToken ct)
    {
        var client = await _repo.GetByIdAsync(id, ct);

        if (client == null || client.IsDeleted || !client.IsActive)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Client.Update.ResourceNotFound"), 410);
            return false;
        }

        // Valida consistência PartyTypeId vs campos
        if (!ValidatePartyTypeConsistency(request, isCreate: false))
            return false;

        if (!await ValidateDependenciesAsync(request.PartyTypeId, request.AcquisitionSourceTypeId, request.StatusDomainId, request.StatusDefinitionId, ct))
            return false;

        client.Update(
            request.PartyTypeId,
            request.AcquisitionSourceTypeId,
            request.UrlImage,
            request.Note,
            request.Name,
            request.PhoneNumber,
            request.CellPhoneNumber,
            request.IsCellPhoneWhatsapp,
            request.Email,
            request.WebsiteUrl,
            request.BirthDate,
            request.Gender,
            request.Nationality,
            request.CompanyRegistrationNumber,
            request.EconomicActivityCode,
            request.NumberOfEmployees,
            request.StatusDefinitionId,
            request.StatusDomainId,
            UserId);

        return await _domain.UpdateAsync(client, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var client = await _repo.GetByIdAsync(id, ct);
        if (client == null || client.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Client.Activate.ResourceNotFound"), 410);
            return false;
        }

        if (client.IsActive)
            return true;

        client.Activate(UserId);
        var success = await _domain.ActivateAsync(client, ct);
        if (!success)
            _notify.Add(_localization.GetMessage("Api.Helpers.NotifyExtensions.Error.Generic"), 400);
        return success;
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken ct)
    {
        var client = await _repo.GetByIdAsync(id, ct);
        if (client == null || client.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Client.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        if (!client.IsActive)
            return true;

        client.Deactivate(UserId);
        var success = await _domain.DeactivateAsync(client, ct);
        if (!success)
            _notify.Add(_localization.GetMessage("Api.Helpers.NotifyExtensions.Error.Generic"), 400);
        return success;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var client = await _repo.GetByIdAsync(id, ct);
        if (client == null || client.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Client.Delete.ResourceNotFound"), 410);
            return false;
        }

        client.Delete(UserId);
        var success = await _domain.DeleteAsync(client, ct);
        if (!success)
            _notify.Add(_localization.GetMessage("Api.Helpers.NotifyExtensions.Error.Generic"), 400);
        return success;
    }

    public async Task<bool> BulkUploadAsync(IFormFile? file, CancellationToken ct)
    {
        // Valida arquivo usando servico centralizado
        if (!_fileValidation.ValidateFile(file))
            return false;

        // Le itens do CSV
        var items = ReadCsvFile(file);
        if (items == null)
            return false;

        if (!items.Any())
        {
            _notify.Add(_localization.GetMessage("Application.Service.Client.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    /// <summary>
    /// Valida a consistência entre PartyTypeId e os campos obrigatórios por tipo de party.
    /// PartyTypeId=1 (pessoa singular): CompanyRegistrationNumber/EconomicActivityCode/NumberOfEmployees devem ser null.
    /// PartyTypeId=2 (pessoa jurídica): BirthDate/Gender/Nationality devem ser null.
    /// </summary>
    private bool ValidatePartyTypeConsistency(CreateClientRequest request, bool isCreate)
    {
        // PartyTypeId=1: pessoa singular → dados de empresa não permitidos
        if (request.PartyTypeId == 1)
        {
            if (!string.IsNullOrWhiteSpace(request.CompanyRegistrationNumber) ||
                !string.IsNullOrWhiteSpace(request.EconomicActivityCode) ||
                request.NumberOfEmployees.HasValue)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Client.Validate.Consistency.IndividualInvalidCompanyFields"), 422);
                return false;
            }
        }
        // PartyTypeId=2: pessoa jurídica → dados pessoais não permitidos
        else if (request.PartyTypeId == 2)
        {
            if (request.BirthDate.HasValue ||
                !string.IsNullOrWhiteSpace(request.Gender) ||
                !string.IsNullOrWhiteSpace(request.Nationality))
            {
                _notify.Add(_localization.GetMessage("Application.Service.Client.Validate.Consistency.CompanyInvalidIndividualFields"), 422);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Valida a consistência entre PartyTypeId e os campos obrigatórios para update.
    /// </summary>
    private bool ValidatePartyTypeConsistency(UpdateClientRequest request, bool isCreate)
    {
        // PartyTypeId=1: pessoa singular → dados de empresa não permitidos
        if (request.PartyTypeId == 1)
        {
            if (!string.IsNullOrWhiteSpace(request.CompanyRegistrationNumber) ||
                !string.IsNullOrWhiteSpace(request.EconomicActivityCode) ||
                request.NumberOfEmployees.HasValue)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Client.Validate.Consistency.IndividualInvalidCompanyFields"), 422);
                return false;
            }
        }
        // PartyTypeId=2: pessoa jurídica → dados pessoais não permitidos
        else if (request.PartyTypeId == 2)
        {
            if (request.BirthDate.HasValue ||
                !string.IsNullOrWhiteSpace(request.Gender) ||
                !string.IsNullOrWhiteSpace(request.Nationality))
            {
                _notify.Add(_localization.GetMessage("Application.Service.Client.Validate.Consistency.CompanyInvalidIndividualFields"), 422);
                return false;
            }
        }

        return true;
    }

    private async Task<bool> ValidateDependenciesAsync(byte partyTypeId, int acquisitionSourceTypeId, int statusDomainId, int statusDefinitionId, CancellationToken ct)
    {
        // Os repositórios são opcionais apenas para manter compatibilidade com testes unitários antigos.
        if (_partyTypes is null || _acquisitionSources is null || _statusDomains is null || _statusDefinitions is null)
            return true;

        var partyType = await _partyTypes.GetByIdAsync(partyTypeId, ct);
        var source = await _acquisitionSources.GetByIdAsync(acquisitionSourceTypeId, ct);
        var domain = await _statusDomains.GetByIdAsync(statusDomainId, ct);
        var definition = await _statusDefinitions.GetByIdAsync(statusDefinitionId, ct);

        if (partyType is null || partyType.IsDeleted || !partyType.IsActive ||
            source is null || source.IsDeleted || !source.IsActive ||
            domain is null || domain.IsDeleted || !domain.IsActive ||
            definition is null || definition.IsDeleted || !definition.IsActive ||
            definition.StatusDomainId != statusDomainId ||
            definition.TenantId != TenantId)
        {
            _notify.Add(_localization.GetMessage("Api.Helpers.NotifyExtensions.Error.ValidationError"), 400);
            return false;
        }

        return true;
    }

    private List<BulkUploadClientItem>? ReadCsvFile(IFormFile file)
    {
        try
        {
            // Cria StreamReader com encoding UTF-8 forcado
            using var reader = file.OpenReadStream().CreateUtf8StreamReader();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ";", // CSV usa ponto e virgula como delimitador
                MissingFieldFound = null,
                HeaderValidated = null,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null // Ignora linhas mal formatadas ao inves de lancar excecao
            };

            using var csv = new CsvReader(reader, config);
            var records = new List<BulkUploadClientItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read())
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadClientItem>();
                    if (record != null)
                    {
                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    // Log linha com erro mas continua processamento
                    _logger.LogWarning("Erro ao processar linha {RowNumber} do CSV de Clients | Exception: {ExceptionDetails}", rowCount + 2, LogValueSanitizer.ExceptionDetails(ex));
                    _notify.Add(_localization.GetMessage("Application.Service.Client.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount > maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.Client.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao ler arquivo CSV de Clients: {Message} | Exception: {ExceptionDetails}", LogValueSanitizer.Sanitize(ex.Message), LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Client.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadClientItem> items, CancellationToken ct)
    {
        var entities = new List<ClientEntity>(items.Count);

        foreach (var item in items)
        {
            // Valida campos obrigatorios
            if (!ValidateBulkItem(item))
            {
                return false;
            }

            if (!await ValidateDependenciesAsync(item.PartyTypeId, item.AcquisitionSourceTypeId, item.StatusDomainId, item.StatusDefinitionId, ct))
                return false;

            // Cria a entidade com campos minimos do CSV
            var entity = new ClientEntity(
                _currentUser.GetTenantId(),
                item.PartyTypeId,
                item.AcquisitionSourceTypeId,
                item.UrlImage,
                item.Note,
                item.Name!,
                item.PhoneNumber,
                null, // CellPhoneNumber
                false, // IsCellPhoneWhatsapp
                item.Email,
                null, // WebsiteUrl
                null, // BirthDate
                null, // Gender
                null, // Nationality
                null, // CompanyRegistrationNumber
                null, // EconomicActivityCode
                null, // NumberOfEmployees
                item.StatusDefinitionId,
                item.StatusDomainId,
                _currentUser.GetUserId());

            entities.Add(entity);
        }

        return await _domain.CreateManyAsync(entities, ct);
    }

    private bool ValidateBulkItem(BulkUploadClientItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Client.ValidateBulkItem.Name"), 400);
            return false;
        }

        if (item.PartyTypeId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Client.ValidateBulkItem.PartyTypeId"), 400);
            return false;
        }

        if (item.AcquisitionSourceTypeId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Client.ValidateBulkItem.AcquisitionSourceTypeId"), 400);
            return false;
        }

        return true;
    }
}
