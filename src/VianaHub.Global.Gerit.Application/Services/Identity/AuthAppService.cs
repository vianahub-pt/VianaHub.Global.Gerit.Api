using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using VianaHub.Global.Gerit.Application.Configuration;
using VianaHub.Global.Gerit.Application.Dtos.Request.Identity.Auth;
using VianaHub.Global.Gerit.Application.Dtos.Response.Billing.Tenant;
using VianaHub.Global.Gerit.Application.Dtos.Response.Identity.Auth;
using VianaHub.Global.Gerit.Application.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Entities.Billing;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Cryptography;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;
using VianaHub.Global.Gerit.Infra.Data.Context;

namespace VianaHub.Global.Gerit.Application.Services.Identity;

public class AuthAppService : IAuthAppService
{
    private readonly INotify _notify;
    private readonly IMapper _mapper;
    private readonly IRolePermissionDataRepository _rolePermissionRepo;
    private readonly IRefreshTokenDataRepository _refreshRepo;
    private readonly IUserDataRepository _userRepo;
    private readonly IUserRoleDataRepository _userRoleRepo;
    private readonly ITenantDataRepository _tenantRepo;
    private readonly IEmailSender _emailSender;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthAppService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IJwtKeyDataRepository _jwtKeyRepo;
    private readonly GeritDbContext _dbContext;
    private readonly ISecretProvider _secretProvider;
    private readonly ISubscriptionDomainService _subscriptionDomain;
    private readonly IRequestTenantContext _requestTenantContext;

    public AuthAppService(
        INotify notify,
        IMapper mapper,
        IRolePermissionDataRepository rolePermissionRepo,
        IRefreshTokenDataRepository refreshRepo,
        IUserDataRepository userRepo,
        IUserRoleDataRepository userRoleRepo,
        ITenantDataRepository tenantRepo,
        IEmailSender emailSender,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        IOptions<JwtSettings> jwtOptions,
        ILogger<AuthAppService> logger,
        IConfiguration configuration,
        IJwtKeyDataRepository jwtKeyRepo,
        GeritDbContext dbContext,
        ISecretProvider secretProvider,
        ISubscriptionDomainService subscriptionDomain,
        IRequestTenantContext requestTenantContext)
    {
        _notify = notify;
        _mapper = mapper;
        _rolePermissionRepo = rolePermissionRepo;
        _refreshRepo = refreshRepo;
        _userRepo = userRepo;
        _userRoleRepo = userRoleRepo;
        _tenantRepo = tenantRepo;
        _emailSender = emailSender;
        _localization = localization;
        _currentUser = currentUser;
        _jwtSettings = jwtOptions.Value;
        _logger = logger;
        _configuration = configuration;
        _jwtKeyRepo = jwtKeyRepo;
        _dbContext = dbContext;
        _secretProvider = secretProvider;
        _subscriptionDomain = subscriptionDomain;
        _requestTenantContext = requestTenantContext;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        // Validar tenantId
        if (request.TenantId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Register.InvalidTenantId"), 400);
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Register.EmailRequired"), 400);
            return null;
        }

        // Propaga o TenantId para o interceptor ANTES de qualquer query
        _requestTenantContext.SetTenantId(request.TenantId);

        // Verificar se email j� existe (no tenant informado)
        var exists = await _userRepo.ExistsByEmailAsync(request.Email, ct);
        if (exists)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Register.EmailAlreadyExists"), 409);
            return null;
        }

        // Hash da senha
        var passwordHash = DomainExtensions.HashClientSecret(request.Password);

        // Criar entidade de usu�rio
        var user = new UserEntity(request.TenantId, request.Name, request.Email, passwordHash, request.PhoneNumber, 0);

        // Persistir via reposit�rio
        var created = await _userRepo.CreateAsync(user, ct);
        if (!created)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Register.FailedToCreateUser"), 500);
            return null;
        }

        // Enviar email de confirma��o (NoOp por enquanto)
        await _emailSender.SendAsync(request.Email, "Application.Service.Auth.Email.Confirm.Subject", "Application.Service.Auth.Email.Confirm.Body", user.Name);

        _requestTenantContext.Clear();

        // Retornar sem tokens (login separado)
        return new AuthResponse
        {
            RoleId = user.UserRoles.FirstOrDefault()?.RoleId ?? 0,
            RoleName = user.UserRoles.FirstOrDefault()?.Role?.Name,
            TenantId = user.TenantId,
            TenantName = user.Tenant?.Name,
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByUserEmailAsync(request.Email, ct);
        if (tenant == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.UserNotAssociateInTenant"), 401);
            return null;
        }

        _requestTenantContext.SetTenantId(tenant.Id);

        var user = await _userRepo.GetByEmailAsync(request.Email, ct);
        if (user == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Login.InvalidCredentials"), 401);
            return null;
        }

        if (!DomainExtensions.VerifyClientSecret(user.PasswordHash, request.Password))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Login.InvalidCredentials"), 401);
            return null;
        }

        if (!await _subscriptionDomain.IsActiveAsync(tenant.Id, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Login.Subscription.NonActive"), 403);
            return null;
        }
        if (await _subscriptionDomain.IsCanceledAsync(tenant.Id, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Login.Subscription.Canceled"), 403);
            return null;
        }
        if (await _subscriptionDomain.IsDeletedAsync(tenant.Id, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Login.Subscription.Deleted"), 403);
            return null;
        }

        var isTrial = await _subscriptionDomain.IsTrialAsync(tenant.Id, ct);
        if (isTrial)
        {
            if (await _subscriptionDomain.IsTrialPeriodExpiredAsync(tenant.Id, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Service.Auth.Login.Subscription.TrialPeriodExpired"), 403);
                return null;
            }
        }
        else if (await _subscriptionDomain.IsSubscriptionPeriodExpiredAsync(tenant.Id, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Login.Subscription.PeriodExpired"), 403);
            return null;
        }

        // Gera tokens (usa chave RSA do tenant)
        var accessToken = await GenerateAccessTokenAsync(user, isTrial, ct);
        if (accessToken.Token == null)
        {
            // Erro j� notificado
            return null;
        }

        // Atualizar LastAccessAt do usu�rio (n�o bloquear login em caso de falha)
        try
        {
            user.UpdateLastAccess();
            await _userRepo.UpdateAsync(user, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Falha ao atualizar LastAccessAt para o usu�rio {UserId} | Exception: {ExceptionDetails}", user.Id, LogValueSanitizer.ExceptionDetails(ex));
        }

        var refreshTokenValue = GenerateRefreshTokenValue();

        var refreshTokenEntity = new RefreshTokensEntity(user.TenantId, user.Id, refreshTokenValue, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays), user.Id);
        await _refreshRepo.AddAsync(refreshTokenEntity);

        _requestTenantContext.Clear();

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            RefreshToken = refreshTokenValue,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshTokenExpiresAt = refreshTokenEntity.ExpiresAt,
            RoleId = user.UserRoles.FirstOrDefault()?.RoleId ?? 0,
            RoleName = user.UserRoles.FirstOrDefault()?.Role?.Name,
            TenantId = user.TenantId,
            TenantName = user.Tenant?.Name,
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email
        };
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        if (request.TenantId <= 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Refresh.InvalidTenantId"), 400);
            return null;
        }

        // Propaga o TenantId para o interceptor ANTES de qualquer query
        _requestTenantContext.SetTenantId(request.TenantId);

        var tokenEntity = await _refreshRepo.GetByTokenAsync(request.RefreshToken, request.TenantId);
        if (tokenEntity == null || !tokenEntity.IsActive())
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Refresh.InvalidRefreshToken"), 401);
            return null;
        }

        // Encontrar usu�rio
        var user = await _userRepo.GetByIdAsync(tokenEntity.UserId, ct);
        if (user == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Refresh.UserNotFound"), 410);
            return null;
        }

        if (!await _subscriptionDomain.IsActiveAsync(request.TenantId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.RefreshToken.Subscription.NonActive"), 403);
            return null;
        }
        if (await _subscriptionDomain.IsCanceledAsync(request.TenantId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.RefreshToken.Subscription.Canceled"), 403);
            return null;
        }
        if (await _subscriptionDomain.IsDeletedAsync(request.TenantId, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.RefreshToken.Subscription.Deleted"), 403);
            return null;
        }

        var isTrial = await _subscriptionDomain.IsTrialAsync(user.Id, ct);
        if (isTrial)
        {
            if (await _subscriptionDomain.IsTrialPeriodExpiredAsync(user.Id, ct))
            {
                _notify.Add(_localization.GetMessage("Application.Service.Auth.RefreshToken.Subscription.TrialPeriodExpired"), 403);
                return null;
            }
        }
        else if (await _subscriptionDomain.IsSubscriptionPeriodExpiredAsync(user.Id, ct))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.RefreshToken.Subscription.PeriodExpired"), 403);
            return null;
        }

        // Rota��o: revogar token antigo e criar novo
        await _refreshRepo.RevokeAsync(tokenEntity.TokenHash, user.Id, request.TenantId);

        var newRefreshValue = GenerateRefreshTokenValue();
        var newRefresh = new RefreshTokensEntity(request.TenantId, user.Id, newRefreshValue, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays), user.Id);
        await _refreshRepo.AddAsync(newRefresh);

        var accessToken = await GenerateAccessTokenAsync(user, isTrial, ct);

        _requestTenantContext.Clear();

        return new AuthResponse
        {
            AccessToken = accessToken.Token,
            RefreshToken = newRefreshValue,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshTokenExpiresAt = newRefresh.ExpiresAt,
            RoleId = user.UserRoles.FirstOrDefault()?.RoleId ?? 0,
            RoleName = user.UserRoles.FirstOrDefault()?.Role?.Name,
            TenantId = user.TenantId,
            TenantName = user.Tenant?.Name,
            UserId = user.Id,
            UserName = user.Name,
            Email = user.Email
        };
    }


    public async Task<IEnumerable<TenantLoginResponse>> GetLoginAsync(CancellationToken ct)
    {
        var entities = await _tenantRepo.GetLoginAsync(ct);
        return _mapper.Map<IEnumerable<TenantLoginResponse>>(entities);
    }

    private async Task<(string? Token, DateTime ExpiresAt)> GenerateAccessTokenAsync(UserEntity user, bool isTrial, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        // Gera claims b�sicos
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (isTrial)
        {
            claims.Add(new Claim("tenant_subscription_type", "trial"));
        }

        string? permissionsJson = null;

        // Adicionar roles e permiss�es ao token para autoriza��o stateless
        try
        {
            // Buscar roles do usu�rio
            var userRoles = await _userRoleRepo.GetByUserAsync(user.TenantId, user.Id, ct);

            var roleNames = userRoles?
                .Where(r => r?.Role != null)
                .Select(r => r.Role?.Name?.Trim())
                .OfType<string>()
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.ToLower())
                .Distinct()
                .ToList() ?? new List<string>();

            foreach (var roleName in roleNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
                // adicionar claim alternativa 'role' para compatibilidade
                claims.Add(new Claim("role", roleName));
            }

            // Coletar permiss�es de cada role agrupadas por recurso
            var permissionsByResource = new Dictionary<string, HashSet<string>>();

            foreach (var role in userRoles ?? Enumerable.Empty<UserRolesEntity>())
            {
                if (role?.Role == null) continue;
                var roleId = role.Role.Id;
                var rolePerms = await _rolePermissionRepo.GetByRoleAsync(roleId, user.TenantId, ct);
                if (rolePerms == null) continue;

                foreach (var rp in rolePerms)
                {
                    var resource = rp.Resource?.Name?.Trim();
                    var action = rp.Action?.Name?.Trim();
                    if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(action)) continue;

                    var resourceKey = resource.ToLower();
                    var actionKey = action.ToLower();

                    if (!permissionsByResource.TryGetValue(resourceKey, out var actions))
                    {
                        actions = new HashSet<string>();
                        permissionsByResource[resourceKey] = actions;
                    }

                    actions.Add(actionKey);
                }
            }

            // Serializar o dicion�rio para JSON e guardar em vari�vel (n�o adicionar como claim string)
            if (permissionsByResource.Any())
            {
                // Converter HashSet<string> para List<string> para serializa��o previs�vel
                var serializable = permissionsByResource.ToDictionary(k => k.Key, x => x.Value.OrderBy(x => x).ToList());
                permissionsJson = JsonSerializer.Serialize(serializable);
                // N�o adicionar como Claim string para evitar escape no payload
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Falha ao incluir roles/permissions no token para user {UserId} | Exception: {ExceptionDetails}", user.Id, LogValueSanitizer.ExceptionDetails(ex));
            // N�o falhar a gera��o do token por causa das claims de permiss�o; conceder token b�sico
        }

        // Carregar chave ativa do tenant
        var keyEntity = await _jwtKeyRepo.GetActiveKeyAsync(user.TenantId, ct);
        if (keyEntity == null)
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Token.NoActiveKey"), 500);
            _logger.LogError("Nenhuma chave JWT ativa encontrada para tenant {TenantId}", user.TenantId);
            return (null, DateTime.MinValue);
        }

        // Obter chave mestra do provider de segredos
        var masterKey = _secretProvider.GetMasterKey();
        if (string.IsNullOrWhiteSpace(masterKey))
        {
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Token.EncryptionKeyMissing"), 500);
            _logger.LogError("Master key for JWT decryption not available");
            return (null, DateTime.MinValue);
        }

        string privatePem;
        try
        {
            privatePem = CryptoRSA.DecryptPrivateKey(keyEntity.PrivateKeyEncrypted, masterKey);
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao descriptografar chave privada para tenant {TenantId} | Exception: {ExceptionDetails}", user.TenantId, LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Token.InvalidPrivateKey"), 500);
            return (null, DateTime.MinValue);
        }

        // Import private key into RSA
        try
        {
            // N�o usar 'using' aqui: precisamos garantir que a inst�ncia RSA permane�a viva
            // durante a opera��o de assinatura (WriteToken). Alguns provedores internos
            // podem acessar o objeto RSA durante a escrita do token e provocar
            // ObjectDisposedException se ele for descartado antes.
            var rsa = RSA.Create();
            try
            {
                var privateKeyBytes = CryptoRSA.ExtractBytesFromPem(privatePem, "PRIVATE KEY");
                rsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);

                var rsaKey = new RsaSecurityKey(rsa) { KeyId = keyEntity.KeyId.ToString() };
                // Evitar cache de SignatureProviders que podem reter refer�ncia ao objeto RSA e
                // causar ObjectDisposedException em chamadas subsequentes. Garantir que cada
                // gera��o de token use um provider fresco.
                rsaKey.CryptoProviderFactory = new CryptoProviderFactory
                {
                    CacheSignatureProviders = false
                };
                var creds = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

                var token = new JwtSecurityToken(
                    issuer: _jwtSettings.Issuer,
                    audience: _jwtSettings.Audience,
                    claims: claims,
                    notBefore: now,
                    expires: expires,
                    signingCredentials: creds
                );

                // Se temos permissionsJson, inserir como objeto JSON no payload para evitar escaping
                if (!string.IsNullOrWhiteSpace(permissionsJson))
                {
                    try
                    {
                        var doc = JsonDocument.Parse(permissionsJson);
                        token.Payload["permissions"] = doc.RootElement.Clone();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Falha ao parsear permissionsJson antes de inserir no payload | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
                    }
                }

                var handler = new JwtSecurityTokenHandler();
                var tokenString = handler.WriteToken(token);
                return (tokenString, expires);
            }
            finally
            {
                // Garantir libera��o expl�cita ap�s a escrita do token
                try { rsa.Dispose(); } catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao criar token JWT para tenant {TenantId} | Exception: {ExceptionDetails}", user.TenantId, LogValueSanitizer.ExceptionDetails(ex));
            _notify.Add(_localization.GetMessage("Application.Service.Auth.Token.CreateFailed"), 500);
            return (null, DateTime.MinValue);
        }
    }

    private string GenerateRefreshTokenValue()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=');
    }
}
