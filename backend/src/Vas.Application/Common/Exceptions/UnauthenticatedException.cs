namespace Vas.Application.Common.Exceptions;

public sealed class UnauthenticatedException(string message = "Authentication is required.")
    : Exception(message);
