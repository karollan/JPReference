public static class Log
{
    private static ILogger _logger;

    public static void Init(ILoggerFactory factory)
    {
        _logger = factory.CreateLogger("JLPTReference.Api");
    }

    public static void Info(string message) => _logger?.LogInformation(message);
    public static void Error(string message) => _logger?.LogError(message);
    public static void Warning(string message) => _logger?.LogWarning(message);
    public static void Debug(string message) => _logger?.LogDebug(message);
    public static void Trace(string message) => _logger?.LogTrace(message);
    public static void Fatal(string message) => _logger?.LogCritical(message);
}