namespace VianaHub.Global.Gerit.Domain.Exceptions;

public sealed class UserEmailConflictException(Exception innerException)
    : Exception("A user email persistence conflict occurred.", innerException);
