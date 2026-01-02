using System;

using SDL3;

namespace Electron2D;

#region EventSystem

/// <summary>
/// Система сбора событий SDL и раскладки их по внутренним каналам движка.
/// </summary>
/// <remarks>
/// Инвариант: методы предполагают, что <see cref="Initialize"/> был вызван до <see cref="BeginFrame"/>/<see cref="EndFrame"/>.
/// </remarks>
internal sealed class EventSystem
{
    #region Constants

    private const int BatchSize = 64;

    #endregion

    #region Instance fields

    private EventQueue _eventQueue = null!;

    private int _maxBatchesPerFrame;
    private bool _quitRequested;

    #endregion

    #region Properties

    /// <summary>True, если в текущем кадре получен запрос завершения.</summary>
    public bool QuitRequested => _quitRequested;

    /// <summary>Очереди событий, заполненные за текущий кадр (после <see cref="EndFrame"/> — в read-буфере).</summary>
    public EventQueue Events => _eventQueue;

    /// <summary>Сколько событий движка было отброшено из-за переполнения канала.</summary>
    public int DroppedEngineEvents { get; private set; }

    /// <summary>Сколько событий окна было отброшено из-за переполнения канала.</summary>
    public int DroppedWindowEvents { get; private set; }

    /// <summary>Сколько событий ввода было отброшено из-за переполнения канала.</summary>
    public int DroppedInputEvents { get; private set; }

    #endregion

    #region Public API

    public void Initialize(EngineConfig config)
    {
        if (!SDL.InitSubSystem(SDL.InitFlags.Events))
            throw new InvalidOperationException($"SDL.InitSubSystem(Events) failed. {SDL.GetError()}");

        _eventQueue = new EventQueue(
            config.EngineEventsPerFrame,
            config.WindowEventsPerFrame,
            config.InputEventsPerFrame);

        // Сколько максимум SDL-событий вычерпываем за кадр.
        // Берём сумму capacity каналов: больше всё равно принять не сможем (TryPublish начнёт возвращать false).
        var maxEventsThisFrame = config.EngineEventsPerFrame + config.WindowEventsPerFrame + config.InputEventsPerFrame;
        _maxBatchesPerFrame = Math.Max(1, (maxEventsThisFrame + BatchSize - 1) / BatchSize);

        _quitRequested = false;
        _eventQueue.ClearAll();
    }

    public void Shutdown()
    {
        SDL.QuitSubSystem(SDL.InitFlags.Events);
        _eventQueue.ClearAll();
    }

    public unsafe void BeginFrame()
    {
        // P0: quit должен быть "в этом кадре", а не "навсегда".
        _quitRequested = false;

        SDL.PumpEvents();

        DroppedEngineEvents = 0;
        DroppedWindowEvents = 0;
        DroppedInputEvents = 0;

        var buffer = stackalloc SDL.Event[BatchSize];
        var bufferPtr = (IntPtr)buffer;

        for (var batchIndex = 0; batchIndex < _maxBatchesPerFrame; batchIndex++)
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
                ref readonly var sdlEvent = ref buffer[i];
                var type = (SDL.EventType)sdlEvent.Type;

                switch (type)
                {
                    case SDL.EventType.Quit:
                        _quitRequested = true;
                        TryPublishEngineQuitRequested();
                        break;

                    case SDL.EventType.WindowShown:
                        TryPublishWindowEvent(WindowEventType.Shown, sdlEvent.Window.Timestamp, sdlEvent.Window.WindowID);
                        break;

                    case SDL.EventType.WindowCloseRequested:
                        TryPublishWindowEvent(WindowEventType.CloseRequested, sdlEvent.Window.Timestamp, sdlEvent.Window.WindowID);
                        break;

                    case SDL.EventType.WindowResized:
                        TryPublishWindowEvent(
                            WindowEventType.Resized,
                            sdlEvent.Window.Timestamp,
                            sdlEvent.Window.WindowID,
                            sdlEvent.Window.Data1,
                            sdlEvent.Window.Data2);
                        break;

                    case SDL.EventType.WindowPixelSizeChanged:
                        TryPublishWindowEvent(
                            WindowEventType.PixelSizeChanged,
                            sdlEvent.Window.Timestamp,
                            sdlEvent.Window.WindowID,
                            sdlEvent.Window.Data1,
                            sdlEvent.Window.Data2);
                        break;

                    case SDL.EventType.WindowFocusGained:
                        TryPublishWindowEvent(WindowEventType.FocusGained, sdlEvent.Window.Timestamp, sdlEvent.Window.WindowID);
                        break;

                    case SDL.EventType.WindowFocusLost:
                        TryPublishWindowEvent(WindowEventType.FocusLost, sdlEvent.Window.Timestamp, sdlEvent.Window.WindowID);
                        break;

                    case SDL.EventType.KeyDown:
                        TryPublishInputEvent(InputEventType.KeyDown, sdlEvent.Key.Timestamp, sdlEvent.Key.Scancode);
                        break;

                    case SDL.EventType.KeyUp:
                        TryPublishInputEvent(InputEventType.KeyUp, sdlEvent.Key.Timestamp, sdlEvent.Key.Scancode);
                        break;

                    // TODO: добавить маппинг остальных SDL input-событий в _eventQueue.Input.TryPublish(...)
                }
            }

            if (got < BatchSize)
                break;
        }
    }

    public void EndFrame()
    {
        _eventQueue.SwapAll();
    }

    #endregion

    #region Private helpers

    private void TryPublishEngineQuitRequested()
    {
        if (!_eventQueue.Engine.TryPublish(new EngineEvent(EngineEventType.QuitRequested)))
            DroppedEngineEvents++;
    }

    private void TryPublishWindowEvent(
        WindowEventType type,
        ulong timestamp,
        uint windowId,
        int data1 = 0,
        int data2 = 0)
    {
        if (!_eventQueue.Window.TryPublish(new WindowEvent(type, timestamp, windowId, data1, data2)))
            DroppedWindowEvents++;
    }

    private void TryPublishInputEvent(InputEventType type, ulong timestamp, SDL.Scancode scancode)
    {
        if (!_eventQueue.Input.TryPublish(new InputEvent(type, timestamp, code: (KeyCode)scancode)))
            DroppedInputEvents++;
    }

    #endregion
}

#endregion
