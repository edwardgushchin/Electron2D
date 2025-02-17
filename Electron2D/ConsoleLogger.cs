namespace Electron2D;

public class ConsoleLogger : ILogger
{
    public LogLevel Level { get; set; } = LogLevel.Debug;

    public void Log(LogLevel level, string message)
    {
        if (level < Level)
            return;
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {level}: {message}");
    }
}