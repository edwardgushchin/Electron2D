namespace Electron2D;

/// <summary>
/// Глобальный фасад логирования. По умолчанию использует no-op реализацию.
/// </summary>
/// <remarks>
/// Реальная реализация должна быть привязана движком через <c>Bind</c>.
/// </remarks>
public static class Log
{
    private static ILogger _logger = NullLogger.Instance;

    #region Internal API
    internal static void Bind(ILogger? logger) => _logger = logger ?? NullLogger.Instance;

    internal static void Unbind() => _logger = NullLogger.Instance;
    #endregion

    #region Public API
    public static bool IsEnabled(LogLevel level) => _logger.IsEnabled(level);

    public static void Debug(string messageTemplate) => _logger.Write(LogLevel.Debug, messageTemplate);

    public static void Debug<T0>(string messageTemplate, T0 arg0) =>
        _logger.Write(LogLevel.Debug, messageTemplate, arg0);

    public static void Info(string messageTemplate) => _logger.Write(LogLevel.Information, messageTemplate);

    public static void Info<T0>(string messageTemplate, T0 arg0) =>
        _logger.Write(LogLevel.Information, messageTemplate, arg0);

    public static void Warn(string messageTemplate) => _logger.Write(LogLevel.Warning, messageTemplate);

    public static void Error(string messageTemplate) => _logger.Write(LogLevel.Error, messageTemplate);
    #endregion
}