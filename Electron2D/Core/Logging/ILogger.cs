namespace Electron2D;

/// <summary>
/// Минимальный интерфейс логгера с поддержкой шаблонов сообщений.
/// </summary>
/// <remarks>
/// Методы <c>Write</c> принимают шаблон и аргументы отдельно, чтобы реализация могла:
/// - избегать лишних аллокаций при выключенном уровне;
/// - выполнять форматирование/структурированное логирование по своим правилам.
/// </remarks>
public interface ILogger
{
    /// <summary>
    /// Возвращает <see langword="true"/>, если указанный уровень логирования включён.
    /// </summary>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Записывает сообщение без аргументов.
    /// </summary>
    void Write(LogLevel level, string messageTemplate);

    /// <summary>
    /// Записывает сообщение с одним аргументом.
    /// </summary>
    void Write<T0>(LogLevel level, string messageTemplate, T0 arg0);

    /// <summary>
    /// Записывает сообщение с двумя аргументами.
    /// </summary>
    void Write<T0, T1>(LogLevel level, string messageTemplate, T0 arg0, T1 arg1);
}