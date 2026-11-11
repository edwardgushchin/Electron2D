using Electron2D;

internal static class CanvasGlowTests
{
    internal static void Run(string profile)
    {
        if (profile == "managed") { Managed(); return; }
        Native(profile == "compatibility");
    }

    private static void Managed()
    {
        using var environment = new CanvasEnvironment();
        Check(!environment.GlowEnabled && environment.GlowBlend == CanvasEnvironment.GlowBlendMode.Screen, "Glow defaults off in screen mode.");
        Check(environment.GlowHDRThreshold == 1 && environment.GlowIntensity == .3f && environment.GetGlowLevel(1) == .8f, "Pinned defaults.");
        Reject<ArgumentOutOfRangeException>(() => environment.GetGlowLevel(7));
        Reject<ArgumentOutOfRangeException>(() => environment.SetGlowLevel(-1, 1));
        Reject<ArgumentOutOfRangeException>(() => environment.GlowIntensity = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => environment.GlowMix = 1.1f);
        Reject<ArgumentOutOfRangeException>(() => environment.GlowBlend = (CanvasEnvironment.GlowBlendMode)42);
        Reject<InvalidOperationException>(() => RenderingServer.EnvironmentGlowSetUseBicubicUpscale(true));
        using var disposed = new CanvasEnvironment();
        disposed.Dispose();
        using var root = new Node();
        var first = new WorldEnvironment { Environment = environment };
        var second = new WorldEnvironment { Environment = environment };
        root.AddChild(first);
        root.AddChild(second);
        Reject<ObjectDisposedException>(() => first.Environment = disposed);
        Check(ReferenceEquals(first.Environment, environment), "Invalid replacement leaves authoring intact.");
        environment.GlowEnabled = true;
        environment.GlowBlend = CanvasEnvironment.GlowBlendMode.Mix;
        environment.GlowMix = .625f;
        environment.SetGlowLevel(6, 2.5f);
        environment.ResourceLocalToScene = true;
        var rid = environment.GetRID();
        Check(rid.IsValid() && environment.GetRID() == rid, "Resource identity is stable.");
        using var packed = new PackedScene();
        first.Owner = root;
        second.Owner = root;
        packed.Pack(root);
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-glow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "environment.e2dscene");
            ResourceSaver.Save(packed, path);
            using var read = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
            using var instance = read.Instantiate();
            var a = (WorldEnvironment)instance.GetChild(0);
            var b = (WorldEnvironment)instance.GetChild(1);
            using var copied = a.Environment!;
            Check(ReferenceEquals(copied, b.Environment) && !ReferenceEquals(copied, environment), "Packed scene restores shared scene-local environment.");
            Check(copied.GlowEnabled && copied.GlowBlend == CanvasEnvironment.GlowBlendMode.Mix && copied.GlowMix == .625f &&
                copied.GetGlowLevel(6) == 2.5f && copied.GetRID() != rid, "Packed settings, defaults and runtime identity.");
            copied.ResetState();
            Check(copied.GlowEnabled && copied.GetGlowLevel(6) == 2.5f, "Resource reset preserves stored glow authoring.");
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine("Canvas glow managed: defaults, validation, stable RID, typed file and shared scene-local copy passed.");
    }

    private static void Native(bool compatibility)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var environment = new CanvasEnvironment { GlowBlend = CanvasEnvironment.GlowBlendMode.Additive, GlowIntensity = 1 };
        using var mapImage = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        mapImage.Fill(Colors.Black);
        using var blackMap = ImageTexture.CreateFromImage(mapImage);
        using var root = new Window { Size = new(96, 64), UseHDR2D = !compatibility, Title = "Canvas glow check" };
        var selector = new WorldEnvironment { Environment = environment };
        var alternate = new WorldEnvironment { Environment = new CanvasEnvironment() };
        using var alternateEnvironment = alternate.Environment!;
        root.AddChild(selector);
        root.AddChild(alternate);
        root.AddChild(BrightRect());
        var child = new SubViewport { Size = new(96, 64), UseHDR2D = !compatibility, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        var rectangle = BrightRect();
        child.AddChild(rectangle);
        root.AddChild(child);
        using var texture = root.GetTexture();
        using var childTexture = child.GetTexture();
        var completed = false;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.ForceDraw();
            using var baseline = texture.GetImage()!;
            var baselinePixel = baseline.GetPixel(38, 30);
            Check(baselinePixel.R == 0 && baselinePixel.A == 1, "The halo probe starts black.");
            environment.GlowEnabled = true;
            RenderingServer.ForceDraw();
            using var active = texture.GetImage()!;
            using var isolated = childTexture.GetImage()!;
            if (compatibility)
            {
                Check(active.GetData().AsSpan().SequenceEqual(baseline.GetData()), "HDR-only glow does not modify the compatibility canvas.");
                completed = true;
                root.Tree!.Quit();
                return;
            }
            Check(active.PixelFormat == Image.Format.Rgbah && active.GetPixel(46, 30).R > 1, "Native HDR pixels retain highlights.");
            Check(active.GetPixel(38, 30).R > .01f && active.GetPixel(38, 30).A == 1 && isolated.GetPixel(38, 30).R == 0,
                "HDR glow produces an exterior halo and preserves alpha; a different world has no effect.");
            child.World = root.FindWorld();
            RenderingServer.ForceDraw();
            using (var shared = childTexture.GetImage()!) Check(shared.GetPixel(38, 30).R > .01f, "Shared worlds apply the same environment to each viewport.");
            var rootID = root.GetViewportRID();
            var childID = child.GetViewportRID();
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetEnvironmentMode(rootID, RenderingServer.ViewportEnvironmentMode.Max));
            RenderingServer.ViewportSetEnvironmentMode(rootID, RenderingServer.ViewportEnvironmentMode.Disabled);
            RenderingServer.ForceDraw();
            using (var inherited = childTexture.GetImage()!) Check(inherited.GetPixel(38, 30).R == 0, "A child inherits its parent's environment mode.");
            RenderingServer.ViewportSetEnvironmentMode(childID, RenderingServer.ViewportEnvironmentMode.Enabled);
            RenderingServer.ForceDraw();
            using (var explicitMode = childTexture.GetImage()!) Check(explicitMode.GetPixel(38, 30).R > .01f, "Explicit enable overrides a disabled parent.");
            RenderingServer.ViewportSetEnvironmentMode(rootID, RenderingServer.ViewportEnvironmentMode.Inherit);
            RenderingServer.ViewportSetEnvironmentMode(childID, RenderingServer.ViewportEnvironmentMode.Inherit);
            using (var peer = new SubViewport { Size = new(8, 8), UseHDR2D = true, RenderTargetUpdateMode = ViewportUpdateMode.Always })
            {
                root.AddChild(peer);
                RenderingServer.ViewportSetParentViewport(childID, peer.GetViewportRID());
                peer.Reparent(child);
                Reject<InvalidOperationException>(() => RenderingServer.ForceDraw());
                peer.Reparent(root);
                RenderingServer.ViewportSetParentViewport(childID, rootID);
            }
            RenderingServer.ForceDraw();
            using (var recovered = childTexture.GetImage()!) Check(recovered.GetPixel(38, 30).R > .01f, "Repaired viewport inheritance resumes rendering.");
            Check(alternate.GetConfigurationWarnings().Length != 0, "Later environment warns.");
            root.MoveChild(alternate, 0);
            RenderingServer.ForceDraw();
            using (var switched = texture.GetImage()!) Check(switched.GetPixel(38, 30).R == 0, "Scene order switches the selected environment.");
            root.MoveChild(selector, 0);
            environment.GlowMap = blackMap;
            environment.GlowMapStrength = 1;
            RenderingServer.ForceDraw();
            using (var masked = texture.GetImage()!) Check(masked.GetPixel(38, 30).R == 0, "Black map masks the glow.");
            environment.GlowMap = null;
            environment.GlowNormalized = true;
            RenderingServer.ForceDraw();
            using (var normalized = texture.GetImage()!)
                Check(normalized.GetPixel(38, 30).R < active.GetPixel(38, 30).R &&
                    normalized.GetPixel(38, 30).R > 0, "Normalized weights reduce the summed halo.");
            environment.GlowNormalized = false;
            environment.GlowHDRThreshold = 8;
            RenderingServer.ForceDraw();
            using (var filtered = texture.GetImage()!) Check(filtered.GetPixel(38, 30).R == 0, "The threshold excludes dim sources.");
            environment.GlowBloom = .5f;
            RenderingServer.ForceDraw();
            using (var bloom = texture.GetImage()!) Check(bloom.GetPixel(38, 30).R > 0, "Bloom includes below-threshold sources.");
            environment.GlowHDRThreshold = 1;
            environment.GlowBloom = 0;
            environment.GlowBlend = CanvasEnvironment.GlowBlendMode.Mix;
            environment.GlowMix = 0;
            RenderingServer.ForceDraw();
            using (var original = texture.GetImage()!) Check(original.GetData().AsSpan().SequenceEqual(baseline.GetData()), "Zero mix reproduces the original canvas.");
            environment.GlowBlend = CanvasEnvironment.GlowBlendMode.Screen;
            RenderingServer.ForceDraw();
            using (var screen = texture.GetImage()!) Check(screen.GetPixel(38, 30).R > 0 && screen.GetPixel(46, 30).R < active.GetPixel(46, 30).R, "Screen differs from additive blending.");
            environment.GlowBlend = CanvasEnvironment.GlowBlendMode.SoftLight;
            RenderingServer.ForceDraw();
            using (var soft = texture.GetImage()!) Check(soft.GetPixel(38, 30).R == 0 && soft.GetPixel(46, 30).G > baseline.GetPixel(46, 30).G, "Soft light changes midtones while retaining black.");

            var weights = new float[] { 0, 1, .5f, 0, 0, 0, 0 };
            var id = environment.GetRID();
            RenderingServer.EnvironmentSetGlow(id, true, weights, 1, 1, .05f, 0, CanvasEnvironment.GlowBlendMode.Additive, 1, 2, 12, 1, default);
            weights[1] = 0;
            Check(environment.GetGlowLevel(1) == 1, "Server publication copies weights.");
            Reject<ArgumentException>(() => RenderingServer.EnvironmentSetGlow(id, false, [], 1, 1, 0, 0, CanvasEnvironment.GlowBlendMode.Additive, 1, 2, 12, 0, default));
            Check(environment.GlowEnabled, "Rejected updates preserve authoring.");
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.EnvironmentGlowSetUseBicubicUpscale(true)).GetAwaiter().GetResult());
            RenderingServer.ForceDraw();
            using var bilinear = texture.GetImage()!;
            RenderingServer.EnvironmentGlowSetUseBicubicUpscale(true);
            RenderingServer.ForceDraw();
            using (var bicubic = texture.GetImage()!) Check(!bicubic.GetData().AsSpan().SequenceEqual(bilinear.GetData()), "Bicubic reconstruction changes actual pixels.");

            child.RenderTargetClearMode = ViewportClearMode.Never;
            rectangle.Visible = false;
            RenderingServer.ForceDraw();
            using var before = childTexture.GetImage()!;
            RenderingServer.ForceDraw();
            using (var after = childTexture.GetImage()!) Check(after.GetData().AsSpan().SequenceEqual(before.GetData()), "Never-clear does not reprocess prior glow.");
            environment.GlowEnabled = false;
            RenderingServer.ForceDraw();
            using (var raw = childTexture.GetImage()!) Check(raw.GetPixel(38, 30).R == 0 && raw.GetPixel(46, 30).R > 1, "Disabling restores raw Never-clear history.");

            environment.GlowEnabled = true;
            environment.GlowBlend = CanvasEnvironment.GlowBlendMode.Replace;
            environment.GlowStrength = 0;
            RenderingServer.ForceDraw();
            using (var zero = texture.GetImage()!) Check(zero.GetPixel(46, 30).R == 0, "Zero-strength replacement has no residual glow.");
            environment.GlowBlend = CanvasEnvironment.GlowBlendMode.Additive;
            environment.GlowStrength = 1;
            root.MSAA2D = Viewport.MSAA.Samples4X;
            RenderingServer.ForceDraw();
            using (var msaa = texture.GetImage()!) Check(msaa.GetPixel(38, 30).R > .01f, "Glow reads resolved MSAA pixels.");
            child.Size = new(101, 65);
            child.RenderTargetClearMode = ViewportClearMode.Always;
            rectangle.Visible = true;
            RenderingServer.ForceDraw();
            using (var resized = childTexture.GetImage()!) Check(resized.Width == 101 && resized.GetPixel(38, 30).R > .01f, "Resize rebuilds the native pyramid.");

            for (var i = 0; i < 48; i++) RenderingServer.ForceDraw();
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) RenderingServer.ForceDraw();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Check(allocated == 0, $"Warmed full-frame glow allocated {allocated} owner bytes.");
            var output = Environment.GetEnvironmentVariable("ELECTRON2D_GLOW_OUTPUT");
            if (output is not null)
            {
                using var presented = RenderingServer.Service!.Readback();
                presented.SavePNG(output);
            }
            completed = true;
            root.Tree!.Quit();
            Console.WriteLine($"Canvas glow GPU: HDR pixels, halo, MSAA, worlds, masks, bicubic, resize, Never-clear and 64 warmed frames ({allocated} owner bytes).");
        };
        Check(Engine.Run(root) == 0 && completed, "The native glow fixture ran to completion.");
        if (compatibility) Console.WriteLine("Canvas glow compatibility: byte canvas and lifecycle passed.");
    }

    private static ColorRect BrightRect() => new() { Position = new(44, 26), Size = new(9, 10), Color = new(2, .3f, 0, 1) };

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
