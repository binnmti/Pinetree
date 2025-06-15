using Pinetree.Services;
using System.Text.Json;

namespace Pinetree.Extensions;

public static class LoggerExtensions
{
    private static IServiceProvider? _serviceProvider;

    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static void LogSecure<T>(this ILogger logger, T obj, string message, LogLevel logLevel = LogLevel.Information)
    {
        if (obj == null)
        {
            logger.Log(logLevel, "{Message} | Data: null", message);
            return;
        }

        try
        {
            var detector = GetSensitiveDataDetector();
            
            if (detector != null)
            {
                var maskedObject = detector.MaskSensitiveObject(obj);
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

    public static void LogSecureInformation<T>(this ILogger logger, T obj, string message)
    {
        logger.LogSecure(obj, message, LogLevel.Information);
    }

    public static void LogSecureWarning<T>(this ILogger logger, T obj, string message)
    {
        logger.LogSecure(obj, message, LogLevel.Warning);
    }

    public static void LogSecureError<T>(this ILogger logger, T obj, string message)
    {
        logger.LogSecure(obj, message, LogLevel.Error);
    }

    public static void LogSecureDebug<T>(this ILogger logger, T obj, string message)
    {
        logger.LogSecure(obj, message, LogLevel.Debug);
    }

    public static void LogSecureTrace<T>(this ILogger logger, T obj, string message)
    {
        logger.LogSecure(obj, message, LogLevel.Trace);
    }

    public static void LogSecureCritical<T>(this ILogger logger, T obj, string message)
    {
        logger.LogSecure(obj, message, LogLevel.Critical);
    }

    private static ISensitiveDataDetectorService? GetSensitiveDataDetector()
    {
        try
        {
            return _serviceProvider?.GetService<ISensitiveDataDetectorService>();
        }
        catch
        {
            // Return null if service cannot be retrieved
            return null;
        }
    }
}
