using Pinetree.Services;

namespace Pinetree.Extensions;

/// <summary>
/// Extension methods for ILogger that provide secure logging capabilities.
/// These methods accept services as parameters to avoid static dependencies.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs an object with secure masking using the provided SecureLoggerService.
    /// </summary>
    public static void LogSecure<T>(this ILogger logger, ISecureLoggerService secureLogger, T obj, string message, LogLevel logLevel = LogLevel.Information)
    {
        secureLogger.LogSecure(logger, obj, message, logLevel);
    }

    /// <summary>
    /// Logs an object with secure masking using the provided SensitiveDataDetectorService directly.
    /// </summary>
    public static void LogSecure<T>(this ILogger logger, ISensitiveDataDetectorService? detector, T obj, string message, LogLevel logLevel = LogLevel.Information)
    {
        var secureLogger = new SecureLoggerService(detector);
        secureLogger.LogSecure(logger, obj, message, logLevel);
    }

    public static void LogSecureInformation<T>(this ILogger logger, ISecureLoggerService secureLogger, T obj, string message)
    {
        secureLogger.LogSecureInformation(logger, obj, message);
    }

    public static void LogSecureInformation<T>(this ILogger logger, ISensitiveDataDetectorService? detector, T obj, string message)
    {
        var secureLogger = new SecureLoggerService(detector);
        secureLogger.LogSecureInformation(logger, obj, message);
    }

    public static void LogSecureWarning<T>(this ILogger logger, ISecureLoggerService secureLogger, T obj, string message)
    {
        secureLogger.LogSecureWarning(logger, obj, message);
    }

    public static void LogSecureWarning<T>(this ILogger logger, ISensitiveDataDetectorService? detector, T obj, string message)
    {
        var secureLogger = new SecureLoggerService(detector);
        secureLogger.LogSecureWarning(logger, obj, message);
    }

    public static void LogSecureError<T>(this ILogger logger, ISecureLoggerService secureLogger, T obj, string message)
    {
        secureLogger.LogSecureError(logger, obj, message);
    }

    public static void LogSecureError<T>(this ILogger logger, ISensitiveDataDetectorService? detector, T obj, string message)
    {
        var secureLogger = new SecureLoggerService(detector);
        secureLogger.LogSecureError(logger, obj, message);
    }

    public static void LogSecureDebug<T>(this ILogger logger, ISecureLoggerService secureLogger, T obj, string message)
    {
        secureLogger.LogSecureDebug(logger, obj, message);
    }

    public static void LogSecureDebug<T>(this ILogger logger, ISensitiveDataDetectorService? detector, T obj, string message)
    {
        var secureLogger = new SecureLoggerService(detector);
        secureLogger.LogSecureDebug(logger, obj, message);
    }

    public static void LogSecureTrace<T>(this ILogger logger, ISecureLoggerService secureLogger, T obj, string message)
    {
        secureLogger.LogSecureTrace(logger, obj, message);
    }

    public static void LogSecureTrace<T>(this ILogger logger, ISensitiveDataDetectorService? detector, T obj, string message)
    {
        var secureLogger = new SecureLoggerService(detector);
        secureLogger.LogSecureTrace(logger, obj, message);
    }

    public static void LogSecureCritical<T>(this ILogger logger, ISecureLoggerService secureLogger, T obj, string message)
    {
        secureLogger.LogSecureCritical(logger, obj, message);
    }

    public static void LogSecureCritical<T>(this ILogger logger, ISensitiveDataDetectorService? detector, T obj, string message)
    {
        var secureLogger = new SecureLoggerService(detector);
        secureLogger.LogSecureCritical(logger, obj, message);
    }
}
