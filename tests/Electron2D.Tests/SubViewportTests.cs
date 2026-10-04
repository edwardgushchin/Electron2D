using Electron2D;

internal static class SubViewportTests
{
    internal static void Run() { Authoring(); SceneCopies(); Input(); RecordingScale(); Console.WriteLine("Offscreen viewport authoring, scene-local copies and input boundary checks passed."); }
    private static void Authoring()
    {
        using var viewport = new SubViewport(); Check(viewport.Size == new Vector2i(512, 512) && viewport.Size2DOverride == Vector2i.Zero && !viewport.Size2DOverrideStretch && !viewport.TransparentBG && viewport.RenderTargetUpdateMode == ViewportUpdateMode.WhenVisible && viewport.RenderTargetClearMode == ViewportClearMode.Always, "Defaults."); var texture = viewport.GetTexture(); Check(ReferenceEquals(texture, viewport.GetTexture()) && texture.GetSize() == new Vector2(512, 512) && texture.GetImage() is null && texture.ResourceLocalToScene && texture.PixelFormat == Image.Format.Rgba8 && !texture.HasAlpha && !texture.HasMipmaps, "Live view defaults without a renderer."); var rid = texture.GetRID(); var viewportRID = viewport.GetViewportRID(); Check(viewportRID.IsValid() && viewportRID == viewport.GetViewportRID() && viewportRID != rid, "Stable distinct viewport identity."); viewport.Size = new(-1, 1); Check(viewport.Size == new Vector2i(2, 2) && texture.GetSize() == new Vector2(2, 2) && texture.GetRID() == rid, "Minimum clamp and stable identity."); var sizes = 0; var changes = 0; viewport.SizeChanged += () => sizes++; texture.Changed += _ => changes++; viewport.Size = new(64, 32); Check(sizes == 1 && changes == 1, "Single texture/size notification."); viewport.Size2DOverride = new(32, 8); viewport.Size2DOverrideStretch = true; Check(viewport.GetVisibleRect() == new Rect2(0, 0, 32, 8) && viewport.GetFinalTransform().Scale == new Vector2(2, 4), "Logical override/stretch."); viewport.GlobalCanvasTransform = new(0, new(2, 3)); Near(viewport.GetFinalTransform() * Vector2.Zero, new(4, 12)); viewport.Size2DOverride = new(0, 8); Check(viewport.GetFinalTransform() == viewport.GlobalCanvasTransform, "Partial-zero override disables stretch."); Reject<ArgumentOutOfRangeException>(() => viewport.RenderTargetClearMode = (ViewportClearMode)99); Reject<ArgumentOutOfRangeException>(() => viewport.RenderTargetUpdateMode = (ViewportUpdateMode)99);
        Action fail = () => throw new InvalidOperationException("size fixture"); viewport.SizeChanged += fail; Reject<AggregateException>(() => viewport.Size = new(80, 40)); Check(texture.GetSize() == new Vector2(80, 40) && changes >= 5, "Committed size and texture delivery after observer failure."); viewport.SizeChanged -= fail; viewport.Dispose(); Check(texture.GetSize() == Vector2.Zero && texture.GetImage() is null && texture.GetRID() == rid && !texture.IsDisposed, "View survives target lifetime as an unresolved resource."); texture.Dispose();
    }
    private static void SceneCopies()
    {
        using var source = new Node { Name = "root" }; var viewport = new SubViewport { Name = "view", Size = new(48, 32), Size2DOverride = new(24, 16), Size2DOverrideStretch = true, TransparentBG = true, RenderTargetUpdateMode = ViewportUpdateMode.Always, RenderTargetClearMode = ViewportClearMode.Once }; var sprite = new Sprite { Name = "sprite", Texture = viewport.GetTexture() }; source.AddChild(viewport); source.AddChild(sprite); viewport.Owner = source; sprite.Owner = source; using var packed = new PackedScene(); packed.Pack(source); using var first = packed.Instantiate(); using var second = packed.Instantiate(); var viewA = (SubViewport)first.GetNode("view"); var spriteA = (Sprite)first.GetNode("sprite"); var viewB = (SubViewport)second.GetNode("view"); var textureA = (ViewportTexture)spriteA.Texture!; var textureB = (ViewportTexture)((Sprite)second.GetNode("sprite")).Texture!; Check(viewA.Size == new Vector2i(48, 32) && viewA.Size2DOverrideStretch && viewA.TransparentBG && viewA.RenderTargetClearMode == ViewportClearMode.Once && textureA.GetSize() == new Vector2(48, 32), "Packed offscreen settings and local path."); Check(!ReferenceEquals(textureA, textureB) && textureA.GetLocalScene() == first && textureB.GetLocalScene() == second, "Independent scene-local views."); viewA.Size = new(64, 32); Check(textureA.GetWidth() == 64 && textureB.GetWidth() == 48 && viewB.Size.X == 48 && viewport.Size.X == 48, "Each cloned view binds its own viewport.");
        using var authored = new ViewportTexture { ViewportPath = "view" }; using var authoredCopy = (ViewportTexture)authored.Duplicate(true); Check(authoredCopy.ViewportPath == "view" && authoredCopy.GetSize() == Vector2.Zero, "Unbound authored path copy."); viewport.GetTexture().Dispose();
    }
    private static void Input()
    {
        var root = new TestViewport(); var outside = new Listener { InputEnabled = true }; var viewport = new SubViewport { Size = new(64, 64), Size2DOverride = new(32, 32), Size2DOverrideStretch = true }; var inside = new Listener { InputEnabled = true }; root.AddChild(outside); root.AddChild(viewport); viewport.AddChild(inside); using var scene = new SceneTree(root); using var input = new InputEventMouseMotion { Position = new(16, 20) }; root.PushInput(input, true); Check(outside.Calls == 1 && inside.Calls == 0, "Root input skips nested viewports."); viewport.PushInput(input); Check(inside.Calls == 1 && outside.Calls == 1 && inside.Position == new Vector2(8, 10), "Explicit input isolates and removes stretch."); Check(inside.GetViewport() == viewport && inside.GetWindow() is null, "Containing viewport remains distinct from a native window."); Task.Run(() => Reject<InvalidOperationException>(() => viewport.Size = new(40, 40))).GetAwaiter().GetResult();
    }
    private static void RecordingScale()
    {
        using var viewport = new SubViewport { Size = new(64, 64) }; using var style = new StyleBoxFlat(); style.SetCornerRadiusAll(4); var painter = new StyledBox(style); viewport.AddChild(painter); using var scene = new SceneTree(viewport); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); painter.PrepareCanvas(); painter.AppendCanvas(vertices, batches, Transform.Identity); var normal = vertices.Min(v => v.Position.X); viewport.Size2DOverride = new(32, 32); viewport.Size2DOverrideStretch = true; painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity); var scaled = vertices.Min(v => v.Position.X); Check(Math.Abs(normal + .5f) < .0001 && Math.Abs(scaled + .25f) < .0001, $"Nonunit recording scale divides AA feather and invalidates retained geometry: {normal}, {scaled}.");
    }
    private sealed class StyledBox(StyleBoxFlat style) : Entity { protected override void OnDraw() => DrawStyleBox(style, new(0, 0, 20, 20)); }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 64, 64); }
    private sealed class Listener : Node { internal int Calls; internal Vector2 Position; protected override void OnInput(InputEvent inputEvent) { Calls++; if (inputEvent is InputEventMouse mouse) Position = mouse.Position; } }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_VIEWPORT_RENDERER") ?? "gpu"; var settings = ProjectSettings.Service; var prior = ProjectSettings.Get(ProjectSettings.RenderingMethod); ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            var scenario = Environment.GetEnvironmentVariable("ELECTRON2D_VIEWPORT_SCENARIO");
            if (scenario == "warm") { Warm(backend); return; }
            if (scenario == "dependencies") { Dependencies(backend); return; }
            using var premultiplied = new CanvasItemMaterial { BlendMode = BlendMode.PremultAlpha };
            for (var run = 0; run < 2; run++)
            {
                var window = new Window { Size = new(128, 96), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var viewport = new SubViewport { Name = "view", Size = new(32, 32), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var box = new Box { Position = new(4, 4) }; viewport.AddChild(box); var viewTexture = viewport.GetTexture(); var sprite = new Sprite { Texture = viewTexture, Centered = false, Position = new(40, 8), TextureFilter = TextureFilter.Nearest }; window.AddChild(viewport); window.AddChild(sprite); var stage = 0;
                window.Ready += _ =>
                {
                    var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.ViewportGetTexture(viewport.GetViewportRID()) == viewTexture.GetRID(), "Server viewport/texture RID projection."); RenderingServer.FramePostDraw += () =>
                    {
                        using var pixels = renderer.Readback(); using var target = viewport.GetTexture().GetImage(); Check(target is not null, "Offscreen image completed.");
                        Check(box.GetWindow() == window, "Offscreen nodes retain the containing window.");
                        var x = stage == 0 ? 4 : 12; if (stage <= 2) { Pixel(target!, x, 4, stage == 0 ? Colors.Red : Colors.Green); Pixel(pixels, 40 + x, 12, stage == 0 ? Colors.Red : Colors.Green); }
                        switch (stage++)
                        {
                            case 0: box.Position = new(12, 4); box.Fill = Colors.Green; box.QueueRedraw(); break;
                            case 1: viewport.RenderTargetUpdateMode = ViewportUpdateMode.Disabled; box.Fill = Colors.Blue; box.QueueRedraw(); break;
                            case 2: viewport.RenderTargetUpdateMode = ViewportUpdateMode.Once; break;
                            case 3: Pixel(target!, 12, 4, Colors.Blue); Check(viewport.RenderTargetUpdateMode == ViewportUpdateMode.Disabled, "Once consumed after submission."); viewport.RenderTargetUpdateMode = ViewportUpdateMode.Always; viewport.Size = new(48, 32); viewport.RenderTargetClearMode = ViewportClearMode.Never; box.Position = new(24, 4); box.Fill = Colors.Red; box.QueueRedraw(); break;
                            case 4: Check(target!.Width == 48, "Native resize."); Pixel(target!, 24, 4, Colors.Red); viewport.RenderTargetClearMode = ViewportClearMode.Once; box.Position = new(4, 4); break;
                            case 5: Pixel(target!, 4, 4, Colors.Red); Pixel(target!, 24, 4, Colors.Black); Check(viewport.RenderTargetClearMode == ViewportClearMode.Never, "Once clear consumed."); box.Position = new(20, 4); break;
                            case 6: Pixel(target!, 4, 4, Colors.Red); Pixel(target!, 20, 4, Colors.Red); viewport.TransparentBG = true; viewport.RenderTargetClearMode = ViewportClearMode.Always; box.Visible = false; break;
                            case 7: Check(target!.GetPixel(1, 1) == default(Color), "Transparent target clears to zero premultiplied RGBA."); sprite.Visible = false; viewport.RenderTargetUpdateMode = ViewportUpdateMode.WhenVisible; box.Visible = true; box.Fill = Colors.Green; box.QueueRedraw(); break;
                            case 8: Check(target!.GetPixel(4, 4).A < .001, "Unsampled WhenVisible retains pixels."); viewport.RenderTargetUpdateMode = ViewportUpdateMode.WhenParentVisible; break;
                            case 9: Pixel(target!, 20, 4, Colors.Green); viewport.RenderTargetUpdateMode = ViewportUpdateMode.Always; box.Position = new(4, 4); box.Fill = new(1, 0, 0, .5f); box.QueueRedraw(); sprite.Visible = true; sprite.Material = premultiplied; break;
                            case 10: var alpha = target!.GetPixel(4, 4); Check(Math.Abs(alpha.R - .5f) < .03 && Math.Abs(alpha.A - .5f) < .03, $"Native target stores premultiplied color/alpha: {alpha}."); Pixel(pixels, 44, 12, new(.5f, 0, 0)); window.Tree!.Quit(); break;
                        }
                    };
                };
                Check(Engine.Run(window) == 0 && stage == 11 && window.IsDisposed, "Offscreen host shutdown."); viewTexture.Dispose(); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "offscreen-host", backend, run, stages = stage, cleaned = window.IsDisposed }));
            }
            Dependencies(backend); Warm(backend); if (backend == "gpu") ShaderUniform();
        }
        finally { ProjectSettings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private static void Dependencies(string backend)
    {
        var window = new Window { Size = new(160, 96), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var a = new SubViewport { Name = "a", Size = new(64, 32), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var b = new SubViewport { Name = "b", Size = new(64, 32), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var source = new Box { Position = new(4, 4) }; a.AddChild(source); var textureA = a.GetTexture(); var textureB = b.GetTexture(); var input = new Sprite { Texture = textureA, Centered = false, TextureFilter = TextureFilter.Nearest }; b.AddChild(input); var output = new Sprite { Texture = textureB, Centered = false, Position = new(40, 8), TextureFilter = TextureFilter.Nearest }; window.AddChild(a); window.AddChild(b); window.AddChild(output); var stage = 0; using var material = new CanvasItemMaterial { BlendMode = BlendMode.PremultAlpha }; var feedback = new Sprite { Texture = textureA, Centered = false, Position = new(32, 0), TextureFilter = TextureFilter.Nearest, Material = material, Visible = false }; a.AddChild(feedback); var externalLayer = new CanvasLayer { CustomViewport = a }; var extra = new Box { Position = new(16, 16), Fill = Colors.Blue }; externalLayer.AddChild(extra); window.AddChild(externalLayer);
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () =>
            {
                using var image = renderer.Readback(); using var first = textureA.GetImage(); using var second = textureB.GetImage(); Check(stage == 4 || first is not null && second is not null, "Dependency targets complete before their consumer at stage " + stage);
                if (stage == 0) { Pixel(image, 44, 12, Colors.Red); Pixel(first!, 16, 16, Colors.Blue); Pixel(image, 16, 16, Colors.Black); source.Fill = Colors.Green; source.QueueRedraw(); feedback.Visible = true; }
                else if (stage == 1) { Pixel(image, 44, 12, Colors.Green); Pixel(first!, 36, 4, Colors.Red); Pixel(second!, 36, 4, Colors.Red); a.Size2DOverride = new(32, 16); a.Size2DOverrideStretch = true; feedback.Visible = false; source.Position = new(4, 4); }
                else if (stage == 2) { Pixel(first!, 8, 8, Colors.Green); a.RenderTargetUpdateMode = ViewportUpdateMode.Always; output.Visible = false; window.Hide(); source.Fill = Colors.Blue; source.QueueRedraw(); }
                else if (stage == 3) { Pixel(first!, 8, 8, Colors.Blue); if (backend == "gpu" && Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "wayland") { Reject<NotSupportedException>(() => window.Show()); Check(!window.Visible, "Unsafe remap rejects before managed visibility changes."); } else window.Show(); window.RemoveChild(a); }
                else if (stage == 4) { Check(textureA.GetImage() is null && !a.IsDisposed, "Detached target releases native storage without owning the node."); a.Dispose(); window.Tree!.Quit(); }
                stage++;
            };
        };
        Check(Engine.Run(window) == 0 && stage == 5 && window.IsDisposed, "Dependency/feedback/hidden/detached host cleanup."); textureA.Dispose(); textureB.Dispose(); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "offscreen-dependencies", backend, stages = stage }));
    }
    private static void Warm(string backend)
    {
        var window = new Window { Size = new(128, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var viewport = new SubViewport { Size = new(32, 32), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var source = new Box { Position = new(4, 4) }; viewport.AddChild(source);
        var texture = viewport.GetTexture(); var rootTexture = window.GetTexture();
        var sprite = new Sprite { Texture = texture, Position = new(48, 24), TextureFilter = TextureFilter.Nearest };
        window.AddChild(viewport); window.AddChild(sprite);
        var frame = 0; var phase = 0; var warm = 32; var measured = 0; var coldResizes = 0;
        long before = 0, active = 0, idle = 0; var previousSize = Vector2.Zero; var frameSize = Vector2.Zero;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ =>
            {
                frameSize = rootTexture.GetSize();
                before = GC.GetAllocatedBytesForCurrentThread();
                if (phase == 0) { source.Fill = (frame & 1) == 0 ? Colors.Red : Colors.Green; source.Position = new((frame & 1) == 0 ? 4 : 8, 4); source.QueueRedraw(); }
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                var size = rootTexture.GetSize();
                // Native DPI/size reconfiguration recreates targets and is a cold operation, outside warmed intervals.
                if (size != previousSize || size != frameSize) { warm = 32; coldResizes++; }
                else if (warm > 0) warm--;
                else
                {
                    if (phase == 0) active += bytes; else idle += bytes;
                    Check(bytes == 0, $"Warm offscreen phase {phase}, frame {frame}: {bytes} bytes with stable dimensions {size}.");
                    if (++measured == 64)
                    {
                        measured = 0;
                        if (phase++ == 0) { viewport.RenderTargetUpdateMode = ViewportUpdateMode.Disabled; sprite.Visible = false; warm = 32; }
                        else window.Tree!.Quit();
                    }
                }
                previousSize = size;
                Check(++frame < 2048, "Native dimensions did not stabilize for warmed measurements.");
            };
        };
        Check(Engine.Run(window) == 0 && phase == 2 && active == 0 && idle == 0, $"Warm offscreen frames allocate active={active}, idle={idle} managed bytes.");
        texture.Dispose(); rootTexture.Dispose();
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "offscreen-warm", backend, active, idle, frames = frame, coldResizes, measuredPerPhase = 64 }));
    }
    internal static void ShaderUniform()
    {
        using var input = typeof(SubViewportTests).Assembly.GetManifestResourceStream("TestShaders.TextureHlsl.spv")!; using var memory = new MemoryStream(); input.CopyTo(memory); using var shader = Shader.CreateFromSPIRV(memory.ToArray()); using var material = new ShaderMaterial { Shader = shader }; using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.White); using var white = ImageTexture.CreateFromImage(image); var window = new Window { Size = new(96, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest }; var viewport = new SubViewport { Name = "uniform_view", Size = new(32, 32) }; var source = new Box { Position = new(4, 4) }; viewport.AddChild(source); var texture = viewport.GetTexture(); material.SetShaderParameter("colorMap", texture); material.SetShaderParameter("detailMap", white); material.SetShaderParameter("tint", Colors.White); var sprite = new Sprite { Texture = white, Centered = false, Scale = new(32, 32), Position = Vector2.Zero, Material = material, TextureFilter = TextureFilter.Nearest }; window.AddChild(viewport); window.AddChild(sprite); var stage = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += () => { using var pixels = renderer.Readback(); Pixel(pixels, 12, 12, stage == 0 ? Colors.Red : Colors.Green); using var serverImage = RenderingServer.Texture2DGet(RenderingServer.ViewportGetTexture(viewport.GetViewportRID())); Pixel(serverImage!, 4, 4, stage == 0 ? Colors.Red : Colors.Green); if (stage++ == 0) { source.Fill = Colors.Green; source.QueueRedraw(); } else window.Tree!.Quit(); };
        };
        Check(Engine.Run(window) == 0 && stage == 2, "Uniform-only visibility dependency and live native rebinding."); texture.Dispose(); Console.WriteLine("Offscreen shader-uniform and server RID pixel checks passed.");
    }
    private sealed class Box : Entity { internal Color Fill = Colors.Red; protected override void OnDraw() => DrawRect(new(0, 0, 8, 8), Fill); }
    private static void Pixel(Image image, int x, int y, Color color) { var pixel = image.GetPixel(x, y); Check(Math.Abs(pixel.R - color.R) < .03 && Math.Abs(pixel.G - color.G) < .03 && Math.Abs(pixel.B - color.B) < .03, $"Pixel {x},{y}: {pixel} != {color}"); }
    private static void Near(Vector2 actual, Vector2 expected) => Check(actual.DistanceTo(expected) < .0001f, "Coordinate transform.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
