using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Base;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.User;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Application.Mappings.Identity;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Exceptions;
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
    private readonly ILocalizationService _localization;
    private readonly IFileValidationService _fileValidation;

    public UserAppService(
        IUserDataRepository repo,
        IUserDomainService domain,
        INotify notify,
        ICurrentUserService currentUser,
        ILocalizationService localization,
        IFileValidationService fileValidation)
    {
        _repo = repo;
        _domain = domain;
        _notify = notify;
        _currentUser = currentUser;
        _localization = localization;
        _fileValidation = fileValidation;
    }

    public async Task<IEnumerable<UserResponse>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _repo.GetAllAsync(ct);
        return UserMapper.ToResponses(entities);
    }

    public async Task<UserDetailResponse> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null)
        {
            var deleted = await _repo.IsDeletedAsync(id, _currentUser.GetTenantId(), ct);
            _notify.Add(_localization.GetMessage("Application.Service.User.GetById.ResourceNotFound"), deleted ? 410 : 404);
            return null!;
        }

        return UserMapper.ToDetail(entity);
    }

    public async Task<ListPageResponse<UserResponse>> GetPagedAsync(PagedFilterRequest request, CancellationToken ct)
    {
        var filter = new PagedFilter(request.Search, request.IsActive, request.PageNumber, request.PageSize, request.SortBy, request.SortDirection);
        var paged = await _repo.GetPagedAsync(filter, ct);
        return UserMapper.ToPaged(paged);
    }

    public async Task<int> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var exists = await _repo.ExistsByEmailAsync(request.Email, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.Create.ResourceAlreadyExists"), 409);
            return 0;
        }

        var passwordHash = DomainExtensions.HashClientSecret(request.Password);
        var tenantId = _currentUser.GetTenantId();
        var entity = new UserEntity(tenantId, request.Name, request.Email, passwordHash, null, _currentUser.GetUserId());

        try
        {
            return await _domain.CreateAsync(entity, ct) ? entity.Id : 0;
        }
        catch (UserEmailConflictException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.Create.ResourceAlreadyExists"), 409);
            return 0;
        }
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
        var currentUserId = _currentUser.GetUserId();
        if (currentUserId <= 0 || id != currentUserId)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.UpdatePassword.OtherUser"), 403);
            return false;
        }

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
        entity.UpdatePassword(newPasswordHash, currentUserId);

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
            _notify.Add(_localization.GetMessage("Application.Service.User.BulkUpload.EmptyFile"), 400);
            return false;
        }

        // Processa cada item
        return await ProcessBulkItemsAsync(items, ct);
    }

    private List<BulkUploadUserItem>? ReadCsvFile(IFormFile file)
    {
        var maxRows = DomainExtensions.GetMaxCsvRows();
        try
        {
            using var reader = new StreamReader(file.OpenReadStream(), new System.Text.UTF8Encoding(true, true), false);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                DetectColumnCountChanges = true,
                IgnoreBlankLines = false,
                ExceptionMessagesContainRawData = false
            });

            if (!csv.Read())
                return [];

            csv.ReadHeader();
            string[] expected = ["Name", "Email", "Password", "PhoneNumber"];
            if (csv.HeaderRecord is not { } header || !header.SequenceEqual(expected))
            {
                _notify.Add(_localization.GetMessage("Application.Service.User.ReadCsvFile.Exception"), 400);
                return null;
            }

            var records = new List<BulkUploadUserItem>();
            while (csv.Read())
            {
                if (records.Count == maxRows)
                {
                    _notify.Add(_localization.GetMessage("Application.Service.User.ReadCsvFile.MaxRows", maxRows), 400);
                    return null;
                }

                var name = csv.GetField(0)?.Trim();
                var email = csv.GetField(1)?.Trim();
                var password = csv.GetField(2);
                var phone = csv.GetField(3)?.Trim();
                if (string.IsNullOrWhiteSpace(name) || !IsSafeCsvField(name) ||
                    string.IsNullOrWhiteSpace(email) || !IsSafeCsvField(email) ||
                    !IsSafeCsvPhone(phone) ||
                    !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email) ||
                    password is null || !System.Text.RegularExpressions.Regex.IsMatch(
                        password, @"\A(?=[\s\S]{8,100}\z)(?=[\s\S]*[A-Z])(?=[\s\S]*[a-z])(?=[\s\S]*[0-9])(?=[\s\S]*[^a-zA-Z0-9])"))
                {
                    _notify.Add(_localization.GetMessage("Application.Service.User.ReadCsvFile.CsvHelperException", csv.Parser.Row), 400);
                    return null;
                }

                records.Add(new BulkUploadUserItem { Name = name, Email = email, Password = password, PhoneNumber = phone });
            }

            return records;
        }
        catch (Exception exception) when (exception is CsvHelperException or System.Text.DecoderFallbackException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.ReadCsvFile.Exception"), 400);
            return null;
        }
    }

    private static bool IsSafeCsvField(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        (value.IsSafeCsvValue() && !value.Any(character =>
            char.IsControl(character) || char.GetUnicodeCategory(character) == UnicodeCategory.Format));

    private static bool IsSafeCsvPhone(string? value) =>
        value?.StartsWith('+') == true
            ? System.Text.RegularExpressions.Regex.IsMatch(value, @"\A\+[0-9][0-9 ().-]*\z")
            : IsSafeCsvField(value);

    private async Task<bool> ProcessBulkItemsAsync(List<BulkUploadUserItem> items, CancellationToken ct)
    {
        var tenantId = _currentUser.GetTenantId();
        var userId = _currentUser.GetUserId();
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (!emails.Add(item.Email!) || await _repo.ExistsByEmailAsync(item.Email, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Service.User.Create.ResourceAlreadyExists"), 409);
                return false;
            }
        }

        var entities = items.Select(item => new UserEntity(
            tenantId, item.Name, item.Email, DomainExtensions.HashClientSecret(item.Password), item.PhoneNumber, userId)).ToArray();
        try
        {
            return await _domain.CreateManyAsync(entities, ct);
        }
        catch (UserEmailConflictException)
        {
            _notify.Add(_localization.GetMessage("Application.Service.User.Create.ResourceAlreadyExists"), 409);
            return false;
        }
    }
}
