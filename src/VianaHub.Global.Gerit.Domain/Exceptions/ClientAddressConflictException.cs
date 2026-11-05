namespace VianaHub.Global.Gerit.Domain.Exceptions;

public sealed class ClientAddressConflictException : Exception
{
    public ClientAddressConflictException(Exception innerException) : base("Client address persistence conflict.", innerException) { }
}
