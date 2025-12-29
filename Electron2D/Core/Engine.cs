namespace Electron2D;

public sealed class Engine : IDisposable
{
    private readonly EngineConfig _cfg;
    private readonly TimeSystem _time = new();
    private readonly EventSystem _events = new();
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
        SceneTree = new SceneTree(new Node("Root"), maxDeferredFreePerFrame: cfg.MaxDeferredFreePerFrame);

        // init order: window -> renderer -> resources -> events/input -> physics
        _window.Initialize(cfg.Window);
        _render.Initialize(_window, cfg);
        _resources.Initialize(_render, cfg);
        _events.Initialize();
        _input.Initialize();
        _physics.Initialize(cfg.Physics);
        _time.Initialize(cfg);
    }

    public void Run()
    {
        _running = true;
        while (_running)
        {
            _prof.BeginFrame();

            _time.BeginFrame();
            _events.BeginFrame();            // SDL_PumpEvents + PeepEvents
            _input.BeginFrame(_events);      // собрать состояния в массивы

            // dispatch: input events -> nodes (опционально)
            // SceneTree.DispatchInput(_events.InputEvents);

            // fixed-step
            while (_time.TryConsumeFixedStep(out var fixedDt))
            {
                _physics.Step(fixedDt, SceneTree);
                SceneTree.PhysicsProcess(fixedDt);
            }

            var dt = _time.DeltaTime;
            SceneTree.Process(dt);

            _render.BeginFrame();
            _render.BuildRenderQueue(SceneTree);
            _render.EndFrame();

            SceneTree.FlushFreeQueue();

            if (_events.QuitRequested) _running = false;

            _prof.EndFrame();
        }
    }

    public void Dispose()
    {
        _resources.Shutdown();
        _render.Shutdown();
        _physics.Shutdown();
        _input.Shutdown();
        _events.Shutdown();
        _window.Shutdown();
    }
}