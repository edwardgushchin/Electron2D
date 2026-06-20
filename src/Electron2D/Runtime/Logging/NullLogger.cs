namespace Electron2D;

#region NullLogger

/// <summary>
/// Логгер-пустышка: игнорирует любые сообщения.
/// </summary>
internal sealed class NullLogger : ILogger
{
    #region Static fields

    /// <summary>Единый экземпляр логгера-пустышки.</summary>
    public static readonly NullLogger Instance = new();

    #endregion

    #region Public API

    public bool IsEnabled(LogLevel level) => false;

    public void Write(LogLevel level, string messageTemplate)
    {
    }

    public void Write<T0>(LogLevel level, string messageTemplate, T0 arg0)
    {
    }

    public void Write<T0, T1>(LogLevel level, string messageTemplate, T0 arg0, T1 arg1)
    {
    }

    #endregion
}

#endregion