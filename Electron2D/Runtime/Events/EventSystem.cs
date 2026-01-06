using System;
using System.Numerics;
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
    private nint _rendererHandleForCoordinates;
    
    private float _wheelX;
    private float _wheelY;

    #endregion

    #region Properties

    /// <summary>True, если в текущем кадре получен запрос завершения.</summary>
    public bool QuitRequested => _quitRequested;

    /// <summary>Очереди событий, заполненные за текущий кадр (после <see cref="EndFrame"/> — в read-буфере).</summary>
    public EventQueue Events => _eventQueue;
    
    public Vector2 MouseWheelDelta => new(_wheelX, _wheelY);

    /// <summary>Сколько событий движка было отброшено из-за переполнения канала.</summary>
    public int DroppedEngineEvents { get; private set; }

    /// <summary>Сколько событий окна было отброшено из-за переполнения канала.</summary>
    public int DroppedWindowEvents { get; private set; }

    /// <summary>Сколько событий ввода было отброшено из-за переполнения канала.</summary>
    public int DroppedKeyboardEvents { get; private set; }
    
    public int DroppedMouseEvents { get; private set; }

    #endregion

    #region Public API

    public void Initialize(EngineConfig config, nint rendererHandleForCoordinates)
    {
        if (!SDL.InitSubSystem(SDL.InitFlags.Events))
            throw new InvalidOperationException($"SDL.InitSubSystem(Events) failed. {SDL.GetError()}");

        _rendererHandleForCoordinates = rendererHandleForCoordinates;

        _eventQueue = new EventQueue(
            config.EngineEventsPerFrame,
            config.WindowEventsPerFrame,
            config.KeyboardEventsPerFrame,
            config.MouseEventsPerFrame);

        var maxEventsThisFrame = config.EngineEventsPerFrame + config.WindowEventsPerFrame + config.KeyboardEventsPerFrame;
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
        
        _wheelX = 0;
        _wheelY = 0;

        SDL.PumpEvents();

        DroppedEngineEvents = 0;
        DroppedWindowEvents = 0;
        DroppedKeyboardEvents = 0;
        DroppedMouseEvents = 0;


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
                // ВАЖНО: делаем копию, чтобы можно было модифицировать через ConvertEventToRenderCoordinates
                var sdlEvent = buffer[i];
                var type = (SDL.EventType)sdlEvent.Type;

                if (_rendererHandleForCoordinates != 0 && NeedsCoordinateConversion(type))
                {
                    // Конвертирует mouse/touch/etc в координаты рендера (логические), учитывая viewport/letterbox/integer-scale.
                    // Возвращает false при ошибке, тогда оставляем исходные координаты.
                    SDL.ConvertEventToRenderCoordinates(_rendererHandleForCoordinates, ref sdlEvent);
                }

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
                        TryPublishKeyboardEvent(KeyboardEventType.KeyDown, sdlEvent.Key.Timestamp, (KeyCode)sdlEvent.Key.Scancode);
                        break;

                    case SDL.EventType.KeyUp:
                        TryPublishKeyboardEvent(KeyboardEventType.KeyUp, sdlEvent.Key.Timestamp, (KeyCode)sdlEvent.Key.Scancode);
                        break;

                    // ---- Mouse (уже в render coordinates, если logical presentation включена)
                    case SDL.EventType.MouseMotion:
                        TryPublishMouseMotion(sdlEvent.Motion.Timestamp, sdlEvent.Motion.X, sdlEvent.Motion.Y);
                        break;

                    case SDL.EventType.MouseButtonDown:
                        TryPublishMouseButton(MouseEventType.MouseButtonDown,
                            sdlEvent.Button.Timestamp,
                            ToMouseButtonMask(sdlEvent.Button.Button),
                            sdlEvent.Button.X,
                            sdlEvent.Button.Y);
                        break;

                    case SDL.EventType.MouseButtonUp:
                        TryPublishMouseButton(MouseEventType.MouseButtonUp,
                            sdlEvent.Button.Timestamp,
                            ToMouseButtonMask(sdlEvent.Button.Button),
                            sdlEvent.Button.X,
                            sdlEvent.Button.Y);
                        break;

                    case SDL.EventType.MouseWheel:
                    {
                        TryPublishMouseWheel(sdlEvent.Wheel.Timestamp, sdlEvent.Wheel.Direction, sdlEvent.Wheel.X, sdlEvent.Wheel.Y);
                        break;
                    }
                    // TODO: остальной input маппинг
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
    
    private static bool NeedsCoordinateConversion(SDL.EventType type)
    {
        // Сюда добавляйте все event types, у которых есть позиция в окне (mouse/touch/pen/drop...).
        return type is SDL.EventType.MouseMotion
            or SDL.EventType.MouseButtonDown
            or SDL.EventType.MouseButtonUp;
        // MouseWheel обычно не нуждается, т.к. X/Y — это scroll delta, но если у вас wheel хранит mouseX/mouseY — добавьте.
    }

    private void TryPublishMouseMotion(ulong timestamp, float x, float y)
    { 
        if (!_eventQueue.Mouse.TryPublish(new MouseEvent(MouseEventType.MouseMotion, timestamp, 0, x, y)))
            DroppedMouseEvents++;
    }

    private void TryPublishMouseButton(MouseEventType type, ulong timestamp, MouseButton button, float x, float y)
    {
        //var code = ToMouseButtonKeyCode(button);
        //TryPublishInputEvent(type, timestamp, 0, x, y);
        if (!_eventQueue.Mouse.TryPublish(new MouseEvent(type, timestamp, button, x, y)))
            DroppedMouseEvents++;
    }

    private void TryPublishMouseWheel(ulong timestamp, SDL.MouseWheelDirection direction, float scrollX, float scrollY)
    {
        if (direction == SDL.MouseWheelDirection.Flipped)
        {
            scrollX = -scrollX;
            scrollY = -scrollY;
        }

        _wheelX += scrollX;
        _wheelY += scrollY;
        
        if (!_eventQueue.Mouse.TryPublish(new MouseEvent(MouseEventType.MouseWheel, timestamp, 0, scrollX, scrollY)))
            DroppedMouseEvents++;
    }

    private void TryPublishKeyboardEvent(KeyboardEventType type, ulong timestamp, KeyCode code)
    {
        if (!_eventQueue.Keyboard.TryPublish(new KeyboardEvent(type, timestamp, code)))
            DroppedKeyboardEvents++;
    }

    private static MouseButton ToMouseButtonMask(byte sdlButton) => sdlButton switch
    {
        1 => MouseButton.Left,
        2 => MouseButton.Middle,
        3 => MouseButton.Right,
        4 => MouseButton.X1,
        5 => MouseButton.X2,
        _ => MouseButton.None
    };

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

    #endregion
}

#endregion
