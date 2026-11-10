namespace VianaHub.Global.Gerit.Domain.Exceptions;

public sealed class UserRoleConflictException(Exception innerException)
    : Exception("A user role persistence conflict occurred.", innerException);
