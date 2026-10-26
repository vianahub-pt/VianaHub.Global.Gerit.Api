using Microsoft.AspNetCore.Http;
using VianaHub.Global.Gerit.Application.Interfaces.Common;
using VianaHub.Global.Gerit.Domain.Helpers;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Application.Services.Common;

/// <summary>
/// Serviço responsável pela validação de arquivos de upload.
/// Centraliza todas as regras de validação para garantir consistência e reusabilidade em toda a aplicação.
/// Implementa os princípios de Single Responsibility e DRY (Don't Repeat Yourself).
/// </summary>
public class FileValidationService : IFileValidationService
{
    private readonly INotify _notify;
    private readonly ILocalizationService _localization;

    public FileValidationService(INotify notify, ILocalizationService localization)
    {
        _notify = notify;
        _localization = localization;
    }

    /// <summary>
    /// Valida um arquivo de upload baseado em regras de segurança e formato.
    /// </summary>
    /// <param name="file">Arquivo a ser validado.</param>
    /// <returns>True se o arquivo é válido, False caso contrário.</returns>
    public bool ValidateFile([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] IFormFile? file)
    {
        // Valida se o arquivo existe e não está vazio
        if (file == null || file.Length == 0)
        {
            _notify.Add(_localization.GetMessage("Application.Service.File.ValidateFile.InvalidFile"), 400);
            return false;
        }

        // Valida tamanho do arquivo
        if (!file.Length.IsValidCsvFileSize())
        {
            _notify.Add(_localization.GetMessage("Application.Service.File.ValidateFile.IsValidCsvFileSize"), 400);
            return false;
        }

        // Valida nome do arquivo (previne path traversal)
        if (!file.FileName.IsSafeCsvFileName())
        {
            _notify.Add(_localization.GetMessage("Application.Service.File.ValidateFile.IsSafeCsvFileName"), 400);
            return false;
        }

        // Valida extensão
        if (!file.FileName.HasValidCsvExtension())
        {
            _notify.Add(_localization.GetMessage("Application.Service.File.ValidateFile.OnlyCsvAllowed"), 400);
            return false;
        }

        // Valida encoding UTF-8
        using (var stream = file.OpenReadStream())
        {
            if (!stream.IsValidUtf8Encoding())
            {
                _notify.Add(_localization.GetMessage("Application.Service.File.ValidateFile.InvalidEncoding"), 400);
                return false;
            }
        }

        return true;
    }
}
