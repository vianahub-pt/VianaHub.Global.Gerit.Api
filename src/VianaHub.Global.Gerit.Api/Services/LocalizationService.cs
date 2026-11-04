using VianaHub.Global.Gerit.Domain.Helpers;
using Serilog;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Api.Services;

/// <summary>
/// Implementação do serviço de localização para a camada de domínio da aplicação Gerit
/// </summary>
public class LocalizationService : ILocalizationService
{
    private static readonly ConcurrentDictionary<string, Lazy<IReadOnlyDictionary<string, string>>> _cache = new();
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalizationService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetMessage(string key)
    {
        var culture = NormalizeCulture(GetCurrentCulture());

        var messages = GetMessages(culture);

        if (messages.TryGetValue(key, out var value))
            return value;

        // Fallback para pt-PT
        if (culture != "pt-PT")
        {
            var fallbackMessages = GetMessages("pt-PT");
            if (fallbackMessages.TryGetValue(key, out var fallbackValue))
                return fallbackValue;
        }
        return key;
    }

    public string GetMessage(string key, params object?[] args)
    {
        var message = GetMessage(key);
        try
        {
            return string.Format(message, args);
        }
        catch
        {
            return message;
        }
    }

    public string GetCurrentCulture()
    {
        // First, try to get culture from HttpContext.Items (set by RequestLocalizationMiddleware)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue("RequestCulture", out var cultureFromContext) == true
            && cultureFromContext is string cultureName)
        {
            return cultureName;
        }

        // Fallback to CurrentUICulture
        var fallbackCulture = CultureInfo.CurrentUICulture.Name;
        return fallbackCulture;
    }

    private static IReadOnlyDictionary<string, string> GetMessages(string culture)
    {
        culture = NormalizeCulture(culture);
        return _cache.GetOrAdd(
            culture,
            static cultureName => new Lazy<IReadOnlyDictionary<string, string>>(
                () => LoadMessages(cultureName),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    private static string NormalizeCulture(string? culture)
    {
        if (string.Equals(culture, "pt-PT", StringComparison.OrdinalIgnoreCase))
            return "pt-PT";
        if (string.Equals(culture, "pt-BR", StringComparison.OrdinalIgnoreCase))
            return "pt-BR";
        if (string.Equals(culture, "en-US", StringComparison.OrdinalIgnoreCase))
            return "en-US";
        if (string.Equals(culture, "es-ES", StringComparison.OrdinalIgnoreCase))
            return "es-ES";

        return "pt-PT";
    }

    private static IReadOnlyDictionary<string, string> LoadMessages(string culture)
    {
        var localesPath = Path.Combine(
            AppContext.BaseDirectory,
            "locales"
        );

        if (!Directory.Exists(localesPath))
        {
            Log.Error("[Gerit:LocalizationService] Locales folder not found: {Path}", localesPath);
            return new Dictionary<string, string>();
        }

        // Carregar o ficheiro comum para a cultura: locales/{culture}/common.json
        var commonFilePath = Path.Combine(localesPath, culture, "common.json");

        if (!File.Exists(commonFilePath))
        {
            Log.Warning("[Gerit:LocalizationService] No common.json found for culture {Culture} at {Path}", culture, commonFilePath);

            // Tentar fallback para pt-PT
            if (culture != "pt-PT")
            {
                var fallbackPath = Path.Combine(localesPath, "pt-PT", "common.json");
                if (File.Exists(fallbackPath))
                {
                    Log.Warning("[Gerit:LocalizationService] Trying pt-PT fallback");
                    commonFilePath = fallbackPath;
                }
                else
                {
                    Log.Error("[Gerit:LocalizationService] No fallback file found for culture {Culture}", culture);
                    return new Dictionary<string, string>();
                }
            }
            else
            {
                Log.Error("[Gerit:LocalizationService] No pt-PT fallback file found");
                return new Dictionary<string, string>();
            }
        }

        Log.Debug("[Gerit:LocalizationService] Loading {File}", commonFilePath);

        try
        {
            var json = File.ReadAllText(commonFilePath);
            var options = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            var messages = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);

            if (messages == null || messages.Count == 0)
            {
                Log.Warning("[Gerit:LocalizationService] File {File} is empty or invalid", Path.GetFileName(commonFilePath));
                return new Dictionary<string, string>();
            }

            Log.Information("[Gerit:LocalizationService] Successfully loaded {Count} messages for culture {Culture}",
                messages.Count, culture);

            return messages;
        }
        // Fronteira intencional de fallback: qualquer falha de leitura ou desserialização
        // retorna um mapa vazio para preservar o contrato de localização sem propagar
        // detalhes de filesystem para a requisição.
        catch (Exception ex)
        {
            Log.Error("[Gerit:LocalizationService] Error loading file {File} | Exception: {ExceptionDetails}", Path.GetFileName(commonFilePath), LogValueSanitizer.ExceptionDetails(ex));
            return new Dictionary<string, string>();
        }
    }
}
