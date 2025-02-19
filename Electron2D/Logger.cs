namespace Electron2D;

public static class Logger
{
    private static ILogger _logger = new ConsoleLogger();

    internal static LogLevel Level
    {
        get => _logger.Level;
        set => _logger.Level = value;
    }

    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    public static void Debug(string message) => _logger.Log(LogLevel.Debug, message);
    
    public static void Info(string message)  => _logger.Log(LogLevel.Info, message);
    
    public static void Warn(string message)  => _logger.Log(LogLevel.Warn, message);
    
    public static void Error(string message) => _logger.Log(LogLevel.Error, message);
    
    public static void Fatal(string message) => _logger.Log(LogLevel.Fatal, message);
}