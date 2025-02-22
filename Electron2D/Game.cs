using System.Reflection;
using System.Runtime.InteropServices;
using SDL3;
using Electron2D.Input;

namespace Electron2D;

public class Game
{
    private bool _isRunning;

    // Подсистемы движка
    private readonly WindowManager _windowManager;
    private readonly Renderer _renderer;
    private readonly EventManager _eventManager;
    private readonly SceneManager _sceneManager;
    
    public Game(string title)
    {
        Logger.Info($"Electron2D Game Engine version {Assembly.GetEntryAssembly()!.GetName().Version}");
        
        var settings = new Settings
        {
            Width = 800,
            Height = 600,
            Fullscreen = FullscreenMode.Disabled,
            Resizable = false,
            VSync = VSyncMode.Disabled
        };
        
        PrintDebugInformation(ref settings);
        
        _windowManager = new WindowManager(title, ref settings);
        _renderer = new Renderer(_windowManager, ref settings);
        _eventManager = new EventManager();
        _sceneManager = new SceneManager();
    }
    
    public Game(string title, ref Settings settings) 
    {
        Logger.Info($"Electron2D Game Engine version {Assembly.GetEntryAssembly()!.GetName().Version}");
        
        PrintDebugInformation(ref settings);
        
        _windowManager = new WindowManager(title, ref settings);
        _renderer = new Renderer(_windowManager, ref settings);
        _eventManager = new EventManager();
        _sceneManager = new SceneManager();
    }

    private void PrintDebugInformation(ref Settings settings)
    {
        Logger.Debug($"Platform info: " +
                     $"OS Type: {PlatformInfo.OSType}, " +
                     $"OS Version: {PlatformInfo.OSVersion}, " +
                     $"Machine Name: {PlatformInfo.MachineName}, " +
                     $"CPU Name: {PlatformInfo.CPUName}, " +
                     $"CPU Architecture: {PlatformInfo.CPUArchitecture}, " +
                     $"CPU Cores {PlatformInfo.CPUCores}, " +
                     $"GPU Name: {PlatformInfo.GPUName}, " +
                     $"GPU Driver: {PlatformInfo.GPUDriver}, " +
                     $"RAM Size: {PlatformInfo.SystemRAM}KB, " +
                     $"User: {PlatformInfo.UserName}, " +
                     $"Current Directory: {PlatformInfo.CurrentDirectory}");
        
        Logger.Debug($"Settings: " +
                     $"Fullscreen: {settings.Fullscreen}, " +
                     $"Resizable: {settings.Resizable}, " +
                     $"VSync: {settings.VSync}, " +
                     $"Size: {settings.Width}x{settings.Height}");
    }
    

    public void AddScene(Scene scene, string name)
    {
        _sceneManager.AddScene(scene, name);
    }

    public void LoadScene(string name)
    {
        _sceneManager.LoadScene(name);
    }

    public void Play()
    {
        Logger.Info("Game engine strting...");
        
        Initialize();
        
        _isRunning = true;
        
        _eventManager.Update();
        
        Keyboard.Update();
        
        _sceneManager.ActiveScene?.OnStart();
        
        _sceneManager.ActiveScene?.OnLoad();

        var lastTime = SDL.GetPerformanceCounter();
        var frequency = SDL.GetPerformanceFrequency();
        
        Logger.Info("The main loop has started.");

        while (_isRunning)
        {
            var currentTime = SDL.GetPerformanceCounter();
            var deltaTime = (currentTime - lastTime) / (float)frequency;
        
            lastTime = currentTime;
            
            Update(deltaTime);
            
            Render();
        }
        
        Logger.Info("The main loop has completed its work.");

        Shutdown();
    }

    
    public void Stop()
    {
        _isRunning = false;
        
        Logger.Info("The game engine is shutting down...");
    }

    
    private void Initialize()
    { 
        _windowManager.Initialize();
        _renderer.Initialize();
        _eventManager.Initialize();
        _sceneManager.Initialize();
        
        SubscribedEvents();
    }

    
    private void Update(float deltaTime)
    {
        _eventManager.Update();
        
        Keyboard.Update();
            
        Mouse.Update();

        _sceneManager.ActiveScene?.Update(deltaTime);
    }

    private void Render()
    {
        _renderer.Clear();
        
        _sceneManager.ActiveScene?.Render();
        
        _renderer.Present();
    }

    private void Shutdown()
    { 
        UnsubscribedEvents();
        
        _sceneManager.Shutdown();
        _eventManager.Shutdown();
        _renderer.Shutdown();
        _windowManager.Shutdown();
        
        SDL.Quit();
    }

    private void SubscribedEvents()
    {
        Logger.Info("Subscribe to events...");
        
        _eventManager.WindowShown += OnWindowShown;
        _eventManager.WindowHidden += OnWindowHidden;
        _eventManager.WindowMoved += OnWindowMoved;
        _eventManager.WindowResized += OnWindowResized;
        _eventManager.WindowMinimized += OnWindowMinimized;
        _eventManager.WindowMaximized += OnWindowMaximized;
        _eventManager.WindowRestored += OnWindowRestored;
        _eventManager.WindowFocusGained += OnWindowFocusGained;
        _eventManager.WindowFocusLost += OnWindowFocusLost;
        _eventManager.WindowCloseRequested += OnWindowCloseRequested;
        
        _eventManager.KeyDown += OnKeyDown;
        _eventManager.KeyUp += OnKeyUp;
        
        _eventManager.TextEditing += OnTextEditing;
        _eventManager.TextInput += OnTextInput;

        _eventManager.MouseMotion += OnMouseMotion;
        _eventManager.MouseButtonDown += OnMouseButtonDown;
        _eventManager.MouseButtonUp += OnMouseButtonUp;
        _eventManager.MouseWheel += OnMouseWheel;

        _eventManager.GamepadAxisMotion += OnGamepadAxisMotion;
        _eventManager.GamepadButtonDown += OnGamepadButtonDown;
        _eventManager.GamepadButtonUp += OnGamepadButtonUp;
        _eventManager.GamepadTouchpadDown += OnGamepadTouchpadDown;
        _eventManager.GamepadTouchpadMotion += OnGamepadTouchpadMotion;
        _eventManager.GamepadTouchpadUp += OnGamepadTouchpadUp;
        _eventManager.GamepadSensorUpdate += OnGamepadSensorUpdate;
        
        _eventManager.FingerDown += OnFingerDown;
        _eventManager.FingerUp += OnFingerUp;
        _eventManager.FingerMotion += OnFingerMotion;
        _eventManager.FingerCanceled += OnFingerCanceled;

        _eventManager.SensorUpdate += OnSensorUpdate;
        
        _eventManager.Quit += OnQuit;
        
        Logger.Info("Subscription to events was successful.");
    }

    private void UnsubscribedEvents()
    {
        Logger.Info("Unsubscribing from events...");
        
        _eventManager.WindowShown -= OnWindowShown;
        _eventManager.WindowHidden -= OnWindowHidden;
        _eventManager.WindowMoved -= OnWindowMoved;
        _eventManager.WindowResized -= OnWindowResized;
        _eventManager.WindowMinimized -= OnWindowMinimized;
        _eventManager.WindowMaximized -= OnWindowMaximized;
        _eventManager.WindowRestored -= OnWindowRestored;
        _eventManager.WindowFocusGained -= OnWindowFocusGained;
        _eventManager.WindowFocusLost -= OnWindowFocusLost;
        _eventManager.WindowCloseRequested -= OnWindowCloseRequested;
        
        _eventManager.KeyDown -= OnKeyDown;
        _eventManager.KeyUp -= OnKeyUp;
        
        _eventManager.TextEditing -= OnTextEditing;
        _eventManager.TextInput -= OnTextInput;

        _eventManager.MouseMotion -= OnMouseMotion;
        _eventManager.MouseButtonDown -= OnMouseButtonDown;
        _eventManager.MouseButtonUp -= OnMouseButtonUp;
        _eventManager.MouseWheel -= OnMouseWheel;

        _eventManager.GamepadAxisMotion -= OnGamepadAxisMotion;
        _eventManager.GamepadButtonDown -= OnGamepadButtonDown;
        _eventManager.GamepadButtonUp -= OnGamepadButtonUp;
        _eventManager.GamepadTouchpadDown -= OnGamepadTouchpadDown;
        _eventManager.GamepadTouchpadMotion -= OnGamepadTouchpadMotion;
        _eventManager.GamepadTouchpadUp -= OnGamepadTouchpadUp;
        _eventManager.GamepadSensorUpdate -= OnGamepadSensorUpdate;

        _eventManager.FingerDown -= OnFingerDown;
        _eventManager.FingerUp -= OnFingerUp;
        _eventManager.FingerMotion -= OnFingerMotion;
        _eventManager.FingerCanceled -= OnFingerCanceled;

        _eventManager.SensorUpdate -= OnSensorUpdate;
        
        _eventManager.Quit -= OnQuit;
        
        Logger.Info("Unsubscribing from events was successful.");
    }

    #region Window events
    
    private void OnWindowShown()
    {
        _sceneManager.ActiveScene?.OnWindowShown();
    }

    private void OnWindowHidden()
    {
        _sceneManager.ActiveScene?.OnWindowHidden();
    }

    private void OnWindowMoved(SDL.WindowEvent e)
    {
        _sceneManager.ActiveScene?.OnWindowMoved(e.Data1, e.Data2);
    }
    
    private void OnWindowResized(SDL.WindowEvent e)
    {
        _sceneManager.ActiveScene?.OnWindowResized(e.Data1, e.Data2);
    }

    private void OnWindowMinimized()
    {
        _sceneManager.ActiveScene?.OnWindowMinimized();
    }
    
    private void OnWindowMaximized()
    {
        _sceneManager.ActiveScene?.OnWindowMaximized();
    }

    private void OnWindowRestored()
    {
        _sceneManager.ActiveScene?.OnWindowRestored();
    }
    
    private void OnWindowFocusGained()
    {
        _sceneManager.ActiveScene?.OnWindowFocusGained();
    }
    
    private void OnWindowFocusLost()
    {
        _sceneManager.ActiveScene?.OnWindowFocusLost();
    }
    
    private void OnWindowCloseRequested()
    {
        _sceneManager.ActiveScene?.OnWindowCloseRequested();
    }
    
    #endregion
    
    #region Keyboard events

    private void OnKeyDown(SDL.KeyboardEvent e)
    {
        _sceneManager.ActiveScene?.OnKeyDown(e.Which, (Keycode)e.Key, (Keymod)e.Mod, e.Repeat);
    }
    
    private void OnKeyUp(SDL.KeyboardEvent e)
    {
        _sceneManager.ActiveScene?.OnKeyUp(e.Which, (Keycode)e.Key, (Keymod)e.Mod, e.Repeat);
    }
    
    #endregion
    
    #region Text events
    
    private void OnTextEditing(SDL.TextEditingEvent e)
    {
        _sceneManager.ActiveScene?.OnTextEditing(Marshal.PtrToStringUTF8(e.Text) ?? "", e.Start, e.Length);
    }

    private void OnTextInput(SDL.TextInputEvent e)
    {
        _sceneManager.ActiveScene?.OnTextInput(Marshal.PtrToStringUTF8(e.Text) ?? "");
    }
    
    #endregion

    #region Mouse events

    private void OnMouseMotion(SDL.MouseMotionEvent e)
    {
        _sceneManager.ActiveScene?.OnMouseMotion(e.Which, (MouseButtonFlags)e.State, e.X, e.Y, e.XRel, e.YRel);
    }
    
    private void OnMouseButtonDown(SDL.MouseButtonEvent e)
    {
        _sceneManager.ActiveScene?.OnMouseButtonDown(e.Which, (MouseButton)e.Button, e.Clicks, e.X, e.Y);
    }
    
    private void OnMouseButtonUp(SDL.MouseButtonEvent e)
    {
        _sceneManager.ActiveScene?.OnMouseButtonUp(e.Which, (MouseButton)e.Button, e.Clicks, e.X, e.Y);
    }

    private void OnMouseWheel(SDL.MouseWheelEvent e)
    {
        _sceneManager.ActiveScene?.OnMouseWheel(e.Which, e.X, e.Y, (MouseWheelDirection)e.Direction, e.MouseX, e.MouseY);
    }
    
    #endregion

    #region Gamepad events

    private void OnGamepadAxisMotion(SDL.GamepadAxisEvent e)
    {
        _sceneManager.ActiveScene?.OnGamepadAxisMotion(e.Which, (GamepadAxis)e.Axis, e.Value);
    }

    private void OnGamepadButtonDown(SDL.GamepadButtonEvent e)
    {
        _sceneManager.ActiveScene?.OnGamepadButtonDown(e.Which, (GamepadButton)e.Button);
    }
    
    private void OnGamepadButtonUp(SDL.GamepadButtonEvent e)
    {
        _sceneManager.ActiveScene?.OnGamepadButtonUp(e.Which, (GamepadButton)e.Button);
    }
    
    private void OnGamepadTouchpadDown(SDL.GamepadTouchpadEvent e)
    {
        _sceneManager.ActiveScene?.OnGamepadTouchpadDown(e.Which, e.Touchpad, e.Finger, e.X, e.Y, e.Pressure);
    }
    
    private void OnGamepadTouchpadMotion(SDL.GamepadTouchpadEvent e)
    {
        _sceneManager.ActiveScene?.OnGamepadTouchpadMotion(e.Which, e.Touchpad, e.Finger, e.X, e.Y, e.Pressure);
    }
    
    private void OnGamepadTouchpadUp(SDL.GamepadTouchpadEvent e)
    {
        _sceneManager.ActiveScene?.OnGamepadTouchpadUp(e.Which, e.Touchpad, e.Finger, e.X, e.Y, e.Pressure);
    }

    private void OnGamepadSensorUpdate(SDL.GamepadSensorEvent e)
    {
        var data = new float[3];
        
        unsafe
        {
            for (var i = 0; i < 3; i++)
            {
                data[i] = e.Data[i];
            }
        }
        
        _sceneManager.ActiveScene?.OnGamepadSensorUpdate(e.Which, e.Sensor, data);
    }
    
    #endregion

    #region Sensor events
    
    private void OnFingerDown(SDL.TouchFingerEvent e)
    {
        _sceneManager.ActiveScene?.OnFingerDown(e.TouchID, e.FingerID, e.X, e.Y, e.DX, e.DY, e.Pressure);
    }
    
    private void OnFingerUp(SDL.TouchFingerEvent e)
    {
        _sceneManager.ActiveScene?.OnFingerUp(e.TouchID, e.FingerID, e.X, e.Y, e.DX, e.DY, e.Pressure);
    }
    
    private void OnFingerMotion(SDL.TouchFingerEvent e)
    {
        _sceneManager.ActiveScene?.OnFingerMotion(e.TouchID, e.FingerID, e.X, e.Y, e.DX, e.DY, e.Pressure);
    }
    
    private void OnFingerCanceled(SDL.TouchFingerEvent e)
    {
        _sceneManager.ActiveScene?.OnFingerCanceled(e.TouchID, e.FingerID, e.X, e.Y, e.DX, e.DY, e.Pressure);
    }

    private void OnSensorUpdate(SDL.SensorEvent e)
    {
        var data = new float[6];
        
        unsafe
        {
            for (var i = 0; i < 3; i++)
            {
                data[i] = e.Data[i];
            }
        }
        
        _sceneManager.ActiveScene?.OnSensorUpdate(e.Which, data);
    }
    
    #endregion

    private void OnQuit()
    {
        _sceneManager.ActiveScene?.OnQuit();
    }
}