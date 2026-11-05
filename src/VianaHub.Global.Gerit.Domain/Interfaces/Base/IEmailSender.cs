namespace VianaHub.Global.Gerit.Domain.Interfaces.Base;

public interface IEmailSender
{
    Task SendAsync(string to, string subjectKey, string bodyKey, params object?[] args);
}
