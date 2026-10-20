using System.Reflection;
using Electron2D;

internal static class WindowGoBackTests
{
    private sealed class ProbeWindow(List<int> trace, int marker) : Window
    {
        internal Action? During;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationWmGoBackRequest) { trace.Add(marker); During?.Invoke(); }
        }
    }

    private sealed class ProbeBody(List<int> trace, int marker) : RigidBody
    {
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationWmGoBackRequest) trace.Add(marker);
        }
    }

    private static Action Bind(Window window) => typeof(Window).GetMethod("HandleGoBack", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Action>(window);

    internal static void Run(PhysicsServer.Backend backend, bool separate = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        try { RunCore(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    internal static void RunNative(PhysicsServer.Backend backend, string renderer, bool separate = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
        try
        {
            foreach (var mode in new[] { "default", "cancel", "blocked", "explicit", "failure" })
                SystemBackContract.RunAsync(mode, Console.WriteLine, Request, backend, renderer).GetAwaiter().GetResult();
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void RunCore(PhysicsServer.Backend backend)
    {
        VerifyInitialPolicies();
        AllocationWarmupCheck.Verify(Collect);
        Check(!DisplayServer.TryQueueSystemGoBack(), "A retired or absent display does not accept host requests.");
        using (var display = DisplayServer.Open("System back contract", new(64, 64), hidden: true))
        using (var world = new World(backend))
        using (var shape = new CircleShape { Radius = 1 })
        {
            Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Back notification checks use the explicitly selected implementation.");
            var trace = new List<int>(4096);
            var window = new ProbeWindow(trace, 1) { World = world, GUIEmbedSubwindows = true };
            var body = new ProbeBody(trace, 2) { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, LinearVelocity = new(60, 0), CollisionLayer = 0, CollisionMask = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); window.AddChild(body);
            var child = new ProbeWindow(trace, 10) { Visible = false }; child.AddChild(new ProbeBody(trace, 11)); window.AddChild(child);
            var host = new SubViewport { World = world, GUIEmbedSubwindows = true }; host.AddChild(window);
            using var tree = new SceneTree(host);
            Check(tree.QuitOnGoBack && tree.AutoAcceptQuit, "Independent quit policies default to true.");
            tree.QuitOnGoBack = false; tree.AutoAcceptQuit = false;
            var descriptor = tree.GetPropertyList().OfType<PropertyDescriptor<SceneTree, bool>>().Single(p => p.Name == nameof(SceneTree.QuitOnGoBack));
            descriptor.SetValue(tree, true); Check(tree.QuitOnGoBack, "Typed policy authoring reaches the actual tree."); tree.QuitOnGoBack = false;
            var signals = 0; var enqueueDuringSignal = false; Exception? signalFailure = null;
            window.GoBackRequested += () =>
            {
                trace.Add(3); Check(!tree.QuitRequested, "Embedded requests do not quit the containing tree.");
                if (enqueueDuringSignal) { enqueueDuringSignal = false; Check(DisplayServer.TryQueueSystemGoBack(), "A subscriber can submit the next host batch."); }
                if (signalFailure is not null) throw signalFailure;
            };
            window.GoBackRequested += () => { trace.Add(4); signals++; };
            var bridge = Bind(window); DisplayServer.GoBackRequested += bridge;
            try
            {
                trace.Clear(); window.Notify(Node.NotificationWmGoBackRequest);
                Sequence(trace, 1); Check(signals == 0 && !tree.QuitRequested, "Manual notification emits no native window signal or quit request.");
                trace.Clear(); Request(); DisplayServer.ProcessEvents(); Sequence(trace, 1, 2, 3, 4);
                child.GoBackRequested += () => trace.Add(12); tree.QuitOnGoBack = true;
                trace.Clear(); Bind(child)(); Sequence(trace, 10, 11, 12); Check(!tree.QuitRequested, "A nested window owns its notification subtree without quitting the application."); tree.QuitOnGoBack = false;

                var before = signals; enqueueDuringSignal = true;
                RunWorker(() => { Request(); Request(); }); trace.Clear(); DisplayServer.ProcessEvents();
                Sequence(trace, 1, 2, 3, 4, 1, 2, 3, 4); Check(signals == before + 2, "Owner delivery captures one finite request batch.");
                trace.Clear(); DisplayServer.ProcessEvents(); Sequence(trace, 1, 2, 3, 4); Check(signals == before + 3, "A request submitted during delivery waits for the next pump.");

                var notificationFailure = new InvalidOperationException("Expected system back notification failure");
                signalFailure = new InvalidOperationException("Expected system back signal failure"); window.During = () => throw notificationFailure;
                before = signals; Request(); Request(); trace.Clear();
                try { DisplayServer.ProcessEvents(); throw new InvalidOperationException("Expected callback aggregate."); }
                catch (AggregateException error)
                {
                    var errors = error.Flatten().InnerExceptions;
                    Check(errors.Count == 4 && errors.Count(e => ReferenceEquals(e, notificationFailure)) == 2 && errors.Count(e => ReferenceEquals(e, signalFailure)) == 2, "Both captured requests preserve both original failures.");
                }
                Sequence(trace, 1, 2, 3, 4, 1, 2, 3, 4); Check(signals == before + 2, "Later receivers, subscribers and requests finish despite failures.");
                signalFailure = null; window.During = null; trace.Clear(); DisplayServer.ProcessEvents(); Sequence(trace);

                Request(); DisplayServer.ForceProcessAndDropEvents(); Sequence(trace, 1, 2, 3, 4);
                tree.QuitOnGoBack = true; window.SetBlockSignals(true); trace.Clear(); Request(); DisplayServer.ProcessEvents();
                Sequence(trace, 1, 2); Check(!tree.QuitRequested, "Blocking the embedded window signal retains its notifications.");
                window.SetBlockSignals(false); tree.QuitOnGoBack = false;
                display.SetBlockSignals(true); trace.Clear(); Request(); DisplayServer.ProcessEvents(); Sequence(trace); display.SetBlockSignals(false);
                DisplayServer.ProcessEvents(); Sequence(trace);
                RunWorker(() => { Reject<InvalidOperationException>(() => _ = tree.QuitOnGoBack); Reject<InvalidOperationException>(() => tree.QuitOnGoBack = true); });

                for (var i = 0; i < 128; i++) Frame();
                Collect(); var start = body.Position.X; var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"Back notification full moving frames allocate {owner}/{all} bytes.");
                Check(MathF.Abs(body.Position.X - start - 128) < .02f, "Event delivery retains genuine fixed-step motion.");
                Console.WriteLine($"System back {backend}/{ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: 128 complete moving event/physics frames {owner}/{all} B.");

                tree.QuitOnGoBack = true; window.During = () => tree.QuitOnGoBack = false;
                trace.Clear(); Request(); DisplayServer.ProcessEvents(); Check(!tree.QuitRequested && !tree.QuitOnGoBack, "Notification can cancel the current request before root policy.");
                window.During = null; tree.QuitOnGoBack = true; trace.Clear(); Request(); DisplayServer.ProcessEvents();
                Sequence(trace, 1, 2, 3, 4); Check(!tree.QuitRequested, "Even an enabled back policy does not turn child requests into application quit.");
                tree.AcceptWindowGoBack(); Check(tree.QuitRequested && tree.ExitCode == 0, "Tree back policy remains independent from disabled close auto-acceptance.");
            }
            finally { DisplayServer.GoBackRequested -= bridge; }
            tree.Dispose(); Reject<ObjectDisposedException>(() => _ = tree.QuitOnGoBack); Reject<ObjectDisposedException>(() => tree.QuitOnGoBack = false);
            var secondWindow = new Window(); var secondHost = new SubViewport { GUIEmbedSubwindows = true }; secondHost.AddChild(secondWindow);
            using var second = new SceneTree(secondHost); var finalBridge = Bind(secondWindow);
            secondWindow.GoBackRequested += () => second.Quit(37);
            DisplayServer.GoBackRequested += finalBridge;
            try { Request(); DisplayServer.ProcessEvents(); Check(second.QuitRequested && second.ExitCode == 37, "A public back subscriber can replace the default exit code."); }
            finally { DisplayServer.GoBackRequested -= finalBridge; }
            Request();

            void Frame() { trace.Clear(); Request(); DisplayServer.ProcessEvents(); Sequence(trace, 1, 2, 3, 4); tree.PhysicsFrame(1d / 60); }
        }
        Check(!DisplayServer.TryQueueSystemGoBack(), "Retired display does not accept late native callbacks.");
        using var next = DisplayServer.Open("Next system back lifetime", new(64, 64), hidden: true);
        var calls = 0; DisplayServer.GoBackRequested += () => calls++; DisplayServer.ProcessEvents();
        Check(calls == 0, "Pending requests from the retired display cannot target a later lifetime."); Request(); DisplayServer.ProcessEvents(); Check(calls == 1, "The next display accepts its own requests.");
    }

    private sealed class PolicyProbe : Node
    {
        protected override void OnReady() { base.OnReady(); Check(!Tree!.AutoAcceptQuit && Tree.QuitOnGoBack, "Configured independent policies and feature overrides apply before Ready."); }
    }

    private static void VerifyInitialPolicies()
    {
        var close = ProjectSettings.Get(ProjectSettings.AutoAcceptQuit); var back = ProjectSettings.Get(ProjectSettings.QuitOnGoBack);
        const string feature = "system-back-contract";
        Check(ProjectSettings.AddCustomFeature(feature), "Fixture owns its feature tag.");
        try
        {
            ProjectSettings.Set(ProjectSettings.AutoAcceptQuit, false); ProjectSettings.Set(ProjectSettings.QuitOnGoBack, false);
            ProjectSettings.SetFeatureOverride(ProjectSettings.QuitOnGoBack, feature, true);
            using var tree = new SceneTree(new PolicyProbe());
            ProjectSettings.Set(ProjectSettings.AutoAcceptQuit, true); ProjectSettings.ClearFeatureOverride(ProjectSettings.QuitOnGoBack, feature);
            Check(!tree.AutoAcceptQuit && tree.QuitOnGoBack, "Later registry edits preserve the current tree's policies.");
            tree.QuitOnGoBack = false; tree.AutoAcceptQuit = true;
            Check(!tree.QuitOnGoBack && tree.AutoAcceptQuit, "Runtime policies remain independently writable.");
            using var next = new SceneTree(new Node()); Check(next.AutoAcceptQuit && !next.QuitOnGoBack, "A subsequent tree samples current configured policies.");
        }
        finally
        {
            ProjectSettings.ClearFeatureOverride(ProjectSettings.QuitOnGoBack, feature); ProjectSettings.RemoveCustomFeature(feature);
            ProjectSettings.Set(ProjectSettings.AutoAcceptQuit, close); ProjectSettings.Set(ProjectSettings.QuitOnGoBack, back);
        }
    }

    private static void Request() => Check(DisplayServer.TryQueueSystemGoBack(), "The active host accepts a system request.");
    private static void Collect() { GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); }
    private static void Sequence(List<int> trace, params ReadOnlySpan<int> expected)
    { Check(trace.Count == expected.Length, "Exact containing-window trace length."); for (var i = 0; i < expected.Length; i++) Check(trace[i] == expected[i], "Exact notification and signal order."); }
    private static void RunWorker(Action action)
    {
        Exception? failure = null; var worker = new Thread(() => { try { action(); } catch (Exception error) { failure = error; } }); worker.Start(); worker.Join(); if (failure is not null) throw failure;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
