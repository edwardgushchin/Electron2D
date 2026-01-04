namespace Electron2D;

public sealed class Engine : IDisposable
{
    #region Instance fields
    private readonly TimeSystem _time = new();
    private readonly EventSystem _events;
    private readonly InputSystem _input = new();
    private readonly PhysicsSystem _physics = new();
    private readonly RenderSystem _render = new();
    private readonly ResourceSystem _resources = new();
    private readonly ProfilerSystem _prof = new();
    private readonly WindowSystem _window = new();
    private readonly AnimationSystem _animation = new();

    private bool _running;
    #endregion

    #region Constructors
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

        // Применяем фактический VSync (RenderSystem мог отключить его из-за неподдержки).
        var effectiveVsync = _render.EffectiveVSync;
        var effectiveMaxFps = cfg.MaxFps;

        if (effectiveVsync == VSyncMode.Disabled && effectiveMaxFps <= 0 && _render.SuggestedMaxFps > 0)
            effectiveMaxFps = _render.SuggestedMaxFps;

        // maxFrameDeltaSeconds сейчас в TimeSystem.Apply не используется — оставляем 0.
        _time.Apply(
            useFixedStep: cfg.UseFixedStep,
            fixedDeltaSeconds: cfg.Physics.FixedDelta,
            maxFixedStepsPerFrame: cfg.MaxFixedStepsPerFrame,
            timeScale: cfg.TimeScale,
            vsync: effectiveVsync,
            maxFps: effectiveMaxFps);

        Resources.Bind(_resources);
        Input.Bind(_input);
        Profiler.Bind(_prof);
    }
    #endregion

    #region Properties
    public SceneTree SceneTree { get; }
    #endregion

    #region Public API
    public void Run()
    {
        _running = true;

        while (_running)
        {
            _prof.BeginFrame();
            using var frameSample = Profiler.Sample(ProfilerSampleId.Frame);

            _time.BeginFrame();

            using (Profiler.Sample(ProfilerSampleId.EventsPump))
                _events.BeginFrame();

            using (Profiler.Sample(ProfilerSampleId.InputPoll))
                _input.BeginFrame(_events);

            using (Profiler.Sample(ProfilerSampleId.EventsSwap))
                _events.EndFrame();

            // Event counters (после SwapAll() уже есть ReadCount у каналов)
            Profiler.SetCounter(ProfilerCounterId.EventsEngineRead, _events.Events.Engine.ReadCount);
            Profiler.SetCounter(ProfilerCounterId.EventsWindowRead, _events.Events.Window.ReadCount);
            Profiler.SetCounter(ProfilerCounterId.EventsInputRead, _events.Events.Input.ReadCount);
            Profiler.SetCounter(ProfilerCounterId.EventsDroppedEngine, _events.DroppedEngineEvents);
            Profiler.SetCounter(ProfilerCounterId.EventsDroppedWindow, _events.DroppedWindowEvents);
            Profiler.SetCounter(ProfilerCounterId.InputDroppedEvents, _events.DroppedInputEvents);

            using (Profiler.Sample(ProfilerSampleId.HandleQuitClose))
                HandleQuitAndCloseRequests();

            using (Profiler.Sample(ProfilerSampleId.SceneDispatchInput))
                SceneTree.DispatchInputEvents(_events.Events.Input.Read);

            var fixedSteps = 0;
            using (Profiler.Sample(ProfilerSampleId.SceneFixedStep))
            {
                while (_time.TryConsumeFixedStep(out var fixedDt))
                {
                    fixedSteps++;
                    _physics.Step(fixedDt, SceneTree);
                    SceneTree.PhysicsProcess(fixedDt);
                }
            }
            Profiler.SetCounter(ProfilerCounterId.FixedSteps, fixedSteps);

            using (Profiler.Sample(ProfilerSampleId.SceneProcess))
                SceneTree.Process(_time.DeltaTime);
            
            using (Profiler.Sample(ProfilerSampleId.Animation))
                _animation.Process(SceneTree, _time.DeltaTime);

            // RenderSystem сам пометит свои фазы (Begin/Build/Sort/Flush/Present)
            _render.BeginFrame(SceneTree);
            _render.BuildRenderQueue(SceneTree, _resources);
            _render.EndFrame();

            using (Profiler.Sample(ProfilerSampleId.SceneFlushFreeQueue))
                SceneTree.FlushFreeQueue();

            if (SceneTree.QuitRequested)
                _running = false;

            _prof.EndFrame();

            // frame-cap (только если VSync выключен и MaxFps > 0)
            _time.EndFrame();
        }
    }

    public void Dispose()
    {
        Resources.Unbind();
        Input.Unbind();
        Profiler.Unbind();

        _resources.Shutdown();
        _render.Shutdown();
        _physics.Shutdown();
        _input.Shutdown();
        _events.Shutdown();
        _window.Shutdown();
    }
    #endregion

    #region Private helpers
    private void HandleQuitAndCloseRequests()
    {
        // 1) window close
        var windowEvents = _events.Events.Window.Read;
        for (var i = 0; i < windowEvents.Length; i++)
        {
            if (windowEvents[i].Type != WindowEventType.CloseRequested)
                continue;

            if (SceneTree.OnWindowCloseRequested.HasSubscribers)
            {
                SceneTree.OnWindowCloseRequested.Emit(windowEvents[i].WindowId);
            }
            else if (SceneTree.OnQuitRequested.HasSubscribers)
            {
                // Fallback: дать перехватить закрытие через общий quit-сигнал.
                SceneTree.OnQuitRequested.Emit();
            }
            else
            {
                SceneTree.Quit();
            }

            return; // максимум один close на кадр — ок
        }

        // 2) global quit
        if (!_events.QuitRequested)
            return;

        if (SceneTree.OnQuitRequested.HasSubscribers)
            SceneTree.OnQuitRequested.Emit();
        else
            SceneTree.Quit();
    }
    #endregion
}
