using System.Diagnostics.CodeAnalysis;

namespace VianaHub.Global.Gerit.Domain.Helpers;

public static class LogValueSanitizer
{
    // Preserva o valor para diagnóstico sem permitir a criação de novas linhas de log.
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Sanitize(string? value) => value?
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\u0085", "\\u0085", StringComparison.Ordinal)
        .Replace("\u2028", "\\u2028", StringComparison.Ordinal)
        .Replace("\u2029", "\\u2029", StringComparison.Ordinal);

    // Mantém detalhes técnicos, mas impede que mensagens de exceção forjem linhas.
    public static string ExceptionDetails(Exception exception) => Sanitize(exception.ToString());
}
