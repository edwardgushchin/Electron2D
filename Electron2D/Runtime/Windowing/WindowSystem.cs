using SDL3;

namespace Electron2D;

/// <summary>
/// Управляет жизненным циклом SDL окна и применяет режим/состояние окна на основе <see cref="WindowConfig"/>.
/// </summary>
internal sealed class WindowSystem
{
    #region Properties

    /// <summary>SDL_Window* (handle). 0 означает “не инициализировано”.</summary>
    internal nint Handle { get; private set; }

    #endregion

    #region Internal API

    internal void Initialize(WindowConfig config)
    {
        if (Handle != 0)
            throw new InvalidOperationException("WindowSystem is already initialized.");

        if (!SDL.InitSubSystem(SDL.InitFlags.Video))
            throw new InvalidOperationException($"SDL.InitSubSystem(Video) failed. {SDL.GetError()}");

        var resizable = config.Resizable ? SDL.WindowFlags.Resizable : 0;
        
        var windowHandle = SDL.CreateWindow(
            title: config.Title,
            w: config.Width,
            h: config.Height,
            flags: resizable);

        if (windowHandle == 0)
        {
            SDL.QuitSubSystem(SDL.InitFlags.Video);
            throw new InvalidOperationException($"SDL.CreateWindow failed. {SDL.GetError()}");
        }

        Handle = windowHandle;

        try
        {
            ApplyWindowMode(windowHandle, config);
            ApplyWindowState(windowHandle, config);
        }
        catch
        {
            // Best-effort cleanup: эти вызовы сегодня не ожидаются бросающими исключения,
            // но так гарантируется отсутствие утечек окна/подсистемы на исключительных путях.
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

    #region Private helpers

    private static void ApplyWindowMode(nint windowHandle, in WindowConfig config)
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
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void ApplyWindowState(nint windowHandle, in WindowConfig config)
    {
        // В fullscreen режимах maximize/minimize могут игнорироваться WM/драйвером.
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
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void ApplyWindowed(nint windowHandle, in WindowConfig config)
    {
        SDL.SetWindowFullscreen(windowHandle, false);
        SDL.SetWindowBordered(windowHandle, true);
        SDL.SetWindowSize(windowHandle, config.Width, config.Height);
    }

    private static void ApplyBorderlessFullscreenDesktop(nint windowHandle)
    {
        // "Desktop fullscreen": borderless + fullscreen без явной смены display mode.
        SDL.SetWindowBordered(windowHandle, false);
        SDL.SetWindowFullscreenMode(windowHandle, IntPtr.Zero);
        SDL.SetWindowFullscreen(windowHandle, true);
    }

    private static void ApplyExclusiveFullscreen(nint windowHandle, in WindowConfig config)
    {
        SDL.SetWindowBordered(windowHandle, false);

        var displayId = SDL.GetDisplayForWindow(windowHandle);

        // refreshRate=0.0f => "desktop refresh rate" (по документации SDL).
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

        // Если WM/драйвер отказался от requested exclusive mode (или подходящего режима нет),
        // откатываемся на desktop (borderless) fullscreen.
        ApplyBorderlessFullscreenDesktop(windowHandle);
    }

    #endregion
}
