using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Domain.Base;
using VianaHub.Global.Gerit.Domain.Entities.Identity;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Billing;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Cryptography;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Domain.Services.Identity;

public class JwtKeyDomainService : IJwtKeyDomainService
{
    private readonly IJwtKeyDataRepository _repo;
    private readonly ITenantDataRepository _tenantRepo;
    private readonly INotify _notify;
    private readonly IEntityDomainValidator<JwtKeysEntity> _validator;
    private readonly ILocalizationService _localization;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<JwtKeyDomainService> _logger;
    private readonly ISecretProvider _secretProvider;

    public JwtKeyDomainService(
        IJwtKeyDataRepository repo,
        ITenantDataRepository tenantRepo,
        INotify notify,
        IEntityDomainValidator<JwtKeysEntity> validator,
        ILocalizationService localization,
        ICurrentUserService currentUser,
        ILogger<JwtKeyDomainService> logger,
        ISecretProvider secretProvider)
    {
        _repo = repo;
        _tenantRepo = tenantRepo;
        _notify = notify;
        _validator = validator;
        _localization = localization;
        _currentUser = currentUser;
        _logger = logger;
        _secretProvider = secretProvider;
    }

    public async Task<JwtKeysEntity?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _repo.GetByIdAsync(id, ct);
    }
    public async Task<JwtKeysEntity?> GetByKeyIdAsync(Guid keyId, CancellationToken ct)
    {
        return await _repo.GetByKeyIdAsync(keyId, ct);
    }
    public async Task<JwtKeysEntity?> GetActiveKeyAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.GetActiveKeyAsync(tenantId, ct);
    }
    public async Task<IEnumerable<JwtKeysEntity>> GetAllAsync(CancellationToken ct)
    {
        return await _repo.GetAllAsync(ct);
    }
    public async Task<IEnumerable<JwtKeysEntity>> GetByTenantAsync(int tenantId, CancellationToken ct)
    {
        return await _repo.GetByTenantAsync(tenantId, ct);
    }

    public async Task<JwtKeysEntity?> CreateAsync(JwtKeysEntity entity, CancellationToken ct)
    {
        // Validar tenant
        if (!await ValidateTenantAsync(entity.TenantId, ct))
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.TenantNotFound"), 400);
            return null;
        }

        // Validar a entidade
        var validation = await _validator.ValidateForCreateAsync(entity);
        if (!validation.IsValid)
        {
            _logger.LogWarning("?? [CreateAsync] Valida��o de JwtKey falhou. Errors={Errors}",
                string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

            foreach (var error in validation.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return null;
        }

        // Verificar se j� existe uma chave ativa para esta combina��o Tenant + Application
        var hasActiveKey = await _repo.HasActiveKeyAsync(entity.TenantId, ct);
        if (hasActiveKey)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.ActiveKeyAlreadyExists"), 409);
            return null;
        }

        var created = await _repo.AddAsync(entity, ct);
        if (!created)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.CreateFailed"), 400);
            _logger.LogError("? [CreateAsync] Falha ao persistir JwtKey. TenantId={TenantId}", entity.TenantId);
            if (_notify.HasNotify())
            {
                _logger.LogDebug("? [CreateAsync] Notifica��es acumuladas: {Notifications}", string.Join("; ", _notify.GetErrorMessage()));
            }
            return null;
        }

        _logger.LogInformation("? [CreateAsync] JwtKey criada com sucesso. Id={Id}, KeyId={KeyId}", entity.Id, entity.KeyId);

        return entity;
    }
    public async Task<bool> ActivateAsync(JwtKeysEntity key, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(key.Id, ct);
        if (existing == null)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.NotFound"), 404);
            return false;
        }

        var validation = await _validator.ValidateForActivateAsync(existing);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }

        // Verificar se j� existe uma chave ativa
        var activeKey = await _repo.GetActiveKeyAsync(existing.TenantId, ct);
        if (activeKey != null && activeKey.Id != existing.Id)
        {
            // Desativar a chave anterior
            activeKey.Deactivate(_currentUser.GetUserId());
            await _repo.UpdateAsync(activeKey, ct);

            _logger.LogInformation("?? [ActivateAsync] Chave anterior desativada. Id={OldKeyId}, KeyId={OldKeyKeyId}",
                activeKey.Id, activeKey.KeyId);
        }

        existing.Activate(_currentUser.GetUserId());
        var updated = await _repo.UpdateAsync(existing, ct);

        if (updated)
        {
            _logger.LogInformation("? [ActivateAsync] JwtKey ativada com sucesso. Id={Id}, KeyId={KeyId}",
                existing.Id, existing.KeyId);
        }

        return updated;
    }
    public async Task<bool> DeactivateAsync(JwtKeysEntity key, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(key.Id, ct);
        if (existing == null)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.NotFound"), 404);
            return false;
        }

        var validation = await _validator.ValidateForDeactivateAsync(existing);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }

        existing.Deactivate(_currentUser.GetUserId());
        var updated = await _repo.UpdateAsync(existing, ct);

        if (updated)
        {
            _logger.LogInformation("? [DeactivateAsync] JwtKey desativada. Id={Id}, KeyId={KeyId}",
                existing.Id, existing.KeyId);
        }

        return updated;
    }
    public async Task<bool> RevokeAsync(int id, string reason, int modifiedBy, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing == null)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.NotFound"), 404);
            return false;
        }

        var validation = await _validator.ValidateForRevokeAsync(existing);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }

        // Se for a �nica chave ativa, gerar uma nova antes de revogar
        if (existing.IsActive)
        {
            var hasOtherActiveKey = await _repo.HasActiveKeyAsync(existing.TenantId, ct);
            if (!hasOtherActiveKey)
            {
                _logger.LogWarning("?? [RevokeAsync] Revogando �nica chave ativa. Gerando nova chave primeiro. Id={Id}",
                    existing.Id);

                // Gerar nova chave
                var (publicKey, privateKeyEncrypted) = await GenerateKeyPairAsync(
                    existing.Algorithm,
                    existing.KeySize,
                    ct);

                var newKey = new JwtKeysEntity(
                    existing.TenantId,
                    publicKey,
                    privateKeyEncrypted,
                    modifiedBy,
                    existing.Algorithm,
                    existing.KeySize,
                    existing.KeyType,
                    existing.RotationPolicyDays,
                    existing.OverlapPeriodDays,
                    existing.MaxTokenLifetimeMinutes);

                await _repo.AddAsync(newKey, ct);
                newKey.Activate(_currentUser.GetUserId());
                await _repo.UpdateAsync(newKey, ct);

                _logger.LogInformation("? [RevokeAsync] Nova chave gerada e ativada. Id={NewKeyId}, KeyId={NewKeyKeyId}", newKey.Id, newKey.KeyId);
            }
        }

        existing.Revoke(reason, _currentUser.GetUserId());
        var updated = await _repo.UpdateAsync(existing, ct);

        if (updated)
        {
            _logger.LogWarning("?? [RevokeAsync] JwtKey revogada. Id={Id}, KeyId={KeyId}, Reason={Reason}", existing.Id, existing.KeyId, reason);
        }

        return updated;
    }
    public async Task<bool> DeleteAsync(JwtKeysEntity key, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(key.Id, ct);
        if (existing == null)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.NotFound"), 404);
            return false;
        }

        var validation = await _validator.ValidateForDeleteAsync(existing);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                _notify.Add(error.ErrorMessage, 400);
            return false;
        }

        existing.Delete(_currentUser.GetUserId());
        return await _repo.UpdateAsync(existing, ct);
    }
    public async Task<bool> UpdateRotationPolicyAsync(int id, int rotationPolicyDays, int overlapPeriodDays, int modifiedBy, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing == null)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.NotFound"), 404);
            return false;
        }

        if (rotationPolicyDays < 30 || rotationPolicyDays > 365)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.InvalidRotationPolicy"), 400);
            return false;
        }

        if (overlapPeriodDays < 1 || overlapPeriodDays > 30)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.InvalidOverlapPeriod"), 400);
            return false;
        }

        if (overlapPeriodDays >= rotationPolicyDays)
        {
            _notify.Add(_localization.GetMessage("Domain.JwtKey.OverlapMustBeLessThanRotation"), 400);
            return false;
        }

        existing.UpdateRotationSchedule(rotationPolicyDays, overlapPeriodDays, modifiedBy);
        return await _repo.UpdateAsync(existing, ct);
    }
    public async Task<(string PublicKey, string PrivateKeyEncrypted)> GenerateKeyPairAsync(string? algorithm, int keySize, CancellationToken ct)
    {
        await Task.CompletedTask; // Para evitar warning de async sem await

        _logger.LogInformation("?? [GenerateKeyPairAsync] Gerando par de chaves. Algorithm={Algorithm}, KeySize={KeySize}", algorithm, keySize);

        // Gerar par de chaves RSA
        var (publicKeyPem, privateKeyPem) = CryptoRSA.GenerateKeyPair(keySize);

        // Obter chave mestra do provider de segredos
        var masterKey = _secretProvider?.GetMasterKey();
        if (string.IsNullOrWhiteSpace(masterKey))
        {
            _logger.LogError("? [GenerateKeyPairAsync] Chave mestra para criptografia n�o dispon�vel");
            _notify.Add(_localization.GetMessage("Domain.JwtKey.EncryptionKeyMissing"), 400);
            throw new InvalidOperationException("Master encryption key is not available");
        }

        try
        {
            // Criptografar chave privada
            var privateKeyEncrypted = CryptoRSA.EncryptPrivateKey(privateKeyPem, masterKey);

            _logger.LogInformation("? [GenerateKeyPairAsync] Par de chaves gerado com sucesso");

            return (publicKeyPem, privateKeyEncrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? [GenerateKeyPairAsync] Erro ao criptografar chave privada");
            _notify.Add(_localization.GetMessage("Domain.JwtKey.CreateFailed"), 500);
            throw;
        }
    }
    public async Task<string> DecryptPrivateKeyAsync(string encryptedPrivateKey, CancellationToken ct)
    {
        await Task.CompletedTask; // Para evitar warning de async sem await

        var masterKey = _secretProvider?.GetMasterKey();
        if (string.IsNullOrWhiteSpace(masterKey))
        {
            _logger.LogError("? [DecryptPrivateKeyAsync] Chave mestra para descriptografia n�o dispon�vel");
            _notify.Add(_localization.GetMessage("Domain.JwtKey.EncryptionKeyMissing"), 400);
            throw new InvalidOperationException("Master encryption key is not available");
        }

        try
        {
            return CryptoRSA.DecryptPrivateKey(encryptedPrivateKey, masterKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? [DecryptPrivateKeyAsync] Erro ao descriptografar chave privada");
            _notify.Add(_localization.GetMessage("Domain.JwtKey.InvalidPrivateKey"), 500);
            throw;
        }
    }
    public async Task<int> RotateKeysAsync(CancellationToken ct)
    {
        _logger.LogInformation("?? [RotateKeysAsync] Iniciando rota��o de chaves");

        var keysToRotate = await _repo.GetKeysEligibleForRotationAsync(ct);
        var keysList = keysToRotate.ToList();
        var rotatedCount = 0;

        _logger.LogInformation("?? [RotateKeysAsync] Encontradas {Count} chaves eleg�veis para rota��o", keysList.Count);

        foreach (var oldKey in keysList)
        {
            // Validar estado
            if (!oldKey.IsActive || oldKey.IsDeleted || oldKey.IsRevoked())
            {
                _logger.LogWarning("?? [RotateKeysAsync] Chave n�o est� mais eleg�vel. Id={Id}, KeyId={KeyId}",
                    oldKey.Id, oldKey.KeyId);
                continue;
            }

            // Validar tenant e application
            if (!await ValidateTenantAsync(oldKey.TenantId, ct))
            {
                _logger.LogWarning("?? [RotateKeysAsync] Tenant ou Application inativo. TenantId={TenantId}", oldKey.TenantId);
                continue;
            }

            // Gerar nova chave
            var (publicKey, privateKeyEncrypted) = await GenerateKeyPairAsync(
                oldKey.Algorithm,
                oldKey.KeySize,
                ct);

            var newKey = new JwtKeysEntity(
                oldKey.TenantId,
                publicKey,
                privateKeyEncrypted,
                0, // Sistema
                oldKey.Algorithm,
                oldKey.KeySize,
                oldKey.KeyType,
                oldKey.RotationPolicyDays,
                oldKey.OverlapPeriodDays,
                oldKey.MaxTokenLifetimeMinutes);

            // Ativar nova chave
            newKey.Activate(0);

            // Inserir nova chave
            await _repo.AddAsync(newKey, ct);

            // Desativar chave antiga e estender expira��o pelo per�odo de sobreposi��o
            oldKey.Deactivate(0);
            oldKey.UpdateRotationSchedule(oldKey.OverlapPeriodDays, 0, 0);

            await _repo.UpdateAsync(oldKey, ct);

            rotatedCount++;

            _logger.LogInformation("? [RotateKeysAsync] Chave rotacionada com sucesso. OldKeyId={OldKeyId}, NewKeyId={NewKeyId}, TenantId={TenantId}", oldKey.KeyId, newKey.KeyId, oldKey.TenantId);
        }

        _logger.LogInformation("? [RotateKeysAsync] Rota��o conclu�da. Total rotacionado: {RotatedCount}/{TotalCount}", rotatedCount, keysList.Count);

        return rotatedCount;
    }
    public async Task<int> CleanupExpiredKeysAsync(int retentionDays, CancellationToken ct)
    {
        _logger.LogInformation("?? [CleanupExpiredKeysAsync] Iniciando limpeza de chaves expiradas. RetentionDays={RetentionDays}", retentionDays);

        var expiredKeys = await _repo.GetExpiredKeysAsync(retentionDays, ct);
        var keysList = expiredKeys.ToList();
        var deletedCount = 0;

        _logger.LogInformation("?? [CleanupExpiredKeysAsync] Encontradas {Count} chaves expiradas para limpeza", keysList.Count);

        foreach (var key in keysList)
        {
            await _repo.UpdateAsync(key, ct);
            deletedCount++;

            _logger.LogInformation("??? [CleanupExpiredKeysAsync] Chave marcada como deletada. Id={Id}, KeyId={KeyId}, ExpiresAt={ExpiresAt}",
                key.Id, key.KeyId, key.ExpiresAt);
        }

        _logger.LogInformation("? [CleanupExpiredKeysAsync] Limpeza conclu�da. Total deletado: {DeletedCount}/{TotalCount}", deletedCount, keysList.Count);

        return deletedCount;
    }
    public async Task<JwtKeysEntity?> EnsureKeyExistsAsync(int tenantId, int createdBy, CancellationToken ct)
    {
        _logger.LogInformation("?? [EnsureKeyExistsAsync] Verificando exist�ncia de chave JWT. TenantId={TenantId}", tenantId);

        // Verificar se j� existe chave ativa
        var existingKey = await _repo.GetActiveKeyAsync(tenantId, ct);
        if (existingKey != null)
        {
            _logger.LogInformation("? [EnsureKeyExistsAsync] Chave ativa j� existe. KeyId={KeyId}", existingKey.KeyId);
            return existingKey;
        }

        // Validar tenant
        if (!await ValidateTenantAsync(tenantId, ct))
        {
            _logger.LogWarning("?? [EnsureKeyExistsAsync] Tenant n�o encontrado ou inativo. TenantId={TenantId}", tenantId);
            _notify.Add(_localization.GetMessage("Domain.JwtKey.TenantNotFound"), 400);
            return null;
        }

        // Criar nova chave com configura��es padr�o
        _logger.LogInformation(
            "?? [EnsureKeyExistsAsync] Criando nova chave JWT com configura��es padr�o. TenantId={TenantId}", tenantId);

        const string defaultAlgorithm = "RS256";
        const int defaultKeySize = 2048;
        const string defaultKeyType = "RSA";
        const int defaultRotationPolicyDays = 90;
        const int defaultOverlapPeriodDays = 7;
        const int defaultMaxTokenLifetimeMinutes = 60;

        try
        {
            // Gerar par de chaves
            var (publicKey, privateKeyEncrypted) = await GenerateKeyPairAsync(
                defaultAlgorithm,
                defaultKeySize,
                ct);

            var newKey = new JwtKeysEntity(
                tenantId,
                publicKey,
                privateKeyEncrypted,
                createdBy,
                defaultAlgorithm,
                defaultKeySize,
                defaultKeyType,
                defaultRotationPolicyDays,
                defaultOverlapPeriodDays,
                defaultMaxTokenLifetimeMinutes);

            // Validar a entidade
            var validation = await _validator.ValidateForCreateAsync(newKey);
            if (!validation.IsValid)
            {
                _logger.LogWarning("?? [EnsureKeyExistsAsync] Valida��o da nova chave falhou. Errors={Errors}", string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));

                foreach (var error in validation.Errors)
                    _notify.Add(error.ErrorMessage, 400);
                return null;
            }

            // Verificar novamente (thread-safety)
            var doubleCheck = await _repo.HasActiveKeyAsync(tenantId, ct);
            if (doubleCheck)
            {
                _logger.LogWarning("?? [EnsureKeyExistsAsync] Chave ativa j� foi criada por outro processo (race condition detectada). TenantId={TenantId}", tenantId);

                // Retornar a chave existente
                return await _repo.GetActiveKeyAsync(tenantId, ct);
            }

            // Inserir no banco
            var created = await _repo.AddAsync(newKey, ct);
            if (!created)
            {
                _logger.LogError("? [EnsureKeyExistsAsync] Falha ao criar chave JWT. TenantId={TenantId}", tenantId);

                if (_notify.HasNotify())
                {
                    _logger.LogError("? [EnsureKeyExistsAsync] Notifica��es: {Notifications}", string.Join("; ", _notify.GetErrorMessage()));
                }

                _notify.Add(_localization.GetMessage("Domain.JwtKey.CreateFailed"), 400);
                return null;
            }

            return newKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? [EnsureKeyExistsAsync] Erro ao criar chave JWT para Tenant {TenantId}", tenantId);
            if (_notify.HasNotify())
            {
                _logger.LogError("? [EnsureKeyExistsAsync] Notifica��es antes da exce��o: {Notifications}", string.Join("; ", _notify.GetErrorMessage()));
            }

            // Repassar uma notifica��o amig�vel
            _notify.Add(_localization.GetMessage("Domain.JwtKey.CreateFailed"), 500);
            return null;
        }
    }

    #region Private Methods

    private async Task<bool> ValidateTenantAsync(int tenantId, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        return tenant != null && tenant.IsActive && !tenant.IsDeleted;
    }

    #endregion
}
