using SDL3;

namespace Electron2D;

internal sealed class RenderSystem
{
    private RenderQueue _queue = null!;
    private nint _renderer; // SDL_Renderer*

    internal nint Handle => _renderer;

    public void Initialize(WindowSystem window, EngineConfig cfg)
    {
        _queue = new RenderQueue(cfg.RenderQueueCapacity);

        var win = window.Handle;
        if (win == 0)
            throw new InvalidOperationException("RenderSystem.Initialize: WindowHandle is not created.");

        _renderer = SDL.CreateRenderer(win, null);
        if (_renderer == 0)
            throw new InvalidOperationException($"SDL.CreateRenderer failed. {SDL.GetError()}");

        ApplyVSync(cfg.Window);
    }

    public void Shutdown()
    {
        if (_renderer != 0)
            SDL.DestroyRenderer(_renderer);

        _renderer = 0;
    }

    public void BeginFrame() => _queue.Clear();

    public void BuildRenderQueue(SceneTree scene)
    {
        // TODO: обход дерева и сбор команд в _queue.
    }

    public void EndFrame()
    {
        // TODO: сортировка/батчинг/submit.
        // Минимальный скелет:
        SDL.RenderPresent(_renderer);
    }

    private void ApplyVSync(WindowConfig cfg)
    {
        // В SDL3 политика vsync зависит от API SDL3-CS.
        // Если есть SDL.SetRenderVSync(renderer, int) — используем.
        var desired = cfg.VSync switch
        {
            VSyncMode.Disabled => 0,
            VSyncMode.Enabled  => Math.Max(1, cfg.VSyncInterval),
            VSyncMode.Adaptive => -1,
            _ => 0
        };

        // Псевдокод: проверь реальное имя функции в SDL3-CS
        if (!SDL.SetRenderVSync(_renderer, desired))
        {
            // Fallback: хотя бы выключить, чтобы поведение было детерминированным
            SDL.SetRenderVSync(_renderer, 0);
        }
    }
    
    public void BuildRenderQueue(SceneTree scene, ResourceSystem resources)
    {
        BuildNode(scene.Root, resources);
    }

    private void BuildNode(Node node, ResourceSystem resources)
    {
        var comps = node.InternalComponents;
        for (var i = 0; i < comps.Length; i++)
        {
            if (comps[i] is SpriteRenderer sr)
                sr.PrepareRender(_queue, resources);
        }

        var childCount = node.GetChildCount();
        for (var c = 0; c < childCount; c++)
            BuildNode(node.GetChild(c), resources);
    }
}