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

        // ВАЖНО: использовать cfg.MaxDeferredFreePerFrame
        SceneTree = new SceneTree(new Node("Root"));

        // ВАЖНО: использовать cfg.MaxEventsPerFrame
        _events = new EventSystem();

        // init order: window -> renderer -> resources -> events -> input -> physics -> time
        _window.Initialize(cfg.Window);
        _render.Initialize(_window, cfg);
        _resources.Initialize(_render, cfg);
        _events.Initialize(cfg);
        _input.Initialize();
        _physics.Initialize(cfg.Physics);
        _time.Initialize(cfg);

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
            _render.BuildRenderQueue(SceneTree);
            _render.EndFrame();

            SceneTree.FlushFreeQueue();

            if (_events.QuitRequested)
                _running = false;

            _prof.EndFrame();
        }
    }

    public void Dispose()
    {
        // Важно разорвать публичный фасад ввода, чтобы после Dispose не дергать мертвую систему
        Input.Unbind();

        _resources.Shutdown();
        _render.Shutdown();
        _physics.Shutdown();
        _input.Shutdown();
        _events.Shutdown();
        _window.Shutdown();
    }
}