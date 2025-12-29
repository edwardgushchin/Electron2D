namespace Electron2D;

public interface ILogger
{
    bool IsEnabled(LogLevel level);

    void Write(LogLevel level, string messageTemplate);
    void Write<T0>(LogLevel level, string messageTemplate, T0 arg0);
    void Write<T0, T1>(LogLevel level, string messageTemplate, T0 arg0, T1 arg1);
}