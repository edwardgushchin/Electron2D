using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Electron2D;
using Path = System.IO.Path;

internal sealed class ScriptDrawTests
{
    private static bool _registered;
    private static readonly string SourcePath = Source();
    internal static bool FailBind, FailClear;
    internal static readonly Exception BindError = new IOException("script draw bind"), ClearError = new IOException("script draw clear");
    private static readonly ConditionalWeakTable<CanvasItem, State> States = new();
    private static RigidBody Create() => new();
    private static Entity CreateCanvas() => new();
    private static void BindCanvas(Entity owner)
    {
        var state = new State(Colors.Yellow); States.Add(owner, state);
        owner.SetScriptCallbacks(new CanvasItemScriptCallbacks<Entity> { Draw = state.Draw, Retired = state.Retire });
    }
    private static void ClearCanvas(Entity owner) { }
    private static void Register()
    {
        if (_registered) return;
        Script.RegisterBinding<RigidBody, ScriptDrawRed>("ScriptDraw.Red", ScriptDrawRed.SourcePath, Create, ScriptDrawRed.Bind, ScriptDrawRed.Clear);
        Script.RegisterBinding<RigidBody, ScriptDrawBlue>("ScriptDraw.Blue", ScriptDrawBlue.SourcePath, Create, ScriptDrawBlue.Bind, ScriptDrawBlue.Clear);
        Script.RegisterBinding<Entity, ScriptDrawTests>("ScriptDraw.Canvas", SourcePath, CreateCanvas, BindCanvas, ClearCanvas);
        _registered = true;
    }
    internal static void Install(RigidBody owner, Color color)
    {
        var state = new State(color); States.Add(owner, state);
        owner.SetScriptCallbacks(new RigidBodyScriptCallbacks<RigidBody> { Draw = state.Draw, Notification = state.Notify, Retired = state.Retire });
        if (FailBind) throw BindError;
    }
    private sealed class State(Color color)
    {
        internal int Draws, Releases;
        internal bool TraceEnabled, FailDraw;
        internal Script? SwitchTo;
        internal readonly Exception DrawError = new IOException("script draw recording");
        internal readonly List<string> Trace = [];
        internal void Record(string value) { if (TraceEnabled) Trace.Add(value); }
        internal void Notify(RigidBody owner, int what, bool reversed) { if (what == CanvasItem.NotificationDraw) Record("notification"); }
        internal void Draw(CanvasItem owner)
        {
            Draws++; Record("script"); owner.DrawRect(new(0, 0, 8, 8), color);
            if (FailDraw) { owner.DrawRect(new(0, 20, 8, 8), Colors.Magenta); throw DrawError; }
            if (SwitchTo is { } next) { SwitchTo = null; owner.SetScript(next); }
        }
        internal void Retire(CanvasItem owner) { States.Remove(owner); Releases++; }
    }
    private sealed class Driver : Node
    {
        internal Action Step = null!;
        protected override void OnProcess(double delta) => Step();
    }
    private sealed class TraceBody : RigidBody
    {
        protected override void OnDraw() { if (States.TryGetValue(this, out var state)) state.Record("native"); }
    }
    internal static void Run(PhysicsServer.Backend backend, string renderer, bool separate, string? childPath = null)
    {
        Register();
        using var red = ResourceLoader.Load<Script>(ScriptDrawRed.SourcePath); using var blue = ResourceLoader.Load<Script>(ScriptDrawBlue.SourcePath);
        using var canvasSource = ResourceLoader.Load<Script>(SourcePath);
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var oldSeparate = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread); var oldFPS = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate); Engine.MaxFPS = 240;
        var folder = Path.Combine(Path.GetTempPath(), "e2d-script-draw-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            using var packed = childPath is null ? null : ResourceLoader.Load<PackedScene>(childPath, ResourceLoader.CacheMode.IgnoreDeep);
            var body = packed is null ? new RigidBody() : (RigidBody)packed.Instantiate();
            body.Position = new(16, 12); body.GravityScale = 0; body.CanSleep = false; body.LinearDampMode = RigidBody.DampMode.Replace; body.LinearDamp = 0; body.LinearVelocity = new(12, 0);
            using var shape = new CircleShape { Radius = 2 };
            if (packed is null) { var collider = new CollisionShape { Shape = shape }; body.AddChild(collider); collider.Owner = body; }
            using var world = new World(backend); var window = new Window { Title = "Live Script draw", Size = new(144, 96), World = world };
            window.AddChild(body); var driver = new Driver { ProcessEnabled = true }; window.AddChild(driver);
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1]; var ticks = 0; var frames = 0; var complete = false;
            window.Ready += _ => { Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "Requested renderer without fallback."); RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.RenderLoopEnabled = false; window.Tree!.PhysicsFrameStarted += _ => ticks++; };
            driver.Step = () =>
            {
                Check(++frames < 120, "Bounded Script draw scenario."); if (ticks < 2) return;
                Check(body.GetType() == typeof(RigidBody) && world.PhysicsBackend == backend && world.PhysicsFallbackReason is null && body.Position.X > 16.1f, "A native owner moves on the selected full solver.");
                var id = body.InstanceID; var rid = body.GetRID(); var point = body.GlobalPosition;
                query.Position = point; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid && ReferenceEquals(hits[0].Collider, body), "Rendered receiver retains physical query identity.");
                RenderingServer.ForceDraw(false);
                if (packed is null) { Pixel(point, Colors.Black); body.SetScript(red); } // Attach after an empty recording was retained.
                RenderingServer.ForceDraw(false); Pixel(point, Colors.Red); Capture("red");
                var state = States.GetValue(body, _ => throw new InvalidOperationException()); var count = state.Draws;
                RenderingServer.ForceDraw(false); Check(state.Draws == count, "Retained replay does not reinvoke live code.");
                var signals = 0; body.Draw += _ => signals++; body.SetBlockSignals(true); body.QueueRedraw(); RenderingServer.ForceDraw(false);
                Check(state.Draws == count + 1 && signals == 0, "Signal blocking preserves the direct live draw hook."); body.SetBlockSignals(false);
                var traced = new TraceBody { Position = new(100, 60), GravityScale = 0 }; window.AddChild(traced); traced.SetScript(red);
                var trace = States.GetValue(traced, _ => throw new InvalidOperationException()); trace.TraceEnabled = true; traced.Draw += _ => trace.Record("signal");
                RenderingServer.ForceDraw(false); Check(trace.Trace.SequenceEqual(["notification", "signal", "native", "script"]), "Live drawing follows notification, signal and native hook."); traced.Dispose();
                state.FailDraw = true; body.QueueRedraw(); Expect(state.DrawError, () => RenderingServer.ForceDraw(false));
                Reject<InvalidOperationException>(() => body.DrawRect(default, Colors.White)); state.FailDraw = false; RenderingServer.ForceDraw(false); Pixel(point, Colors.Red); Pixel(point + new Vector2(0, 20), Colors.Black);
                body.Hide(); count = state.Draws; body.QueueRedraw(); RenderingServer.ForceDraw(false); Pixel(point, Colors.Black); Check(state.Draws == count, "Hidden items do not record live code.");
                body.Show(); RenderingServer.ForceDraw(false); Pixel(point, Colors.Red);
                window.RemoveChild(body); count = state.Draws; RenderingServer.ForceDraw(false); Pixel(point, Colors.Black); Check(state.Draws == count, "Detached canvas does not record."); window.AddChild(body); RenderingServer.ForceDraw(false); Pixel(point, Colors.Red);
                using (var copy = (RigidBody)body.Duplicate())
                {
                    Check(copy.GetType() == typeof(RigidBody) && States.TryGetValue(copy, out var copied) && !ReferenceEquals(copied, state), "Native copies bind independent drawing state.");
                    copy.Position = new(80, 12); window.AddChild(copy); RenderingServer.ForceDraw(false); Pixel(copy.GlobalPosition, Colors.Red); window.RemoveChild(copy);
                }
                if (childPath is null)
                {
                    using var scene = new PackedScene(); scene.Pack(body); var path = Path.Combine(folder, "draw.e2dscene"); ResourceSaver.Save(scene, path, SaverFlags.BundleResources); Child(path, backend, renderer, separate);
                }
                using (var canvas = canvasSource.New<Entity>())
                { canvas.Position = new(64, 60); window.AddChild(canvas); RenderingServer.ForceDraw(false); Pixel(canvas.GlobalPosition, Colors.Yellow); canvas.SetScript(null); RenderingServer.ForceDraw(false); Pixel(canvas.GlobalPosition, Colors.Black); window.RemoveChild(canvas); }
                body.SetScript(blue); RenderingServer.ForceDraw(false); Pixel(point, Colors.Blue); Capture("blue"); Check(state.Releases == 1, "Replacement retires old state once.");
                body.SetScript(null); RenderingServer.ForceDraw(false); Pixel(point, Colors.Black);
                FailBind = true; Expect(BindError, () => body.SetScript(red)); FailBind = false; RenderingServer.ForceDraw(false); Pixel(point, Colors.Black); Check(!States.TryGetValue(body, out _), "Partial bind removes slots and stale commands.");
                body.SetScript(red); RenderingServer.ForceDraw(false); FailClear = true; Expect(ClearError, () => body.SetScript(blue)); FailClear = false;
                RenderingServer.ForceDraw(false); Pixel(point, Colors.Black); Check(body.GetScript() is null, "Failed Clear invalidates retired commands and leaves owner unbound.");
                body.SetScript(red); state = States.GetValue(body, _ => throw new InvalidOperationException()); state.SwitchTo = blue;
                RenderingServer.ForceDraw(false); RenderingServer.ForceDraw(false); Pixel(point, Colors.Blue);
                Check(state.Releases == 1 && States.TryGetValue(body, out var current) && current.Draws == 1, "An in-draw replacement schedules the new recording after the active callback.");
                for (var i = 0; i < 128; i++) { body.QueueRedraw(); RenderingServer.ForceDraw(false); }
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
                var measured = States.GetValue(body, _ => throw new InvalidOperationException()); var before = measured.Draws;
                var active = Measure(true); Check(measured.Draws == before + 64, "Each measured redraw executes the live callback."); before = measured.Draws;
                var idle = Measure(false); Check(measured.Draws == before, "Measured retained frames reuse the recorded commands.");
                Check(active == (0, 0) && idle == (0, 0), "Live explicit/retained render intervals allocate zero owner/all-thread managed bytes.");
                Pixel(point, Colors.Blue); Check(body.GetRID() == rid && body.InstanceID == id && body.GlobalPosition == point, "Drawing retains physical state and identity.");
                body.Dispose(); RenderingServer.ForceDraw(false); Pixel(point, Colors.Black); Capture("retired");
                RenderingServer.RenderLoopEnabled = true; complete = true; window.Tree!.Quit();
            };
            Engine.Run(window); Check(complete, "The native scenario completed.");
            Console.WriteLine($"Live Script draw {backend}, renderer={renderer}, separate={separate}: native canvas/body pixels, replacement/copies/child files, failures and 0/0 render allocations passed.");

            (long, long) Measure(bool redraw)
            { var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); for (var i = 0; i < 64; i++) { if (redraw) body.QueueRedraw(); RenderingServer.ForceDraw(false); } return (GC.GetAllocatedBytesForCurrentThread() - owner, GC.GetTotalAllocatedBytes(true) - all); }
            void Pixel(Vector2 point, Color expected)
            { using var image = window.GetTexture().GetImage()!; var value = image.GetPixel((int)((point.X + 4) * image.Width / window.Size.X), (int)((point.Y + 4) * image.Height / window.Size.Y)); Check(Math.Abs(value.R - expected.R) < .05f && Math.Abs(value.G - expected.G) < .05f && Math.Abs(value.B - expected.B) < .05f, "Native pixels reflect the current installed code."); }
            void Capture(string name)
            { if (childPath is null && Environment.GetEnvironmentVariable("ELECTRON2D_SCRIPT_DRAW_CAPTURE") is { } directory) { Directory.CreateDirectory(directory); using var image = window.GetTexture().GetImage()!; image.SavePNG(Path.Combine(directory, name + ".png")); } }
        }
        finally { Directory.Delete(folder, true); ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSeparate); Engine.MaxFPS = oldFPS; }
    }
    private static void Child(string path, PhysicsServer.Backend backend, string renderer, bool separate)
    {
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in new[] { Assembly.GetExecutingAssembly().Location, "--script-draw-child", path }) start.ArgumentList.Add(argument);
        start.Environment["ELECTRON2D_TEST_SCRIPT_DRAW"] = backend == PhysicsServer.Backend.GPU ? "gpu" : "cpu"; start.Environment["ELECTRON2D_TEST_SCRIPT_DRAW_RENDERER"] = renderer; start.Environment["ELECTRON2D_TEST_THREADED_COLLIDER"] = separate ? "1" : "0";
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60000)) { process.Kill(true); throw new TimeoutException("Script draw child."); }
        Check(process.ExitCode == 0 && output.Result.Contains("Live Script draw " + backend, StringComparison.Ordinal), "Script drawing child failed: " + output.Result + error.Result);
    }
    private static void Expect(Exception expected, Action action) { try { action(); } catch (Exception error) when (ReferenceEquals(error, expected)) { return; } throw new InvalidOperationException("Expected original error: " + expected.Message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string Source([CallerFilePath] string path = "") => path;
}
