namespace VianaHub.Global.Gerit.Domain.Exceptions;

public sealed class ActionConflictException : Exception
{
    public ActionConflictException(Exception innerException)
        : base("Action persistence conflict.", innerException) { }
}
