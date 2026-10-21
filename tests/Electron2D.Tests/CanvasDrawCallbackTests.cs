using Electron2D;

internal static class CanvasDrawCallbackTests
{
    private sealed class Driver : Node
    {
        internal Action Step = null!;
        protected override void OnProcess(double delta) => Step();
    }

    private sealed class Painter : RigidBody
    {
        internal int ExpectedThread, Notifications, Signals, Draws, Phase, FailStage = -1;
        internal bool Reenter, QueueDeletion;
        internal readonly Exception Failure = new InvalidOperationException("draw callback fixture");
        internal Painter()
        {
            Draw += _ =>
            {
                Check(Phase == 1, "Draw follows its notification"); Phase = 2; Signals++;
                DrawRect(new(12, 0, 8, 8), Colors.Green); Fail(1);
            };
        }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationDraw) return;
            Check(Environment.CurrentManagedThreadId == ExpectedThread, "Drawing stays on the scene owner");
            Phase = 1; Notifications++; DrawRect(new(0, 0, 8, 8), Colors.Red); Fail(0);
        }
        protected override void OnDraw()
        {
            Check(Phase == (IsBlockingSignals() ? 1 : 2), "The virtual callback follows notification and synchronous signal");
            Phase = 3; Draws++; DrawRect(new(24, 0, 8, 8), Colors.Blue); Fail(2);
            QueueRedraw(); // An in-scope request belongs to this recording.
            if (Reenter) Reject<InvalidOperationException>(() => RenderingServer.ForceDraw(false));
            if (QueueDeletion) QueueFree();
        }
        private void Fail(int stage)
        {
            if (FailStage != stage) return;
            DrawRect(new(0, 20, 8, 8), Colors.Magenta);
            throw Failure;
        }
    }

    internal static void Run(PhysicsServer.Backend backend, string renderer, bool threaded)
    {
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldFPS = Engine.MaxFPS; var oldTicks = Engine.PhysicsTicksPerSecond;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer);
            ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
            Engine.MaxFPS = 240; Engine.PhysicsTicksPerSecond = 60;
            using var world = new World(backend); using var shape = new CircleShape { Radius = 2 };
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            var window = new Window { Title = "Canvas draw callback", Size = new(128, 80), World = world };
            var body = new Painter
            {
                Position = new(8, 8),
                ExpectedThread = Environment.CurrentManagedThreadId,
                GravityScale = 0,
                CanSleep = false,
                CollisionMask = 0,
                LinearDamp = 0,
                AngularDamp = 0,
                LinearVelocity = new(60, 0)
            };
            body.AddChild(new CollisionShape { Shape = shape }); window.AddChild(body);
            var driver = new Driver { ProcessEnabled = true }; window.AddChild(driver);
            var ticks = 0; var process = 0; var phase = 0; var deferredDone = false; var complete = false;
            EventConnection? deferred = null; RID identity = default; Vector2 departed = default;
            (long Owner, long All) active = default, idle = default;
            window.Ready += _ =>
            {
                Check(RenderingServer.GetCurrentRenderingMethod() == renderer, "The requested renderer runs without fallback");
                RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.RenderLoopEnabled = false;
                window.Tree!.PhysicsFrameStarted += _ => ticks++;
            };
            driver.Step = () =>
            {
                Check(++process < 120, "The callback scenario completes within its frame bound");
                if (ticks < 2) return;
                if (phase == 0)
                {
                    Check(body.GlobalPosition.X > 8.1f && world.PhysicsBackend == backend && world.PhysicsFallbackReason is null,
                        "The selected physical receiver actually moves before callback verification");
                    identity = body.GetRID(); VerifyBody();
                    RenderingServer.ForceDraw(false); Pixels(true);
                    var before = body.Draws; RenderingServer.ForceDraw(false);
                    Check(body.Draws == before, "Retained replay does not repeat the callback or its in-scope redraw request");
                    body.QueueRedraw(); body.QueueRedraw(); body.QueueRedraw(); RenderingServer.ForceDraw(false);
                    Check(body.Draws == before + 1, "Several requests coalesce into one callback");
                    Reject<InvalidOperationException>(() => body.DrawRect(default, Colors.White));
                    body.SetBlockSignals(true); before = body.Signals; var draws = body.Draws;
                    body.QueueRedraw(); RenderingServer.ForceDraw(false);
                    Check(body.Signals == before && body.Draws == draws + 1, "Signal blocking preserves the virtual callback");
                    body.SetBlockSignals(false);
                    body.Hide(); before = body.Draws; body.QueueRedraw(); RenderingServer.ForceDraw(false);
                    Check(body.Draws == before, "Hidden items do not record"); Pixels(false); VerifyBody();
                    body.Show(); RenderingServer.ForceDraw(false); Check(body.Draws == before + 1, "Showing requests fresh recording");
                    window.RemoveChild(body); before = body.Draws; body.QueueRedraw(); RenderingServer.ForceDraw(false);
                    Check(body.Draws == before && body.Tree is null, "Detached redraw requests do not execute");
                    window.AddChild(body); RenderingServer.ForceDraw(false);
                    Check(body.Draws == before + 1 && body.GetRID() == identity, "Reentry records with the same public identity"); VerifyBody();
                    for (var stage = 0; stage < 3; stage++)
                    {
                        body.FailStage = stage; body.QueueRedraw();
                        try { RenderingServer.ForceDraw(false); throw new InvalidOperationException("Expected drawing failure"); }
                        catch (Exception error) when (ReferenceEquals(error, body.Failure)) { }
                        Reject<InvalidOperationException>(() => body.DrawRect(default, Colors.White));
                        body.FailStage = -1; RenderingServer.ForceDraw(false); Pixels(true);
                    }
                    body.Reenter = true; body.QueueRedraw(); RenderingServer.ForceDraw(false); body.Reenter = false;
                    Exception? wrongThread = null;
                    var worker = new Thread(() => { try { body.DrawRect(default, Colors.White); } catch (Exception error) { wrongThread = error; } });
                    worker.Start(); Check(worker.Join(5000) && wrongThread is InvalidOperationException, "An unrelated thread cannot draw");
                    for (var i = 0; i < 128; i++) { body.QueueRedraw(); RenderingServer.ForceDraw(false); }
                    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
                    before = body.Draws; active = Measure(true);
                    Check(body.Draws == before + 64, "Every measured explicit redraw invokes the virtual callback once");
                    before = body.Draws; idle = Measure(false);
                    Check(body.Draws == before, "Measured retained frames reuse commands");
                    Check(active == (0, 0) && idle == (0, 0), $"Draw callback frames allocated {active}/{idle} bytes");
                    Pixels(true); VerifyBody();
                    deferred = EventConnection.Subscribe<CanvasItem>(h => body.Draw += h, h => body.Draw -= h,
                        item => { Reject<InvalidOperationException>(() => item.DrawRect(default, Colors.White)); deferredDone = true; },
                        oneShot: true, defer: window.Tree!.Defer);
                    body.QueueRedraw(); RenderingServer.ForceDraw(false);
                    Check(!deferred.IsConnected && !deferredDone, "Deferred signal work leaves the recording scope"); phase = 1;
                }
                else if (phase == 1 && deferredDone)
                {
                    deferred!.Dispose(); departed = body.GlobalPosition;
                    body.QueueDeletion = true; body.QueueRedraw(); RenderingServer.ForceDraw();
                    Check(body.IsQueuedForDeletion && !body.IsDisposed, "Queued deletion lets the recording finish");
                    Pixels(true); phase = 2;
                }
                else if (phase == 2)
                {
                    Check(body.IsDisposed, "Queued deletion retires the physical drawing receiver");
                    query.Position = departed; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 0, "Retirement removes query geometry");
                    RenderingServer.ForceDraw(); Pixels(false);
                    RenderingServer.RenderLoopEnabled = true; complete = true; window.Tree!.Quit();
                }
            };
            Check(Engine.Run(window) == 0 && complete && window.IsDisposed && body.IsDisposed, "Native callback lifecycle completes and releases the host");
            Console.WriteLine($"Draw callback {backend}/{renderer}/threaded={threaded}: ordered stages, 3 failure retries, visibility/reentry/deferred/deletion; active={active.Owner}/{active.All} B, idle={idle.Owner}/{idle.All} B.");

            (long Owner, long All) Measure(bool redraw)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 64; i++) { if (redraw) body.QueueRedraw(); RenderingServer.ForceDraw(false); }
                return (GC.GetAllocatedBytesForCurrentThread() - owner, GC.GetTotalAllocatedBytes(true) - all);
            }
            void VerifyBody()
            {
                query.Position = body.GlobalPosition;
                Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == identity && ReferenceEquals(hits[0].Collider, body),
                    "The drawing receiver preserves its live public physical identity");
            }
            void Pixels(bool visible)
            {
                using var image = window.GetTexture().GetImage()!;
                var point = body.IsDisposed ? departed : body.GlobalPosition;
                var sx = image.Width / (float)window.Size.X; var sy = image.Height / (float)window.Size.Y;
                foreach (var (offset, expected) in new[] { (4, Colors.Red), (16, Colors.Green), (28, Colors.Blue) })
                {
                    var pixel = image.GetPixel((int)((point.X + offset) * sx), (int)((point.Y + 4) * sy));
                    var color = visible ? expected : Colors.Black;
                    Check(Math.Abs(pixel.R - color.R) < .05f && Math.Abs(pixel.G - color.G) < .05f && Math.Abs(pixel.B - color.B) < .05f,
                        "Actual pixels contain exactly the completed notification/signal/virtual stages");
                }
                Check(image.GetPixel((int)((point.X + 4) * sx), (int)((point.Y + 24) * sy)).R < .05f, "Failed commands do not survive retry");
                if ((phase == 1 && visible || phase == 2 && !visible) && Environment.GetEnvironmentVariable("ELECTRON2D_DRAW_CALLBACK_CAPTURE") is { } path)
                    image.SavePNG(System.IO.Path.Combine(path, visible ? "drawn.png" : "retired.png"));
            }
        }
        finally
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback);
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread); Engine.MaxFPS = oldFPS; Engine.PhysicsTicksPerSecond = oldTicks;
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
