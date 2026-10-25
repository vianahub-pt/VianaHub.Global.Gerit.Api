namespace VianaHub.Global.Gerit.Domain.Interfaces.Base;

/// <summary>
/// Serviço de localização para mensagens de validação e erro
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Obtém uma mensagem localizada pela chave
    /// </summary>
    /// <param name="key">Chave da mensagem</param>
    /// <returns>Mensagem localizada</returns>
    string GetMessage(string key);

    /// <summary>
    /// Obtém uma mensagem localizada pela chave com formatação
    /// </summary>
    /// <param name="key">Chave da mensagem</param>
    /// <param name="args">Argumentos para formatação</param>
    /// <returns>Mensagem localizada e formatada</returns>
    string GetMessage(string key, params object?[] args);

    /// <summary>
    /// Obtém a cultura atual da requisição (ex: pt-PT, en-US, es-ES).
    /// </summary>
    /// <returns>Código da cultura atual</returns>
    string GetCurrentCulture();
}
