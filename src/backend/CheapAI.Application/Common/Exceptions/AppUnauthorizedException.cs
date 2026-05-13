namespace CheapAI.Application.Common.Exceptions;

public sealed class AppUnauthorizedException(string message) : Exception(message);
