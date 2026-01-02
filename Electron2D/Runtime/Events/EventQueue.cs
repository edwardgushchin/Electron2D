namespace Electron2D;

internal sealed class EventQueue
{
    public EventChannel<EngineEvent> Engine { get; }
    public EventChannel<WindowEvent> Window { get; }
    public EventChannel<InputEvent> Input { get; }

    public EventQueue(int engineCapacity, int windowCapacity, int inputCapacity)
    {
        Engine = new EventChannel<EngineEvent>(engineCapacity);
        Window = new EventChannel<WindowEvent>(windowCapacity);
        Input  = new EventChannel<InputEvent>(inputCapacity);
    }

    public void SwapAll()
    {
        Engine.Swap();
        Window.Swap();
        Input.Swap();
    }

    public void ClearAll()
    {
        Engine.Clear();
        Window.Clear();
        Input.Clear();
    }
}