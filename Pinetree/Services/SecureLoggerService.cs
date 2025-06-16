using System.Text.Json;

namespace Pinetree.Services;

public interface ISecureLoggerService
{
    void LogSecure<T>(ILogger logger, T obj, string message, LogLevel logLevel = LogLevel.Information);
    void LogSecureInformation<T>(ILogger logger, T obj, string message);
    void LogSecureWarning<T>(ILogger logger, T obj, string message);
    void LogSecureError<T>(ILogger logger, T obj, string message);
    void LogSecureDebug<T>(ILogger logger, T obj, string message);
    void LogSecureTrace<T>(ILogger logger, T obj, string message);
    void LogSecureCritical<T>(ILogger logger, T obj, string message);
}

public class SecureLoggerService : ISecureLoggerService
{
    private readonly ISensitiveDataDetectorService? _sensitiveDataDetector;

    public SecureLoggerService(ISensitiveDataDetectorService? sensitiveDataDetector = null)
    {
        _sensitiveDataDetector = sensitiveDataDetector;
    }

    public void LogSecure<T>(ILogger logger, T obj, string message, LogLevel logLevel = LogLevel.Information)
    {
        if (obj == null)
        {
            logger.Log(logLevel, "{Message} | Data: null", message);
            return;
        }

        try
        {
            if (_sensitiveDataDetector != null)
            {
                var maskedObject = _sensitiveDataDetector.MaskSensitiveObject(obj);
                var maskedJson = JsonSerializer.Serialize(maskedObject, new JsonSerializerOptions 
                { 
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                logger.Log(logLevel, "{Message} | Data: {MaskedData}", message, maskedJson);
            }
            else
            {
                // Fallback if service is not available - log basic information without sensitive data
                var typeName = obj.GetType().Name;
                logger.Log(logLevel, "{Message} | Data: [SENSITIVE_DATA_MASKED] Type: {TypeName}", message, typeName);
            }
        }
        catch (Exception ex)
        {
            // If anything goes wrong with masking, don't log the original object
            logger.LogError(ex, "Failed to mask sensitive data for logging. Message: {Message}", message);
            logger.Log(logLevel, "{Message} | Data: [ERROR_MASKING_DATA]", message);
        }
    }

    public void LogSecureInformation<T>(ILogger logger, T obj, string message)
    {
        LogSecure(logger, obj, message, LogLevel.Information);
    }

    public void LogSecureWarning<T>(ILogger logger, T obj, string message)
    {
        LogSecure(logger, obj, message, LogLevel.Warning);
    }

    public void LogSecureError<T>(ILogger logger, T obj, string message)
    {
        LogSecure(logger, obj, message, LogLevel.Error);
    }

    public void LogSecureDebug<T>(ILogger logger, T obj, string message)
    {
        LogSecure(logger, obj, message, LogLevel.Debug);
    }

    public void LogSecureTrace<T>(ILogger logger, T obj, string message)
    {
        LogSecure(logger, obj, message, LogLevel.Trace);
    }

    public void LogSecureCritical<T>(ILogger logger, T obj, string message)
    {
        LogSecure(logger, obj, message, LogLevel.Critical);
    }
}
