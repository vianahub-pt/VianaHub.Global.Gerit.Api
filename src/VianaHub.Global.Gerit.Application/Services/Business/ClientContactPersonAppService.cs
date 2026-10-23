using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientContact;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientContact;
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
/// Servi�o de aplica��o para ClientContact
/// </summary>
public class ClientContactPersonAppService : IClientContactPersonAppService
{
    public int TenantId { get; set; }
    public int UserId { get; set; }
    private readonly IClientContactPersonDataRepository _repo;
    private readonly IClientContactPersonDomainService _domain;
    private readonly IMapper _mapper;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;
    private readonly ILogger<ClientContactPersonAppService> _logger;

    public ClientContactPersonAppService(IClientContactPersonDataRepository repo, IClientContactPersonDomainService domain, IMapper mapper, INotify notify, ILocalizationService localization, ICurrentUserService currentUser, IFileValidationService fileValidation, ILogger<ClientContactPersonAppService> logger)
    {
        _repo = repo;
        _domain = domain;
        _mapper = mapper;
        _notify = notify;
        _localization = localization;
        _currentUser = currentUser;
        _fileValidation = fileValidation;
        _logger = logger;
        TenantId = _currentUser.GetTenantId();
        UserId = _currentUser.GetUserId();
    }

    public async Task<IEnumerable<ClientContactPersonResponse>> GetAllAsync(int clientId, CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(clientId, ct);
        return _mapper.Map<IEnumerable<ClientContactPersonResponse>>(entities);
    }

    public async Task<ClientContactPersonDetailResponse> GetByIdAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.GetById.ResourceNotFound"), 410);
            return null;
        }
        return _mapper.Map<ClientContactPersonDetailResponse>(entity);
    }

    public async Task<ListPageResponse<ClientContactPersonResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(clientId, filter, ct);
        return _mapper.Map<ListPageResponse<ClientContactPersonResponse>>(paged);
    }

    public async Task<int> CreateAsync(int clientId, CreateClientContactRequest request, CancellationToken ct)
    {
        var exists = await _repo.ExistsByClientAndEmailAsync(clientId, request.Name, request.Email, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var clientContact = new ClientContactPersonsEntity(
            TenantId,
            clientId,
            request.Name,
            request.PhoneNumber,
            request.CellPhoneNumber,
            request.IsWhatsapp,
            request.Email,
            request.IsPrimary,
            UserId,
            request.JobTitle,
            request.Department,
            request.IsCellPhoneWhatsapp
        );

        var success = await _domain.CreateAsync(clientContact, ct);
        return success ? clientContact.Id : 0;
    }

    public async Task<bool> UpdateAsync(int clientId, int id, UpdateClientContactPersonRequest request, CancellationToken ct)
    {
        var clientContact = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientContact == null || clientContact.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.Update.ResourceNotFound"), 410);
            return false;
        }

        clientContact.Update(request.Name, request.PhoneNumber, request.CellPhoneNumber, request.IsWhatsapp, request.Email, request.IsPrimary, UserId, request.JobTitle, request.Department, request.IsCellPhoneWhatsapp);

        return await _repo.UpdateAsync(clientContact, ct);
    }

    public async Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct)
    {
        var clientContact = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientContact == null || clientContact.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.Activate.ResourceNotFound"), 410);
            return false;
        }

        clientContact.Activate(UserId);

        return await _repo.UpdateAsync(clientContact, ct);
    }

    public async Task<bool> DeactivateAsync(int clientId, int id, CancellationToken ct)
    {
        var clientContact = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientContact == null || clientContact.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.Deactivate.ResourceNotFound"), 410);
            return false;
        }

        clientContact.Deactivate(UserId);

        return await _repo.UpdateAsync(clientContact, ct);
    }

    public async Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct)
    {
        var clientContact = await _repo.GetByIdAsync(clientId, id, ct);
        if (clientContact == null || clientContact.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.Delete.ResourceNotFound"), 410);
            return false;
        }

        clientContact.Delete(UserId);

        return await _repo.UpdateAsync(clientContact, ct);
    }

    public async Task<bool> BulkUploadAsync(int clientId, IFormFile file, CancellationToken ct)
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
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(clientId, items, ct);
    }

    private List<BulkUploadClientContactPersonItem> ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadClientContactPersonItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadClientContactPersonItem>();
                    if (record != null)
                    {
                        // Sanitiza e normaliza campos
                        record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8();
                        record.Email = record.Email?.SanitizeCsvInput().NormalizeUtf8();
                        record.Phone = record.Phone?.SanitizeCsvInput().NormalizeUtf8();

                        // Valida se os campos n�o cont�m conte�do perigoso
                        if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Email) && !record.Email.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ReadCsvFile.Email.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Phone) && !record.Phone.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ReadCsvFile.Phone.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    // Log linha com erro mas continua processamento
                    _logger.LogWarning(ex, "Erro ao processar linha {RowNumber} do CSV de ClientContactPersons", rowCount + 2);
                    _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao ler arquivo CSV de ClientContactPersons: {Message}", ex.Message);
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(int clientId, List<BulkUploadClientContactPersonItem> items, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var userId = _currentUser.GetUserId();
        var hasErrors = false;

        foreach (var item in items)
        {
            // Valida��o b�sica
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            // Verificar se j� existe
            var exists = await _repo.ExistsByClientAndEmailAsync(clientId, item.Email, null, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ProcessBulkItems.ExistsByClientAndEmail", item.Email, clientId), 400);
                hasErrors = true;
                continue;
            }

            //var entity = new ClientContactPersonsEntity(
            //    tenantId,
            //    clientId,
            //    item.Name,
            //    item.Email,
            //    item.Phone,
            //    item.IsPrimary,
            //    userId
            //);

            //var created = await _domain.CreateAsync(entity, ct);
            //if (!created)
            //{
            //    _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ProcessBulkItems.FailedToCreate", item.Name), 400);
            //    hasErrors = true;
            //}
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadClientContactPersonItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ValidateBulkItem.Name"), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Email))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientContactPerson.ValidateBulkItem.Email"), 400);
            return false;
        }

        return true;
    }
}

