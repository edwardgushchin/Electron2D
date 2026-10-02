using System.Buffers.Binary;
using Electron2D;

internal static class MeshRenderingTests
{
    internal static void Run()
    {
        var method = Environment.GetEnvironmentVariable("ELECTRON2D_MESH_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var settings = ProjectSettings.Instance; var previous = settings.Get(ProjectSettings.RenderingMethod); settings.Set(ProjectSettings.RenderingMethod, method);
        try { VerifyPixels(method); VerifyPrimitives(method); VerifyWarm(method); }
        finally { settings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static void VerifyPixels(string method)
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(), flags: Mesh.ArrayFormat.UseDynamicUpdate);
        var window = new Window { Size = new(96, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var node = new MeshInstance { Mesh = mesh, Position = new(4, 4) }; window.AddChild(node); RID owned = default; var frames = 0; var attributes = new byte[12 * 4]; var vertices = new byte[8 * 4];
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black);
            owned = renderer.MeshCreate(); renderer.MeshAddSurfaceFromArrays(owned, Mesh.PrimitiveType.Triangles, MeshTests.Quad());
            Check(renderer.MeshGetSurfaceCount(owned) == 1 && renderer.MeshSurfaceGetArrayLen(owned, 0) == 4, "Owned server mesh producer/query.");
            Check(renderer.MeshSurfaceGetFormatVertexStride(mesh.SurfaceGetFormat(0), 4) == 8 && renderer.MeshSurfaceGetFormatAttributeStride(mesh.SurfaceGetFormat(0), 4) == 12, "Pinned 2D channel strides.");
            Reject<NotSupportedException>(() => renderer.MeshSurfaceGetFormatVertexStride(Mesh.ArrayFormat.Vertex, 4));
            Reject<NotSupportedException>(() => renderer.MeshSurfaceGetFormatAttributeStride((Mesh.ArrayFormat)2, 4));
            Reject<ArgumentOutOfRangeException>(() => renderer.MeshSurfaceGetFormatVertexStride(Mesh.ArrayFormat.None, -1));
            Reject<InvalidOperationException>(() => renderer.FreeRID(mesh.GetRID()));
            Reject<InvalidOperationException>(() => renderer.MeshClear(mesh.GetRID()));
            Task.Run(() => Reject<InvalidOperationException>(() => renderer.MeshGetSurfaceCount(owned))).GetAwaiter().GetResult();
            renderer.FreeRID(owned); owned = renderer.MeshCreate();
            renderer.MeshAddSurfaceFromArrays(owned, Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.Green));
            window.AddChild(new DrawRIDNode(owned) { Position = new(64, 4) });
            renderer.FramePostDraw += () =>
            {
                using var image = renderer.Readback();
                Pixel(image, 72, 12, Colors.Green);
                if (frames == 0) { Pixel(image, 8, 8, Colors.Red); Pixel(image, 22, 22, Colors.Red); Pixel(image, 28, 8, Colors.Black); }
                else if (frames == 1) { Pixel(image, 8, 8, Colors.Blue); Pixel(image, 22, 22, Colors.Blue); }
                else if (frames == 2) { Pixel(image, 8, 8, Colors.Black); Pixel(image, 38, 8, Colors.Blue); }
                else { Pixel(image, 38, 8, Colors.Black); window.Tree!.Quit(); }
                image.SavePNG($"/tmp/e2d-mesh-{method}-{frames}.png"); frames++;
                if (frames == 1)
                {
                    for (var i = 0; i < 4; i++) { attributes[i * 12 + 2] = 255; attributes[i * 12 + 3] = 255; }
                    mesh.SurfaceUpdateAttributeRegion(0, 0, attributes);
                }
                else if (frames == 2)
                {
                    var source = MeshTests.Quad().Vertices;
                    for (var i = 0; i < 4; i++) { BinaryPrimitives.WriteSingleLittleEndian(vertices.AsSpan(i * 8), source[i].X + 30); BinaryPrimitives.WriteSingleLittleEndian(vertices.AsSpan(i * 8 + 4), source[i].Y); }
                    mesh.SurfaceUpdateVertexRegion(0, 0, vertices);
                }
                else if (frames == 3) mesh.ClearSurfaces();
            };
        };
        Engine.Instance.Run(window); Reject<ArgumentException>(() => RenderingMeshRegistry.Resolve(owned)); Check(window.IsDisposed && frames == 4, "Native mesh visible edit/remove phases.");
        Console.WriteLine($"Mesh native pixels passed ({method}).");
    }
    private sealed class DrawRIDNode(RID mesh) : Entity
    {
        protected override void OnDraw() => DrawMesh(mesh);
    }
    private static void VerifyPrimitives(string method)
    {
        var resources = new List<ArrayMesh>(); var window = new Window { Size = new(100, 60), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        using var image = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8); image.SetPixel(0, 0, Colors.Red); image.SetPixel(1, 0, Colors.Green);
        using var texture = ImageTexture.CreateFromImage(image); using var material = new CanvasItemMaterial { BlendMode = BlendMode.Add }; using var fallback = new CanvasItemMaterial { BlendMode = BlendMode.Mul };
        foreach (var primitive in Enum.GetValues<Mesh.PrimitiveType>())
        {
            var mesh = new ArrayMesh(); resources.Add(mesh);
            var points = primitive == Mesh.PrimitiveType.Triangles ? new Vector2[] { new(0, 0), new(12, 0), new(0, 12) } : new Vector2[] { new(0, 0), new(12, 0), new(0, 12), new(12, 12) };
            mesh.AddSurfaceFromArrays(primitive, new() { Vertices = points, UVs = points.Select(p => p / 12).ToArray() }); mesh.SurfaceSetMaterial(0, material);
            window.AddChild(new MeshInstance { Name = primitive.ToString(), Mesh = mesh, Material = fallback, Texture = primitive == Mesh.PrimitiveType.TriangleStrip ? texture : null, Position = new(6 + (int)primitive * 18, 8) });
        }
        var frames = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black);
            renderer.FramePostDraw += () =>
            {
                using var frame = renderer.Readback();
                // Filled primitives and framebuffer-width primitives exercise every live topology without assuming edge rasterization equivalence.
                Pixel(frame, 63, 11, Colors.White); Pixel(frame, 81, 11, Colors.Red); Pixel(frame, 87, 11, Colors.Green);
                for (var primitive = 0; primitive < 3; primitive++)
                {
                    var colored = 0;
                    for (var y = 5; y < 23; y++) for (var x = 3 + primitive * 18; x < 21 + primitive * 18; x++) if (frame.GetPixel(x, y).R > .5f) colored++;
                    Check(colored > 0, $"Topology {primitive} produces framebuffer geometry with its surface material override.");
                }
                frame.SavePNG($"/tmp/e2d-mesh-primitives-{method}.png"); frames++; window.Tree!.Quit();
            };
        };
        try { Engine.Instance.Run(window); Check(frames == 1, "All mesh primitive topologies render."); }
        finally { foreach (var resource in resources) resource.Dispose(); }
    }
    private static void VerifyWarm(string method)
    {
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(), flags: Mesh.ArrayFormat.UseDynamicUpdate);
        var window = new Window { Size = new(96, 64) }; var node = new MeshInstance { Mesh = mesh }; window.AddChild(node);
        var frames = 0; var bytes = new byte[8]; long start = 0, active = 0, idle = 0, renderStart = 0;
        var cpuBytes = new long[148]; var renderBytes = new long[148];
        window.Ready += _ =>
        {
            RenderingServer.Instance!.FramePreDraw += () => { renderStart = GC.GetAllocatedBytesForCurrentThread(); cpuBytes[frames] = renderStart - start; };
            window.Tree!.ProcessFrameStarted += _ =>
            {
                start = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 84) { BinaryPrimitives.WriteSingleLittleEndian(bytes, frames % 2 == 0 ? 0 : 1); mesh.SurfaceUpdateVertexRegion(0, 0, bytes); }
            };
            RenderingServer.Instance!.FramePostDraw += () =>
            {
                var used = GC.GetAllocatedBytesForCurrentThread() - start;
                renderBytes[frames] = GC.GetAllocatedBytesForCurrentThread() - renderStart;
                if (frames is >= 20 and < 84) active += used; if (frames >= 84) idle += used;
                if (++frames == 148) window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window);
        for (var i = 20; i < 148; i++) if (cpuBytes[i] != 0 || renderBytes[i] != 0) Console.WriteLine($"Mesh frame {i}: scene={cpuBytes[i]}, renderer={renderBytes[i]} managed bytes.");
        Check(active == 0 && idle == 0, $"Native mesh warm managed bytes active={active}, idle={idle}.");
        Console.WriteLine($"Mesh 64 active/64 idle warmed render frames passed ({method}).");
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .05 && Math.Abs(actual.G - expected.G) < .05 && Math.Abs(actual.B - expected.B) < .05, $"Pixel {x},{y}: {actual} != {expected}."); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
}
