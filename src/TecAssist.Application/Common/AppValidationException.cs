namespace TecAssist.Application.Common;

public sealed class AppValidationException(string message) : Exception(message);
