using System;
using SDL3;

namespace Electron2D;

internal sealed class WindowSystem
{
    #region Internal properties
    internal nint Handle { get; private set; }
    #endregion

    #region Internal methods
    internal void Initialize(WindowConfig config)
    {
        if (Handle != 0)
            throw new InvalidOperationException("WindowSystem is already initialized.");

        if (!SDL.InitSubSystem(SDL.InitFlags.Video))
            throw new InvalidOperationException($"SDL.InitSubSystem(Video) failed. {SDL.GetError()}");

        var windowHandle = SDL.CreateWindow(config.Title, config.Width, config.Height, SDL.WindowFlags.Resizable);
        if (windowHandle == 0)
        {
            var error = SDL.GetError();
            SDL.QuitSubSystem(SDL.InitFlags.Video);
            throw new InvalidOperationException($"SDL.CreateWindow failed. {error}");
        }

        Handle = windowHandle;

        try
        {
            ApplyWindowMode(windowHandle, config);
            ApplyWindowState(windowHandle, config);
        }
        catch
        {
            // Best-effort cleanup: these calls are not expected to throw today,
            // but this guarantees no leaked window/subsystem on exceptional paths.
            SDL.DestroyWindow(windowHandle);
            Handle = 0;
            SDL.QuitSubSystem(SDL.InitFlags.Video);
            throw;
        }
    }

    internal void Shutdown()
    {
        var windowHandle = Handle;
        if (windowHandle != 0)
        {
            SDL.DestroyWindow(windowHandle);
            Handle = 0;
        }

        SDL.QuitSubSystem(SDL.InitFlags.Video);
    }
    #endregion

    #region Private methods
    private static void ApplyWindowMode(nint windowHandle, WindowConfig config)
    {
        switch (config.Mode)
        {
            case WindowMode.Windowed:
                ApplyWindowed(windowHandle, config);
                break;

            case WindowMode.BorderlessFullscreen:
                ApplyBorderlessFullscreenDesktop(windowHandle);
                break;

            case WindowMode.ExclusiveFullscreen:
                ApplyExclusiveFullscreen(windowHandle, config);
                break;
        }
    }

    private static void ApplyWindowState(nint windowHandle, WindowConfig config)
    {
        // In fullscreen modes, maximize/minimize may be ignored by the window manager.
        switch (config.State)
        {
            case WindowState.Normal:
                SDL.RestoreWindow(windowHandle);
                break;

            case WindowState.Minimized:
                SDL.MinimizeWindow(windowHandle);
                break;

            case WindowState.Maximized:
                SDL.MaximizeWindow(windowHandle);
                break;
        }
    }

    private static void ApplyWindowed(nint windowHandle, WindowConfig config)
    {
        SDL.SetWindowFullscreen(windowHandle, false);
        SDL.SetWindowBordered(windowHandle, true);
        SDL.SetWindowSize(windowHandle, config.Width, config.Height);
    }

    private static void ApplyBorderlessFullscreenDesktop(nint windowHandle)
    {
        // "Desktop fullscreen": borderless + fullscreen with no explicit display mode change.
        SDL.SetWindowBordered(windowHandle, false);
        SDL.SetWindowFullscreenMode(windowHandle, IntPtr.Zero);
        SDL.SetWindowFullscreen(windowHandle, true);
    }

    private static void ApplyExclusiveFullscreen(nint windowHandle, WindowConfig config)
    {
        SDL.SetWindowBordered(windowHandle, false);

        var displayId = SDL.GetDisplayForWindow(windowHandle);

        // refreshRate=0.0f => "desktop refresh rate" per SDL docs.
        if (SDL.GetClosestFullscreenDisplayMode(
                displayID: displayId,
                w: config.Width,
                h: config.Height,
                refreshRate: 0.0f,
                includeHighDensityModes: true,
                out var closest) &&
            SDL.SetWindowFullscreenMode(windowHandle, closest))
        {
            SDL.SetWindowFullscreen(windowHandle, true);
            return;
        }

        // If the WM/driver refuses the requested exclusive mode (or no suitable mode exists),
        // fall back to desktop (borderless) fullscreen.
        ApplyBorderlessFullscreenDesktop(windowHandle);
    }
    #endregion
}