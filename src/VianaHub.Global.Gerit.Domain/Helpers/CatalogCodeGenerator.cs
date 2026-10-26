using System.Globalization;
using System.Text;

namespace VianaHub.Global.Gerit.Domain.Helpers;

/// <summary>
/// Normalização sintática da fonte canónica inglesa fornecida e derivação técnica de formatos.
/// Não traduz conceitos nem deteta se o texto fornecido está semanticamente em inglês.
/// </summary>
public static class CatalogCodeGenerator
{
    public const string CanonicalLanguageCode = "en-US";
    /// <summary>
    /// Normaliza a fonte fornecida sem inferir o seu significado ou idioma.
    /// A responsabilidade pelo nome inglês pertence à tradução en-US fornecida.
    /// </summary>
    public static string Generate(string? value, int maxLength = 50)
    {
        if (string.IsNullOrWhiteSpace(value) || maxLength <= 0) return string.Empty;
        var result = new StringBuilder();
        foreach (var character in value.Trim().Normalize(NormalizationForm.FormD).ToUpperInvariant())
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            if (character is >= 'A' and <= 'Z' or >= '0' and <= '9') result.Append(character);
            else if ((char.IsWhiteSpace(character) || char.IsPunctuation(character) || char.IsSymbol(character)) && result.Length > 0 && result[^1] != '_') result.Append('_');
        }
        var code = result.ToString().Trim('_');
        return code.Length <= maxLength ? code : code[..maxLength].TrimEnd('_');
    }

    /// <summary>
    /// Usa exclusivamente uma tradução canónica en-US fornecida; não traduz outros idiomas.
    /// Fontes ausentes, duplicadas ou sem nome normalizável não geram um código.
    /// </summary>
    public static string FromCanonicalTranslation<T>(IEnumerable<T>? translations, Func<T, string?> language, Func<T, string?> name)
    {
        var items = translations?.ToList();
        if (items == null || items.Any(t => t is null)) return string.Empty;
        var canonical = items.Where(t => string.Equals(language(t), CanonicalLanguageCode, StringComparison.OrdinalIgnoreCase)).ToList();
        return canonical.Count == 1 ? Generate(name(canonical[0])) : string.Empty;
    }
    /// <summary>
    /// Deriva o formato técnico da extensão e preserva os aliases canónicos dos seeds.
    /// O ponto inicial não faz parte da chave; nomes traduzidos não participam desta regra.
    /// </summary>
    public static string ForFileExtension(string? extension)
    {
        var code = Generate(extension?.Trim().TrimStart('.'));
        return code switch
        {
            "JPG" or "JPEG" => "JPEG",
            "7Z" => "SEVEN_ZIP",
            "BIN" => "BINARY",
            _ => code
        };
    }
}
