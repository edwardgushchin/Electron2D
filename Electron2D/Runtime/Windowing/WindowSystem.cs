using SDL3;

namespace Electron2D;

internal sealed class WindowSystem
{
    internal nint WindowHandle { get; private set; }
    internal nint RendererHandle { get; private set; }

    public void Initialize(WindowConfig cfg)
    {
        if (!SDL.Init(SDL.InitFlags.Video))
            throw new InvalidOperationException($"SDL.Init failed. {SDL.GetError()}");

        var handle = SDL.CreateWindow(cfg.Title, cfg.Width, cfg.Height, 0);

        //SDL.CreateWindowAndRenderer(cfg.Title, cfg.Width, cfg.Height, 0, out var win, out var ren);

        if (handle == 0)
            throw new InvalidOperationException($"SDL.CreateWindow failed. {SDL.GetError()}");

        WindowHandle = handle;
        //RendererHandle = ren;
    }

    public void Shutdown()
    {
        // В SDL3-CS могут быть DestroyWindow/DestroyRenderer; если их нет — SDL.Quit() всё равно освободит подсистемы.
        if (RendererHandle != 0) SDL.DestroyRenderer(RendererHandle);
        if (WindowHandle != 0) SDL.DestroyWindow(WindowHandle);

        RendererHandle = 0;
        WindowHandle = 0;

        SDL.Quit();
    }
}