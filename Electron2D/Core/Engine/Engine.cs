// FILE: Electron2D/Core/Engine/Engine.cs
namespace Electron2D;

public sealed class Engine : IDisposable
{
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
        SceneTree = new SceneTree(new Node("Root"), cfg.DeferredFreeQueueCapacity);

        _events = new EventSystem();

        _window.Initialize(cfg.Window);
        _render.Initialize(_window.Handle, cfg);
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
            _events.EndFrame();
            
            HandleQuitAndCloseRequests();

            // Input pipeline (Godot-like)
            SceneTree.DispatchInputEvents(_events.Events.Input.Read);

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
            
            if (SceneTree.QuitRequested) _running = false;

            _prof.EndFrame();

            // P0: включаем frame-cap (используется только если VSync выключен и MaxFps > 0)
            _time.EndFrame();
        }
    }

    private void HandleQuitAndCloseRequests()
    {
        var win = _events.Events.Window.Read;
        for (var i = 0; i < win.Length; i++)
        {
            if (win[i].Type != WindowEventType.CloseRequested) continue;

            if (SceneTree.OnWindowCloseRequested.HasSubscribers)
                SceneTree.OnWindowCloseRequested.Emit(win[i].WindowId);
            else
                SceneTree.Quit();

            return; // максимум один close на кадр — ок
        }

        // 2) global quit
        if (!_events.QuitRequested) return;

        if (SceneTree.OnQuitRequested.HasSubscribers)
            SceneTree.OnQuitRequested.Emit();
        else
            SceneTree.Quit();
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
