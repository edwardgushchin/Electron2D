using SDL3;

namespace Electron2D;

internal sealed class EventSystem
{
    private EventQueue _events = null!;

    private const int BatchSize = 64;
    private int _maxBatchesPerFrame;

    private bool _quitRequested;
    public bool QuitRequested => _quitRequested;

    public EventQueue Events => _events;
    
    public int DroppedEngineEvents { get; private set; }
    
    public int DroppedWindowEvents { get; private set; }
    
    public int DroppedInputEvents { get; private set; }


    public void Initialize(EngineConfig cfg)
    {
        if (!SDL.InitSubSystem(SDL.InitFlags.Events))
            throw new InvalidOperationException($"SDL.InitSubSystem(Events) failed. {SDL.GetError()}");

        _events = new EventQueue(
            cfg.EngineEventsPerFrame,
            cfg.WindowEventsPerFrame,
            cfg.InputEventsPerFrame);

        // Сколько максимум SDL событий будем вычерпывать за кадр.
        // Берём сумму capacity каналов (реально принять мы больше всё равно не сможем).
        var maxEventsThisFrame = cfg.EngineEventsPerFrame + cfg.WindowEventsPerFrame + cfg.InputEventsPerFrame;
        _maxBatchesPerFrame = Math.Max(1, (maxEventsThisFrame + BatchSize - 1) / BatchSize);

        _quitRequested = false;
        _events.ClearAll();
    }

    public void Shutdown()
    {
        SDL.QuitSubSystem(SDL.InitFlags.Events);
        _events.ClearAll();
    }

    public unsafe void BeginFrame()
    {
        _quitRequested = false; // P0: quit должен быть "в этом кадре", а не "навсегда"

        SDL.PumpEvents();
        
        DroppedEngineEvents = 0;
        DroppedWindowEvents = 0;
        DroppedInputEvents = 0;


        var buffer = stackalloc SDL.Event[BatchSize];
        var bufferPtr = (IntPtr)buffer;

        for (var batch = 0; batch < _maxBatchesPerFrame; batch++)
        {
            var got = SDL.PeepEvents(
                events: bufferPtr,
                numevents: BatchSize,
                action: SDL.EventAction.GetEvent,
                minType: (uint)SDL.EventType.First,
                maxType: (uint)SDL.EventType.Last);

            if (got <= 0) break;

            for (var i = 0; i < got; i++)
            {
                ref readonly var e = ref buffer[i];
                var type = (SDL.EventType)e.Type;

                switch (type)
                {
                    case SDL.EventType.Quit:
                        _quitRequested = true;
                        if (!_events.Engine.TryPublish(new EngineEvent(EngineEventType.QuitRequested)))
                            DroppedEngineEvents++;
                        break;

                    case SDL.EventType.WindowShown:
                    {
                        var timestamp = e.Window.Timestamp;
                        var windowId = e.Window.WindowID;
                        if (!_events.Window.TryPublish(new WindowEvent(WindowEventType.Shown, timestamp, windowId)))
                            DroppedWindowEvents++;
                        break;
                    }

                    case SDL.EventType.WindowCloseRequested:
                    {
                        var timestamp = e.Window.Timestamp;
                        var windowId = e.Window.WindowID;
                        if (!_events.Window.TryPublish(new WindowEvent(WindowEventType.CloseRequested, timestamp, windowId)))
                            DroppedWindowEvents++;
                        break;
                    }

                    case SDL.EventType.WindowResized:
                    {
                        var timestamp = e.Window.Timestamp;
                        var windowId = e.Window.WindowID;
                        var w = e.Window.Data1;
                        var h = e.Window.Data2;
                        if (!_events.Window.TryPublish(new WindowEvent(WindowEventType.Resized, timestamp, windowId, w, h)))
                            DroppedWindowEvents++;
                        break;
                    }
                    case SDL.EventType.WindowPixelSizeChanged:
                    {
                        var timestamp = e.Window.Timestamp;
                        var windowId = e.Window.WindowID;
                        var w = e.Window.Data1;
                        var h = e.Window.Data2;
                        if (!_events.Window.TryPublish(new WindowEvent(WindowEventType.PixelSizeChanged, timestamp, windowId, w, h)))
                            DroppedWindowEvents++;
                        break;
                    }

                    case SDL.EventType.WindowFocusGained:
                    {
                        var timestamp = e.Window.Timestamp;
                        var windowId = e.Window.WindowID;
                        if (!_events.Window.TryPublish(new WindowEvent(WindowEventType.FocusGained, timestamp, windowId)))
                            DroppedWindowEvents++;
                        break;
                    }

                    case SDL.EventType.WindowFocusLost:
                    {
                        var timestamp = e.Window.Timestamp;
                        var windowId = e.Window.WindowID;
                        if (!_events.Window.TryPublish(new WindowEvent(WindowEventType.FocusLost, timestamp, windowId)))
                            DroppedWindowEvents++;
                        break;
                    }
                    case SDL.EventType.KeyDown:
                    {
                        if (!_events.Input.TryPublish(new InputEvent(InputEventType.KeyDown, code: e.Key.Scancode)))
                            DroppedInputEvents++;
                        break;
                    }
                    
                    case SDL.EventType.KeyUp:
                    {
                        if (!_events.Input.TryPublish(new InputEvent(InputEventType.KeyUp, code: e.Key.Scancode)))
                            DroppedInputEvents++;
                        break;
                    }

                    // TODO: сюда же маппинг input событий в _events.Input.TryPublish(...)
                    /*Shown,
                    Hidden,
                    Exposed,
                    Moved,
                    Resized,
                    PixelSizeChanged,
                    MetalViewResized,
                    Minimized,
                    Maximized,
                    Restored,
                    MouseEnter,
                    MouseLeave,
                    FocusGained,
                    FocusLost,
                    CloseRequested,
                    HitTest,
                    ICCProfChanged,
                    DisplayChanged,
                    DisplayScaleChanged,
                    SafeAreaChanged,
                    Occluded,
                    EnterFullscreen,
                    LeaveFullscreen,
                    Destroyed,
                    HDRStateChanged,*/
                }
            }

            if (got < BatchSize) break;
        }
    }
    
    public void EndFrame()
    {
        _events.SwapAll();
    }
}
