using VianaHub.Global.Gerit.Domain.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VianaHub.Global.Gerit.Application.Configuration;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Interfaces.Identity;
using VianaHub.Global.Gerit.Domain.Tools.Cryptography;

namespace VianaHub.Global.Gerit.Api.Configuration;

/// <summary>
/// Configuração JWT com suporte a chaves dinâmicas e rotação
/// </summary>
public static class JwtSetup
{
    /// <summary>
    /// Adiciona a configuração de autenticação JWT ao pipeline de serviços.
    /// Suporta chaves dinâmicas do banco de dados e rotação sem downtime.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação.</param>
    /// <returns>A própria coleção de serviços para encadeamento.</returns>
    public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var apiSettingSection = configuration.GetSection("ApplicationSettings");
        services.Configure<ApplicationSettings>(apiSettingSection);

        var jwtSettings = configuration.GetSection("JwtSettings");
        services.Configure<JwtSettings>(jwtSettings);

        var jwtConfig = jwtSettings.Get<JwtSettings>() ?? new JwtSettings();

        // Validar configurações obrigatórias
        ValidateJwtConfiguration(jwtConfig, configuration);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(bearerOptions =>
        {
            bearerOptions.SaveToken = true;

            // HTTPS é obrigatório em produção
            bearerOptions.RequireHttpsMetadata = jwtConfig.RequireHttpsMetadata;

            bearerOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                // Issuer e Audience serão validados dinamicamente por Tenant
                ValidIssuers = jwtConfig.ValidIssuers ?? new[] { jwtConfig.Issuer },
                ValidAudiences = jwtConfig.ValidAudiences ?? new[] { jwtConfig.Audience },

                // Clock skew para compensar diferenças de relógio entre servidores
                ClockSkew = TimeSpan.FromMinutes(jwtConfig.ClockSkewMinutes),

                // Resolver chaves de assinatura dinamicamente do banco de dados
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    var httpContext = new HttpContextAccessor().HttpContext;
                    httpContext?.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("VianaHub.Global.Gerit.AuthTrace")
                        .LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=IssuerSigningKeyResolver Kid={Kid} KeysReturned={KeysReturned}",
                            LogValueSanitizer.Sanitize(httpContext.TraceIdentifier), LogValueSanitizer.Sanitize(kid), 0);
                    // NOTA: Chaves serão resolvidas no evento OnMessageReceived
                    // que tem acesso ao HttpContext e IServiceProvider
                    return Array.Empty<SecurityKey>();
                }
            };

            // Eventos para logging, auditoria e resolução de chaves
            bearerOptions.Events = new JwtBearerEvents
            {
                OnMessageReceived = async context =>
                {
                    var path = context.HttpContext.Request.Path;
                    var trace = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("VianaHub.Global.Gerit.AuthTrace");
                    var requestId = LogValueSanitizer.Sanitize(context.HttpContext.TraceIdentifier);
                    trace.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=MessageReceived Path={Path} Method={Method} AuthorizationPresent={AuthorizationPresent} BearerPresent={BearerPresent}",
                        requestId, LogValueSanitizer.Sanitize(path.Value), LogValueSanitizer.Sanitize(context.Request.Method),
                        context.Request.Headers.ContainsKey("Authorization"),
                        context.Request.Headers.Authorization.FirstOrDefault()?.StartsWith("Bearer ") == true);

                    // 🔥 IGNORAR HANGFIRE
                    if (path.StartsWithSegments("/hangfire"))
                    {
                        context.NoResult();
                        return;
                    }

                    // 🔥 SE NÃO TEM TOKEN, NÃO TENTE VALIDAR
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    {
                        context.NoResult();
                        return;
                    }

                    // Aqui temos acesso ao HttpContext e podemos resolver chaves corretamente
                    var keyRepository = context.HttpContext.RequestServices
                        .GetRequiredService<IJwtKeyDataRepository>();
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerHandler>>();

                    // tentar resolver serviço de localização para mensagens legíveis
                    var localization = context.HttpContext.RequestServices.GetService(typeof(ILocalizationService)) as ILocalizationService;

                    try
                    {
                        // ---------------------------------------------------------------
                        // PASSO CRÍTICO: Extrair tenant_id do token SEM validá-lo ainda.
                        //
                        // O token ainda não foi validado neste evento — o usuário não está
                        // autenticado. Por isso os interceptores EF Core não conseguem
                        // resolver o TenantId via JWT claim, e o SESSION_CONTEXT do SQL
                        // Server fica vazio, fazendo o RLS bloquear a query em dbo.JwtKeys.
                        //
                        // Solução: decodificar o payload do JWT (sem verificar assinatura)
                        // apenas para ler o tenant_id, e populá-lo no IRequestTenantContext
                        // para que os interceptores consigam definir o SESSION_CONTEXT antes
                        // de executar qualquer query no banco.
                        // ---------------------------------------------------------------
                        var rawToken = context.Request.Headers.Authorization
                            .FirstOrDefault()?.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
                            ?? context.Token;

                        if (!string.IsNullOrWhiteSpace(rawToken))
                        {
                            LogTokenMetadata(rawToken, trace, requestId);
                            var requestTenantContext = context.HttpContext.RequestServices
                                .GetRequiredService<IRequestTenantContext>();

                            var tenantId = ExtractTenantIdFromRawToken(rawToken, logger);
                            trace.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=TenantExtracted TenantId={TenantId} Extracted={Extracted}",
                                requestId, tenantId, tenantId.HasValue);
                            if (tenantId.HasValue)
                            {
                                requestTenantContext.SetTenantId(tenantId.Value);
                                trace.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=RequestTenantContextSet TenantId={TenantId} ContextTenantId={ContextTenantId}",
                                    requestId, tenantId.Value, requestTenantContext.TenantId);
                                logger.LogDebug(
                                    "[JWT] TenantId={TenantId} extraído do token (pré-validação) e definido no IRequestTenantContext",
                                    tenantId.Value);
                            }
                            else
                            {
                                logger.LogWarning("[JWT] Não foi possível extrair tenant_id do token antes da validação. RLS pode bloquear a busca de chaves.");
                            }
                        }

                        // ---------------------------------------------------------------
                        // Buscar chaves do banco — agora com SESSION_CONTEXT corretamente
                        // definido pelos interceptores EF Core.
                        // ---------------------------------------------------------------
                        trace.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=JwtKeysLookupStarted", requestId);
                        var allKeys = await keyRepository.GetAllAsync(default);
                        trace.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=JwtKeysLookupCompleted Count={Count}", requestId, allKeys.Count());
                        foreach (var key in allKeys)
                        {
                            trace.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=JwtKeyLoaded TenantId={TenantId} KeyId={KeyId} IsActive={IsActive} IsDeleted={IsDeleted} RevokedAtPresent={RevokedAtPresent} ExpiresAt={ExpiresAt} ValidForValidation={ValidForValidation}",
                                requestId, key.TenantId, key.KeyId, key.IsActive, key.IsDeleted, key.RevokedAt.HasValue, key.ExpiresAt, key.IsValidForValidation());
                        }
                        var keys = allKeys.Where(k => k.IsValidForValidation());
                        trace.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=ValidKeys Count={Count}", requestId, keys.Count());

                        if (!keys.Any())
                        {
                            logger.LogError("❌ Nenhuma chave JWT ativa encontrada no banco de dados");
                            var msg = localization?.GetMessage("Api.Configuration.Jwt.NoActiveSigningKeys") ?? "Api.Configuration.Jwt.NoActiveSigningKeys";
                            context.Fail(msg);
                            return;
                        }

                        // Configurar chaves no contexto de validação
                        var securityKeys = keys.Select(key =>
                        {
                            try
                            {
                                var rsa = System.Security.Cryptography.RSA.Create();

                                // CORREÇÃO: Extrair bytes do formato PEM antes de importar
                                var publicKeyBytes = CryptoRSA.ExtractBytesFromPem(key.PublicKey, "PUBLIC KEY");
                                rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

                                return new RsaSecurityKey(rsa) { KeyId = key.KeyId.ToString() } as SecurityKey;
                            }
                            catch (Exception ex)
                            {
                                logger.LogError("❌ Erro ao processar chave pública. KeyId={KeyId} | Exception: {ExceptionDetails}", key.KeyId, LogValueSanitizer.ExceptionDetails(ex));
                                return null;
                            }
                        })
                        .Where(k => k != null)
                        .ToList();

                        trace.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=SecurityKeys Count={Count} KeyIds={KeyIds}",
                            requestId, securityKeys.Count, LogValueSanitizer.Sanitize(string.Join(",", securityKeys.Select(k => k!.KeyId))));

                        if (!securityKeys.Any())
                        {
                            logger.LogError("❌ Nenhuma chave JWT válida encontrada após processamento");
                            var msg = localization?.GetMessage("Api.Configuration.Jwt.NoValidSigningKeys") ?? "Api.Configuration.Jwt.NoValidSigningKeys";
                            context.Fail(msg);
                            return;
                        }

                        // Atualizar parâmetros de validação com as chaves carregadas
                        context.Options.TokenValidationParameters.IssuerSigningKeys = securityKeys;

                        logger.LogDebug(
                            "✅ {Count} chave(s) JWT carregada(s) para validação",
                            securityKeys.Count);
                    }
                    catch (Exception ex)
                    {
                        trace.LogWarning("[AUTH-TRACE] RequestId={RequestId} Step=JwtKeysLookupFailed ExceptionType={ExceptionType} Message={Message}",
                            requestId, ex.GetType().FullName, SafeDiagnosticMessage(ex.Message));
                        logger.LogError("❌ Erro ao carregar chaves JWT do banco de dados | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
                        var msg = localization?.GetMessage("Api.Configuration.Jwt.FailedToLoadSigningKeys") ?? "Api.Configuration.Jwt.FailedToLoadSigningKeys";
                        context.Fail(msg);
                    }
                },

                OnAuthenticationFailed = context =>
                {
                    context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("VianaHub.Global.Gerit.AuthTrace")
                        .LogWarning("[AUTH-TRACE] RequestId={RequestId} Step=AuthenticationFailed Path={Path} ExceptionType={ExceptionType} Message={Message} InnerExceptionType={InnerExceptionType} InnerMessage={InnerMessage}",
                            LogValueSanitizer.Sanitize(context.HttpContext.TraceIdentifier), LogValueSanitizer.Sanitize(context.Request.Path.Value),
                            context.Exception.GetType().FullName, SafeDiagnosticMessage(context.Exception.Message),
                            context.Exception.InnerException?.GetType().FullName, SafeDiagnosticMessage(context.Exception.InnerException?.Message));
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerHandler>>();

                    logger.LogWarning(
                        "❌ JWT Authentication failed: {Error} | Path: {Path} | IP: {IP}",
                        LogValueSanitizer.Sanitize(context.Exception.Message),
                        LogValueSanitizer.Sanitize(context.Request.Path.Value),
                        context.HttpContext.Connection.RemoteIpAddress);

                    return Task.CompletedTask;
                },

                OnTokenValidated = context =>
                {
                    context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("VianaHub.Global.Gerit.AuthTrace")
                        .LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=TokenValidated Path={Path} Sub={Sub} TenantId={TenantId} IsAuthenticated={IsAuthenticated} Roles={Roles} PermissionsPresent={PermissionsPresent}",
                            LogValueSanitizer.Sanitize(context.HttpContext.TraceIdentifier), LogValueSanitizer.Sanitize(context.Request.Path.Value),
                            LogValueSanitizer.Sanitize(context.Principal?.FindFirst("sub")?.Value ?? context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value),
                            LogValueSanitizer.Sanitize(context.Principal?.FindFirst("tenant_id")?.Value), context.Principal?.Identity?.IsAuthenticated,
                            LogValueSanitizer.Sanitize(string.Join(",", context.Principal?.Claims.Where(c => c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>())),
                            context.Principal?.HasClaim(c => c.Type == "permissions") == true);
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerHandler>>();

                    var userId = context.Principal?.FindFirst("sub")?.Value;

                    logger.LogInformation(
                        "✅ JWT Token validated successfully | User: {UserId} | Path: {Path}",
                        LogValueSanitizer.Sanitize(userId),
                        LogValueSanitizer.Sanitize(context.Request.Path.Value));

                    return Task.CompletedTask;
                },

                OnChallenge = context =>
                {
                    context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger("VianaHub.Global.Gerit.AuthTrace")
                        .LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=Challenge Path={Path} Error={Error} ErrorDescription={ErrorDescription} IsAuthenticated={IsAuthenticated} ResponseStatus={ResponseStatus}",
                            LogValueSanitizer.Sanitize(context.HttpContext.TraceIdentifier), LogValueSanitizer.Sanitize(context.Request.Path.Value),
                            SafeDiagnosticMessage(context.Error), SafeDiagnosticMessage(context.ErrorDescription),
                            context.HttpContext.User.Identity?.IsAuthenticated, context.Response.StatusCode);
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerHandler>>();

                    logger.LogWarning(
                        "⚠️ JWT Challenge issued | Path: {Path} | Error: {Error}",
                        LogValueSanitizer.Sanitize(context.Request.Path.Value),
                        LogValueSanitizer.Sanitize(context.Error));

                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    // Metadados não validados servem apenas ao diagnóstico; nunca participam da autorização.
    private static void LogTokenMetadata(string rawToken, ILogger logger, string requestId)
    {
        try
        {
            var handler = new JsonWebTokenHandler();
            if (!handler.CanReadToken(rawToken)) return;
            var token = handler.ReadJsonWebToken(rawToken);
            logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=TokenMetadata TenantId={TenantId} Sub={Sub} Kid={Kid} Alg={Alg} Iss={Iss} Aud={Aud} Nbf={Nbf} Exp={Exp} Validated={Validated}",
                requestId, LogValueSanitizer.Sanitize(token.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value),
                LogValueSanitizer.Sanitize(token.Subject), LogValueSanitizer.Sanitize(token.Kid), LogValueSanitizer.Sanitize(token.Alg),
                LogValueSanitizer.Sanitize(token.Issuer), LogValueSanitizer.Sanitize(string.Join(",", token.Audiences)),
                LogValueSanitizer.Sanitize(token.Claims.FirstOrDefault(c => c.Type == "nbf")?.Value),
                LogValueSanitizer.Sanitize(token.Claims.FirstOrDefault(c => c.Type == "exp")?.Value), false);
        }
        catch (Exception ex)
        {
            logger.LogInformation("[AUTH-TRACE] RequestId={RequestId} Step=TokenMetadataUnavailable ExceptionType={ExceptionType}", requestId, ex.GetType().FullName);
        }
    }

    // Remove JWTs de mensagens técnicas antes de aplicar a sanitização existente.
    private static string? SafeDiagnosticMessage(string? message) => LogValueSanitizer.Sanitize(message is null ? null :
        System.Text.RegularExpressions.Regex.Replace(message, @"[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]*", "[REDACTED]"));

    /// <summary>
    /// Decodifica o payload do JWT sem validar a assinatura para extrair o tenant_id.
    /// Utilizado exclusivamente no evento OnMessageReceived, antes da validação completa
    /// do token, para permitir que os interceptores EF Core configurem o SESSION_CONTEXT
    /// do SQL Server e assim o RLS não bloqueie a query de busca de chaves.
    ///
    /// SEGURANÇA: Este método NÃO valida a assinatura, expiração ou qualquer outra claim.
    /// O tenant_id extraído aqui é usado APENAS para configurar o SESSION_CONTEXT antes
    /// de buscar as chaves públicas — a validação real do token ocorre logo em seguida
    /// pelo framework com as chaves carregadas do banco.
    /// </summary>
    private static int? ExtractTenantIdFromRawToken(string rawToken, ILogger logger)
    {
        try
        {
            var handler = new JsonWebTokenHandler();

            // Lê o token sem validar — apenas para inspecionar o payload
            var jwt = handler.ReadJsonWebToken(rawToken);
            if (jwt is null)
                return null;

            // Tenta as três variações de nome da claim
            var tenantClaim = jwt.TryGetClaim("tenant_id", out var c1) ? c1.Value
                            : jwt.TryGetClaim("tenantId", out var c2) ? c2.Value
                            : jwt.TryGetClaim("tenant", out var c3) ? c3.Value
                            : null;

            if (tenantClaim is not null && int.TryParse(tenantClaim, out var tenantId))
                return tenantId;

            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug("[JWT] Falha ao decodificar token para extração do tenant_id (pré-validação) | Exception: {ExceptionDetails}", LogValueSanitizer.ExceptionDetails(ex));
            return null;
        }
    }

    /// <summary>
    /// Valida configurações obrigatórias do JWT
    /// </summary>
    private static void ValidateJwtConfiguration(JwtSettings jwtConfig, IConfiguration configuration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(jwtConfig.Issuer))
        {
            errors.Add("JwtSettings:Issuer não está configurado");
        }

        if (string.IsNullOrWhiteSpace(jwtConfig.Audience))
        {
            errors.Add("JwtSettings:Audience não está configurado");
        }

        if (jwtConfig.AccessTokenExpirationMinutes <= 0)
        {
            errors.Add("JwtSettings:AccessTokenExpirationMinutes deve ser maior que zero");
        }

        if (jwtConfig.RefreshTokenExpirationDays <= 0)
        {
            errors.Add("JwtSettings:RefreshTokenExpirationDays deve ser maior que zero");
        }

        // Em produção, validações mais rigorosas
        var env = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
        if (env?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (jwtConfig.AccessTokenExpirationMinutes > 60)
            {
                errors.Add("⚠️ Em produção, AccessTokenExpirationMinutes não deve exceder 60 minutos");
            }

            if (!jwtConfig.RequireHttpsMetadata)
            {
                errors.Add("❌ CRÍTICO: RequireHttpsMetadata deve ser true em produção");
            }

            if (jwtConfig.ClockSkewMinutes > 5)
            {
                errors.Add("⚠️ Em produção, ClockSkewMinutes recomendado é máximo 5 minutos (configurado: {0})"
                    .Replace("{0}", jwtConfig.ClockSkewMinutes.ToString()));
            }
        }

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Configuração JWT inválida:\n{string.Join("\n", errors)}");
        }
    }
}
