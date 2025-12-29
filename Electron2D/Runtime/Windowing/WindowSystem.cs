using SDL3;

namespace Electron2D;

internal sealed class WindowSystem
{
    internal nint Handle { get; private set; }

    public void Initialize(WindowConfig cfg)
    {
        if (!SDL.Init(SDL.InitFlags.Video))
            throw new InvalidOperationException($"SDL.Init(Video) failed. {SDL.GetError()}");

        var handle = SDL.CreateWindow(cfg.Title, cfg.Width, cfg.Height, 0);
        if (handle == 0)
            throw new InvalidOperationException($"SDL.CreateWindow failed. {SDL.GetError()}");

        Handle = handle;

        // TODO: применить cfg.Mode/cfg.State через SDL.SetWindowFullscreen/SetWindowBordered/Maximize/Minimize и т.п.
        // (не обязательно для вопроса про renderer handle)
    }

    public void Shutdown()
    {
        if (Handle != 0)
            SDL.DestroyWindow(Handle);

        Handle = 0;

        SDL.QuitSubSystem(SDL.InitFlags.Video);
        // Если ты используешь SDL.Init(...) в нескольких местах, договорись о единой политике init/quit,
        // но это отдельный вопрос.
    }
}