namespace Vas.Application.Common.Exceptions;

public sealed class ForbiddenException(string message = "You do not have access to this resource.")
    : Exception(message);
