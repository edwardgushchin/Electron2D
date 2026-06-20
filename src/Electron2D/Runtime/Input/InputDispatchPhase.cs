namespace Electron2D;

#region InputDispatchPhase

/// <summary>
/// Фазы диспетчеризации ввода (порядок обработки input в UI/сцене).
/// </summary>
internal enum InputDispatchPhase
{
    /// <summary>Обычная обработка ввода.</summary>
    Input,

    /// <summary>Обработка шорткатов (горячих клавиш).</summary>
    ShortcutInput,

    /// <summary>Ввод клавиатуры, не обработанный на предыдущих фазах.</summary>
    UnhandledKeyInput,

    /// <summary>Ввод, не обработанный на предыдущих фазах.</summary>
    UnhandledInput,
}

#endregion