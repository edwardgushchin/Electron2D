using SDL3;

namespace Electron2D;

internal class WindowManager(string title, ref Settings settings)
{
    private Settings _settings = settings;
    private IntPtr _windowHandle;
    
    internal void Initialize()
    {
        Logger.Info("Initializing window manager...");
        
        if (!SDL.Init(SDL.InitFlags.Video))
        {
            throw new ElectronException($"Failed to initialize SDL: {SDL.GetError()}");
        }
        
        SDL.WindowFlags windowFlags = 0;
        
        if (_settings.Fullscreen == FullscreenMode.Enabled)
        {
            _settings.Resizable = false;
            
            windowFlags += (ulong) SDL.WindowFlags.Fullscreen;
        }
        else if (_settings.Fullscreen == FullscreenMode.Borderless)
        {
            _settings.Resizable = false;
            
            var currentDisplay = SDL.GetPrimaryDisplay();
            
            SDL.GetDisplayBounds(currentDisplay, out var displayBounds);
            
            _settings.Width = displayBounds.W;
            _settings.Height = displayBounds.H;
            
            windowFlags += (ulong) SDL.WindowFlags.Borderless;
        }
        else if (_settings is {Fullscreen: FullscreenMode.Disabled, Resizable: true})
        {
            windowFlags += (ulong) SDL.WindowFlags.Resizable;
        }
        
        _windowHandle = SDL.CreateWindow(title, _settings.Width, _settings.Height, windowFlags);
        
        Logger.Info("Window manager initialization was successful.");
    }

    internal void Shutdown()
    {
        SDL.DestroyWindow(_windowHandle);
        Logger.Info("The window manager has been successfully shutdown.");
    }
    
    internal IntPtr GetWindowHandle() => _windowHandle;
}