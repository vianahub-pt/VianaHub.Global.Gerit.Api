using VianaHub.Global.Gerit.Domain.Helpers;
using Microsoft.Extensions.Logging;
using VianaHub.Global.Gerit.Domain.Interfaces.Base;

namespace VianaHub.Global.Gerit.Infra.Integration.Messaging;

public class NoOpEmailSender : IEmailSender
{
    private readonly ILogger<NoOpEmailSender> _logger;
    private readonly ILocalizationService _localization;

    public NoOpEmailSender(ILogger<NoOpEmailSender> logger, ILocalizationService localization)
    {
        _logger = logger;
        _localization = localization;
    }

    public Task SendAsync(string to, string subjectKey, string bodyKey, params object?[] args)
    {
        var subject = _localization?.GetMessage(subjectKey) ?? subjectKey;
        var body = _localization?.GetMessage(bodyKey, args) ?? bodyKey;

        _logger.LogInformation("[NoOpEmailSender] Simulating email to {To} | Subject: {Subject}", LogValueSanitizer.Sanitize(to), LogValueSanitizer.Sanitize(subject));
        _logger.LogDebug("[NoOpEmailSender] Email body: {Body}", LogValueSanitizer.Sanitize(body));
        return Task.CompletedTask;
    }
}
