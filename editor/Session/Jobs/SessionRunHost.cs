namespace Electron2D.Editor;

/// <summary>Hosts a compiled scenario launched by the snapshot job owner.</summary>
/// <remarks>This is a tooling assembly, separate from the runtime. The session job owns process isolation,
/// build validation, deadlines and cancellation. Calling this entry point directly does not isolate a hung callback.</remarks>
public static class SessionRunHost
{
    /// <summary>Executes one versioned controller request on the calling engine owner thread.</summary>
    /// <param name="args">The four arguments supplied by the controller; arbitrary application arguments reject.</param>
    /// <param name="factory">Creates a fresh scenario; its Dispose runs after host cleanup.</param>
    /// <returns>Zero only for explicitly completed simulation; otherwise a protocol failure code.</returns>
    public static int Run(string[] args, Func<SessionScenario> factory)
    {
        ArgumentNullException.ThrowIfNull(args); ArgumentNullException.ThrowIfNull(factory);
        if (args.Length != 4 || args[0] != "--electron2d-job" || !Guid.TryParseExact(args[3], "N", out _))
        { Console.Error.WriteLine("Launch this project through the owning project session."); return 2; }
        var output = IOPath.GetFullPath(args[2]);
        var report = new RunReport { Job = args[3], Project = Environment.CurrentDirectory };
        SessionScenario? scenario = null; SessionRunContext? context = null; Node? root = null; Window? nativeRoot = null; SceneTree? manualTree = null;
        try
        {
            if (Engine.MainLoop is not null) throw new JobFailure("RUNTIME_BUSY", 4, "A snapshot child requires an idle engine.");
            var request = JobProtocol.Read(args[1], JobJSON.Default.RunRequest); request.Validate();
            report.Mode = request.Mode; report.Seed = request.Seed; report.FPS = request.FPS; report.StepBudget = request.Steps;
            report.RequestSHA256 = JobProtocol.Hash(args[1]);
            report.EngineVersion = Engine.VersionInfo.InformationalVersion;
            report.EngineAssemblySHA256 = JobProtocol.Hash(typeof(Engine).Assembly.Location);
            report.ProjectAssemblySHA256 = JobProtocol.Hash(System.Reflection.Assembly.GetEntryAssembly()!.Location);
            context = new(request, report);
            var manifest = JobProtocol.Read(request.SnapshotManifest, JobJSON.Default.SnapshotManifest);
            SnapshotStore.Verify(manifest, Environment.CurrentDirectory, request.SnapshotID, request.SnapshotSHA256);
            report.SnapshotID = request.SnapshotID; report.SnapshotSHA256 = request.SnapshotSHA256;
            report.SceneRevision = request.SceneRevision; report.CodeRevision = request.CodeRevision;
            report.SDKAssemblySHA256 = JobProtocol.Hash(typeof(SessionRunHost).Assembly.Location);
            scenario = factory() ?? throw new InvalidOperationException("The scenario factory returned null.");
            scenario.Setup(context);
            var sourcePath = JobProtocol.ChildPath(Environment.CurrentDirectory, request.Scene);
            report.SceneSHA256 = JobProtocol.Hash(sourcePath);
            using (var packed = ResourceLoader.Load<PackedScene>(sourcePath, ResourceLoader.CacheMode.IgnoreDeep)) root = packed.Instantiate();
            SceneOverlay.Apply(root, JobProtocol.Read(JobProtocol.ChildPath(Environment.CurrentDirectory, ".session-nodes.json"), SessionJSON.Default.NodeStateArray));
            context.Attach(root); scenario.Loaded(context);
            {
                Engine.PhysicsTicksPerSecond = request.FPS; Engine.PhysicsJitterFix = 0; Engine.TimeScale = 1;
                var inputIndex = 0;
                void Prepare()
                {
                    if (context.Step >= request.Steps) throw new JobFailure("BUDGET_EXHAUSTED", 6, "The scenario did not complete within its step budget.");
                    context.Step++;
                    while (inputIndex < request.Inputs.Length && request.Inputs[inputIndex].Step == context.Step)
                    {
                        var source = request.Inputs[inputIndex++];
                        if (source.Action is { } named && !InputMap.HasAction(named)) throw new JobFailure("INVALID_INPUT", 2, $"Action '{named}' is not registered.");
                        using InputEvent input = source.Action is { } action
                            ? new InputEventAction { Action = action, Pressed = source.Pressed, Strength = source.Pressed ? source.Strength : 0 }
                            : new InputEventKey { Keycode = source.Key!.Value, Pressed = source.Pressed, Echo = source.Echo };
                        Input.ParseInputEvent(input);
                    }
                    scenario.Before(context);
                }
                void Finish(Image? captured = null)
                {
                    report.CompletedSteps++;
                    if (captured is not null && request.CaptureSteps.Contains(context.Step))
                    {
                        var path = IOPath.Combine(output, $"frame-{context.Step:D6}.png"); captured.SavePNG(path);
                        report.Artifacts.Add(new("image", path, context.Step, JobProtocol.Hash(path)));
                    }
                    scenario.After(context);
                    if (!context.Completed && context.Step == request.Steps)
                        throw new JobFailure("BUDGET_EXHAUSTED", 6, "The scenario exhausted its step budget without Complete.");
                }
                if (request.Mode == SessionRunMode.Headless)
                {
                    if (root is Window) throw new JobFailure("HEADLESS_WINDOW_UNAVAILABLE", 7, "Window activation requires rendered mode; headless scenes use ordinary Node roots.");
                    manualTree = new SceneTree(root); Engine.Start(manualTree);
                    while (!context.Completed)
                    {
                        Prepare(); var stopped = Engine.AdvanceFrame(1d / request.FPS); Finish();
                        if (stopped && !context.Completed) throw new JobFailure("EARLY_EXIT", 4, "The scene quit before scenario completion.");
                    }
                    Engine.Stop();
                }
                else
                {
                    nativeRoot = root as Window ?? new Window { Name = "ProjectRun" };
                    if (!ReferenceEquals(root, nativeRoot)) nativeRoot.AddChild(root);
                    nativeRoot.Size = new(request.Width, request.Height); nativeRoot.ContentScaleSize = nativeRoot.Size; nativeRoot.ContentScaleMode = ContentScaleMode.Viewport;
                    ProjectSettings.Set(ProjectSettings.RenderingMethod, request.Backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
                    using var writer = new CaptureWriter(context, Finish); MovieWriter.AddWriter(writer);
                    var code = Engine.Run((window, next) =>
                    {
                        if (next != (ulong)context.Step + 1) throw new InvalidOperationException("The host frame sequence changed.");
                        report.Renderer = RenderingServer.GetCurrentRenderingMethod(); report.Driver = RenderingServer.GetCurrentRenderingDriverName();
                        Prepare();
                    }, nativeRoot, IOPath.Combine(output, "capture.e2dcapture"), request.FPS);
                    if (code != 0) throw new JobFailure("GAME_EXIT", 4, $"The scene returned exit code {code}.");
                }
                if (!context.Completed || report.CompletedSteps != context.Step)
                    throw new JobFailure("EARLY_EXIT", 4, "The scene exited before the requested completion boundary.");
            }
            report.Success = report.ExitCode == 0;
        }
        catch (Exception error) { JobProtocol.Fail(report, error); }
        finally
        {
            if (manualTree is not null && ReferenceEquals(Engine.MainLoop, manualTree)) { try { Engine.Stop(); } catch (Exception error) { JobProtocol.Fail(report, error); } }
            try { manualTree?.Dispose(); } catch (Exception error) { JobProtocol.Fail(report, error); }
            try { nativeRoot?.Dispose(); } catch (Exception error) { JobProtocol.Fail(report, error); }
            try { root?.Dispose(); } catch (Exception error) { JobProtocol.Fail(report, error); }
            try { scenario?.Dispose(); } catch (Exception error) { JobProtocol.Fail(report, error); }
            try { context?.Release(); } catch (Exception error) { JobProtocol.Fail(report, error); }
        }
        try { JobProtocol.Save(IOPath.Combine(output, "run.json"), report); }
        catch (Exception error) { Console.Error.WriteLine(error); return 4; }
        return report.ExitCode;
    }

    private sealed class CaptureWriter(SessionRunContext context, Action<Image?> finish) : MovieWriter
    {
        protected override string[] OnGetSupportedExtensions() => ["e2dcapture"];
        protected override bool OnHandlesFile(string path) => IOPath.GetExtension(path) == ".e2dcapture";
        protected override void OnWriteBegin(Vector2i movieSize, int fps, string basePath)
        {
            if (movieSize != new Vector2i(context.Width, context.Height) || fps != context.FPS)
                throw new InvalidOperationException("Native capture does not match requested dimensions or cadence.");
        }
        protected override void OnWriteFrame(Image frameImage, ReadOnlySpan<int> audioFrameBlock)
        {
            finish(frameImage);
            if (context.Completed) context.Root.Tree!.Quit();
        }
        protected override void OnWriteEnd() { }
    }
}
