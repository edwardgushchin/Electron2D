using Electron2D;

internal static class CanvasCompositionTests
{
    internal static void Run()
    {
        CanvasClipTests.Run();
        using var group = new CanvasGroup(); using var copy = new BackBufferCopy();
        Check(group.FitMargin == 10 && group.ClearMargin == 10 && !group.UseMipmaps && copy.CopyMode == BackBufferCopyMode.Rect && copy.Rect == new Rect2(-100, -100, 200, 200), "Defaults.");
        Reject<ArgumentOutOfRangeException>(() => group.FitMargin = -1); Reject<ArgumentOutOfRangeException>(() => group.ClearMargin = float.NaN);
        Reject<ArgumentException>(() => copy.Rect = new(float.PositiveInfinity, 0, 1, 1)); Reject<ArgumentOutOfRangeException>(() => copy.CopyMode = (BackBufferCopyMode)99);
        Check(group.FitMargin == 10 && group.ClearMargin == 10 && copy.Rect == new Rect2(-100, -100, 200, 200), "Invalid writes preserve values.");
        var changes = 0; copy.PropertyListChanged += _ => changes++; copy.CopyMode = copy.CopyMode; Check(changes == 1, "Equal copy mode notifies property list.");
        var rects = 0; copy.ItemRectChanged += _ => rects++; copy.Rect = copy.Rect; Check(rects == 1, "Equal rectangle emits item change.");
        Action<ElectronObject> fail = _ => throw new InvalidOperationException("copy fixture"); copy.PropertyListChanged += fail; Reject<InvalidOperationException>(() => copy.CopyMode = BackBufferCopyMode.Viewport); Check(copy.CopyMode == BackBufferCopyMode.Viewport, "Observer failure commits policy."); copy.PropertyListChanged -= fail;
        using var source = new Node { Name = "scene" }; var storedGroup = new CanvasGroup { Name = "group", FitMargin = 3.5f, ClearMargin = 7, UseMipmaps = true, SelfModulate = new(1, 1, 1, .5f) }; var storedCopy = new BackBufferCopy { Name = "copy", CopyMode = BackBufferCopyMode.Viewport, Rect = new(16, 8, -32, 24) }; source.AddChild(storedGroup); source.AddChild(storedCopy); storedGroup.Owner = source; storedCopy.Owner = source; using var packed = new PackedScene(); packed.Pack(source); using var instance = packed.Instantiate(); var restored = (CanvasGroup)instance.GetNode("group"); var restoredCopy = (BackBufferCopy)instance.GetNode("copy"); Check(restored.FitMargin == 3.5f && restored.ClearMargin == 7 && restored.UseMipmaps && restored.SelfModulate.A == .5f && restoredCopy.CopyMode == BackBufferCopyMode.Viewport && restoredCopy.Rect == storedCopy.Rect, "Typed scene reconstruction.");
        var root = new TestViewport(); var outer = new CanvasGroup { Name = "outer" }; var inner = new CanvasGroup(); root.AddChild(outer); outer.AddChild(inner); using var tree = new SceneTree(root); Check(inner.GetConfigurationWarnings().Length == 1 && outer.GetConfigurationWarnings().Length == 0, "Attached nested-group warning."); Task.Run(() => Reject<InvalidOperationException>(() => inner.ClearMargin = 1)).GetAwaiter().GetResult();
        Console.WriteLine("Canvas composition authoring, callbacks, packing and owner checks passed.");
    }
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 128, 96); }
    private sealed class Box : Entity
    {
        private static int _names;
        internal Box() { Name = "box_" + ++_names; }
        internal Color Fill = Colors.Red; internal Vector2 Extent = new(16, 16); internal bool Stripes;
        protected override void OnDraw()
        {
            if (Stripes) { for (var x = 0; x < 16; x++) DrawRect(new(x, 0, 1, 16), (x & 1) == 0 ? Colors.Red : Colors.Blue); }
            else DrawRect(new(Vector2.Zero, Extent), Fill);
        }
    }
    private sealed class MaskedGroup : CanvasGroup { protected override void OnDraw() => DrawRect(new(0, 0, 8, 8), Colors.White); }
    private static Shader Load(string language)
    {
        using var input = typeof(CanvasCompositionTests).Assembly.GetManifestResourceStream("TestShaders.Screen" + language + ".spv")!; using var memory = new MemoryStream(); input.CopyTo(memory); return Shader.CreateFromSPIRV(memory.ToArray());
    }
    private static ShaderMaterial Screen(Shader shader)
    {
        var material = new ShaderMaterial { Shader = shader }; material.SetShaderParameter("tint", Colors.White); material.SetShaderParameter("offset", Vector2.Zero);
        Reject<ArgumentException>(() => material.SetShaderParameter("SCREEN_PIXEL_SIZE", Vector2.One)); Reject<ArgumentException>(() => material.SetShaderParameter("SCREEN_TEXTURE", (Texture?)null)); Reject<ArgumentException>(() => shader.SetDefaultTextureParameter("SCREEN_TEXTURE", null));
        Check(shader.GetShaderUniformList().All(p => p.Name is not "SCREEN_TEXTURE" and not "SCREEN_PIXEL_SIZE"), "Engine bindings excluded from material descriptors.");
        return material;
    }
    internal static void RunHost()
    {
        Run(); var backend = Environment.GetEnvironmentVariable("ELECTRON2D_COMPOSITION_RENDERER") ?? "gpu"; var settings = ProjectSettings.Instance; var prior = settings.Get(ProjectSettings.RenderingMethod); settings.Set(ProjectSettings.RenderingMethod, backend);
        try
        {
            if (Environment.GetEnvironmentVariable("SDL_VIDEODRIVER") == "dummy")
            {
                CanvasClipTests.RejectSoftware();
                Copy(backend, null);
                var rejected = new Window(); var softwareGroup = new CanvasGroup(); softwareGroup.AddChild(new Box()); rejected.AddChild(softwareGroup);
                Reject<NotSupportedException>(() => Engine.Instance.Run(rejected)); Check(rejected.IsDisposed, "Software group capability cleanup."); return;
            }
            CanvasClipTests.RunHost(backend);
            Group(backend); ParentYSort(backend); Copy(backend, null); Offscreen(backend); Warm(backend);
            if (backend == "gpu") foreach (var language in new[] { "Hlsl", "Glsl" }) { using var shader = Load(language); using var material = Screen(shader); CanvasClipTests.Materials(material, language); Copy(backend, material); Effects(material, language); }
            else RejectMipmaps();
            RejectNested();
        }
        finally { settings.Set(ProjectSettings.RenderingMethod, prior); }
    }
    private static void Group(string backend)
    {
        var window = new Window { Size = new(128, 96) }; var group = new CanvasGroup { ClipChildren = ClipChildrenMode.Only, Position = new(8, 8), FitMargin = 0, ClearMargin = 0, SelfModulate = new(1, 1, 1, .5f) }; var a = new Box(); var b = new Box { Position = new(8, 0) }; var escaped = new Box { Position = new(0, 24), Fill = Colors.Lime, ZIndex = 1 }; group.AddChild(a); group.AddChild(new BackBufferCopy { CopyMode = BackBufferCopyMode.Viewport }); group.AddChild(b); group.AddChild(escaped); window.AddChild(group); window.AddChild(new Box { Position = new(80, 8), Fill = Colors.Blue }); var mask = new MaskedGroup { Name = "mask", Position = new(48, 48), FitMargin = 500, SelfModulate = new(1, 1, 1, .5f) }; mask.AddChild(new Box()); window.AddChild(mask); window.AddChild(new MaskedGroup { Name = "empty", Position = new(80, 48) }); var stage = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black); renderer.FramePostDraw += () =>
            {
                using var image = renderer.Readback();
                if (stage == 0) { Pixel(image, 12, 12, new(.5f, 0, 0)); Pixel(image, 20, 12, new(.5f, 0, 0)); Pixel(image, 12, 36, Colors.Lime); group.SelfModulate = new(1, 1, 1, .25f); }
                else if (stage == 1) { Pixel(image, 20, 12, new(.25f, 0, 0)); group.Position = new(64, 16); group.Rotation = Mathf.Pi / 2; }
                else if (stage == 2) { Pixel(image, 56, 28, new(.25f, 0, 0)); Pixel(image, 12, 12, Colors.Black); group.Visible = false; }
                else if (stage == 3) { Pixel(image, 56, 28, Colors.Black); group.Visible = true; group.SelfModulate = Colors.White; group.YSortEnabled = true; a.Position = new(0, 8); b.Fill = Colors.Lime; b.QueueRedraw(); }
                else if (stage == 4) { Pixel(image, 52, 28, Colors.Red); group.YSortEnabled = false; group.Position = new(8, 8); group.Rotation = 0; group.Scale = new(2, 1); group.SelfModulate = new(1, 1, 1, .5f); a.Position = Vector2.Zero; b.Fill = Colors.Red; b.QueueRedraw(); group.FitMargin = 3.5f; group.ClearMargin = 2.5f; }
                else if (stage == 5) { Pixel(image, 36, 12, new(.5f, 0, 0)); window.Tree!.Quit(); }
                Pixel(image, 84, 12, Colors.Blue); Pixel(image, 52, 52, new(.5f, 0, 0)); Pixel(image, 60, 52, Colors.Black); Pixel(image, 84, 52, Colors.Black); stage++;
            };
        };
        Check(Engine.Instance.Run(window) == 0 && stage == 6 && window.IsDisposed, "Group host lifecycle."); Console.WriteLine($"CanvasGroup opacity, overlaps, same-Z isolation, transforms, visibility and Y-sort passed ({backend}).");
    }
    private static void ParentYSort(string backend)
    {
        var window = new Window { Size = new(96, 64) }; var parent = new Entity { YSortEnabled = true };
        var group = new CanvasGroup { YSortEnabled = true, FitMargin = 0, ClearMargin = 0, SelfModulate = new(1, 1, 1, .5f) };
        group.AddChild(new Box { Position = new(0, 16) }); parent.AddChild(group);
        var outside = new Box { Position = new(0, 8), Fill = Colors.Blue }; parent.AddChild(outside); window.AddChild(parent); var stage = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black); renderer.FramePostDraw += () =>
        {
            using var image = renderer.Readback(); Pixel(image, 4, 20, stage == 0 ? Colors.Blue : new(.5f, 0, 0));
            if (stage++ == 0) outside.Position = new(0, -8); else window.Tree!.Quit();
        };
        };
        Check(Engine.Instance.Run(window) == 0 && stage == 2, "Ancestor Y-sort group boundary."); Console.WriteLine($"Ancestor Y-sort retains atomic group and internal child sorting ({backend}).");
    }
    private static void Copy(string backend, ShaderMaterial? material)
    {
        var window = new Window { Size = new(96, 64), CanvasCullMask = 1 }; var red = new Box { Extent = new(96, 64) }; var copy = new BackBufferCopy { Position = new(8, 8), Rect = new(0, 0, 16, 16) }; var blue = new Box { Fill = Colors.Blue, Extent = new(96, 64) }; var reader = new Box { Fill = Colors.White, Position = new(8, 8), Material = material }; window.AddChild(red); window.AddChild(copy); window.AddChild(blue); if (material is not null) window.AddChild(reader); var stage = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black); renderer.FramePostDraw += () =>
            {
                using var screen = renderer.ReadbackBackBuffer(window); using var image = renderer.Readback();
                if (stage == 0) { Pixel(screen, 12, 12, Colors.Red); if (material is not null) Pixel(image, 12, 12, Colors.Red); copy.CopyMode = BackBufferCopyMode.Viewport; }
                else if (stage == 1) { Pixel(screen, 72, 48, Colors.Red); copy.CopyMode = BackBufferCopyMode.Rect; copy.Rect = default; }
                else if (stage == 2) { Pixel(screen, 72, 48, Colors.Red); copy.Rect = new(0, 0, 16, 16); copy.Position = new(-8, 8); reader.Position = new(0, 8); reader.Extent = new(8, 16); reader.QueueRedraw(); }
                else if (stage == 3) { Pixel(screen, 4, 12, Colors.Red); if (material is not null) Pixel(image, 4, 12, Colors.Red); copy.CopyMode = BackBufferCopyMode.Disabled; }
                else if (stage == 4) { if (material is not null) { Pixel(screen, 4, 12, Colors.Blue); Pixel(image, 4, 12, Colors.Blue); } copy.CopyMode = BackBufferCopyMode.Rect; copy.Visible = false; }
                else if (stage == 5) { if (material is not null) Pixel(image, 4, 12, Colors.Blue); copy.Visible = true; copy.VisibilityLayer = 2; }
                else if (stage == 6) { if (material is not null) Pixel(image, 4, 12, Colors.Blue); copy.VisibilityLayer = 1; copy.CopyMode = BackBufferCopyMode.Viewport; copy.ZIndex = -1; }
                else if (stage == 7) { Pixel(screen, 4, 12, Colors.Black); if (material is not null) Pixel(image, 4, 12, Colors.Black); copy.ZIndex = 0; copy.CopyMode = BackBufferCopyMode.Rect; copy.Rect = new(16, 0, -16, 16); copy.Position = new(8, 8); reader.Position = new(8, 8); reader.Extent = new(16, 16); reader.QueueRedraw(); }
                else if (stage == 8) { Pixel(screen, 12, 12, Colors.Red); if (material is not null) Pixel(image, 12, 12, Colors.Red); window.Tree!.Quit(); }
                stage++;
            };
        };
        Check(Engine.Instance.Run(window) == 0 && stage == 9, "Copy host lifecycle."); Console.WriteLine($"BackBufferCopy rect/full/sentinel/clipping/disabled/visibility/mask/Z/negative-size passed ({backend}, shader={material is not null}).");
    }
    private static void Offscreen(string backend)
    {
        using var premultiplied = new CanvasItemMaterial { BlendMode = BlendMode.PremultAlpha };
        var window = new Window { Size = new(96, 64) }; var view = new SubViewport { Size = new(32, 32), TransparentBG = true, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        var group = new CanvasGroup { FitMargin = 0, ClearMargin = 0, SelfModulate = new(1, 1, 1, .5f) }; group.AddChild(new Box()); view.AddChild(group); var texture = view.GetTexture();
        window.AddChild(view); window.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(8, 8), TextureFilter = TextureFilter.Nearest, Material = premultiplied }); var stage = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black); renderer.FramePostDraw += () =>
        {
            using var output = renderer.Readback(); using var target = texture.GetImage(); var opacity = stage == 0 ? .5f : .25f;
            Check(target is not null && Math.Abs(target.GetPixel(4, 4).A - opacity) < .03, "Independent group target alpha."); Pixel(output, 12, 12, new(opacity, 0, 0));
            if (stage == 0) group.SelfModulate = new(1, 1, 1, .25f); else if (stage == 1) view.Size = new(64, 32); else window.Tree!.Quit(); stage++;
        };
        };
        Check(Engine.Instance.Run(window) == 0 && stage == 3, "Offscreen composition lifecycle."); texture.Dispose(); Console.WriteLine($"Independent SubViewport group composition and resize passed ({backend}).");
    }
    private static void Effects(ShaderMaterial material, string language)
    {
        var window = new Window { Size = new(128, 64) }; window.AddChild(new Box { Extent = new(128, 64) }); window.AddChild(new BackBufferCopy { CopyMode = BackBufferCopyMode.Viewport }); window.AddChild(new Box { Fill = Colors.Blue, Extent = new(128, 64) }); var group = new CanvasGroup { Position = new(8, 8), FitMargin = 10, ClearMargin = 0, Material = material }; var child = new Box { Fill = Colors.Lime }; group.AddChild(child); window.AddChild(group); material.SetShaderParameter("offset", new Vector2(8, 0)); material.SetShaderParameter("unpremultiply", 1f); var stage = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Instance!; renderer.SetDefaultClearColor(Colors.Black); renderer.FramePostDraw += () =>
            {
                using var image = renderer.Readback();
                if (stage == 0) { Pixel(image, 28, 12, Colors.Red); group.ClearMargin = 10; }
                else if (stage == 1) { Pixel(image, 28, 12, Colors.Blue); material.SetShaderParameter("offset", Vector2.Zero); material.SetShaderParameter("lod", 1f); group.UseMipmaps = true; child.Stripes = true; child.QueueRedraw(); }
                else if (stage == 2) { Pixel(image, 16, 16, new(.5f, 0, .5f)); group.SelfModulate = new(1, 1, 1, .5f); }
                else if (stage == 3) { Pixel(image, 16, 16, new(.25f, 0, .75f)); window.Tree!.Quit(); }
                stage++;
            };
        };
        Check(Engine.Instance.Run(window) == 0 && stage == 4, "Group effect host lifecycle."); material.SetShaderParameter("lod", 0f); material.SetShaderParameter("unpremultiply", 0f); Console.WriteLine($"Group custom screen shader, clear margin, generated LOD and opacity passed ({language}).");
    }
    private static void Warm(string backend)
    {
        var window = new Window { Size = new(128, 64) }; window.AddChild(new Box { Extent = new(128, 64), Fill = Colors.Black }); var copy = new BackBufferCopy { CopyMode = BackBufferCopyMode.Viewport }; window.AddChild(copy); var group = new CanvasGroup { Position = new(8, 8), FitMargin = 0, ClearMargin = 2, SelfModulate = new(1, 1, 1, .5f) }; var a = new Box(); var b = new Box { Position = new(8, 0) }; group.AddChild(a); group.AddChild(b); window.AddChild(group); var texture = window.GetTexture(); var phase = 0; var warm = 32; var measured = 0; var frame = 0; var cold = 0; long before = 0, active = 0, idle = 0; var priorSize = Vector2.Zero; var frameSize = Vector2.Zero;
        window.Ready += _ =>
        {
            window.Tree!.ProcessFrameStarted += _ => { frameSize = texture.GetSize(); before = GC.GetAllocatedBytesForCurrentThread(); if (phase == 0) { a.Fill = (frame & 1) == 0 ? Colors.Red : Colors.Blue; a.QueueRedraw(); group.SelfModulate = new(1, 1, 1, (frame & 1) == 0 ? .5f : .25f); group.FitMargin = (frame & 1) == 0 ? 0 : 2; copy.CopyMode = (frame & 1) == 0 ? BackBufferCopyMode.Viewport : BackBufferCopyMode.Rect; } };
            RenderingServer.Instance!.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; var size = texture.GetSize();
                if (size != priorSize || size != frameSize) { warm = 32; cold++; } else if (warm > 0) warm--; else { Check(bytes == 0, $"Composition phase {phase} frame {frame}: {bytes} managed bytes."); if (phase == 0) active += bytes; else idle += bytes; if (++measured == 64) { measured = 0; if (phase++ == 0) warm = 32; else window.Tree!.Quit(); } }
                priorSize = size; Check(++frame < 2048, "Composition native dimensions did not stabilize.");
            };
        };
        Check(Engine.Instance.Run(window) == 0 && phase == 2 && active == 0 && idle == 0, "Warm composition lifecycle."); texture.Dispose(); Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario = "canvas-composition-warm", backend, frame, cold, active, idle, measuredPerPhase = 64 }));
    }
    private static void RejectMipmaps()
    {
        var window = new Window(); var group = new CanvasGroup { UseMipmaps = true }; group.AddChild(new Box()); window.AddChild(group); Reject<NotSupportedException>(() => Engine.Instance.Run(window)); Check(window.IsDisposed, "Rejected mipmap cleanup."); Console.WriteLine("Compatibility group mipmaps reject before submission and clean up.");
    }
    private static void RejectNested()
    {
        var window = new Window(); var outer = new CanvasGroup(); var inner = new CanvasGroup(); inner.AddChild(new Box()); outer.AddChild(inner); window.AddChild(outer); Reject<NotSupportedException>(() => Engine.Instance.Run(window)); Check(window.IsDisposed, "Rejected nested group cleanup.");
    }
    private static void Pixel(Image image, int x, int y, Color expected)
    { var actual = image.GetPixel(x, y); Check(Math.Abs(actual.R - expected.R) < .04 && Math.Abs(actual.G - expected.G) < .04 && Math.Abs(actual.B - expected.B) < .04, $"Composition pixel {x},{y}: {actual} != {expected}."); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
