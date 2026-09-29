namespace TecAssist.Application.Common;

public sealed class ChatGenerationException(string message, Exception? innerException = null) : Exception(message, innerException);
