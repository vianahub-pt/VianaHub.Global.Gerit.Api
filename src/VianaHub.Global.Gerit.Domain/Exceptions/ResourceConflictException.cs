namespace VianaHub.Global.Gerit.Domain.Exceptions;

public sealed class ResourceConflictException : Exception
{
    public ResourceConflictException(Exception innerException)
        : base("Resource persistence conflict.", innerException) { }
}
