using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static class NativeStrokeTests
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint GLString(uint name);
    private sealed class Painter : Entity
    {
        internal Action<Painter> Paint = null!;
        protected override void OnDraw() => Paint(this);
    }
    private sealed class MovingLine : RigidBody
    {
        protected override void OnDraw()
        {
            DrawPolyline([Vector2.Zero, new(30, 3), new(10, 10)], Colors.Cyan, -1);
            DrawPrimitive([new(0, 15), new(20, 18)], [Colors.Magenta], []);
            DrawPrimitive([new(0, 20)], [Colors.Yellow], []);
        }
    }
    private sealed record Case(string Name, Vector2[] Points, Color[] Colors, bool Connected, Action<Painter> Draw, bool VertexColors = false, Vector2[]? UVs = null);
    internal static void Managed()
    {
        CanvasStrokeTests.Run();
        CanvasPolygonTests.Run();
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = [new(1, 1), new(5, 1), new(1, 5)] });
        using var painter = new Painter
        {
            Paint = n =>
        {
            n.DrawLine(new(.25f, .5f), new(10.25f, 4.5f), Colors.White);
            n.DrawLine(new(20, 0), new(30, 4), Colors.White);
            n.DrawMesh(mesh);
            n.DrawPolyline([Vector2.Zero, new(10, 0), new(10, 10)], Colors.White);
            n.DrawPolyline([new(20, 0), new(30, 0), new(30, 10)], Colors.White);
            n.DrawLine(new(0, 20), new(10, 20), Colors.White, -1, true);
        }
        };
        using var viewport = new SubViewport { SnapVerticesToPixel = true };
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); painter.PrepareCanvas();
        painter.AppendCanvas(vertices, batches, Transform.Identity, renderViewport: viewport);
        Check(batches[0].Primitive == Mesh.PrimitiveType.Lines && batches[0].Count == 4 && batches[0].SnapNativeLines, "Native lists merge without expanding or prematurely snapping endpoints");
        Check(batches[0].RasterMargin == 1 && (batches[0] with { SnapNativeLines = false }).RasterMargin == .5f && batches[1].RasterMargin == 0, "Native bounds retain half-pixel coverage plus endpoint snapping margin");
        Check(vertices[0].Position == new Vector2(.25f, .5f) && batches[1].Primitive == Mesh.PrimitiveType.Triangles && batches[1].Count == 3, "Mesh triangles cannot merge into native line batches");
        Check(batches.Count(b => b.Primitive == Mesh.PrimitiveType.LineStrip) == 2 && batches.Where(b => b.Primitive == Mesh.PrimitiveType.LineStrip).All(b => b.Count == 3), "Independent strips never acquire a connecting segment");
        Check(batches[^2].Primitive == Mesh.PrimitiveType.Lines && batches[^2].Count == 2 && batches[^1].Primitive == Mesh.PrimitiveType.Triangles && batches[^1].Count > 0, "The native single-line core precedes its local antialias triangles");
        painter.Paint = n => n.DrawLine(Vector2.Zero, new(10, 5), Colors.White, -1, true); painter.InvalidateCanvas(); painter.PrepareCanvas();
        vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, new Transform(Vector2.Zero, Vector2.Zero, Vector2.Zero));
        Check(batches.All(b => b.Primitive == Mesh.PrimitiveType.Triangles && b.Count % 3 == 0), "Collapsed native cores cannot consume antialias triangle vertices");
        painter.Paint = n =>
        {
            n.DrawPrimitive([new(.25f, .5f)], [Colors.Cyan], [new(.2f, .3f)]);
            n.DrawPrimitive([new(.25f, .5f), new(4, 7)], [Colors.Red, Colors.Blue], [Vector2.Zero, Vector2.One]);
            n.DrawPrimitive([Vector2.Zero, Vector2.Right, Vector2.Down], [], []);
        };
        painter.InvalidateCanvas(); painter.PrepareCanvas(); vertices.Clear(); batches.Clear();
        painter.AppendCanvas(vertices, batches, Transform.Identity, renderViewport: viewport);
        Check(batches.Count == 3 && batches[0].Primitive == Mesh.PrimitiveType.Points && batches[0].Count == 1 &&
              batches[1].Primitive == Mesh.PrimitiveType.Lines && batches[1].Count == 2 && batches[2].Primitive == Mesh.PrimitiveType.Triangles && batches[2].Count == 3,
            "Point and line primitives retain native topology independently of adjacent triangles");
        Check(vertices.Count == 6 && vertices[0].Position == new Vector2(0, 1) && vertices[0].UV == new Vector2(.2f, .3f) &&
              vertices[1].Color == Colors.Red && vertices[2].Color == Colors.Blue && !batches[0].SnapNativeLines && batches[0].RasterMargin == .5f,
            "Primitive positions, point UVs, endpoint colors and snap bounds survive native batching");
        Console.WriteLine("Native stroke/primitive topology, mesh boundaries, strip separation and split AA/snap policy passed.");
    }
    private static byte[] Module(string name)
    {
        using var stream = typeof(NativeStrokeTests).Assembly.GetManifestResourceStream("Electron2D.Device." + name)!;
        using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray();
    }
    private static Case[] Cases()
    {
        Vector2[] line = [new(8.25f, 9.5f), new(53.75f, 49.25f)];
        Vector2[] strip = [new(7.25f, 12.5f), new(50.5f, 24.75f), new(11.5f, 49.5f)];
        Vector2[] segments = [new(5.75f, 10.25f), new(51.25f, 28.5f), new(10.5f, 49.25f), new(52.75f, 34.5f)];
        Color[] gradient = [Colors.Red, Colors.Green, Colors.Blue]; Color[] segmentColors = [Colors.Yellow, Colors.Magenta];
        var center = new Vector2(32.25f, 30.75f);
        Vector2[] Curve(float x, float y, float from, float to, int count, bool loop = false)
        {
            var points = new Vector2[count];
            for (var i = 0; i < count; i++) { var angle = loop ? i * (Mathf.Tau / 64) : i / (count - 1f) * (to - from) + from; points[i] = center + new Vector2(x * MathF.Cos(angle), y * MathF.Sin(angle)); }
            if (loop) points[^1] = points[0]; return points;
        }
        Vector2[] rectangle = [new(9.25f, 12.5f), new(52.75f, 12.5f), new(52.75f, 48.25f), new(9.25f, 48.25f), new(9.25f, 12.5f)];
        Vector2[] dash = [new(8.25f, 20.5f), new(14.25f, 20.5f), new(20.25f, 20.5f), new(26.25f, 20.5f), new(32.25f, 20.5f), new(38.25f, 20.5f), new(44.25f, 20.5f), new(50.25f, 20.5f)];
        Vector2[] primitiveLineUVs = [new(.25f, .5f), new(.75f, .125f)];
        return [
            new("server primitive integer point", [new(16, 12)], [Colors.Cyan], false, n => RenderingServer.CanvasItemAddPrimitive(n.GetCanvasItem(), [new(16, 12)], [Colors.Cyan], [], default)),
            new("primitive fractional point", [new(16.25f, 12.5f)], [Colors.Yellow], false, n => n.DrawPrimitive([new(16.25f, 12.5f)], [Colors.Yellow], [])),
            new("primitive line", line, [Colors.Red, Colors.Blue], false, n => n.DrawPrimitive(line, [Colors.Red, Colors.Blue], []), true),
            new("primitive UV line", line, [Colors.Cyan, Colors.Magenta], false, n => RenderingServer.CanvasItemAddPrimitive(n.GetCanvasItem(), line, [Colors.Cyan, Colors.Magenta], primitiveLineUVs, default), true, primitiveLineUVs),
            new("line", line, [Colors.White], false, n => n.DrawLine(line[0], line[1], Colors.White)),
            new("polyline", strip, [Colors.White], true, n => n.DrawPolyline(strip, Colors.White, -1, true)),
            new("polyline colors", strip, gradient, true, n => n.DrawPolylineColors(strip, gradient, -1, true)),
            new("multiline", segments, [Colors.White], false, n => n.DrawMultiline(segments, Colors.White, -1, true)),
            new("multiline colors", segments, segmentColors, false, n => n.DrawMultilineColors(segments, segmentColors, -1, true)),
            new("arc", Curve(20, 20, .2f, 4.7f, 17), [Colors.White], true, n => n.DrawArc(center, 20, .2f, 4.7f, 17, Colors.White, -1, true)),
            new("circle", Curve(20, 20, 0, Mathf.Tau, 65, true), [Colors.White], true, n => n.DrawCircle(center, 20, Colors.White, false, -1, true)),
            new("ellipse", Curve(23, 13, 0, Mathf.Tau, 65, true), [Colors.White], true, n => n.DrawEllipse(center, 23, 13, Colors.White, false, -1, true)),
            new("ellipse arc", Curve(23, 13, 4.7f, .2f, 17), [Colors.White], true, n => n.DrawEllipseArc(center, 23, 13, 4.7f, .2f, 17, Colors.White, -1, true)),
            new("rectangle", rectangle, [Colors.White], true, n => n.DrawRect(new(rectangle[0], rectangle[2] - rectangle[0]), Colors.White, false)),
            new("dashes", dash, [Colors.White], false, n => n.DrawDashedLine(new(8.25f, 20.5f), new(56.25f, 20.5f), Colors.White, -1, 6, false, true)),
            new("short dash", [new(8.25f, 20.5f), new(11.25f, 20.5f)], [Colors.White], false, n => n.DrawDashedLine(new(8.25f, 20.5f), new(11.25f, 20.5f), Colors.White, -1, 6))
        ];
    }
    internal static void Run(string profile)
    {
        var gpu = profile.StartsWith("gpu", StringComparison.Ordinal); var baseline = Environment.GetEnvironmentVariable("ELECTRON2D_NATIVE_STROKE_BASELINE") == "1";
        var snap = profile.Contains("snap", StringComparison.Ordinal); var hdr = profile.Contains("hdr", StringComparison.Ordinal);
        var language = profile.Contains("hlsl", StringComparison.Ordinal) ? "HLSL" : "GLSL";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, gpu ? "gpu" : "compatibility"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.SnapVerticesToPixel, snap); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, profile.Contains("separate", StringComparison.Ordinal)); Engine.MaxFPS = 120;
        using var world = new World(profile.Contains("physics-gpu", StringComparison.Ordinal) ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU);
        var cases = Cases(); var height = ((cases.Length + 3) / 4) * 160 + 64;
        using var window = new Window { Size = new(768, height), World = world, UseHDR2D = hdr, MSAA2D = profile.Contains("msaa", StringComparison.Ordinal) ? Viewport.MSAA.Samples4X : Viewport.MSAA.Disabled };
        var customVertex = profile.Contains("vertex", StringComparison.Ordinal);
        using var fragment = gpu && (profile.Contains("material", StringComparison.Ordinal) || customVertex) ? customVertex
            ? Shader.CreateFromSPIRV(Module($"DeviceNative{(profile.Contains("declared", StringComparison.Ordinal) ? "Stroke" : "Primitive")}{language}.vert.spv"), Module($"DeviceNativeStroke{language}.frag.spv"))
            : Shader.CreateFromSPIRV(Module($"DeviceNativeStroke{language}.frag.spv")) : null;
        using var subjectMaterial = fragment is null ? null : new ShaderMaterial { Shader = fragment };
        var comparisons = new List<(Vector2i Origin, string Name)>();
        var subjects = new List<(Painter Painter, Case Definition, Vector2i Origin)>(); Image? oracle = null;
        Transform[] bases = [Transform.Identity, new(.18f, new(1.1f, .8f), .14f, new(3, -2)), new(0, new(-.8f, 1.1f), 0, new(56, 0)), new(0, new(1.25f, 1.5f), 0, new(-10.25f, -8.25f))];
        for (var row = 0; row < cases.Length; row++)
            for (var column = 0; column < bases.Length; column++)
            {
                var item = cases[row]; var basis = bases[column]; var origin = new Vector2i((row % 4) * 192, (row / 4) * 160 + column * 36);
                var cell = new Control { Position = origin, Size = new(64, 32), ClipContents = true };
                var painter = new Painter { Transform = basis, Paint = item.Draw, Material = subjectMaterial }; if (item.Name == "short dash" && !profile.StartsWith("software", StringComparison.Ordinal)) { var group = new CanvasGroup { FitMargin = 0, ClearMargin = 0 }; group.AddChild(painter); cell.AddChild(group); }
                else cell.AddChild(painter);
                window.AddChild(cell);
                comparisons.Add((origin, item.Name + "/" + column));
                subjects.Add((painter, item, origin));
            }
        var modulation = new CanvasModulate(); window.AddChild(modulation);
        var moving = new MovingLine { Position = new(12, height - 34), GravityScale = 0, CanSleep = false, CollisionLayer = 0, CollisionMask = 0, LinearVelocity = new(6, 0), LinearDamp = 0, LinearDampMode = RigidBody.DampMode.Replace };
        using var shape = new CircleShape { Radius = 1 }; moving.AddChild(new CollisionShape { Shape = shape }); window.AddChild(moving);
        var physicsTicks = 0; long physicsBefore = 0, physicsAllBefore = 0, physicsOwn = 0, physicsAll = 0;
        var frame = 0; long ownBefore = 0, allBefore = 0, activeOwn = 0, activeAll = 0, idleOwn = 0, idleAll = 0; var comparedPixels = 0; var colored = 0;
        window.Ready += _ =>
        {
            Console.WriteLine($"Selected canvas driver: {RenderingServer.GetCurrentRenderingDriverName()}");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            window.Tree!.PhysicsFrameStarted += _ => { physicsBefore = GC.GetAllocatedBytesForCurrentThread(); physicsAllBefore = GC.GetTotalAllocatedBytes(true); };
            PhysicsServer.BodySetStateSyncCallback(moving.GetRID(), _ =>
            {
                if (frame < 32) return;
                physicsOwn += GC.GetAllocatedBytesForCurrentThread() - physicsBefore; physicsAll += GC.GetTotalAllocatedBytes(true) - physicsAllBefore; physicsTicks++;
            });
            window.Tree!.ProcessFrameStarted += _ =>
            {
                ownBefore = GC.GetAllocatedBytesForCurrentThread(); allBefore = GC.GetTotalAllocatedBytes(true);
                if (frame is >= 32 and < 96) modulation.Color = (frame & 1) == 0 ? Colors.White : new(.5f, .5f, .5f, 1);
            };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame is >= 32 and < 96) { activeOwn += GC.GetAllocatedBytesForCurrentThread() - ownBefore; activeAll += GC.GetTotalAllocatedBytes(true) - allBefore; }
                if (frame is >= 96 and < 160) { idleOwn += GC.GetAllocatedBytesForCurrentThread() - ownBefore; idleAll += GC.GetTotalAllocatedBytes(true) - allBefore; }
                if (frame < 2)
                {
                    if (frame == 0 && RenderingServer.GetCurrentRenderingDriverName() == "opengl")
                    {
                        var query = Marshal.GetDelegateForFunctionPointer<GLString>(SDL3.SDL.GLGetProcAddress("glGetString"));
                        Console.WriteLine($"OpenGL vendor/renderer: {Marshal.PtrToStringAnsi(query(0x1F00))} / {Marshal.PtrToStringAnsi(query(0x1F01))}");
                    }
                    using var image = window.GetTexture().GetImage()!;
                    var marker = moving.GlobalPosition + new Vector2(0, 20); var markerPixels = 0; var markerPeak = 0f;
                    for (var y = (int)marker.Y - 2; y <= (int)marker.Y + 2; y++)
                        for (var x = (int)marker.X - 2; x <= (int)marker.X + 2; x++)
                        {
                            var pixel = image.GetPixel(x, y);
                            if (pixel.R > .1f && pixel.G > .1f && MathF.Abs(pixel.R - pixel.G) < .01f && pixel.B < .05f)
                            { markerPixels++; markerPeak = MathF.Max(markerPeak, pixel.R); }
                        }
                    Check(markerPixels > 0, "The moving physical subclass renders its inherited point primitive");
                    Console.WriteLine($"Physical primitive marker {profile}: pixels={markerPixels}, peak={markerPeak}.");
                    if (gpu) oracle ??= Oracle(subjects, new(image.Width, image.Height), snap, hdr, window.MSAA2D != Viewport.MSAA.Disabled, language, customVertex ? 2 : fragment is null ? 0 : 1);
                    if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } folder) { Directory.CreateDirectory(folder); image.SavePNG(System.IO.Path.Combine(folder, $"native-strokes-{profile}-{frame}.png")); oracle?.SavePNG(System.IO.Path.Combine(folder, $"native-strokes-{profile}-oracle.png")); }
                    using var golden = !gpu && Environment.GetEnvironmentVariable("ELECTRON2D_NATIVE_STROKE_GOLDEN") is { } path ? Image.LoadFromFile(path) : null;
                    foreach (var (origin, name) in comparisons)
                    {
                        var nonempty = 0;
                        for (var y = 0; y < 32; y++) for (var x = 0; x < 64; x++)
                            {
                                var actual = image.GetPixel(origin.X + x, origin.Y + y); if (actual.R + actual.G + actual.B > .1f) { colored++; nonempty++; }
                                if (baseline || !gpu && golden is null) continue;
                                var expected = gpu ? oracle!.GetPixel(origin.X + x, origin.Y + y) : golden!.GetPixel(origin.X + x, origin.Y + y);
                                if (MathF.Abs(actual.R - expected.R) > .012f || MathF.Abs(actual.G - expected.G) > .012f || MathF.Abs(actual.B - expected.B) > .012f || MathF.Abs(actual.A - expected.A) > .012f)
                                    throw new InvalidOperationException($"Native stroke {profile}/{name} at {x},{y}: actual={actual}, native={expected}");
                                comparedPixels++;
                            }
                        Check(nonempty > 0, $"Every clipped affine stroke case renders actual pixels: {name}");
                    }
                }
                if (++frame != 160) return;
                Check(physicsTicks > 8 && physicsOwn == 0 && physicsAll == 0, $"Selected moving physics allocated {physicsOwn}/{physicsAll} B in {physicsTicks} intervals");
                Check(activeOwn == 0 && activeAll == 0 && idleOwn == 0 && idleAll == 0, $"Native stroke managed bytes active={activeOwn}/{activeAll}, idle={idleOwn}/{idleAll}");
                Check(colored > 100 && PhysicsServer.SpaceGetTick(world.Space) > 2 && moving.Position.X > 12.05f && world.PhysicsFallbackReason is null, "Actual colored strokes accompany moving selected physics without fallback");
                window.Tree!.Quit();
            };
        };
        try { Check(Engine.Run(window) == 0 && frame == 160, "Native stroke frame loop completed"); }
        finally { window.Dispose(); oracle?.Dispose(); }
        Console.WriteLine($"Native strokes {profile}: {cases.Length * bases.Length} clipped affine cases, compared={comparedPixels}, colored={colored}; moving {world.PhysicsBackend} physics; active={activeOwn}/{activeAll}, idle={idleOwn}/{idleAll} B; physics={physicsOwn}/{physicsAll} B in {physicsTicks} intervals.");
    }
    private static Image Oracle(List<(Painter Painter, Case Definition, Vector2i Origin)> subjects, Vector2i size, bool snap, bool hdr, bool msaa, string language, int shaderMode)
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        using var module = new RDShaderSPIRV { BytecodeVertex = Module($"DeviceNativePrimitiveOracle{language}.vert.spv"), BytecodeFragment = Module($"DeviceNativeStroke{language}.frag.spv") }; var shader = device.ShaderCreateFromSPIRV(module);
        using var position = new RDVertexAttribute { Binding = 0, Location = 0, Stride = 32, Format = DataFormat.R32G32SFLOAT };
        using var color = new RDVertexAttribute { Binding = 0, Location = 1, Offset = 8, Stride = 32, Format = DataFormat.R32G32B32A32SFLOAT };
        using var uv = new RDVertexAttribute { Binding = 0, Location = 2, Offset = 24, Stride = 32, Format = DataFormat.R32G32SFLOAT };
        var vertexFormat = device.VertexFormatCreate([position, color, uv]);
        using var textureFormat = new RDTextureFormat { Width = (uint)size.X, Height = (uint)size.Y, Format = hdr ? DataFormat.R16G16B16A16SFLOAT : DataFormat.R8G8B8A8UNORM, IsResolveBuffer = msaa, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom };
        using var textureView = new RDTextureView(); var output = device.TextureCreate(textureFormat, textureView);
        RID framebuffer;
        if (msaa)
        {
            textureFormat.Samples = TextureSamples.Samples4; textureFormat.IsResolveBuffer = false; textureFormat.UsageBits = TextureUsageBits.ColorAttachment;
            var multisample = device.TextureCreate(textureFormat, textureView);
            using var pass = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [1] };
            framebuffer = device.FramebufferCreateMultipass([multisample, output], [pass]);
        }
        else framebuffer = device.FramebufferCreate([output]);
        using var raster = new RDPipelineRasterizationState(); using var blendAttachment = new RDPipelineColorBlendStateAttachment();
        using var blend = new RDPipelineColorBlendState { Attachments = [blendAttachment] };
        using var samples = new RDPipelineMultisampleState { SampleCount = msaa ? TextureSamples.Samples4 : TextureSamples.Samples1 };
        var lines = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), vertexFormat, RenderPrimitive.Lines, raster, blend, multisampleState: samples);
        var points = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), vertexFormat, RenderPrimitive.Points, raster, blend, multisampleState: samples);
        var strip = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), vertexFormat, RenderPrimitive.LineStrips, raster, blend, multisampleState: samples);
        float[] dimensions = [size.X, size.Y, shaderMode, 0]; var frameBuffer = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(dimensions.AsSpan()));
        using var frameUniform = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; frameUniform.AddID(frameBuffer);
        var frameSet = device.UniformSetCreate([frameUniform], shader, 1); var draws = new List<(RID Vertices, RID Pipeline, Vector2i Origin)>();
        foreach (var (painter, definition, origin) in subjects)
        {
            var pose = painter.GetGlobalTransform(); var data = new float[definition.Points.Length * 8];
            for (var i = 0; i < definition.Points.Length; i++)
            {
                var point = pose * definition.Points[i]; if (snap) point = (point + new Vector2(.5f, .5f)).Floor();
                var tint = definition.Colors[definition.VertexColors ? i < definition.Colors.Length ? i : 0 : Math.Min(definition.Colors.Length - 1, definition.Connected ? i : i / 2)];
                data[i * 8] = point.X; data[i * 8 + 1] = point.Y;
                if (definition.UVs is { } uvs) { data[i * 8 + 6] = uvs[i].X; data[i * 8 + 7] = uvs[i].Y; }
                data[i * 8 + 2] = tint.R; data[i * 8 + 3] = tint.G; data[i * 8 + 4] = tint.B; data[i * 8 + 5] = tint.A;
            }
            var buffer = device.VertexBufferCreate((uint)(data.Length * 4), MemoryMarshal.AsBytes(data.AsSpan()));
            draws.Add((device.VertexArrayCreate((uint)definition.Points.Length, vertexFormat, [buffer]), definition.Points.Length == 1 ? points : definition.Connected ? strip : lines, origin));
        }
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, msaa ? [Colors.Black, Colors.Black] : [Colors.Black]);
        foreach (var draw in draws)
        {
            device.DrawListBindRenderPipeline(list, draw.Pipeline); device.DrawListBindVertexArray(list, draw.Vertices);
            device.DrawListBindUniformSet(list, frameSet, 1); device.DrawListEnableScissor(list, new(draw.Origin, new Vector2(64, 32)));
            device.DrawListDraw(list, false, 1);
        }
        device.DrawListEnd(); var bytes = device.TextureGetData(output, 0);
        return Image.CreateFromData(size.X, size.Y, false, hdr ? Image.Format.Rgbah : Image.Format.Rgba8, bytes);
    }

}
