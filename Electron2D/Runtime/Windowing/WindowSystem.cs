using SDL3;

namespace Electron2D;

internal sealed class WindowSystem
{
    internal nint Handle { get; private set; }

    public void Initialize(WindowConfig cfg)
    {
        if (!SDL.Init(SDL.InitFlags.Video))
            throw new InvalidOperationException($"SDL.Init(Video) failed. {SDL.GetError()}");

        var handle = SDL.CreateWindow(cfg.Title, cfg.Width, cfg.Height, SDL.WindowFlags.Resizable);
        if (handle == 0)
            throw new InvalidOperationException($"SDL.CreateWindow failed. {SDL.GetError()}");

        Handle = handle;

        ApplyMode(handle, cfg);
        ApplyState(handle, cfg);
    }

    private static void ApplyMode(nint window, WindowConfig cfg)
    {
        switch (cfg.Mode)
        {
            case WindowMode.Windowed:
                SDL.SetWindowFullscreen(window, false);
                SDL.SetWindowBordered(window, true);
                SDL.SetWindowSize(window, cfg.Width, cfg.Height);
                break;

            case WindowMode.BorderlessFullscreen:
                // “desktop fullscreen” (без смены display mode)
                SDL.SetWindowBordered(window, false);
                SDL.SetWindowFullscreenMode(window, IntPtr.Zero);
                SDL.SetWindowFullscreen(window, true);
                break;

            case WindowMode.ExclusiveFullscreen:
                // Exclusive: нужен fullscreen mode != null
                SDL.SetWindowBordered(window, false);

                uint displayId = SDL.GetDisplayForWindow(window);

                // 0.0f => “desktop refresh rate”, как в доке
                if (SDL.GetClosestFullscreenDisplayMode(
                        displayID: displayId,
                        w: cfg.Width,
                        h: cfg.Height,
                        refreshRate: 0.0f,
                        includeHighDensityModes: true,
                        out var closest))
                {
                    if (!SDL.SetWindowFullscreenMode(window, closest))
                    {
                        // Если WM/драйвер отказал — fallback в borderless desktop fullscreen
                        // (mode == null)
                        SDL.SetWindowFullscreenMode(window, IntPtr.Zero);
                        // при желании: Log.Warn(SDL.GetError());
                    }
                }
                else
                {
                    // Если все доступные режимы меньше запрошенного — функция вернёт false
                    // fallback в borderless desktop fullscreen
                    SDL.SetWindowFullscreenMode(window, IntPtr.Zero);
                    // при желании: Log.Warn(SDL.GetError());
                }

                // Включаем fullscreen (это отдельный шаг от установки mode)
                SDL.SetWindowFullscreen(window, true);

                // По доке применение может быть async — если нужно “сразу”, синхронизируем:
                // SDL.SyncWindow(window);

                break;
        }
    }

    private static void ApplyState(nint window, WindowConfig cfg)
    {
        // В fullscreen-режимах “maximized/minimized” часто игнорируются WM’ом — это норм.
        switch (cfg.State)
        {
            case WindowState.Normal:
                SDL.RestoreWindow(window);
                break;
            case WindowState.Minimized:
                SDL.MinimizeWindow(window);
                break;
            case WindowState.Maximized:
                SDL.MaximizeWindow(window);
                break;
        }
    }

    public void Shutdown()
    {
        if (Handle != 0)
            SDL.DestroyWindow(Handle);

        Handle = 0;
        SDL.QuitSubSystem(SDL.InitFlags.Video);
    }
}
