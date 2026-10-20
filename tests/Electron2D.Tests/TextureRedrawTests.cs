using System.Runtime.CompilerServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class TextureRedrawTests
{
    private sealed class ExpectedFailure : Exception;
    private static ImageTexture Solid(Color color)
    { using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(color); return ImageTexture.CreateFromImage(image); }
    internal static void Managed()
    { Reject<InvalidOperationException>(() => RenderingServer.TextureSetForceRedrawIfVisible(default, true)); Console.WriteLine("Visible texture redraw rejects an absent renderer."); }
    private sealed class Patch : Entity
    {
        private Texture? _texture; private RID _rid;
        internal int Draws;
        internal void Use(Texture texture) { _texture = texture; _rid = default; QueueRedraw(); }
        internal void Use(RID texture) { _texture = null; _rid = texture; QueueRedraw(); }
        protected override void OnDraw() { Draws++; if (_texture is not null) DrawTextureRect(_texture, new(0, 0, 32, 32), false); else if (_rid.IsValid()) DrawTextureRect(_rid, new(0, 0, 32, 32), false); }
    }
    private sealed class GuardedTexture(Image image, Action sample) : Texture
    {
        public override int GetWidth() => image.Width;
        public override int GetHeight() => image.Height;
        public override Image GetImage() { sample(); return (Image)image.Duplicate(); }
    }
    private static void Settle() { for (var i = 0; i < 3; i++) RenderingServer.ForceDraw(); }
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Abandon()
    { var texture = Solid(Colors.Red); RenderingServer.TextureSetForceRedrawIfVisible(texture.GetRID(), true); return new WeakReference(texture); }
    internal static void Native(string backend)
    {
        var previous = ProjectSettings.Get(ProjectSettings.RenderingMethod); var fallback = ProjectSettings.Get(ProjectSettings.RenderingFallback); var low = ProjectSettings.Get(ProjectSettings.LowProcessorMode); var sleep = ProjectSettings.Get(ProjectSettings.LowProcessorModeSleepUsec); var fps = Engine.MaxFPS;
        try
        {
            ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); ProjectSettings.Set(ProjectSettings.LowProcessorMode, true); ProjectSettings.Set(ProjectSettings.LowProcessorModeSleepUsec, 0); Engine.MaxFPS = 0;
            using var shared = Solid(Colors.Red); using var root = new Window { Size = new(256, 128), Title = "Electron2D visible image policy" }; var patch = new Patch { Position = new(8, 8) }; var clip = new Control { Size = new(256, 128) }; clip.AddChild(patch); root.AddChild(clip);
            root.Ready += _ =>
            {
                RenderingServer.RenderLoopEnabled = false; RenderingServer.SetDefaultClearColor(Colors.Black);
                Reject<ArgumentException>(() => RenderingServer.TextureSetForceRedrawIfVisible(default, true)); using var material = new ShaderMaterial(); Reject<ArgumentException>(() => RenderingServer.TextureSetForceRedrawIfVisible(material.GetRID(), true));
                using var mesh = new MeshTexture { BaseTexture = shared }; Reject<ArgumentException>(() => RenderingServer.TextureSetForceRedrawIfVisible(mesh.GetRID(), true));
                Task.Run(() => Reject<InvalidOperationException>(() => RenderingServer.TextureSetForceRedrawIfVisible(shared.GetRID(), true))).GetAwaiter().GetResult();
                var dead = Solid(Colors.Red); var deadRID = dead.GetRID(); dead.Dispose(); Reject<ArgumentException>(() => RenderingServer.TextureSetForceRedrawIfVisible(deadRID, true));
                Sample(root, clip, patch, shared.GetRID(), shared, default, "ordinary");
                using var animated = new AnimatedTexture { Pause = true }; animated.SetFrameTexture(0, shared); Sample(root, clip, patch, shared.GetRID(), animated, default, "paused animation source");
                using var atlas = new AtlasTexture { Atlas = shared, Region = new(0, 0, 1, 1) }; Sample(root, clip, patch, atlas.GetRID(), atlas, default, "atlas");
                var a = RenderingServer.TextureProxyCreate(shared.GetRID()); var b = RenderingServer.TextureProxyCreate(a); var c = RenderingServer.TextureProxyCreate(b);
                Sample(root, clip, patch, b, null, c, "middle proxy"); Sample(root, clip, patch, c, null, c, "outer proxy"); Sample(root, clip, patch, shared.GetRID(), null, c, "proxy source");
                using var other = Solid(Colors.Blue); animated.Frames = 2; animated.SetFrameTexture(1, other); animated.CurrentFrame = 1; patch.Use(animated); RenderingServer.TextureSetForceRedrawIfVisible(shared.GetRID(), true); Settle(); Check(!RenderingServer.HasChanged(), "An unselected animation frame does not consume its source policy."); RenderingServer.TextureSetForceRedrawIfVisible(shared.GetRID(), false); RenderingServer.TextureSetForceRedrawIfVisible(shared.GetRID(), true); patch.Use(c); Settle(); Check(RenderingServer.HasChanged(), "Current proxy source requests frames."); RenderingServer.TextureProxyUpdate(b, other.GetRID()); Settle(); Check(!RenderingServer.HasChanged(), "Retargeting stops observing the old source flag."); RenderingServer.TextureSetForceRedrawIfVisible(shared.GetRID(), false); RenderingServer.TextureSetForceRedrawIfVisible(c, true); Settle(); Check(RenderingServer.HasChanged(), "Proxy identity policy survives source retargeting."); RenderingServer.TextureSetForceRedrawIfVisible(c, false);
                using var normal = Solid(new(.5f, .5f, 1)); using var specular = Solid(Colors.White); using var channels = new CanvasTexture { DiffuseTexture = shared, NormalTexture = normal, SpecularTexture = specular };
                Sample(root, clip, patch, channels.GetRID(), channels, default, "canvas descriptor"); Sample(root, clip, patch, normal.GetRID(), channels, default, "normal channel"); Sample(root, clip, patch, specular.GetRID(), channels, default, "specular channel");
                var guarded = false; using var pixels = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); pixels.Fill(Colors.Red);
                using var custom = new GuardedTexture(pixels, () => { Reject<InvalidOperationException>(() => RenderingServer.TextureSetForceRedrawIfVisible(shared.GetRID(), true)); guarded = true; });
                patch.Use(custom); Settle(); Check(guarded, "Visible policy mutation rejects native image callbacks before changing state.");
                Replacement(patch); Viewports(root, patch, shared); Geometry(root, patch, shared);
                patch.Use(shared); RenderingServer.TextureSetForceRedrawIfVisible(shared.GetRID(), true); RenderingServer.ViewportAttachToScreen(root.GetViewportRID(), new(500, 500, 256, 128)); Settle(); Check(!RenderingServer.HasChanged(), "A completely off-client viewport composition does not observe its flagged image."); RenderingServer.ViewportAttachToScreen(root.GetViewportRID(), new(0, 0, 256, 128)); Settle(); Check(RenderingServer.HasChanged(), "Returning composition to the client resumes its image policy."); RenderingServer.TextureSetForceRedrawIfVisible(shared.GetRID(), false);
                using var dpi = DPITexture.CreateFromString(DPITextureTests.Source); Sample(root, clip, patch, dpi.GetRID(), dpi, default, "scaled SVG identity");
                if (backend == "gpu") GPU(root, clip, patch);
                patch.Visible = false; RenderingServer.FreeRID(c); RenderingServer.FreeRID(b); RenderingServer.FreeRID(a); Settle(); Check(!RenderingServer.HasChanged(), "Hidden and freed texture policies do not keep idle frames pending.");
                var weak = Abandon(); Collect(); Check(!weak.IsAlive, "Session policy does not retain an unused borrowed image.");
                patch.Use(shared); patch.Visible = true; RenderingServer.TextureSetForceRedrawIfVisible(shared.GetRID(), true); Settle();
                var failure = new ExpectedFailure(); Action before = () => throw failure; RenderingServer.FramePreDraw += before;
                try { RenderingServer.ForceDraw(); throw new InvalidOperationException("Expected failed frame."); } catch (ExpectedFailure error) { Check(ReferenceEquals(error, failure), "Recording error retains identity."); } finally { RenderingServer.FramePreDraw -= before; }
                Check(RenderingServer.HasChanged(), "A failed continuous frame stays pending for a valid retry."); Settle(); Check(RenderingServer.HasChanged(), "Visible source resumes its policy after recovery.");
                root.Tree!.Quit();
            };
            Check(Engine.Run(root) == 0 && root.IsDisposed, "Manual texture policy cases retire their session."); Automatic(shared, backend); Managed();
            Console.WriteLine(backend + ": visible image flags, aliases/channels/arrays, replacement, ownership, failure recovery, native idle/active output and zero warm allocations passed.");
        }
        finally { Engine.MaxFPS = fps; ProjectSettings.Set(ProjectSettings.RenderingMethod, previous); ProjectSettings.Set(ProjectSettings.RenderingFallback, fallback); ProjectSettings.Set(ProjectSettings.LowProcessorMode, low); ProjectSettings.Set(ProjectSettings.LowProcessorModeSleepUsec, sleep); }
    }
    private static void Sample(Window root, Control clip, Patch patch, RID flag, Texture? texture, RID drawn, string name)
    {
        patch.Position = new(8, 8); patch.Visible = true; if (texture is not null) patch.Use(texture); else patch.Use(drawn); RenderingServer.TextureSetForceRedrawIfVisible(flag, true); Settle(); Check(RenderingServer.HasChanged(), name + " visible native binding requests another frame.");
        patch.Visible = false; Settle(); Check(!RenderingServer.HasChanged(), name + " hidden binding stops requests."); patch.Visible = true; patch.Position = new(500, 500); Settle(); Check(!RenderingServer.HasChanged(), name + " offscreen binding stops requests.");
        patch.Position = new(8, 8); clip.Size = new(2, 2); clip.ClipContents = true; Settle(); Check(!RenderingServer.HasChanged(), name + " completely clipped binding stops requests."); clip.ClipContents = false; clip.Size = new(256, 128); Settle(); Check(RenderingServer.HasChanged(), name + " restored binding resumes requests.");
        var recorded = patch.Draws; Collect(); for (var i = 0; i < 16; i++) RenderingServer.ForceDraw(); var start = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) RenderingServer.ForceDraw(); Check(GC.GetAllocatedBytesForCurrentThread() == start && patch.Draws == recorded, "Prepared forced texture frames allocate zero owner bytes and preserve retained recordings.");
        RenderingServer.TextureSetForceRedrawIfVisible(flag, false); Settle(); Check(!RenderingServer.HasChanged(), name + " disabled policy returns to idle.");
    }
    private static void Replacement(Patch patch)
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); image.Fill(Colors.Red); var destination = RenderingServer.Texture2DCreate(image); var source = RenderingServer.Texture2DCreate(image); patch.Use(destination); RenderingServer.TextureSetForceRedrawIfVisible(destination, true); RenderingServer.TextureReplace(destination, source); Settle(); Check(RenderingServer.HasChanged(), "Destination identity retains policy across storage replacement.");
        RenderingServer.TextureSetForceRedrawIfVisible(destination, false); source = RenderingServer.Texture2DCreate(image); RenderingServer.TextureSetForceRedrawIfVisible(source, true); RenderingServer.TextureReplace(destination, source); Settle(); Check(!RenderingServer.HasChanged(), "A consumed replacement source does not transfer its identity policy."); Reject<ArgumentException>(() => RenderingServer.TextureSetForceRedrawIfVisible(source, false)); patch.Visible = false; RenderingServer.FreeRID(destination); patch.Visible = true;
    }
    private static void Viewports(Window root, Patch patch, ImageTexture source)
    {
        patch.Visible = false; using var sub = new SubViewport { Size = new(32, 32), RenderTargetUpdateMode = ViewportUpdateMode.WhenVisible }; var child = new Patch(); child.Use(source); sub.AddChild(child); root.AddChild(sub); RenderingServer.TextureSetForceRedrawIfVisible(source.GetRID(), true); Settle(); Check(!RenderingServer.HasChanged(), "Unsubmitted viewport does not observe its child's image flag.");
        patch.Use(sub.GetTexture()); patch.Visible = true; Settle(); Check(RenderingServer.HasChanged(), "Sampled viewport submits the flagged image dependency."); sub.RenderTargetUpdateMode = ViewportUpdateMode.Disabled; Settle(); Check(!RenderingServer.HasChanged(), "Disabled viewport stops consuming the image dependency.");
        RenderingServer.TextureSetForceRedrawIfVisible(sub.GetTexture().GetRID(), true); Settle(); Check(RenderingServer.HasChanged(), "Visible completed viewport image can retain its own scheduling policy."); RenderingServer.TextureSetForceRedrawIfVisible(sub.GetTexture().GetRID(), false); RenderingServer.TextureSetForceRedrawIfVisible(source.GetRID(), false); patch.Visible = false; root.RemoveChild(sub); sub.Dispose(); patch.Visible = true;
    }
}
