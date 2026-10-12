namespace SmartTasks.Api.Application.Ai;

public class AiServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
