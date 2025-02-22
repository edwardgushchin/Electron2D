using Electron2D.Input;
using SDL3;

namespace Electron2D;

internal class EventManager
{
    public event Action? WindowShown;
    public event Action? WindowHidden;
    public event Action<SDL.WindowEvent>? WindowMoved;
    public event Action<SDL.WindowEvent>? WindowResized;
    public event Action? WindowMinimized;
    public event Action? WindowMaximized;
    public event Action? WindowRestored;
    public event Action? WindowFocusGained;
    public event Action? WindowFocusLost;
    public event Action? WindowCloseRequested;
    
    public event Action<SDL.KeyboardEvent>? KeyDown;
    public event Action<SDL.KeyboardEvent>? KeyUp;
    
    public event Action<SDL.TextEditingEvent>? TextEditing;
    public event Action<SDL.TextInputEvent>? TextInput;
    
    public event Action<SDL.MouseMotionEvent>? MouseMotion;
    public event Action<SDL.MouseButtonEvent>? MouseButtonDown;
    public event Action<SDL.MouseButtonEvent>? MouseButtonUp;
    public event Action<SDL.MouseWheelEvent>? MouseWheel;
    
    public event Action<SDL.GamepadAxisEvent>? GamepadAxisMotion;
    public event Action<SDL.GamepadButtonEvent>? GamepadButtonDown;
    public event Action<SDL.GamepadButtonEvent>? GamepadButtonUp;
    public event Action<SDL.GamepadTouchpadEvent>? GamepadTouchpadDown;
    public event Action<SDL.GamepadTouchpadEvent>? GamepadTouchpadMotion;
    public event Action<SDL.GamepadTouchpadEvent>? GamepadTouchpadUp;
    public event Action<SDL.GamepadSensorEvent>? GamepadSensorUpdate;
    
    public event Action<SDL.TouchFingerEvent>? FingerDown;
    public event Action<SDL.TouchFingerEvent>? FingerUp;
    public event Action<SDL.TouchFingerEvent>? FingerMotion;
    public event Action<SDL.TouchFingerEvent>? FingerCanceled;
    
    public event Action<SDL.SensorEvent>? SensorUpdate;
    
    public event Action? Quit;

    private const SDL.InitFlags InitSubsystems = SDL.InitFlags.Events | SDL.InitFlags.Gamepad | SDL.InitFlags.Sensor;

    public void Initialize()
    {
        Logger.Info("Initializing event manager...");
        
        if (!SDL.InitSubSystem(InitSubsystems))
        {
            throw new ElectronException($"Failed to initialize SDL: {SDL.GetError()}");
        }
        
        Logger.Info("The event manager has been initialized successfully.");
    }

    public void Update()
    {
        while (SDL.PollEvent(out var e))
        {
            switch ((SDL.EventType) e.Type)
            {
                case SDL.EventType.Quit:
                    Quit?.Invoke();
                    break;
                case SDL.EventType.Terminating:
                    break;
                case SDL.EventType.LowMemory:
                    break;
                case SDL.EventType.WillEnterBackground:
                    break;
                case SDL.EventType.DidEnterBackground:
                    break;
                case SDL.EventType.WillEnterForeground:
                    break;
                case SDL.EventType.DidEnterForeground:
                    break;
                case SDL.EventType.LocaleChanged:
                    break;
                case SDL.EventType.SystemThemeChanged:
                    break;
                case SDL.EventType.DisplayOrientation:
                    break;
                case SDL.EventType.DisplayAdded:
                    break;
                case SDL.EventType.DisplayRemoved:
                    break;
                case SDL.EventType.DisplayMoved:
                    break;
                case SDL.EventType.DisplayDesktopModeChanged:
                    break;
                case SDL.EventType.DisplayCurrentModeChanged:
                    break;
                case SDL.EventType.DisplayContentScaleChanged:
                    break;
                case SDL.EventType.WindowShown:
                    WindowShown?.Invoke();
                    break;
                case SDL.EventType.WindowHidden:
                    WindowHidden?.Invoke();
                    break;
                case SDL.EventType.WindowExposed:
                    break;
                case SDL.EventType.WindowMoved:
                    WindowMoved?.Invoke(e.Window);
                    break;
                case SDL.EventType.WindowResized:
                    WindowResized?.Invoke(e.Window);
                    break;
                case SDL.EventType.WindowPixelSizeChanged:
                    break;
                case SDL.EventType.WindowMetalViewResized:
                    break;
                case SDL.EventType.WindowMinimized:
                    WindowMinimized?.Invoke();
                    break;
                case SDL.EventType.WindowMaximized:
                    WindowMaximized?.Invoke();
                    break;
                case SDL.EventType.WindowRestored:
                    WindowRestored?.Invoke();
                    break;
                case SDL.EventType.WindowMouseEnter:
                    break;
                case SDL.EventType.WindowMouseLeave:
                    break;
                case SDL.EventType.WindowFocusGained:
                    WindowFocusGained?.Invoke();
                    break;
                case SDL.EventType.WindowFocusLost:
                    WindowFocusLost?.Invoke();
                    break;
                case SDL.EventType.WindowCloseRequested:
                    WindowCloseRequested?.Invoke();
                    break;
                case SDL.EventType.WindowHitTest:
                    break;
                case SDL.EventType.WindowICCProfChanged:
                    break;
                case SDL.EventType.WindowDisplayChanged:
                    break;
                case SDL.EventType.WindowDisplayScaleChanged:
                    break;
                case SDL.EventType.WindowSafeAreaChanged:
                    break;
                case SDL.EventType.WindowOccluded:
                    break;
                case SDL.EventType.WindowEnterFullscreen:
                    break;
                case SDL.EventType.WindowLeaveFullscreen:
                    break;
                case SDL.EventType.WindowDestroyed:
                    break;
                case SDL.EventType.WindowHDRStateChanged:
                    break;
                case SDL.EventType.KeyDown:
                    KeyDown?.Invoke(e.Key);
                    break;
                case SDL.EventType.KeyUp:
                    KeyUp?.Invoke(e.Key);
                    break;
                case SDL.EventType.TextEditing:
                    TextEditing?.Invoke(e.Edit);
                    break;
                case SDL.EventType.TextInput:
                    TextInput?.Invoke(e.Text);
                    break;
                case SDL.EventType.KeymapChanged:
                    break;
                case SDL.EventType.KeyboardAdded:
                    break;
                case SDL.EventType.KeyboardRemoved:
                    break;
                case SDL.EventType.TextEditingCandidates:
                    break;
                case SDL.EventType.MouseMotion:
                    MouseMotion?.Invoke(e.Motion);
                    break;
                case SDL.EventType.MouseButtonDown:
                    MouseButtonDown?.Invoke(e.Button);
                    break;
                case SDL.EventType.MouseButtonUp:
                    MouseButtonUp?.Invoke(e.Button);
                    break;
                case SDL.EventType.MouseWheel:
                    MouseWheel?.Invoke(e.Wheel);
                    break;
                case SDL.EventType.MouseAdded:
                    break;
                case SDL.EventType.MouseRemoved:
                    break;
                case SDL.EventType.JoystickAxisMotion:
                    break;
                case SDL.EventType.JoystickBallMotion:
                    break;
                case SDL.EventType.JoystickHatMotion:
                    break;
                case SDL.EventType.JoystickButtonDown:
                    break;
                case SDL.EventType.JoystickButtonUp:
                    break;
                case SDL.EventType.JoystickAdded:
                    break;
                case SDL.EventType.JoystickRemoved:
                    break;
                case SDL.EventType.JoystickBatteryUpdated:
                    break;
                case SDL.EventType.JoystickUpdateComplete:
                    break;
                case SDL.EventType.GamepadAxisMotion:
                    GamepadAxisMotion?.Invoke(e.GAxis);
                    break;
                case SDL.EventType.GamepadButtonDown:
                    GamepadButtonDown?.Invoke(e.GButton);
                    break;
                case SDL.EventType.GamepadButtonUp:
                    GamepadButtonUp?.Invoke(e.GButton);
                    break;
                case SDL.EventType.GamepadAdded:
                    Gamepad.Add(e.GDevice.Which);
                    break;
                case SDL.EventType.GamepadRemoved:
                    Gamepad.Remove(e.GDevice.Which);
                    break;
                case SDL.EventType.GamepadRemapped:
                    break;
                case SDL.EventType.GamepadTouchpadDown:
                    GamepadTouchpadDown?.Invoke(e.GTouchpad);
                    break;
                case SDL.EventType.GamepadTouchpadMotion:
                    GamepadTouchpadMotion?.Invoke(e.GTouchpad);
                    break;
                case SDL.EventType.GamepadTouchpadUp:
                    GamepadTouchpadUp?.Invoke(e.GTouchpad);
                    break;
                case SDL.EventType.GamepadSensorUpdate:
                    GamepadSensorUpdate?.Invoke(e.GSensor);
                    break;
                case SDL.EventType.GamepadUpdateComplete:
                    break;
                case SDL.EventType.GamepadSteamHandleUpdated:
                    break;
                case SDL.EventType.FingerDown:
                    FingerDown?.Invoke(e.TFinger);
                    break;
                case SDL.EventType.FingerUp:
                    FingerUp?.Invoke(e.TFinger);
                    break;
                case SDL.EventType.FingerMotion:
                    FingerMotion?.Invoke(e.TFinger);
                    break;
                case SDL.EventType.FingerCanceled:
                    FingerCanceled?.Invoke(e.TFinger);
                    break;
                case SDL.EventType.ClipboardUpdate:
                    break;
                case SDL.EventType.DropFile:
                    break;
                case SDL.EventType.DropText:
                    break;
                case SDL.EventType.DropBegin:
                    break;
                case SDL.EventType.DropComplete:
                    break;
                case SDL.EventType.DropPosition:
                    break;
                case SDL.EventType.AudioDeviceAdded:
                    break;
                case SDL.EventType.AudioDeviceRemoved:
                    break;
                case SDL.EventType.AudioDeviceFormatChanged:
                    break;
                case SDL.EventType.SensorUpdate:
                    SensorUpdate?.Invoke(e.Sensor);
                    break;
                case SDL.EventType.PenProximityIn:
                    break;
                case SDL.EventType.PenProximityOut:
                    break;
                case SDL.EventType.PenDown:
                    break;
                case SDL.EventType.PenUp:
                    break;
                case SDL.EventType.PenButtonDown:
                    break;
                case SDL.EventType.PenButtonUp:
                    break;
                case SDL.EventType.PenMotion:
                    break;
                case SDL.EventType.PenAxis:
                    break;
                case SDL.EventType.CameraDeviceAdded:
                    break;
                case SDL.EventType.CameraDeviceRemoved:
                    break;
                case SDL.EventType.CameraDeviceApproved:
                    break;
                case SDL.EventType.CameraDeviceDenied:
                    break;
                case SDL.EventType.RenderTargetsReset:
                    break;
                case SDL.EventType.RenderDeviceReset:
                    break;
                case SDL.EventType.RenderDeviceLost:
                    break;
                case SDL.EventType.PollSentinel:
                    break;
                case SDL.EventType.User:
                    break;
            }
        }
    }

    public void Shutdown()
    {
        SDL.QuitSubSystem(InitSubsystems);
        Logger.Info("The event manager has been successfully shutdown.");
    }
}