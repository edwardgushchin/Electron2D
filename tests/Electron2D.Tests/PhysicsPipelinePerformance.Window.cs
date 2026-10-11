using System.Diagnostics;
using Electron2D;
using static PhysicsDebugTests;

internal static partial class PhysicsPipelinePerformance
{
    private sealed class SceneDisplay : IDisposable
    {
        private readonly string _renderMethod = ProjectSettings.Get(ProjectSettings.RenderingMethod);
        private readonly bool _fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback);
        private readonly ArrayMesh _mesh = new();
        private readonly MultiMesh _instances = new() { UseColors = true };
        private readonly float[] _current, _previous;
        private readonly float _scale;
        private readonly uint _windowID;
        internal bool InjectInput { get; } = Environment.GetEnvironmentVariable("ELECTRON2D_PHYSICS_PERFORMANCE_INJECT_INPUT") == "1";
        internal Window Window { get; }
        internal string VSync { get; private set; } = "";
        internal double PublishMS { get; private set; }
        internal double RenderMS { get; private set; }
        internal long PublishOwnerBytes { get; private set; }
        internal long ProcessOwnerBytes { get; private set; }
        internal long RenderOwnerBytes { get; private set; }

        internal SceneDisplay(World world, int count, float bottom)
        {
            Window = new Window
            {
                World = world,
                Title = $"Electron2D · {world.RequestedPhysicsBackend} monitored scene · {count}",
                Size = new(1152, 800),
                Unresizable = true
            };
            _current = new float[count * 12]; _previous = new float[count * 12];
            _scale = Math.Min(1088 / 1152f, 696 / (bottom - 190 + 24));
            try
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
                Window.OpenNative();
                var windows = SDL3.SDL.GetWindows(out var windowCount);
                Check(windowCount == 1 && windows is { Length: 1 }, "The scene host owns one native window");
                _windowID = SDL3.SDL.GetWindowID(windows![0]);
                Check(RenderingServer.GetCurrentRenderingMethod() == "gpu", "The monitored scene opens a real GPU-rendered window for either physics backend");
                DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled); VSync = DisplayServer.WindowGetVSyncMode().ToString();
                RenderingServer.SetDefaultClearColor(new(.145f, .105f, .16f));
                var vertices = new Vector2[9]; var indices = new int[24];
                for (var i = 0; i < 8; i++)
                { vertices[i + 1] = Vector2.FromAngle(i * Mathf.Tau / 8); indices[i * 3 + 1] = i + 1; indices[i * 3 + 2] = (i + 1) % 8 + 1; }
                _mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = vertices, Indices = indices });
                _instances.Mesh = _mesh; _instances.InstanceCount = count;
                _instances.CustomAABB = new(0, 0, 1152 * _scale, (bottom - 190 + 24) * _scale);
                Window.AddChild(new MultiMeshInstance { Name = "SceneInstances", MultiMesh = _instances, Position = new(32, 64) });
            }
            catch { Dispose(); throw; }
        }

        internal void Pump(SceneTree tree)
        {
            if (InjectInput)
            {
                var input = new SDL3.SDL.Event { Motion = new SDL3.SDL.MouseMotionEvent { Type = SDL3.SDL.EventType.MouseMotion, WindowID = _windowID, X = 10, Y = 10, XRel = 1 } };
                Check(SDL3.SDL.PushEvent(ref input), "The scene host accepts deterministic native input");
            }
            Window.PumpEvents(); Check(!tree.QuitRequested, "The native window was not closed before measurement completed");
        }

        internal void Draw(SceneTree tree, RigidBody[] bodies, double delta)
        {
            var start = Stopwatch.GetTimestamp();
            var owner = GC.GetAllocatedBytesForCurrentThread();
            _current.CopyTo(_previous, 0);
            for (var i = 0; i < bodies.Length; i++)
            {
                var pose = bodies[i].GlobalTransform; var record = _current.AsSpan(i * 12, 12);
                record[0] = pose.X.X * 6 * _scale; record[1] = pose.Y.X * 6 * _scale;
                record[4] = pose.X.Y * 6 * _scale; record[5] = pose.Y.Y * 6 * _scale;
                record[3] = pose.Origin.X * _scale; record[7] = (pose.Origin.Y - 190) * _scale;
                record[8] = .94f; record[9] = i % 3 == 0 ? .62f : .48f; record[10] = i % 3 == 0 ? .45f : .68f; record[11] = 1;
            }
            _instances.SetBufferInterpolated(_current, _previous);
            PublishMS = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            PublishOwnerBytes = GC.GetAllocatedBytesForCurrentThread() - owner;
            owner = GC.GetAllocatedBytesForCurrentThread();
            tree.ProcessFrame(delta);
            ProcessOwnerBytes = GC.GetAllocatedBytesForCurrentThread() - owner;
            owner = GC.GetAllocatedBytesForCurrentThread();
            start = Stopwatch.GetTimestamp(); Window.Render(tree, delta);
            RenderMS = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            RenderOwnerBytes = GC.GetAllocatedBytesForCurrentThread() - owner;
        }

        internal string Capture(RigidBody[] bodies)
        {
            Check(_instances.InstanceCount == bodies.Length, "All simulated bodies retain a rendered instance");
            for (var i = 0; i < bodies.Length; i++)
                Check(_instances.GetInstanceTransform2D(i).Origin.IsEqualApprox(new(bodies[i].GlobalPosition.X * _scale, (bodies[i].GlobalPosition.Y - 190) * _scale)),
                    "Rendered current records match every physical scene body");
            using var image = Window.GetTexture().GetImage()!;
            Check(image.Width == 1152 && image.Height == 800, "The real monitored scene capture has the requested size");
            var colored = 0;
            for (var y = 64; y < 760; y += 2) for (var x = 32; x < 1120; x += 2) if (image.GetPixel(x, y).R > .5f) colored++;
            Check(colored > 100, "The captured native window contains actual particle pixels");
            var output = System.IO.Path.GetFullPath(Environment.GetEnvironmentVariable("ELECTRON2D_PHYSICS_PERFORMANCE_OUTPUT") ?? "bin/physics-scene-pipeline");
            Directory.CreateDirectory(output); var path = System.IO.Path.Combine(output, Window.World!.PhysicsBackend + "-window.png"); image.SavePNG(path);
            return path;
        }

        public void Dispose()
        {
            try { Window.CloseNative(); }
            finally
            {
                try { Window.Dispose(); _instances.Dispose(); _mesh.Dispose(); }
                finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, _renderMethod); ProjectSettings.Set(ProjectSettings.RenderingFallback, _fallback); }
            }
        }
    }
}
