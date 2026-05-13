namespace CheapAI.Application.Common.Exceptions;

public sealed class AppNotFoundException(string message) : Exception(message);
