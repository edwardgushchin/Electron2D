// FILE: Electron2D/Core/Engine/Engine.cs
namespace Electron2D;

public sealed class Engine : IDisposable
{
    private readonly EngineConfig _cfg;
    private readonly TimeSystem _time = new();
    private readonly EventSystem _events;
    private readonly InputSystem _input = new();
    private readonly PhysicsSystem _physics = new();
    private readonly RenderSystem _render = new();
    private readonly ResourceSystem _resources = new();
    private readonly ProfilerSystem _prof = new();
    private readonly WindowSystem _window = new();

    public SceneTree SceneTree { get; }

    private bool _running;

    public Engine(EngineConfig cfg)
    {
        _cfg = cfg;

        // P0: применяем лимит deferred-free
        SceneTree = new SceneTree(new Node("Root"), cfg.DeferredFreeQueueCapacity);

        _events = new EventSystem();

        _window.Initialize(cfg.Window);
        _render.Initialize(_window, cfg);
        _resources.Initialize(_render, cfg);
        _events.Initialize(cfg);
        _input.Initialize();
        _physics.Initialize(cfg.Physics);
        _time.Initialize(cfg);

        Resources.Bind(_resources);
        Input.Bind(_input);
    }

    public void Run()
    {
        _running = true;

        while (_running)
        {
            _prof.BeginFrame();

            _time.BeginFrame();
            _events.BeginFrame();
            _input.BeginFrame(_events);

            while (_time.TryConsumeFixedStep(out var fixedDt))
            {
                _physics.Step(fixedDt, SceneTree);
                SceneTree.PhysicsProcess(fixedDt);
            }

            SceneTree.Process(_time.DeltaTime);

            _render.BeginFrame();
            _render.BuildRenderQueue(SceneTree, _resources);
            _render.EndFrame();

            SceneTree.FlushFreeQueue();

            if (_events.QuitRequested)
                _running = false;

            _prof.EndFrame();

            // P0: включаем frame-cap (используется только если VSync выключен и MaxFps > 0)
            _time.EndFrame();
        }
    }

    public void Dispose()
    {
        Resources.Unbind();
        Input.Unbind();

        _resources.Shutdown();
        _render.Shutdown();
        _physics.Shutdown();
        _input.Shutdown();
        _events.Shutdown();
        _window.Shutdown();
    }
}
