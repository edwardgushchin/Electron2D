using Electron2D;

internal static class MultiMeshRenderingTests
{
    internal static void Run()
    {
        var method = Environment.GetEnvironmentVariable("ELECTRON2D_MESH_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
        try { Pixels(method); Warm(method); InterpolatedPixels(method); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
        Console.WriteLine($"MultiMesh native resource/RID/color/custom-data/interpolation pixels and warm replay passed ({method}).");
    }
    private static void Pixels(string method)
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var resource = new MultiMesh { Mesh = mesh, UseColors = true, UseCustomData = true, InstanceCount = 2 };
        resource.SetInstanceTransform2D(0, new Transform(0, new(10, 10))); resource.SetInstanceTransform2D(1, new Transform(0, new(40, 10)));
        resource.SetInstanceColor(0, Colors.Red); resource.SetInstanceColor(1, Colors.Green); resource.SetInstanceCustomData(0, Colors.Blue); resource.SetInstanceCustomData(1, Colors.Yellow);
        using var shader = LoadShader(); using var material = new ShaderMaterial { Shader = shader };
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.SetPixel(0, 0, Colors.Magenta); using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(240, 120), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var node = new MultiMeshInstance { MultiMesh = resource }; window.AddChild(node);
        var phase = 0; RID owned = default;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            owned = RenderingServer.MultiMeshCreate(); RenderingServer.MultiMeshAllocateData(owned, 1, useColors: true, useCustomData: true); RenderingServer.MultiMeshSetMesh(owned, mesh.GetRID());
            var ownedMesh = RenderingServer.MeshCreate(); RenderingServer.MeshAddSurfaceFromArrays(ownedMesh, Mesh.PrimitiveType.Triangles, MeshTests.Quad()); RenderingServer.MultiMeshSetMesh(owned, ownedMesh);
            Check(RenderingServer.MultiMeshGetMesh(owned) == ownedMesh, "Owned child mesh identity remains canonical through instance queries."); RenderingServer.MultiMeshSetMesh(owned, mesh.GetRID()); RenderingServer.FreeRID(ownedMesh);
            RenderingServer.MultiMeshInstanceSetTransform2D(owned, 0, new Transform(0, new(70, 10))); RenderingServer.MultiMeshInstanceSetColor(owned, 0, Colors.Blue);
            Check(RenderingServer.MultiMeshGetInstanceCount(owned) == 1 && RenderingServer.MultiMeshGetBuffer(owned).Length == 16 && RenderingServer.MultiMeshGetMesh(owned) == mesh.GetRID(), "Owned server producer/read path.");
            Reject<InvalidOperationException>(() => RenderingServer.FreeRID(resource.GetRID()));
            Reject<InvalidOperationException>(() => RenderingServer.MultiMeshSetBuffer(resource.GetRID(), resource.Buffer));
            Reject<NotSupportedException>(() => RenderingServer.MultiMeshAllocateData(owned, 1, useIndirect: true));
            Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.MultiMeshGetInstanceCount(owned))).GetAwaiter().GetResult();
            window.AddChild(new RIDNode(owned));
            RenderingServer.FramePostDraw += () =>
            {
                using var frame = renderer.Readback();
                if (phase == 0) { Pixel(frame, window, 15, 15, Colors.Red); Pixel(frame, window, 45, 15, Colors.Green); Pixel(frame, window, 75, 15, Colors.Blue); resource.SetInstanceTransform2D(1, new Transform(0, new(40, 40))); resource.SetInstanceColor(0, Colors.Green); }
                else if (phase == 1) { Pixel(frame, window, 15, 15, Colors.Green); Pixel(frame, window, 45, 15, Colors.Black); Pixel(frame, window, 45, 45, Colors.Green); resource.VisibleInstanceCount = 1; }
                else if (phase == 2) { Pixel(frame, window, 45, 45, Colors.Black); resource.VisibleInstanceCount = -1; RenderingServer.MultiMeshSetVisibleInstances(owned, 0); }
                else if (phase == 3)
                {
                    Pixel(frame, window, 75, 15, Colors.Black);
                    if (method == "gpu") node.Material = material; else resource.CustomAABB = new(new(10000, 10000), new(10, 10));
                }
                else if (phase == 4)
                {
                    if (method == "gpu") { Pixel(frame, window, 15, 15, Colors.Blue); Pixel(frame, window, 45, 45, Colors.Yellow); resource.SetInstanceCustomData(0, Colors.Red); }
                    else { Pixel(frame, window, 15, 15, Colors.Black); resource.CustomAABB = default; }
                }
                else if (phase == 5)
                {
                    Pixel(frame, window, 15, 15, method == "gpu" ? Colors.Red : Colors.Green); resource.CustomAABB = new(new(10000, 10000), new(10, 10));
                }
                else if (phase == 6) { Pixel(frame, window, 15, 15, Colors.Black); resource.CustomAABB = default; node.Material = null; resource.SetInstanceColor(0, Colors.White); resource.SetInstanceColor(1, Colors.White); node.Texture = texture; }
                else { Pixel(frame, window, 15, 15, Colors.Magenta); Pixel(frame, window, 45, 45, Colors.Magenta); RenderingServer.FreeRID(owned); window.Tree!.Quit(); }
                frame.SavePNG(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-multimesh-{method}-{phase}.png")); phase++;
            };
        };
        Check(Engine.Run(window) == 0 && window.IsDisposed && phase == 8 && !resource.IsDisposed && !mesh.IsDisposed && !texture.IsDisposed, "Resource/node ownership, actual native frames and teardown.");
        Reject<ArgumentException>(() => RenderingMultiMeshRegistry.Resolve(owned));
        if (method == "compatibility")
        {
            var rejected = new Window { Size = new(240, 120) }; rejected.AddChild(new MultiMeshInstance { MultiMesh = resource, Material = material });
            Reject<NotSupportedException>(() => Engine.Run(rejected)); Check(rejected.IsDisposed, "Fallback rejects the custom shader and releases its host.");
        }
    }
    private sealed class RIDNode(RID resource) : Entity
    {
        protected override void OnDraw() => DrawMultiMesh(resource);
    }
    private static void Warm(string method)
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var resource = new MultiMesh { Mesh = mesh, UseColors = true, InstanceCount = 256 };
        for (var i = 0; i < 256; i++) { resource.SetInstanceTransform2D(i, new Transform(0, new((i % 16) * 12, i / 16 * 6))); resource.SetInstanceColor(i, Colors.Red); }
        var window = new Window { Size = new(240, 120) }; var node = new MultiMeshInstance { MultiMesh = resource }; window.AddChild(node);
        var frames = 0; long activeBytes = 0, idleBytes = 0, start = 0;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ =>
            {
                if (frames is >= 20 and < 84) { start = GC.GetAllocatedBytesForCurrentThread(); resource.SetInstanceTransform2D(0, new Transform(0, new(frames % 2, 0))); resource.SetInstanceColor(0, frames % 2 == 0 ? Colors.Red : Colors.Blue); }
                else if (frames >= 104) start = GC.GetAllocatedBytesForCurrentThread();
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frames is >= 20 and < 84) activeBytes += GC.GetAllocatedBytesForCurrentThread() - start;
                else if (frames >= 104) idleBytes += GC.GetAllocatedBytesForCurrentThread() - start;
                frames++;
                if (frames == 168) window.Tree!.Quit();
            };
        };
        Engine.Run(window); Check(activeBytes == 0 && idleBytes == 0, $"256 prepared instances, 64 active and 64 idle frames after 20 warm frames: {activeBytes}/{idleBytes} managed bytes ({method}).");
    }
    private static void InterpolatedPixels(string method)
    {
        var settings = ProjectSettings.Service; var old = ProjectSettings.Get(ProjectSettings.PhysicsInterpolation); var fps = Engine.MaxFPS; var ticks = Engine.PhysicsTicksPerSecond;
        ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true); Engine.MaxFPS = 120; Engine.PhysicsTicksPerSecond = 30;
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [Vector2.Zero, new(6, 0), new(0, 6)], Colors = [Colors.Red, Colors.Red, Colors.Red] });
        using var resource = new MultiMesh { Mesh = mesh, InstanceCount = 1 }; resource.SetInstanceTransform2D(0, new Transform(0, new(10, 40)));
        var window = new Window { Size = new(240, 120) }; window.AddChild(new MultiMeshInstance { MultiMesh = resource }); var mover = new Mover(resource); window.AddChild(mover);
        var frames = 0; var sampled = false; ulong firstTick = 0;
        try
        {
            window.Ready += _ =>
            {
                firstTick = Engine.PhysicsFrames; var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    if (++frames > 500) throw new InvalidOperationException("No suitable instance interpolation frame.");
                    var fraction = Engine.PhysicsInterpolationFraction;
                    if (Engine.PhysicsFrames - firstTick < 3 || fraction is < .2 or > .6) return;
                    var logical = resource.GetInstanceTransform2D(0).Origin.X; using var image = renderer.Readback();
                    Pixel(image, window, logical - 24 + 24 * (float)fraction + 1, 41, Colors.Red); Pixel(image, window, logical + 1, 41, Colors.Black);
                    sampled = true; window.Tree!.Quit();
                };
            };
            Engine.Run(window); Check(sampled && window.IsDisposed, "Native output presents the instance pose between actual physics ticks.");
        }
        finally { if (!window.IsDisposed) window.Dispose(); Engine.MaxFPS = fps; Engine.PhysicsTicksPerSecond = ticks; ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, old); }
    }
    private sealed class Mover(MultiMesh resource) : Node
    {
        protected override void OnReady() => PhysicsProcessEnabled = true;
        protected override void OnPhysicsProcess(double delta) { var pose = resource.GetInstanceTransform2D(0); pose.Origin.X += 24; resource.SetInstanceTransform2D(0, pose); }
    }
    private static Shader LoadShader()
    {
        using var input = typeof(MultiMeshRenderingTests).Assembly.GetManifestResourceStream("TestShaders.InstanceCustom.spv")!; using var output = new MemoryStream(); input.CopyTo(output); return Shader.CreateFromSPIRV(output.ToArray());
    }
    private static void Pixel(Image image, Window window, float x, float y, Color expected)
    {
        var actual = image.GetPixel((int)MathF.Round(x * image.Width / window.Size.X), (int)MathF.Round(y * image.Height / window.Size.Y));
        Check(Math.Abs(actual.R - expected.R) < .01f && Math.Abs(actual.G - expected.G) < .01f && Math.Abs(actual.B - expected.B) < .01f, $"Pixel {x},{y}: {actual} vs {expected}.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
