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
        SDL.PumpEvents();

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

            if (got <= 0)
                break;

            for (var i = 0; i < got; i++)
            {
                ref readonly var e = ref buffer[i];
                var type = (SDL.EventType)e.Type;

                switch (type)
                {
                    case SDL.EventType.Quit:
                        _quitRequested = true;
                        _events.Engine.TryPublish(new EngineEvent(EngineEventType.QuitRequested));
                        break;

                    case SDL.EventType.WindowCloseRequested:
                    {
                        var windowId = e.Window.WindowID;
                        _events.Window.TryPublish(new WindowEvent(WindowEventType.CloseRequested, windowId));
                        break;
                    }

                    case SDL.EventType.WindowResized:
                    case SDL.EventType.WindowPixelSizeChanged:
                    {
                        var windowId = e.Window.WindowID;
                        var w = e.Window.Data1;
                        var h = e.Window.Data2;
                        _events.Window.TryPublish(new WindowEvent(WindowEventType.Resized, windowId, w, h));
                        break;
                    }

                    case SDL.EventType.WindowFocusGained:
                    {
                        var windowId = e.Window.WindowID;
                        _events.Window.TryPublish(new WindowEvent(WindowEventType.FocusGained, windowId));
                        break;
                    }

                    case SDL.EventType.WindowFocusLost:
                    {
                        var windowId = e.Window.WindowID;
                        _events.Window.TryPublish(new WindowEvent(WindowEventType.FocusLost, windowId));
                        break;
                    }

                    // TODO: сюда же маппинг input событий в _events.Input.TryPublish(...)
                }
            }

            if (got < BatchSize)
                break;
        }

        _events.SwapAll();
    }
}
