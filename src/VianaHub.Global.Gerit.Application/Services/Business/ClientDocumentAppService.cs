using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.ClientDocument;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.ClientDocument;
using VianaHub.Global.Gerit.Application.Interfaces.Business;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Mappings.Business;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Business;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Business;

public class ClientDocumentAppService : IClientDocumentAppService
{
    private readonly IClientDocumentDataRepository _repo;
    private readonly IClientRepository _clients;
    private readonly IDocumentTypeDataRepository _documentTypes;
    private readonly IClientDocumentDomainService _domain;
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileValidationService _fileValidation;

    public ClientDocumentAppService(IClientDocumentDataRepository repo, IClientRepository clients, IDocumentTypeDataRepository documentTypes,
        IClientDocumentDomainService domain, INotify notify, ILocalizationService localization, ICurrentUserService currentUser, IFileValidationService fileValidation)
    {
        _repo = repo; _clients = clients; _documentTypes = documentTypes; _domain = domain; _notify = notify; _localization = localization; _currentUser = currentUser; _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<ClientDocumentResponse>> GetAllAsync(int clientId, CancellationToken ct) =>
        (await _repo.GetAllAsync(_currentUser.GetTenantId(), clientId, ct)).Select(ClientDocumentMapper.ToResponse);

    public async Task<ClientDocumentDetailResponse?> GetByIdAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(_currentUser.GetTenantId(), clientId, id, ct);
        if (entity == null || entity.IsDeleted)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.ResourceNotFound"), 410);
            return null;
        }
        return ClientDocumentMapper.ToDetail(entity);
    }

    public async Task<ListPageResponse<ClientDocumentResponse>> GetPagedAsync(int clientId, PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        return ClientDocumentMapper.ToPaged(await _repo.GetPagedAsync(_currentUser.GetTenantId(), clientId, filter, ct));
    }

    public async Task<int> CreateAsync(int clientId, CreateClientDocumentRequest request, CancellationToken ct)
    {
        if (!await EnsureActiveClientAsync(clientId, "Create", ct) || !await EnsureActiveDocumentTypeAsync(request.DocumentTypeId, "Create", ct)) return 0;
        var tenantId = _currentUser.GetTenantId();
        var country = request.IssuingCountryCode.Trim().ToUpperInvariant();
        var number = request.DocumentNumber.Trim();
        if (await _repo.ExistsDuplicateAsync(tenantId, request.DocumentTypeId, country, number, null, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.DuplicateDocument"), 409);
            return 0;
        }
        var entity = new ClientDocumentsEntity(tenantId, clientId, request.DocumentTypeId, number, country, request.IssuedAt, request.ExpiresAt, request.IsPrimary, _currentUser.GetUserId());
        return await _domain.CreateAsync(entity, ct) ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int clientId, int id, UpdateClientDocumentRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(_currentUser.GetTenantId(), clientId, id, ct);
        if (entity == null || entity.IsDeleted) return NotFound("Update");
        if (!await EnsureActiveClientAsync(clientId, "Update", ct) || !await EnsureActiveDocumentTypeAsync(request.DocumentTypeId, "Update", ct)) return false;
        var country = request.IssuingCountryCode.Trim().ToUpperInvariant();
        var number = request.DocumentNumber.Trim();
        if (await _repo.ExistsDuplicateAsync(entity.TenantId, request.DocumentTypeId, country, number, id, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.DuplicateDocument"), 409);
            return false;
        }
        entity.Update(number, country, request.IssuedAt, request.ExpiresAt, request.IsPrimary, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int clientId, int id, CancellationToken ct) => await ChangeStateAsync(clientId, id, true, ct);
    public async Task<bool> DeactivateAsync(int clientId, int id, CancellationToken ct) => await ChangeStateAsync(clientId, id, false, ct);

    public async Task<bool> DeleteAsync(int clientId, int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(_currentUser.GetTenantId(), clientId, id, ct);
        if (entity == null || entity.IsDeleted) return NotFound("Delete");
        if (!await EnsureActiveClientAsync(clientId, "Delete", ct)) return false;
        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(int clientId, IFormFile? file, CancellationToken ct)
    {
        if (!await EnsureActiveClientAsync(clientId, "BulkUpload", ct) || !_fileValidation.ValidateFile(file)) return false;
        var items = await ReadCsvFileAsync(file!, ct);
        if (items is null || items.Count == 0)
        {
            if (items is not null) _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.EmptyFile"), 400);
            return false;
        }
        if (items.Count(x => x.IsPrimary) > 1)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.MultiplePrimary"), 400);
            return false;
        }

        var tenantId = _currentUser.GetTenantId();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entities = new List<ClientDocumentsEntity>(items.Count);
        foreach (var item in items)
        {
            var country = item.IssuingCountryCode.Trim().ToUpperInvariant();
            var number = item.DocumentNumber.Trim();
            var key = $"{item.DocumentTypeId}|{country}|{number}";
            if (!seen.Add(key))
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.DuplicateInFile", item.LineNumber), 409);
                return false;
            }
            if (!await EnsureActiveDocumentTypeAsync(item.DocumentTypeId, "BulkUpload", ct)) return false;
            if (await _repo.ExistsDuplicateAsync(tenantId, item.DocumentTypeId, country, number, null, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.DuplicateExisting", item.LineNumber), 409);
                return false;
            }
            entities.Add(new ClientDocumentsEntity(tenantId, clientId, item.DocumentTypeId, number, country, item.IssuedAt, item.ExpiresAt, item.IsPrimary, _currentUser.GetUserId()));
        }
        return await _domain.CreateManyAsync(entities, ct);
    }

    private async Task<List<BulkUploadClientDocumentItem>?> ReadCsvFileAsync(IFormFile file, CancellationToken ct)
    {
        try
        {
            using var reader = file.OpenReadStream().CreateUtf8StreamReader();
            var content = await reader.ReadToEndAsync(ct);
            if (string.IsNullOrWhiteSpace(content)) return [];
            var firstLine = content.Split(["\r\n", "\n"], StringSplitOptions.None).FirstOrDefault() ?? string.Empty;
            var delimiter = firstLine.Contains(';') ? ";" : ",";
            using var csv = new CsvReader(new StringReader(content), new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = delimiter,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            });
            if (!await csv.ReadAsync() || !csv.ReadHeader() || csv.HeaderRecord is null)
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.MissingHeader"), 400);
                return null;
            }
            var headers = csv.HeaderRecord.Select((value, index) => new { Name = value.Trim(), Index = index })
                .ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);
            var required = new[] { "DocumentTypeId", "DocumentNumber", "IssuingCountryCode", "IssuedAt", "ExpiresAt", "IsPrimary" };
            if (required.Any(x => !headers.ContainsKey(x)))
            {
                _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.MissingHeader"), 400);
                return null;
            }
            var items = new List<BulkUploadClientDocumentItem>();
            var maxRows = DomainExtensions.GetMaxCsvRows();
            while (await csv.ReadAsync())
            {
                var line = csv.Context.Parser.Row;
                if (Enumerable.Range(0, csv.Parser.Count).All(index => string.IsNullOrWhiteSpace(csv.GetField(index))))
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.InvalidCsv", line), 400);
                    return null;
                }
                if (items.Count >= maxRows)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.MaxRows", maxRows), 400);
                    return null;
                }
                var typeText = csv.GetField(headers["DocumentTypeId"])?.Trim();
                var number = csv.GetField(headers["DocumentNumber"])?.SanitizeCsvInput() ?? string.Empty;
                var country = csv.GetField(headers["IssuingCountryCode"])?.SanitizeCsvInput() ?? string.Empty;
                if (!int.TryParse(typeText, NumberStyles.None, CultureInfo.InvariantCulture, out var typeId) || typeId <= 0)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.InvalidDocumentTypeId", line), 400);
                    return null;
                }
                if (string.IsNullOrWhiteSpace(number) || number.Length > 100 || !number.IsSafeCsvValue())
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.InvalidDocumentNumber", line), 400);
                    return null;
                }
                if (country.Length != 2 || !country.IsSafeCsvValue())
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.InvalidCountry", line), 400);
                    return null;
                }
                var issuedText = csv.GetField(headers["IssuedAt"])?.Trim();
                var expiresText = csv.GetField(headers["ExpiresAt"])?.Trim();
                if (!TryParseDate(issuedText, out var issuedAt) || !TryParseDate(expiresText, out var expiresAt))
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.InvalidDate", line), 400);
                    return null;
                }
                var primaryText = csv.GetField(headers["IsPrimary"])?.Trim();
                if (!bool.TryParse(primaryText, out var isPrimary))
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.InvalidBoolean", line), 400);
                    return null;
                }
                if (issuedAt.HasValue && expiresAt.HasValue && expiresAt < issuedAt)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.ExpiryBeforeIssue", line), 400);
                    return null;
                }
                items.Add(new BulkUploadClientDocumentItem { DocumentTypeId = typeId, DocumentNumber = number, IssuingCountryCode = country, IssuedAt = issuedAt, ExpiresAt = expiresAt, IsPrimary = isPrimary, LineNumber = line });
            }
            return items;
        }
        catch (CsvHelperException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.InvalidCsv"), 400);
            return null;
        }
        catch (IOException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.ClientDocument.BulkUpload.InvalidCsv"), 400);
            return null;
        }
    }

    private static bool TryParseDate(string? value, out DateOnly? date)
    {
        if (string.IsNullOrWhiteSpace(value)) { date = null; return true; }
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) { date = parsed; return true; }
        date = null; return false;
    }

    private async Task<bool> ChangeStateAsync(int clientId, int id, bool activate, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(_currentUser.GetTenantId(), clientId, id, ct);
        if (entity == null || entity.IsDeleted) return NotFound(activate ? "Activate" : "Deactivate");
        if (!await EnsureActiveClientAsync(clientId, activate ? "Activate" : "Deactivate", ct)) return false;
        if (activate) entity.Activate(_currentUser.GetUserId()); else entity.Deactivate(_currentUser.GetUserId());
        return activate ? await _domain.ActivateAsync(entity, ct) : await _domain.DeactivateAsync(entity, ct);
    }

    private async Task<bool> EnsureActiveClientAsync(int clientId, string operation, CancellationToken ct)
    {
        var client = await _clients.GetByIdAsync(clientId, ct);
        if (client != null && client.IsActive && !client.IsDeleted) return true;
        _notify.Add(_localization.GetMessage($"Application.Service.ClientDocument.{operation}.ClientNotFound"), 410);
        return false;
    }

    private async Task<bool> EnsureActiveDocumentTypeAsync(int id, string operation, CancellationToken ct)
    {
        var type = id > 0 ? await _documentTypes.GetByIdAsync(id, ct) : null;
        if (type == null || type.IsDeleted)
        {
            _notify.Add(_localization.GetMessage($"Application.Service.ClientDocument.{operation}.DocumentTypeNotFound"), 410);
            return false;
        }
        if (!type.IsActive)
        {
            _notify.Add(_localization.GetMessage($"Application.Service.ClientDocument.{operation}.DocumentTypeInactive"), 409);
            return false;
        }
        return true;
    }

    private bool NotFound(string operation)
    {
        _notify.Add(_localization.GetMessage($"Application.Service.ClientDocument.{operation}.ResourceNotFound"), 410);
        return false;
    }
}
