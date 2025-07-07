using Electron2D.Graphics;
using SDL3;

namespace Electron2D.Platform;

internal class Window : IDisposable
{
    internal IntPtr Handle { get; }

    public Window(string title, Settings settings)
    {
        SDL.WindowFlags windowFlags = 0;
        
        if (settings.Fullscreen == FullscreenMode.Enabled)
        {
            settings.Resizable = false;
            
            windowFlags += (ulong) SDL.WindowFlags.Fullscreen;
        }
        else if (settings.Fullscreen == FullscreenMode.Borderless)
        {
            settings.Resizable = false;
            
            var currentDisplay = SDL.GetPrimaryDisplay();
            
            SDL.GetDisplayBounds(currentDisplay, out var displayBounds);
            
            settings.Width = displayBounds.W;
            settings.Height = displayBounds.H;
            
            windowFlags += (ulong) SDL.WindowFlags.Borderless;
        }
        else if (settings is {Fullscreen: FullscreenMode.Disabled, Resizable: true})
        {
            windowFlags += (ulong) SDL.WindowFlags.Resizable;
        }
        
        Handle = SDL.CreateWindow(title, settings.Width, settings.Height, windowFlags);
        if (Handle == IntPtr.Zero)
        {
            throw new Exception($"Window could not be created! SDL Error: {SDL.GetError()}");
        }
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            SDL.DestroyWindow(Handle);
        }
    }
}