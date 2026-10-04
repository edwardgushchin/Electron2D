using Electron2D;

internal static class ImmediateMeshRenderingTests
{
    internal static void Run()
    {
        var method = Environment.GetEnvironmentVariable("ELECTRON2D_MESH_RENDERER") == "compatibility" ? "compatibility" : "gpu";
        var settings = ProjectSettings.Service; var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, method);
        try { VerifyPixels(method); VerifyTopologies(method); VerifyWarm(method); }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); }
    }
    private static void VerifyPixels(string method)
    {
        using var mesh = new ImmediateMesh(); using var image = Image.CreateEmpty(2, 1, false, Image.Format.Rgba8);
        image.SetPixel(0, 0, Colors.Red); image.SetPixel(1, 0, Colors.Green); using var texture = ImageTexture.CreateFromImage(image);
        using var multiply = new CanvasItemMaterial { BlendMode = BlendMode.Mul }; using var add = new CanvasItemMaterial { BlendMode = BlendMode.Add };
        var window = new Window { Size = new(96, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var node = new MeshInstance { Mesh = mesh, Position = new(6, 6) }; window.AddChild(node);
        window.AddChild(new RIDNode(mesh.GetRID()) { Position = new(40, 6) }); var frames = 0;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles); mesh.SurfaceSetColor(Colors.Red);
        mesh.SurfaceAddVertex2D(Vector2.Zero); mesh.SurfaceAddVertex2D(new(20, 0)); mesh.SurfaceAddVertex2D(new(0, 20));
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            Check(RenderingServer.MeshGetSurfaceCount(mesh.GetRID()) == 0, "Server reads see no draft.");
            RenderingServer.FramePostDraw += () =>
            {
                using var frame = renderer.Readback();
                Pixel(frame, 8, 8, frames is 0 or 2 or 4 or 6 ? Colors.Black : Colors.Red);
                Pixel(frame, 42, 8, frames is 0 or 2 or 4 or 6 ? Colors.Black : frames == 3 || frames == 5 ? Colors.White : Colors.Red);
                if (frames is 3 or 5) Pixel(frame, 20, 8, Colors.Green);
                frame.SavePNG($"/tmp/e2d-immediate-{method}-{frames}.png");
                switch (++frames)
                {
                    case 1: mesh.SurfaceEnd(); Check(RenderingServer.MeshGetSurfaceCount(mesh.GetRID()) == 1, "Server sees the commit."); break;
                    case 2: mesh.ClearSurfaces(); break;
                    case 3:
                        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles); mesh.SurfaceSetUV(Vector2.Zero); mesh.SurfaceAddVertex2D(Vector2.Zero);
                        mesh.SurfaceSetUV(new(1, 0)); mesh.SurfaceAddVertex2D(new(20, 0)); mesh.SurfaceSetUV(new(0, 1)); mesh.SurfaceAddVertex2D(new(0, 20)); mesh.SurfaceEnd(); node.Texture = texture;
                        break;
                    case 4: node.Material = add; mesh.SurfaceSetMaterial(0, multiply); break;
                    case 5: mesh.SurfaceSetMaterial(0, null); break;
                    case 6: mesh.ClearSurfaces(); break;
                    default: window.Tree!.Quit(); break;
                }
            };
        };
        Engine.Run(window); Check(window.IsDisposed && frames == 7, "Seven native draft/commit/clear/UV/material phases.");
        Console.WriteLine($"ImmediateMesh native pixel and RID phases passed ({method}).");
    }
    private static void VerifyTopologies(string method)
    {
        var meshes = new List<ImmediateMesh>(); var window = new Window { Size = new(140, 48) };
        foreach (var primitive in Enum.GetValues<Mesh.PrimitiveType>())
        {
            var mesh = new ImmediateMesh(); meshes.Add(mesh); mesh.SurfaceBegin(primitive); mesh.SurfaceSetColor(Colors.Green);
            mesh.SurfaceAddVertex2D(Vector2.Zero); mesh.SurfaceAddVertex2D(new(16, 0));
            if (primitive is Mesh.PrimitiveType.Triangles or Mesh.PrimitiveType.TriangleStrip) mesh.SurfaceAddVertex2D(new(0, 16));
            mesh.SurfaceEnd(); window.AddChild(new MeshInstance { Name = primitive.ToString(), Mesh = mesh, Position = new(8 + (int)primitive * 26, 8) });
        }
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var frame = renderer.Readback();
                for (var i = 0; i < 5; i++)
                {
                    var green = 0; for (var y = 5; y < 28; y++) for (var x = 5 + i * 26; x < 30 + i * 26; x++) if (frame.GetPixel(x, y).G > .5f) green++;
                    Check(green > 0, $"Immediate topology {i} produces geometry.");
                }
                frame.SavePNG($"/tmp/e2d-immediate-topologies-{method}.png"); window.Tree!.Quit();
            };
        };
        try { Engine.Run(window); }
        finally { foreach (var mesh in meshes) mesh.Dispose(); }
    }
    private static void VerifyWarm(string method)
    {
        using var mesh = new ImmediateMesh(); ImmediateMeshTests.Triangle(mesh);
        using var material = new CanvasItemMaterial { BlendMode = BlendMode.Add };
        var window = new Window { Size = new(96, 64) }; var node = new MeshInstance { Mesh = mesh }; window.AddChild(node);
        var frames = 0; long start = 0, active = 0, idle = 0, preDraw = 0;
        var mutations = new long[148]; var sceneBytes = new long[148]; var renderBytes = new long[148];
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ =>
            {
                start = GC.GetAllocatedBytesForCurrentThread();
                if (frames < 84) { material.BlendMode = (BlendMode)(frames % 5); mesh.SurfaceSetMaterial(0, frames % 2 == 0 ? material : null); node.Position = new(frames % 2, 0); }
                mutations[frames] = GC.GetAllocatedBytesForCurrentThread() - start;
            };
            RenderingServer.FramePreDraw += () => { preDraw = GC.GetAllocatedBytesForCurrentThread(); sceneBytes[frames] = preDraw - start; };
            RenderingServer.FramePostDraw += () =>
            {
                var used = GC.GetAllocatedBytesForCurrentThread() - start;
                renderBytes[frames] = GC.GetAllocatedBytesForCurrentThread() - preDraw;
                if (frames is >= 20 and < 84) active += used; if (frames >= 84) idle += used;
                if (++frames == 148) window.Tree!.Quit();
            };
        };
        Engine.Run(window);
        for (var i = 20; i < 148; i++) if (sceneBytes[i] != 0 || renderBytes[i] != 0) Console.WriteLine($"ImmediateMesh frame {i}: mutation={mutations[i]}, scene={sceneBytes[i]}, renderer={renderBytes[i]} bytes.");
        Check(active == 0 && idle == 0, $"ImmediateMesh warm managed bytes: active={active}, idle={idle}.");
        Console.WriteLine($"ImmediateMesh 64 active/64 idle warmed native frames passed ({method}).");
    }
    private sealed class RIDNode(RID mesh) : Entity
    {
        protected override void OnDraw() => DrawMesh(mesh);
    }
    private static void Pixel(Image image, int x, int y, Color expected) { var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .05 && Math.Abs(actual.G - expected.G) < .05 && Math.Abs(actual.B - expected.B) < .05, $"Pixel {x},{y}: {actual} != {expected}."); }
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
}
