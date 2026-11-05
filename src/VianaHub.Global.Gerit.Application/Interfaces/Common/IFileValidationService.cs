using Microsoft.AspNetCore.Http;

namespace VianaHub.Global.Gerit.Application.Interfaces.Common;

/// <summary>
/// Interface responsável pela validação de arquivos de upload.
/// Centraliza as regras de validação para garantir reusabilidade e manutenibilidade.
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Valida um arquivo de upload baseado em regras de segurança e formato.
    /// </summary>
    /// <param name="file">Arquivo a ser validado.</param>
    /// <returns>True se o arquivo é válido, False caso contrário.</returns>
    bool ValidateFile([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] IFormFile? file);
}
