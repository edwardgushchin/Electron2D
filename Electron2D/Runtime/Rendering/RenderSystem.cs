namespace Electron2D;

internal sealed class RenderSystem
{
    private RenderQueue _queue = null!;

    public void Initialize(WindowSystem window, EngineConfig cfg)
    {
        _queue = new RenderQueue(cfg.RenderQueueCapacity);
    }

    public void Shutdown() { }

    public void BeginFrame() => _queue.Clear();

    public void BuildRenderQueue(SceneTree scene)
    {
        // TODO: обход дерева и сбор команд.
        // На этом этапе достаточно компилируемого каркаса.
    }

    public void EndFrame()
    {
        // TODO: сортировка/батчинг/submit
    }
}