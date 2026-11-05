namespace VianaHub.Global.Gerit.Domain.Exceptions;

public sealed class ClientDocumentConflictException : Exception
{
    public ClientDocumentConflictException(Exception innerException) : base("Client document persistence conflict.", innerException) { }
}
