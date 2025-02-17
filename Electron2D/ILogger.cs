namespace Electron2D;

public interface ILogger
{
    LogLevel Level { get; set; }
    
    void Log(LogLevel level, string message);
}