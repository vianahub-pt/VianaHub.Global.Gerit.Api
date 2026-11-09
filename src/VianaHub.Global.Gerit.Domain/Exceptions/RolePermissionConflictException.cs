namespace VianaHub.Global.Gerit.Domain.Exceptions;

public sealed class RolePermissionConflictException : Exception
{
    public RolePermissionConflictException(Exception innerException)
        : base("A role permission persistence conflict occurred.", innerException)
    {
    }
}
