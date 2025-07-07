using SDL3;

namespace Electron2D.Platform;

internal class EventSystem
{
    #region Application events
    /// <summary>
    /// User-requested quit
    /// </summary>
    public event Action? Quit;
    
    /// <summary>
    /// The application is being terminated by the OS. This event must be handled in a callback set with <see cref="SDL.AddEventWatch"/>.
    /// Called on iOS in applicationWillTerminate()
    /// Called on Android in onDestroy()
    /// </summary>
    public event Action? Terminating;
    
    /// <summary>
    /// The application is low on memory, free memory if possible. This event must be handled in a callback set with <see cref="SDL.AddEventWatch"/>.
    /// Called on iOS in applicationDidReceiveMemoryWarning()
    /// Called on Android in onTrimMemory()
    /// </summary>
    public event Action? LowMemory;
    
    /// <summary>
    /// The application is about to enter the background. This event must be handled in a callback set with <see cref="SDL.AddEventWatch"/>.
    /// Called on iOS in applicationWillResignActive()
    /// Called on Android in onPause()
    /// </summary>
    public event Action? WillEnterBackground;
    
    /// <summary>
    /// The application did enter the background and may not get CPU for some time. This event must be handled in a callback set with <see cref="SDL.AddEventWatch"/>.
    /// Called on iOS in applicationDidEnterBackground()
    /// Called on Android in onPause()
    /// </summary>
    public event Action? WillEnterForeground;
    
    /// <summary>
    /// The application is now interactive. This event must be handled in a callback set with <see cref="SDL.AddEventWatch"/>.
    /// Called on iOS in applicationDidBecomeActive()
    /// Called on Android in onResume()
    /// </summary>
    public event Action? DidEnterForeground;
    
    /// <summary>
    /// The user's locale preferences have changed.
    /// </summary>
    public event Action? LocaleChanged;
    
    /// <summary>
    /// The system theme changed
    /// </summary>
    public event Action? SystemThemeChanged;
    #endregion
    
    #region Display events
    /// <summary>
    /// Display orientation has changed to data1
    /// </summary>
    public event Action? DisplayOrientation;
    
    /// <summary>
    /// Display has been added to the system
    /// </summary>
    public event Action? DisplayAdded;
    
    /// <summary>
    /// Display has been removed from the system
    /// </summary>
    public event Action? DisplayRemoved;
    
    /// <summary>
    /// Display has changed position
    /// </summary>
    public event Action? DisplayMoved;
    
    /// <summary>
    /// Display has changed desktop mode
    /// </summary>
    public event Action? DisplayDesktopModeChanged;
    
    /// <summary>
    /// Display has changed current mode
    /// </summary>
    public event Action? DisplayCurrentModeChanged;
    
    /// <summary>
    /// Display has changed content scale
    /// </summary>
    public event Action? DisplayContentScaleChanged;
    #endregion
    
    #region Window events
    /// <summary>
    /// Window has been shown
    /// </summary>
    public event Action? WindowShown;
    
    /// <summary>
    /// Window has been hidden
    /// </summary>
    public event Action? WindowHidden;
    
    /// <summary>
    /// Window has been exposed and should be redrawn, and can be redrawn directly from event watchers for this event
    /// </summary>
    public event Action? WindowExposed;
    
    /// <summary>
    /// Window has been moved to data1, data2
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowMoved;
    
    /// <summary>
    /// Window has been moved to data1, data2
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowResized;

    /// <summary>
    /// The pixel size of the window has changed to data1xdata2
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowPixelSizeChanged;
    
    /// <summary>
    /// The pixel size of a Metal view associated with the window has changed
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowMetalViewResized;

    /// <summary>
    /// Window has been minimized
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowMinimized;
    
    /// <summary>
    /// Window has been maximized
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowMaximized;
    
    /// <summary>
    /// Window has been restored to normal size and position
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowRestored;
    
    /// <summary>
    /// Window has gained mouse focus
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowMouseEnter;
    
    /// <summary>
    /// Window has lost mouse focus
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowMouseLeave;
    
    /// <summary>
    /// Window has gained keyboard focus
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowFocusGained;
    
    /// <summary>
    /// Window has lost keyboard focus
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowFocusLost;
    
    /// <summary>
    /// The window manager requests that the window be closed
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowCloseRequested;

    /// <summary>
    /// Window had a hit test that wasn't <see cref="SDL.HitTestResult.Normal"/>
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowHitTest;
    
    /// <summary>
    /// The ICC profile of the window's display has changed
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowICCProfChanged;
    
    /// <summary>
    /// Window has been moved to display data1
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowDisplayChanged;
    
    /// <summary>
    /// Window display scale has been changed
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowDisplayScaleChanged;

    /// <summary>
    /// The window safe area has been changed
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowSafeAreaChanged;

    /// <summary>
    /// The window has been occluded
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowOccluded;

    /// <summary>
    /// The window has entered fullscreen mode
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowEnterFullscreen;

    /// <summary>
    /// The window has left fullscreen mode
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowLeaveFullscreen;

    /// <summary>
    /// The window with the associated ID is being or has been destroyed. If this message is being handled
    /// in an event watcher, the window handle is still valid and can still be used to retrieve any properties
    /// associated with the window. Otherwise, the handle has already been destroyed and all resources
    /// associated with it are invalid
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowDestroyed;

    /// <summary>
    /// Window HDR properties have changed
    /// </summary>
    public event Action<SDL.WindowEvent>? WindowHDRStateChanged;
    #endregion

    #region Keyboard events
    /// <summary>
    /// Key pressed
    /// </summary>
    public event Action<SDL.KeyboardEvent>? KeyDown;

    /// <summary>
    /// Key released
    /// </summary>
    public event Action<SDL.KeyboardEvent>? KeyUp;

    /// <summary>
    /// Keyboard text editing (composition)
    /// </summary>
    public event Action<SDL.KeyboardEvent>? TextEditing;

    /// <summary>
    /// Keyboard text input
    /// </summary>
    public event Action<SDL.KeyboardEvent>? TextInput;

    /// <summary>
    /// Keymap changed due to a system event such as an
    /// input language or keyboard layout change.
    /// </summary>
    public event Action<SDL.KeyboardEvent>? KeymapChanged;

    /// <summary>
    /// A new keyboard has been inserted into the system
    /// </summary>
    public event Action<SDL.KeyboardEvent>? KeyboardAdded;

    /// <summary>
    /// A keyboard has been removed
    /// </summary>
    public event Action<SDL.KeyboardEvent>? KeyboardRemoved;

    /// <summary>
    /// Keyboard text editing candidates
    /// </summary>
    public event Action<SDL.KeyboardEvent>? TextEditingCandidates;
    #endregion

    #region Mouse events

    /// <summary>
    /// Mouse moved
    /// </summary>
    public event Action<SDL.MouseMotionEvent>? MouseMotion;

    /// <summary>
    /// Mouse button pressed
    /// </summary>
    public event Action<SDL.MouseButtonEvent>? MouseButtonDown;

    /// <summary>
    /// Mouse button released
    /// </summary>
    public event Action<SDL.MouseButtonEvent>? MouseButtonUp;

    /// <summary>
    /// Mouse wheel motion
    /// </summary>
    public event Action<SDL.MouseWheelEvent>? MouseWheel;

    /// <summary>
    /// A new mouse has been inserted into the system
    /// </summary>
    public event Action<SDL.MouseDeviceEvent>? MouseAdded;

    /// <summary>
    /// A mouse has been removed
    /// </summary>
    public event Action<SDL.MouseDeviceEvent>? MouseRemoved;
    #endregion

    #region Joystick events

    /// <summary>
    /// Joystick axis motion
    /// </summary>
    public event Action<SDL.JoyAxisEvent>? JoystickAxisMotion;

    /// <summary>
    /// Joystick trackball motion
    /// </summary>
    public event Action<SDL.JoyBallEvent>? JoystickBallMotion;

    /// <summary>
    /// Joystick hat position change
    /// </summary>
    public event Action<SDL.JoyHatEvent>? JoystickHatMotion;

    /// <summary>
    /// Joystick button pressed
    /// </summary>
    public event Action<SDL.JoyButtonEvent>? JoystickButtonDown;

    /// <summary>
    /// Joystick button released
    /// </summary>
    public event Action<SDL.JoyButtonEvent>? JoystickButtonUp;

    /// <summary>
    /// A new joystick has been inserted into the system
    /// </summary>
    public event Action<SDL.JoyDeviceEvent>? JoystickAdded;

    /// <summary>
    /// An opened joystick has been removed
    /// </summary>
    public event Action<SDL.JoyDeviceEvent>? JoystickRemoved;

    /// <summary>
    /// Joystick battery level change
    /// </summary>
    public event Action<SDL.JoyBatteryEvent>? JoystickBatteryUpdated;

    /// <summary>
    /// Joystick update is complete
    /// </summary>
    public event Action<SDL.JoyDeviceEvent>? JoystickUpdateComplete;
    #endregion

    #region Gamepad events

    /// <summary>
    /// Gamepad axis motion
    /// </summary>
    public event Action<SDL.GamepadAxisEvent>? GamepadAxisMotion;

    /// <summary>
    /// Gamepad button pressed
    /// </summary>
    public event Action<SDL.GamepadButtonEvent>? GamepadButtonDown;

    /// <summary>
    /// Gamepad button released
    /// </summary>
    public event Action<SDL.GamepadButtonEvent>? GamepadButtonUp;

    /// <summary>
    /// A new gamepad has been inserted into the system
    /// </summary>
    public event Action<SDL.GamepadDeviceEvent>? GamepadAdded;

    /// <summary>
    /// A gamepad has been removed
    /// </summary>
    public event Action<SDL.GamepadDeviceEvent>? GamepadRemoved;

    /// <summary>
    /// The gamepad mapping was updated
    /// </summary>
    public event Action<SDL.GamepadDeviceEvent>? GamepadRemapped;

    /// <summary>
    /// Gamepad touchpad was touched
    /// </summary>
    public event Action<SDL.GamepadTouchpadEvent>? GamepadTouchpadDown;

    /// <summary>
    /// Gamepad touchpad finger was moved
    /// </summary>
    public event Action<SDL.GamepadTouchpadEvent>? GamepadTouchpadMotion;

    /// <summary>
    /// Gamepad touchpad finger was lifted
    /// </summary>
    public event Action<SDL.GamepadTouchpadEvent>? GamepadTouchpadUp;

    /// <summary>
    /// Gamepad sensor was updated
    /// </summary>
    public event Action<SDL.GamepadSensorEvent>? GamepadSensorUpdate;

    /// <summary>
    /// Gamepad update is complete
    /// </summary>
    public event Action<SDL.GamepadDeviceEvent>? GamepadUpdateComplete;

    /// <summary>
    /// Gamepad Steam handle has changed 
    /// </summary>
    public event Action<SDL.GamepadDeviceEvent>? GamepadSteamHandleUpdated;
    #endregion

    #region Touch events
    public event Action<SDL.TouchFingerEvent>? FingerDown;
    public event Action<SDL.TouchFingerEvent>? FingerUp;
    public event Action<SDL.TouchFingerEvent>? FingerMotion;
    public event Action<SDL.TouchFingerEvent>? FingerCanceled;
    #endregion

    #region Clipboard events

    /// <summary>
    /// The clipboard or primary selection changed
    /// </summary>
    public event Action<SDL.ClipboardEvent>? ClipboardUpdate;
    #endregion

    #region Drag and drop events

    /// <summary>
    /// The system requests a file open
    /// </summary>
    public event Action<SDL.DropEvent>? DropFile;

    /// <summary>
    /// text/plain drag-and-drop event
    /// </summary>
    public event Action<SDL.DropEvent>? DropText;

    /// <summary>
    /// A new set of drops is beginning (NULL filename)
    /// </summary>
    public event Action<SDL.DropEvent>? DropBegin;

    /// <summary>
    /// Current set of drops is now complete (NULL filename)
    /// </summary>
    public event Action<SDL.DropEvent>? DropComplete;

    /// <summary>
    /// Position while moving over the window
    /// </summary>
    public event Action<SDL.DropEvent>? DropPosition;
    #endregion

    #region Audio hotplug events

    /// <summary>
    /// A new audio device is available
    /// </summary>
    public event Action<SDL.AudioDeviceEvent>? AudioDeviceAdded;
    /// <summary>
    /// An audio device has been removed.
    /// </summary>
    public event Action<SDL.AudioDeviceEvent>? AudioDeviceRemoved;

    /// <summary>
    /// An audio device's format has been changed by the system.
    /// </summary>
    public event Action<SDL.AudioDeviceEvent>? AudioDeviceFormatChanged;
    #endregion

    #region Sensor events
    /// <summary>
    /// A sensor was updated
    /// </summary>
    public event Action<SDL.SensorEvent>? SensorUpdate;
    #endregion

    #region Pressure-sensitive pen events

    /// <summary>
    /// Pressure-sensitive pen has become available
    /// </summary>
    public event Action<SDL.PenProximityEvent>? PenProximityIn;

    /// <summary>
    /// Pressure-sensitive pen has become unavailable
    /// </summary>
    public event Action<SDL.PenProximityEvent>? PenProximityOut;

    /// <summary>
    /// Pressure-sensitive pen touched drawing surface
    /// </summary>
    public event Action<SDL.PenButtonEvent>? PenDown;

    /// <summary>
    /// Pressure-sensitive pen stopped touching drawing surface
    /// </summary>
    public event Action<SDL.PenButtonEvent>? PenUp;

    /// <summary>
    /// Pressure-sensitive pen button pressed
    /// </summary>
    public event Action<SDL.PenButtonEvent>? PenButtonDown;

    /// <summary>
    /// Pressure-sensitive pen button released
    /// </summary>
    public event Action<SDL.PenButtonEvent>? PenButtonUp;

    /// <summary>
    /// Pressure-sensitive pen is moving on the tablet
    /// </summary>
    public event Action<SDL.PenMotionEvent>? PenMotion;

    /// <summary>
    /// Pressure-sensitive pen angle/pressure/etc changed
    /// </summary>
    public event Action<SDL.PenAxisEvent>? PenAxis;
    #endregion

    #region Camera hotplug events
    /// <summary>
    /// A new camera device is available
    /// </summary>
    public event Action<SDL.CameraDeviceEvent>? CameraDeviceAdded;

    /// <summary>
    /// A camera device has been removed.
    /// </summary>
    public event Action<SDL.CameraDeviceEvent>? CameraDeviceRemoved;

    /// <summary>
    /// A camera device has been approved for use by the user.
    /// </summary>
    public event Action<SDL.CameraDeviceEvent>? CameraDeviceApproved;

    /// <summary>
    /// A camera device has been denied for use by the user.
    /// </summary>
    public event Action<SDL.CameraDeviceEvent>? CameraDeviceDenied;
    #endregion

    #region Render events
    /// <summary>
    /// The render targets have been reset and their contents need to be updated
    /// </summary>
    public event Action<SDL.RenderEvent>? RenderTargetsReset;

    /// <summary>
    /// The device has been reset and all textures need to be recreated
    /// </summary>
    public event Action<SDL.RenderEvent>? RenderDeviceReset;

    /// <summary>
    /// The device has been lost and can't be recovered.
    /// </summary>
    public event Action<SDL.RenderEvent>? RenderDeviceLost;
    #endregion

    #region Internal events
    /// <summary>
    /// Signals the end of an event poll cycle
    /// </summary>
    public event Action? PollSentinel;
    #endregion
    
    public void PollEvent()
    {
        while (SDL.PollEvent(out var e))
        {
            switch (e.Type)
            {
                case (uint)SDL.EventType.Quit:
                    Quit?.Invoke();
                    break;
                case (uint)SDL.EventType.Terminating:
                    Terminating?.Invoke();
                    break;
                case (uint)SDL.EventType.LowMemory:
                    LowMemory?.Invoke();
                    break;
                case (uint)SDL.EventType.WillEnterBackground:
                    WillEnterBackground?.Invoke();
                    break;
                case (uint)SDL.EventType.WillEnterForeground:
                    WillEnterForeground?.Invoke();
                    break;
                case (uint)SDL.EventType.DidEnterForeground:
                    DidEnterForeground?.Invoke();
                    break;
                case (uint)SDL.EventType.LocaleChanged:
                    LocaleChanged?.Invoke();
                    break;
                case (uint)SDL.EventType.SystemThemeChanged:
                    SystemThemeChanged?.Invoke();
                    break;
                case (uint)SDL.EventType.DisplayOrientation:
                    DisplayOrientation?.Invoke();
                    break;
                case (uint)SDL.EventType.DisplayAdded:
                    DisplayAdded?.Invoke();
                    break;
                case (uint)SDL.EventType.DisplayRemoved:
                    DisplayRemoved?.Invoke();
                    break;
                case (uint)SDL.EventType.DisplayMoved:
                    DisplayMoved?.Invoke();
                    break;
                case (uint)SDL.EventType.DisplayDesktopModeChanged:
                    DisplayDesktopModeChanged?.Invoke();
                    break;
                case (uint)SDL.EventType.DisplayCurrentModeChanged:
                    DisplayCurrentModeChanged?.Invoke();
                    break;
                case (uint)SDL.EventType.DisplayContentScaleChanged:
                    DisplayContentScaleChanged?.Invoke();
                    break;
                case (uint)SDL.EventType.WindowShown:
                    WindowShown?.Invoke();
                    break;
                case (uint)SDL.EventType.WindowHidden:
                    WindowHidden?.Invoke();
                    break;
                case (uint)SDL.EventType.WindowExposed:
                    WindowExposed?.Invoke();
                    break;
                case (uint)SDL.EventType.WindowMoved:
                    WindowMoved?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowResized:
                    WindowResized?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowPixelSizeChanged:
                    WindowPixelSizeChanged?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowMetalViewResized:
                    WindowMetalViewResized?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowMinimized:
                    WindowMinimized?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowMaximized:
                    WindowMaximized?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowRestored:
                    WindowRestored?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowMouseEnter:
                    WindowMouseEnter?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowMouseLeave:
                    WindowMouseLeave?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowFocusGained:
                    WindowFocusGained?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowFocusLost:
                    WindowFocusLost?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowCloseRequested:
                    WindowCloseRequested?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowHitTest:
                    WindowHitTest?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowICCProfChanged:
                    WindowICCProfChanged?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowDisplayChanged:
                    WindowDisplayChanged?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowDisplayScaleChanged:
                    WindowDisplayScaleChanged?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowSafeAreaChanged:
                    WindowSafeAreaChanged?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowOccluded:
                    WindowOccluded?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowEnterFullscreen:
                    WindowEnterFullscreen?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowLeaveFullscreen:
                    WindowLeaveFullscreen?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowDestroyed:
                    WindowDestroyed?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.WindowHDRStateChanged:
                    WindowHDRStateChanged?.Invoke(e.Window);
                    break;
                case (uint)SDL.EventType.KeyDown:
                    KeyDown?.Invoke(e.Key);
                    break;
                case (uint)SDL.EventType.KeyUp:
                    KeyUp?.Invoke(e.Key);
                    break;
                case (uint)SDL.EventType.TextEditing:
                    TextEditing?.Invoke(e.Key);
                    break;
                case (uint)SDL.EventType.TextInput:
                    TextInput?.Invoke(e.Key);
                    break;
                case (uint)SDL.EventType.KeymapChanged:
                    KeymapChanged?.Invoke(e.Key);
                    break;
                case (uint)SDL.EventType.KeyboardAdded:
                    KeyboardAdded?.Invoke(e.Key);
                    break;
                case (uint)SDL.EventType.KeyboardRemoved:
                    KeyboardRemoved?.Invoke(e.Key);
                    break;
                case (uint)SDL.EventType.TextEditingCandidates:
                    TextEditingCandidates?.Invoke(e.Key);
                    break;
                case (uint)SDL.EventType.MouseMotion:
                    MouseMotion?.Invoke(e.Motion);
                    break;
                case (uint)SDL.EventType.MouseButtonDown:
                    MouseButtonDown?.Invoke(e.Button);
                    break;
                case (uint)SDL.EventType.MouseButtonUp:
                    MouseButtonUp?.Invoke(e.Button);
                    break;
                case (uint)SDL.EventType.MouseWheel:
                    MouseWheel?.Invoke(e.Wheel);
                    break;
                case (uint)SDL.EventType.MouseAdded:
                    MouseAdded?.Invoke(e.MDevice);
                    break;
                case (uint)SDL.EventType.MouseRemoved:
                    MouseRemoved?.Invoke(e.MDevice);
                    break;
                case (uint)SDL.EventType.JoystickAxisMotion:
                    JoystickAxisMotion?.Invoke(e.JAxis);
                    break;
                case (uint)SDL.EventType.JoystickBallMotion:
                    JoystickBallMotion?.Invoke(e.JBall);
                    break;
                case (uint)SDL.EventType.JoystickHatMotion:
                    JoystickHatMotion?.Invoke(e.JHat);
                    break;
                case (uint)SDL.EventType.JoystickButtonDown:
                    JoystickButtonDown?.Invoke(e.JButton);
                    break;
                case (uint)SDL.EventType.JoystickButtonUp:
                    JoystickButtonUp?.Invoke(e.JButton);
                    break;
                case (uint)SDL.EventType.JoystickAdded:
                    JoystickAdded?.Invoke(e.JDevice);
                    break;
                case (uint)SDL.EventType.JoystickRemoved:
                    JoystickRemoved?.Invoke(e.JDevice);
                    break;
                case (uint)SDL.EventType.JoystickBatteryUpdated:
                    JoystickBatteryUpdated?.Invoke(e.JBattery);
                    break;
                case (uint)SDL.EventType.JoystickUpdateComplete:
                    JoystickUpdateComplete?.Invoke(e.JDevice);
                    break;
                case (uint)SDL.EventType.GamepadAxisMotion:
                    GamepadAxisMotion?.Invoke(e.GAxis);
                    break;
                case (uint)SDL.EventType.GamepadButtonDown:
                    GamepadButtonDown?.Invoke(e.GButton);
                    break;
                case (uint)SDL.EventType.GamepadButtonUp:
                    GamepadButtonUp?.Invoke(e.GButton);
                    break;
                case (uint)SDL.EventType.GamepadAdded:
                    GamepadAdded?.Invoke(e.GDevice);
                    break;
                case (uint)SDL.EventType.GamepadRemoved:
                    GamepadRemoved?.Invoke(e.GDevice);
                    break;
                case (uint)SDL.EventType.GamepadRemapped:
                    GamepadRemapped?.Invoke(e.GDevice);
                    break;
                case (uint)SDL.EventType.GamepadTouchpadDown:
                    GamepadTouchpadDown?.Invoke(e.GTouchpad);
                    break;
                case (uint)SDL.EventType.GamepadTouchpadMotion:
                    GamepadTouchpadMotion?.Invoke(e.GTouchpad);
                    break;
                case (uint)SDL.EventType.GamepadTouchpadUp:
                    GamepadTouchpadUp?.Invoke(e.GTouchpad);
                    break;
                case (uint)SDL.EventType.GamepadSensorUpdate:
                    GamepadSensorUpdate?.Invoke(e.GSensor);
                    break;
                case (uint)SDL.EventType.GamepadUpdateComplete:
                    GamepadUpdateComplete?.Invoke(e.GDevice);
                    break;
                case (uint)SDL.EventType.GamepadSteamHandleUpdated:
                    GamepadSteamHandleUpdated?.Invoke(e.GDevice);
                    break;
                case (uint)SDL.EventType.FingerDown:
                    FingerDown?.Invoke(e.TFinger);
                    break;
                case (uint)SDL.EventType.FingerUp:
                    FingerUp?.Invoke(e.TFinger);
                    break;
                case (uint)SDL.EventType.FingerMotion:
                    FingerMotion?.Invoke(e.TFinger);
                    break;
                case (uint)SDL.EventType.FingerCanceled:
                    FingerCanceled?.Invoke(e.TFinger);
                    break;
                case (uint)SDL.EventType.ClipboardUpdate:
                    ClipboardUpdate?.Invoke(e.Clipboard);
                    break;
                case (uint)SDL.EventType.DropFile:
                    DropFile?.Invoke(e.Drop);
                    break;
                case (uint)SDL.EventType.DropText:
                    DropText?.Invoke(e.Drop);
                    break;
                case (uint)SDL.EventType.DropBegin:
                    DropBegin?.Invoke(e.Drop);
                    break;
                case (uint)SDL.EventType.DropComplete:
                    DropComplete?.Invoke(e.Drop);
                    break;
                case (uint)SDL.EventType.DropPosition:
                    DropPosition?.Invoke(e.Drop);
                    break;
                case (uint)SDL.EventType.AudioDeviceAdded:
                    AudioDeviceAdded?.Invoke(e.ADevice);
                    break;
                case (uint)SDL.EventType.AudioDeviceRemoved:
                    AudioDeviceRemoved?.Invoke(e.ADevice);
                    break;
                case (uint)SDL.EventType.AudioDeviceFormatChanged:
                    AudioDeviceFormatChanged?.Invoke(e.ADevice);
                    break;
                case (uint)SDL.EventType.SensorUpdate:
                    SensorUpdate?.Invoke(e.Sensor);
                    break;
                case (uint)SDL.EventType.PenProximityIn:
                    PenProximityIn?.Invoke(e.PProximity);
                    break;
                case (uint)SDL.EventType.PenProximityOut:
                    PenProximityOut?.Invoke(e.PProximity);
                    break;
                case (uint)SDL.EventType.PenDown:
                    PenDown?.Invoke(e.PButton);
                    break;
                case (uint)SDL.EventType.PenUp:
                    PenUp?.Invoke(e.PButton);
                    break;
                case (uint)SDL.EventType.PenButtonDown:
                    PenButtonDown?.Invoke(e.PButton);
                    break;
                case (uint)SDL.EventType.PenButtonUp:
                    PenButtonUp?.Invoke(e.PButton);
                    break;
                case (uint)SDL.EventType.PenMotion:
                    PenMotion?.Invoke(e.PMotion);
                    break;
                case (uint)SDL.EventType.PenAxis:
                    PenAxis?.Invoke(e.PAxis);
                    break;
                case (uint)SDL.EventType.CameraDeviceAdded:
                    CameraDeviceAdded?.Invoke(e.CDevice);
                    break;
                case (uint)SDL.EventType.CameraDeviceRemoved:
                    CameraDeviceRemoved?.Invoke(e.CDevice);
                    break;
                case (uint)SDL.EventType.CameraDeviceApproved:
                    CameraDeviceApproved?.Invoke(e.CDevice);
                    break;
                case (uint)SDL.EventType.CameraDeviceDenied:
                    CameraDeviceDenied?.Invoke(e.CDevice);
                    break;
                case (uint)SDL.EventType.RenderTargetsReset:
                    RenderTargetsReset?.Invoke(e.Render);
                    break;
                case (uint)SDL.EventType.RenderDeviceReset:
                    RenderDeviceReset?.Invoke(e.Render);
                    break;
                case (uint)SDL.EventType.RenderDeviceLost:
                    RenderDeviceLost?.Invoke(e.Render);
                    break;
                case (uint)SDL.EventType.PollSentinel:
                    PollSentinel?.Invoke();
                    break;
            }
        }
    }
}