using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.User;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.ReadModels;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Identity;

public class UserAppService : IUserAppService
{
    private readonly IUserDataRepository _repo;
    private readonly IUserDomainService _domain;
    private readonly ICurrentUserService _currentUser;
    private readonly INotify _notify;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localization;
    private readonly IFileValidationService _fileValidation;
    private readonly IRequestTenantContext _requestTenantContext;

    public UserAppService(
        IUserDataRepository repo,
        IUserDomainService domain,
        INotify notify,
        IMapper mapper,
        ICurrentUserService currentUser,
        ILocalizationService localization,
        IFileValidationService fileValidation,
        IRequestTenantContext requestTenantContext)
    {
        _repo = repo;
        _domain = domain;
        _notify = notify;
        _mapper = mapper;
        _currentUser = currentUser;
        _localization = localization;
        _fileValidation = fileValidation;
        _requestTenantContext = requestTenantContext;
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<UserResponse>>(entities);
    }

    public async Task<UserDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        return _mapper.Map<UserDetailResponse>(entity);
    }

    public async Task<ListPageResponse<UserResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return _mapper.Map<ListPageResponse<UserResponse>>(paged);
    }

    public async Task<int> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var exists = await _repo.ExistsByEmailAsync(request.Email, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.Create.ResourceAlreadyExists"), 400);
            return 0;
        }

        var passwordHash = DomainExtensions.HashClientSecret(request.Password);
        var tenantId = _currentUser.GetTenantId();
        var entity = new UserEntity(tenantId, request.Name, request.Email, passwordHash, null, _currentUser.GetUserId());

        var success = await _domain.CreateAsync(entity, ct);
        return success ? entity.Id : 0;
    }

    public async Task<bool> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.Update.ResourceNotFound"), 410);
            return false;
        }

        entity.Update(request.Name, request.PhoneNumber, _currentUser.GetUserId());
        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> UpdatePasswordAsync(int id, UpdatePasswordRequest request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.UpdatePassword.ResourceNotFound"), 410);
            return false;
        }

        // Valida a senha atual usando PBKDF2
        if (!DomainExtensions.VerifyClientSecret(entity.PasswordHash, request.CurrentPassword))
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.UpdatePassword.InvalidCurrentPassword"), 400);
            return false;
        }

        // A valida��o de senha forte j� � feita no validador de rota (UpdatePasswordRouteValidator)
        var newPasswordHash = DomainExtensions.HashClientSecret(request.NewPassword);
        entity.UpdatePassword(newPasswordHash, _currentUser.GetUserId());

        return await _domain.UpdateAsync(entity, ct);
    }

    public async Task<bool> ActivateAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.Activate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.User.Deactivate.ResourceNotFound"), 410);
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
            _notify.Add(_localization.GetMessage("Application.Service.User.Delete.ResourceNotFound"), 410);
            return false;
        }

        entity.Delete(_currentUser.GetUserId());
        return await _domain.DeleteAsync(entity, ct);
    }

    public async Task<bool> BulkUploadAsync(IFormFile file, CancellationToken ct)
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
            _notify.Add(_localization.GetMessage("Application.Service.User.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadUserItem> ReadCsvFile(IFormFile file)
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
            var records = new List<BulkUploadUserItem>();

            csv.Read();
            csv.ReadHeader();

            int rowCount = 0;
            int maxRows = DomainExtensions.GetMaxCsvRows();

            while (csv.Read() && rowCount < maxRows)
            {
                try
                {
                    var record = csv.GetRecord<BulkUploadUserItem>();
                    if (record != null)
                    {
                        // Sanitiza e normaliza campos
                        record.Name = record.Name?.SanitizeCsvInput().NormalizeUtf8();
                        record.Email = record.Email?.SanitizeCsvInput().NormalizeUtf8();
                        record.Password = record.Password?.SanitizeCsvInput().NormalizeUtf8();
                        record.PhoneNumber = record.PhoneNumber?.SanitizeCsvInput().NormalizeUtf8();

                        // Valida se os campos n�o cont�m conte�do perigoso
                        if (!string.IsNullOrEmpty(record.Name) && !record.Name.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.User.ReadCsvFile.Name.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        if (!string.IsNullOrEmpty(record.Email) && !record.Email.IsSafeCsvValue())
                        {
                            _notify.Add(_localization.GetMessage("Application.Service.User.ReadCsvFile.Email.IsSafeCsvValue", rowCount + 2), 400);
                            continue;
                        }

                        records.Add(record);
                    }
                    rowCount++;
                }
                catch (CsvHelperException ex)
                {
                    // Log linha com erro mas continua processamento
                    _notify.Add(_localization.GetMessage("Application.Service.User.ReadCsvFile.CsvHelperException", rowCount + 2), 400);
                    rowCount++;
                    continue;
                }
            }

            if (rowCount >= maxRows)
            {
                _notify.Add(_localization.GetMessage("Application.Service.User.ReadCsvFile.MaxRows", maxRows), 400);
                return null;
            }

            return records;
        }
        catch (Exception ex)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadUserItem> items, CancellationToken ct)
    {
        var hasErrors = false;
        var tenantId = _currentUser.GetTenantId();
        _requestTenantContext.SetTenantId(tenantId);

        foreach (var item in items)
        {
            // Valida campos obrigat�rios
            if (!ValidateBulkItem(item))
            {
                hasErrors = true;
                continue;
            }

            // Verifica duplicidade
            var exists = await _repo.ExistsByEmailAsync(item.Email, ct);
            if (exists)
            {
                _notify.Add(_localization.GetMessage("Application.Service.User.ProcessBulkItems.ExistsByEmail", item.Email), 400);
                hasErrors = true;
                continue;
            }

            // Cria a entidade usando hash PBKDF2
            var passwordHash = DomainExtensions.HashClientSecret(item.Password);
            var entity = new UserEntity(tenantId, item.Name, item.Email, passwordHash, item.PhoneNumber, _currentUser.GetUserId());

            // Tenta criar no dom�nio
            var success = await _domain.CreateAsync(entity, ct);

            if (!success)
            {
                _notify.Add(_localization.GetMessage("Application.Service.User.ProcessBulkItems.FailedToCreate", item.Email), 400);
                hasErrors = true;
            }
        }

        return !hasErrors;
    }

    private bool ValidateBulkItem(BulkUploadUserItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.ValidateBulkItem.Name", item.Email ?? ""), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Email))
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.ValidateBulkItem.Email", item.Email ?? ""), 400);
            return false;
        }

        if (string.IsNullOrWhiteSpace(item.Password))
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.ValidateBulkItem.Password", item.Email), 400);
            return false;
        }

        // A valida��o de senha forte deveria ser feita aqui, mas como bulk upload
        // n�o passa pelos validadores de rota, precisamos validar manualmente
        // Por ora, apenas validamos se a senha n�o est� vazia
        // TODO: Considerar adicionar valida��o de senha forte aqui ou criar um validador compartilhado

        return true;
    }
}
