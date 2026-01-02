namespace Electron2D;

#region EventQueue

/// <summary>
/// Набор каналов событий движка с единым управлением стадиями (Swap/Clear) для всех очередей.
/// </summary>
internal sealed class EventQueue(int engineCapacity, int windowCapacity, int inputCapacity)
{
    #region Properties

    /// <summary>Канал событий движка.</summary>
    public EventChannel<EngineEvent> Engine { get; } = new(engineCapacity);

    /// <summary>Канал событий окна.</summary>
    public EventChannel<WindowEvent> Window { get; } = new(windowCapacity);

    /// <summary>Канал событий ввода.</summary>
    public EventChannel<InputEvent> Input { get; } = new(inputCapacity);

    #endregion

    #region Public API

    /// <summary>
    /// Выполнить <see cref="EventChannel{TEvent}.Swap"/> для всех каналов.
    /// </summary>
    public void SwapAll()
    {
        Engine.Swap();
        Window.Swap();
        Input.Swap();
    }

    /// <summary>
    /// Выполнить <see cref="EventChannel{TEvent}.Clear"/> для всех каналов.
    /// </summary>
    public void ClearAll()
    {
        Engine.Clear();
        Window.Clear();
        Input.Clear();
    }

    #endregion
}

#endregion