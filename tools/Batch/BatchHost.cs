namespace Electron2D.Tools.Batch;

/// <summary>Hosts a compiled scenario launched by the project batch controller.</summary>
/// <remarks>This is a tooling assembly, separate from the runtime. Use the controller CLI for process isolation,
/// build validation, deadlines and cancellation. Calling this entry point directly does not isolate a hung callback.</remarks>
public static class BatchHost
{
    /// <summary>Executes one versioned controller request on the calling engine owner thread.</summary>
    /// <param name="args">The four arguments supplied by the controller; arbitrary application arguments reject.</param>
    /// <param name="factory">Creates a fresh scenario; its Dispose runs after host cleanup.</param>
    /// <returns>Zero only for successful authoring or explicitly completed simulation; otherwise a protocol failure code.</returns>
    public static int Run(string[] args, Func<BatchScenario> factory)
    {
        ArgumentNullException.ThrowIfNull(args); ArgumentNullException.ThrowIfNull(factory);
        if (args.Length != 4 || args[0] != "--electron2d-batch" || !Guid.TryParseExact(args[3], "N", out _))
        { Console.Error.WriteLine("Launch this project through the Electron2D.Batch controller."); return 2; }
        var output = IOPath.GetFullPath(args[2]);
        var report = new BatchReport { Job = args[3], Project = Environment.CurrentDirectory };
        BatchScenario? scenario = null; BatchContext? context = null; Node? root = null; Window? nativeRoot = null; SceneTree? manualTree = null;
        try
        {
            if (Engine.MainLoop is not null) throw new BatchFailure("RUNTIME_BUSY", 4, "A batch child requires an idle engine.");
            var request = Protocol.Read(args[1], ProtocolJSON.Default.BatchRequest); request.Validate();
            report.Mode = request.Mode; report.Seed = request.Seed; report.FPS = request.FPS; report.StepBudget = request.Steps;
            report.RequestSHA256 = Protocol.Hash(args[1]);
            report.EngineVersion = Engine.VersionInfo.InformationalVersion;
            report.EngineAssemblySHA256 = Protocol.Hash(typeof(Engine).Assembly.Location);
            report.ProjectAssemblySHA256 = Protocol.Hash(System.Reflection.Assembly.GetEntryAssembly()!.Location);
            context = new(request, report); scenario = factory() ?? throw new InvalidOperationException("The scenario factory returned null.");
            scenario.Setup(context);
            if (request.Scene is { } scenePath)
            {
                var sourcePath = Protocol.ChildPath(Environment.CurrentDirectory, scenePath);
                if (!File.Exists(sourcePath)) throw new BatchFailure("INVALID_SCENE", 2, "The requested scene file does not exist.");
                report.SceneSHA256 = Protocol.Hash(sourcePath);
                using var packed = ResourceLoader.Load<PackedScene>(sourcePath, ResourceLoader.CacheMode.Ignore);
                root = packed.Instantiate();
            }
            else
            {
                var created = scenario.Create(context) ?? throw new InvalidOperationException("The scene factory returned null.");
                if (created.IsDisposed || created.IsInsideTree || created.Parent is not null || created.IsQueuedForDeletion)
                    throw new BatchFailure("INVALID_SCENE", 2, "The scene factory must return a live detached unqueued root.");
                root = created;
            }
            context.Attach(root);
            if (request.Mode == BatchMode.Authoring)
            {
                scenario.AuthorScene(context);
                if (root.IsInsideTree || Engine.MainLoop is not null) throw new BatchFailure("INVALID_SCENE", 2, "Authoring hooks must retain a detached inactive scene.");
                var destination = Protocol.ChildPath(Environment.CurrentDirectory, request.Scene ?? "scene.e2dscene");
                if (request.Scene is null && IOPath.Exists(destination)) throw new BatchFailure("SCENE_EXISTS", 2, "Open the existing scene explicitly before updating it.");
                ResourceSaver.SaveScene(root, destination);
                report.Artifacts.Add(new("scene", destination, 0, Protocol.Hash(destination)));
            }
            else
            {
                Engine.PhysicsTicksPerSecond = request.FPS; Engine.PhysicsJitterFix = 0; Engine.TimeScale = 1;
                var inputIndex = 0;
                void Prepare()
                {
                    if (context.Step >= request.Steps) throw new BatchFailure("BUDGET_EXHAUSTED", 6, "The scenario did not complete within its step budget.");
                    context.Step++;
                    while (inputIndex < request.Inputs.Length && request.Inputs[inputIndex].Step == context.Step)
                    {
                        var source = request.Inputs[inputIndex++];
                        if (source.Action is { } named && !InputMap.HasAction(named)) throw new BatchFailure("INVALID_INPUT", 2, $"Action '{named}' is not registered.");
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
                        report.Artifacts.Add(new("image", path, context.Step, Protocol.Hash(path)));
                    }
                    scenario.After(context);
                    if (!context.Completed && context.Step == request.Steps)
                        throw new BatchFailure("BUDGET_EXHAUSTED", 6, "The scenario exhausted its step budget without Complete.");
                }
                if (request.Mode == BatchMode.Headless)
                {
                    if (root is Window) throw new BatchFailure("HEADLESS_WINDOW_UNAVAILABLE", 7, "Window activation requires rendered mode; headless scenes use ordinary Node roots.");
                    manualTree = new SceneTree(root); Engine.Start(manualTree);
                    while (!context.Completed)
                    {
                        Prepare(); var stopped = Engine.AdvanceFrame(1d / request.FPS); Finish();
                        if (stopped && !context.Completed) throw new BatchFailure("EARLY_EXIT", 4, "The scene quit before scenario completion.");
                    }
                    Engine.Stop();
                }
                else
                {
                    nativeRoot = root as Window ?? new Window { Name = "BatchHost" };
                    if (!ReferenceEquals(root, nativeRoot)) nativeRoot.AddChild(root);
                    nativeRoot.Size = new(request.Width, request.Height); nativeRoot.ContentScaleSize = nativeRoot.Size; nativeRoot.ContentScaleMode = ContentScaleMode.Viewport;
                    ProjectSettings.Set(ProjectSettings.RenderingMethod, request.Backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
                    using var writer = new CaptureWriter(context, Finish); MovieWriter.AddWriter(writer);
                    var code = Engine.Run((window, next) =>
                    {
                        if (next != (ulong)context.Step + 1) throw new InvalidOperationException("The host frame sequence changed.");
                        report.Renderer = RenderingServer.GetCurrentRenderingMethod(); report.Driver = RenderingServer.GetCurrentRenderingDriverName();
                        Prepare();
                    }, nativeRoot, IOPath.Combine(output, "capture.e2dbatch"), request.FPS);
                    if (code != 0) throw new BatchFailure("GAME_EXIT", 4, $"The scene returned exit code {code}.");
                }
                if (!context.Completed || report.CompletedSteps != context.Step)
                    throw new BatchFailure("EARLY_EXIT", 4, "The scene exited before the requested completion boundary.");
            }
            report.Success = report.ExitCode == 0;
        }
        catch (Exception error) { Protocol.Fail(report, error); }
        finally
        {
            if (manualTree is not null && ReferenceEquals(Engine.MainLoop, manualTree)) { try { Engine.Stop(); } catch (Exception error) { Protocol.Fail(report, error); } }
            try { manualTree?.Dispose(); } catch (Exception error) { Protocol.Fail(report, error); }
            try { nativeRoot?.Dispose(); } catch (Exception error) { Protocol.Fail(report, error); }
            try { root?.Dispose(); } catch (Exception error) { Protocol.Fail(report, error); }
            try { scenario?.Dispose(); } catch (Exception error) { Protocol.Fail(report, error); }
            try { context?.Release(); } catch (Exception error) { Protocol.Fail(report, error); }
        }
        try { Protocol.Save(IOPath.Combine(output, "run.json"), report); }
        catch (Exception error) { Console.Error.WriteLine(error); return 4; }
        return report.ExitCode;
    }

    private sealed class CaptureWriter(BatchContext context, Action<Image?> finish) : MovieWriter
    {
        protected override string[] OnGetSupportedExtensions() => ["e2dbatch"];
        protected override bool OnHandlesFile(string path) => IOPath.GetExtension(path) == ".e2dbatch";
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
