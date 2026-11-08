namespace VianaHub.Global.Gerit.Domain.Exceptions;

public sealed class RoleConflictException : Exception
{
    public RoleConflictException(Exception innerException)
        : base("A role name conflict occurred.", innerException)
    {
    }
}
