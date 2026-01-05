namespace Electron2D;

#region EventQueue

/// <summary>
/// Набор каналов событий движка с единым управлением стадиями (Swap/Clear) для всех очередей.
/// </summary>
internal sealed class EventQueue(int engineCapacity, int windowCapacity, int keyboardCapacity, int mouseCapacity)
{
    #region Properties

    /// <summary>Канал событий движка.</summary>
    public EventChannel<EngineEvent> Engine { get; } = new(engineCapacity);

    /// <summary>Канал событий окна.</summary>
    public EventChannel<WindowEvent> Window { get; } = new(windowCapacity);

    /// <summary>Канал событий ввода.</summary>
    //public EventChannel<InputEvent> Input { get; } = new(inputCapacity);
    
    public EventChannel<MouseEvent> Mouse { get; } = new(mouseCapacity);
    
    public EventChannel<KeyboardEvent> Keyboard { get; } = new(keyboardCapacity);

    #endregion

    #region Public API

    /// <summary>
    /// Выполнить <see cref="EventChannel{TEvent}.Swap"/> для всех каналов.
    /// </summary>
    public void SwapAll()
    {
        Engine.Swap();
        Window.Swap();
        Mouse.Swap();
        Keyboard.Swap();
    }

    /// <summary>
    /// Выполнить <see cref="EventChannel{TEvent}.Clear"/> для всех каналов.
    /// </summary>
    public void ClearAll()
    {
        Engine.Clear();
        Window.Clear();
        Mouse.Clear();
        Keyboard.Clear();
    }

    #endregion
}

#endregion