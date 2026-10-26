using Electron2D;

internal static partial class SkeletonGizmoTests
{
    private sealed class Rig : Skeleton
    {
        protected override void OnDraw() => DrawRect(new(4, 4, 6, 6), Colors.Blue);
    }
    private sealed class Gizmo : SkeletonModification
    {
        internal int Draws, Executes;
        internal Action? Inspect;
        internal bool Fail;
        protected override void OnExecute(double delta) => Executes++;
        protected override void OnDrawEditorGizmo()
        {
            Draws++; Inspect?.Invoke();
            var rig = GetModificationStack()!.GetSkeleton()!;
            rig.DrawRect(new(20, 4, 6, 6), new(0, 1, 0));
            rig.DrawSetTransform(new(80, 0));
            if (Fail) throw new InvalidOperationException("Gizmo drawing failure");
        }
        protected override Resource CreateDuplicateInstance() => new Gizmo();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
            => CopyModificationState((Gizmo)target);
    }
    private sealed class Driver : Node
    {
        internal Action? Step;
        protected override void OnProcess(double delta) => Step?.Invoke();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }

    public static void Run(PhysicsServer.Backend backend, bool threaded, string renderer)
    {
        var oldHint = Engine.IsEditorHint; var oldThreads = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var oldRenderer = ProjectSettings.Get(ProjectSettings.RenderingMethod); var oldFallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        var oldFPS = Engine.MaxFPS;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, renderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.IsEditorHint = false; Engine.MaxFPS = 120;
        try
        {
            using var world = new World(backend);
            using var window = new Window { Title = "Electron2D skeletal editor drawing", World = world, Size = new(192, 128), PhysicsInterpolationMode = PhysicsInterpolationMode.Off };
            var authored = new Node { Name = "Authored" }; window.AddChild(authored);
            var rig = new Rig { Name = "Rig", ProcessMode = ProcessMode.Disabled }; authored.AddChild(rig);
            using var stack = new SkeletonModificationStack { Enabled = true };
            using var mod = new Gizmo(); Check(!mod.EditorDrawGizmo, "Base editor drawing defaults false");
            mod.EditorDrawGizmo = true; stack.AddModification(mod); rig.SetModificationStack(stack);
            using var shape = new CircleShape { Radius = 2 };
            var moving = new RigidBody { Position = new(170, 100), GravityScale = 0, CanSleep = false, LinearVelocity = new(1, 0), CollisionMask = 0 };
            moving.AddChild(new CollisionShape { Shape = shape }); window.AddChild(moving);
            var driver = new Driver { ProcessEnabled = true }; window.AddChild(driver);
            var ticks = 0; var frames = 0; var complete = false; var owner = Environment.CurrentManagedThreadId;
            window.Ready += _ =>
            {
                RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.RenderLoopEnabled = false;
                window.Tree!.PhysicsFrameStarted += _ => ticks++;
            };
            driver.Step = () =>
            {
                Check(++frames < 120, "The native scenario reaches its bounded verification phase");
                if (ticks < 2) return;
                Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null && moving.Position.X > 170,
                    "Requested physics actually advances independently of editor drawing and renderer");
                RenderingServer.ForceDraw(false); Check(mod.Draws == 0, "Runtime drawing does not invoke editor callbacks");
                Engine.IsEditorHint = true; RenderingServer.ForceDraw(false);
                Check(mod.Draws == 0, "An editor hint without an edited-scene owner does not invoke callbacks");
                window.Tree!.EditedSceneRoot = authored;
                mod.Inspect = () =>
                {
                    Check(Environment.CurrentManagedThreadId == owner, "Editor callbacks use the scene owner");
                    Reject<InvalidOperationException>(() => mod.Dispose());
                    Reject<InvalidOperationException>(() => stack.Dispose());
                    Reject<InvalidOperationException>(() => rig.Dispose());
                    Reject<InvalidOperationException>(() => rig.SetModificationStack(null));
                    Reject<InvalidOperationException>(() => RenderingServer.ForceDraw(false));
                };
                RenderingServer.ForceDraw(false); mod.Inspect = null;
                Check(mod.Draws == 1 && mod.Executes == 0, "Editor registration draws once without numerical execution"); Pixels(true);
                Task.Run(() => Reject<InvalidOperationException>(() => mod.EditorDrawGizmo = false)).GetAwaiter().GetResult();
                var before = mod.Draws; mod.EditorDrawGizmo = false; RenderingServer.ForceDraw(false);
                Check(mod.Draws == before, "Disabled editor drawing does not dispatch"); Pixels(false);
                mod.EditorDrawGizmo = true; RenderingServer.ForceDraw(false); Pixels(true);
                mod.Fail = true;
                try { RenderingServer.ForceDraw(false); throw new Exception("Expected a failed gizmo recording"); }
                catch (InvalidOperationException error) { Check(error.Message == "Gizmo drawing failure", "The original callback error survives"); }
                mod.Fail = false; RenderingServer.ForceDraw(false); Pixels(true);
                Builtins(rig, stack, window);
                stack.SetModification(0, mod);
                for (var i = 0; i < 128; i++) RenderingServer.ForceDraw(false);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
                var allocatedOwner = GC.GetAllocatedBytesForCurrentThread(); var allocatedAll = GC.GetTotalAllocatedBytes(true);
                before = mod.Draws;
                for (var i = 0; i < 128; i++) RenderingServer.ForceDraw(false);
                allocatedOwner = GC.GetAllocatedBytesForCurrentThread() - allocatedOwner;
                allocatedAll = GC.GetTotalAllocatedBytes(true) - allocatedAll;
                Check(mod.Draws == before + 128 && allocatedOwner == 0 && allocatedAll == 0, $"Active editor redraws allocate {allocatedOwner}/{allocatedAll} B");
                window.Tree.EditedSceneRoot = null; before = mod.Draws; RenderingServer.ForceDraw(false);
                Check(mod.Draws == before && mod.Executes == 0, "Leaving authoring clears callbacks without executing the stack"); Pixels(false);
                window.Tree.EditedSceneRoot = authored; RenderingServer.ForceDraw(false); Pixels(true);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_GIZMO_CAPTURE") is { } path)
                { using var image = window.GetTexture().GetImage()!; image.SavePNG(path); }
                complete = true; window.Tree.Quit();
                Console.WriteLine($"Editor gizmos {backend}/{renderer}/{threaded}: owner registration, enable/copy/storage, real built-in/custom pixels, failure retry and 128 active redraws {allocatedOwner}/{allocatedAll} B passed.");
            };
            Check(Engine.Run(window) == 0 && complete && rig.IsDisposed, "Native editor callback host completes and retires");

            void Pixels(bool gizmo)
            {
                using var image = window.GetTexture().GetImage()!;
                var scaleX = image.Width / (float)window.Size.X; var scaleY = image.Height / (float)window.Size.Y;
                var blue = image.GetPixel((int)(6 * scaleX), (int)(6 * scaleY)); var green = image.GetPixel((int)(22 * scaleX), (int)(6 * scaleY));
                Check(blue.B > .8f && blue.R < .1f, "Gizmo transforms reset before ordinary canvas drawing");
                Check(gizmo ? green.G > .8f && green.B < .1f : green.G < .1f, "Actual target pixels follow editor gizmo visibility");
            }
        }
        finally
        {
            Engine.IsEditorHint = oldHint; Engine.MaxFPS = oldFPS;
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThreads);
            ProjectSettings.Set(ProjectSettings.RenderingMethod, oldRenderer); ProjectSettings.Set(ProjectSettings.RenderingFallback, oldFallback);
        }
    }
}
