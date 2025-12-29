namespace Electron2D;

public static class Log
{
    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();
        public bool IsEnabled(LogLevel level) => false;
        public void Write(LogLevel level, string messageTemplate) { }
        public void Write<T0>(LogLevel level, string messageTemplate, T0 arg0) { }
        public void Write<T0, T1>(LogLevel level, string messageTemplate, T0 arg0, T1 arg1) { }
    }

    private static ILogger _impl = NullLogger.Instance;

    internal static void Bind(ILogger impl) => _impl = impl ?? NullLogger.Instance;
    internal static void Unbind() => _impl = NullLogger.Instance;

    public static bool IsEnabled(LogLevel level) => _impl.IsEnabled(level);

    public static void Debug(string t) => _impl.Write(LogLevel.Debug, t);
    public static void Debug<T0>(string t, T0 a0) => _impl.Write(LogLevel.Debug, t, a0);

    public static void Info(string t) => _impl.Write(LogLevel.Information, t);
    public static void Info<T0>(string t, T0 a0) => _impl.Write(LogLevel.Information, t, a0);

    public static void Warn(string t) => _impl.Write(LogLevel.Warning, t);
    public static void Error(string t) => _impl.Write(LogLevel.Error, t);
}