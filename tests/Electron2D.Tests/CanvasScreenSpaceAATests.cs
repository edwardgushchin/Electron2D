using Electron2D;
using static TextureArrayTests;

internal static class CanvasScreenSpaceAATests
{
    private static readonly Vector2[] Triangle = [new(4.2f, 5.4f), new(48.4f, 13.1f), new(14.3f, 48.8f)];

    private sealed class Shape(Color color) : Entity
    {
        internal Action? CheckCapture;

        protected override void OnDraw()
        {
            CheckCapture?.Invoke();
            DrawColoredPolygon(Triangle, color);
        }
    }

    private sealed class MipPatch(Texture texture) : Entity
    {
        protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false);
    }

    internal static void Run(string profile, ScreenSpaceAAMode mode)
    {
        if (profile == "managed") { Managed(mode); return; }
        var compatibility = profile is "compatibility" or "software";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, compatibility ? "compatibility" : "gpu");
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        ProjectSettings.Set(ProjectSettings.ScreenSpaceAA, mode);
        if (compatibility) Compatibility(mode);
        else Render(profile, mode);
        Console.WriteLine($"Canvas {mode} verified ({profile}).");
    }

    private static void Managed(ScreenSpaceAAMode mode)
    {
        using var viewport = new SubViewport();
        Check(viewport.ScreenSpaceAA == ScreenSpaceAAMode.Disabled, "The viewport default is disabled.");
        viewport.ScreenSpaceAA = mode;
        using var scene = new PackedScene();
        scene.Pack(viewport);
        using var copy = (SubViewport)scene.Instantiate();
        Check(copy.ScreenSpaceAA == mode, "The packed scene retains the authored mode.");
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"e2d-screen-space-aa-{Guid.NewGuid():N}.e2dscene");
        try
        {
            ResourceSaver.Save(scene, file);
            using var stored = ResourceLoader.Load<PackedScene>(file, ResourceLoader.CacheMode.Ignore);
            using var restored = (SubViewport)stored.Instantiate();
            Check(restored.ScreenSpaceAA == mode, "Scene files reconstruct the typed antialiasing mode.");
        }
        finally { File.Delete(file); }
        foreach (var invalid in new[] { -1, (int)ScreenSpaceAAMode.Max, 99 })
        {
            Reject<ArgumentOutOfRangeException>(() => viewport.ScreenSpaceAA = (ScreenSpaceAAMode)invalid);
            Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.ScreenSpaceAA, (ScreenSpaceAAMode)invalid));
        }
        Check(viewport.ScreenSpaceAA == mode, "Refused values preserve the authored mode.");
        Reject<InvalidOperationException>(() => RenderingServer.ViewportSetScreenSpaceAA(default, ScreenSpaceAAMode.Disabled));
        Console.WriteLine($"Typed {mode} scene storage and mode validation passed.");
    }

    private static void Compatibility(ScreenSpaceAAMode mode)
    {
        var configured = new Window { Size = new(64, 64) };
        configured.AddChild(new Shape(Colors.White));
        Reject<NotSupportedException>(() => Engine.Run(configured));
        Check(configured.IsDisposed && !RenderingServer.IsAvailable, "Unsupported startup retires the renderer.");

        var explicitMode = new Window { Size = new(64, 64), ScreenSpaceAA = ScreenSpaceAAMode.Disabled };
        explicitMode.Ready += _ =>
        {
            Check(explicitMode.ScreenSpaceAA == ScreenSpaceAAMode.Disabled, "Explicit Disabled takes precedence over the startup mode.");
            Reject<NotSupportedException>(() => explicitMode.ScreenSpaceAA = mode);
            Reject<NotSupportedException>(() => RenderingServer.ViewportSetScreenSpaceAA(explicitMode.GetViewportRID(), mode));
            Check(explicitMode.ScreenSpaceAA == ScreenSpaceAAMode.Disabled, "Live rejection preserves the authored value.");
            explicitMode.Tree!.Quit();
        };
        Check(Engine.Run(explicitMode) == 0, "The renderer still runs with antialiasing disabled.");
    }

    private static void Render(string driver, ScreenSpaceAAMode mode)
    {
        using var world = new World();
        using var mips = ViewportMIPMapBiasTests.Mips();
        using var environment = new CanvasEnvironment { TonemapExposure = 2 };
        var root = new Window { Size = new(64, 64) };
        var rootShape = new Shape(Colors.White);
        var child = new SubViewport { Size = new(64, 64), TransparentBG = true, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        var childShape = new Shape(Colors.Black);
        var HDR = new SubViewport
        {
            Size = new(64, 64),
            UseHDR2D = true,
            World = world,
            MSAA2D = Viewport.MSAA.Samples4X,
            ScreenSpaceAA = mode,
            RenderTargetUpdateMode = ViewportUpdateMode.Always
        };
        root.AddChild(rootShape);
        root.AddChild(new MipPatch(mips) { Position = new(44, 44), TextureFilter = TextureFilter.LinearWithMipmaps });
        root.AddChild(child);
        child.AddChild(childShape);
        root.AddChild(HDR);
        HDR.AddChild(new WorldEnvironment { Environment = environment });
        HDR.AddChild(new Shape(Colors.White));
        using var rootTexture = root.GetTexture();
        using var childTexture = child.GetTexture();
        using var HDRTexture = HDR.GetTexture();
        var done = false;
        root.Ready += _ =>
        {
            Check(RenderingServer.GetCurrentRenderingDriverName() == driver &&
                root.ScreenSpaceAA == mode && child.ScreenSpaceAA == ScreenSpaceAAMode.Disabled,
                "The project mode is applied to the unassigned root and keeps offscreen selection independent.");
            RenderingServer.SetDefaultClearColor(Colors.Black);
            var rootRID = root.GetViewportRID();
            rootShape.CheckCapture = () => Reject<InvalidOperationException>(() => root.ScreenSpaceAA = ScreenSpaceAAMode.Disabled);
            Reject<InvalidOperationException>(() => Task.Run(() => root.ScreenSpaceAA = ScreenSpaceAAMode.Disabled).GetAwaiter().GetResult());
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ViewportSetScreenSpaceAA(rootRID, ScreenSpaceAAMode.Max));
            Reject<ArgumentException>(() => RenderingServer.ViewportSetScreenSpaceAA(default, mode));

            var owned = RenderingServer.ViewportCreate();
            RenderingServer.ViewportSetSize(owned, 64, 64);
            RenderingServer.ViewportSetActive(owned, true);
            RenderingServer.ViewportSetUpdateMode(owned, ViewportUpdateMode.Always);
            RenderingServer.ViewportSetScreenSpaceAA(owned, mode);
            var ownedTexture = RenderingServer.ViewportGetTexture(owned);
            var canvas = RenderingServer.CanvasCreate();
            RenderingServer.ViewportAttachCanvas(owned, canvas);
            var item = RenderingServer.CanvasItemCreate();
            RenderingServer.CanvasItemSetParent(item, canvas);
            RenderingServer.CanvasItemAddPolygon(item, Triangle, [Colors.White]);
            RenderingServer.ForceDraw();
            using (var result = rootTexture.GetImage()!)
            {
                Check(Partial(result, alpha: false) > 10, "Screen-space antialiasing smooths visible edges on the root.");
                var sample = result.GetPixel(52, 52);
                var expectedRed = mode == ScreenSpaceAAMode.FXAA ? .25f : 0f;
                Check(MathF.Abs(sample.R - expectedRed) < .02f && MathF.Abs(sample.G - (1 - expectedRed)) < .02f &&
                    root.TextureMIPMapBias == 0, $"The selected mode applies its mip policy: {sample}.");
            }
            using (var result = RenderingServer.Texture2DGet(ownedTexture)!) Check(Partial(result, alpha: false) > 10, "The RID viewport applies screen-space antialiasing.");
            using var sharp = childTexture.GetImage()!;
            Check(Partial(sharp, alpha: true) == 0, "Disabled transparent viewport has single-sample coverage.");
            using var filteredHDR = HDRTexture.GetImage()!;
            Check(filteredHDR.PixelFormat == Image.Format.Rgbah && filteredHDR.GetPixel(16, 20).R > 1.9f,
                "Screen-space antialiasing preserves the half-float output and flat highlights after tone mapping.");
            HDR.ScreenSpaceAA = ScreenSpaceAAMode.Disabled;
            child.ScreenSpaceAA = mode;
            RenderingServer.ViewportSetScreenSpaceAA(rootRID, ScreenSpaceAAMode.Disabled);
            rootShape.CheckCapture = null;
            RenderingServer.ForceDraw();
            using (var result = rootTexture.GetImage()!)
                Check(Partial(result, alpha: false) == 0 && root.ScreenSpaceAA == mode &&
                    result.GetPixel(52, 52) == Colors.Green,
                    "The renderer override removes the pass and mip bias while preserving scene authoring.");
            using (var result = HDRTexture.GetImage()!)
                Check(!result.GetData().AsSpan().SequenceEqual(filteredHDR.GetData()),
                    "The pass filters the image after MSAA resolve and tone mapping.");
            using var smoothed = childTexture.GetImage()!;
            Check(Partial(smoothed, alpha: true) > 10 && smoothed.GetPixel(16, 20) == Colors.Black,
                "A black silhouette over transparency receives fractional coverage with a flat interior.");
            var device = RenderingServer.GetRenderingDevice()!;
            var export = RenderingServer.TextureGetRDTexture(childTexture.GetRID());
            Check(device.TextureGetData(export, 0).AsSpan().SequenceEqual(smoothed.GetData()), "Device export sees the filtered completed image.");
            child.RenderTargetClearMode = ViewportClearMode.Never;
            childShape.Visible = false;
            root.ScreenSpaceAA = root.ScreenSpaceAA;
            for (var i = 0; i < 3; i++) RenderingServer.ForceDraw();
            using (var result = rootTexture.GetImage()!) Check(Partial(result, alpha: false) > 10, "An equal scene assignment republishes the antialiasing mode.");
            using (var retained = childTexture.GetImage()!)
                Check(retained.GetData().AsSpan().SequenceEqual(smoothed.GetData()), "Clear-never frames retain unfiltered history and avoid cumulative blur.");
            if (mode == ScreenSpaceAAMode.SMAA)
            {
                child.ScreenSpaceAA = ScreenSpaceAAMode.FXAA;
                RenderingServer.ForceDraw();
                using (var result = childTexture.GetImage()!)
                    Check(!result.GetData().AsSpan().SequenceEqual(smoothed.GetData()), "The two filters produce distinct edge coverage.");
                child.ScreenSpaceAA = ScreenSpaceAAMode.SMAA;
                RenderingServer.ForceDraw();
                using var restored = childTexture.GetImage()!;
                Check(restored.GetData().AsSpan().SequenceEqual(smoothed.GetData()), "SMAA recreates its intermediates on the retained source.");
            }
            child.ScreenSpaceAA = ScreenSpaceAAMode.Disabled;
            RenderingServer.ForceDraw();
            using (var result = childTexture.GetImage()!)
                Check(result.GetData().AsSpan().SequenceEqual(sharp.GetData()), "Disabling antialiasing publishes the original retained frame.");
            Check(RenderingServer.TextureGetRDTexture(childTexture.GetRID()) == export, "Toggling antialiasing keeps the viewport device identity.");
            child.Size = new(80, 72);
            child.RenderTargetClearMode = ViewportClearMode.Always;
            childShape.Visible = true;
            child.ScreenSpaceAA = mode;
            RenderingServer.ForceDraw();
            using (var resized = childTexture.GetImage()!)
                Check(resized.Size == new Vector2i(80, 72) && Partial(resized, alpha: true) > 10, "Resizing prepares new render history and still filters edges.");

            RenderingServer.FreeRID(item);
            RenderingServer.FreeRID(canvas);
            RenderingServer.FreeRID(owned);
            Reject<ArgumentException>(() => RenderingServer.ViewportSetScreenSpaceAA(owned, ScreenSpaceAAMode.Disabled));
            done = true;
            root.Tree!.Quit();
        };
        Check(Engine.Run(root) == 0 && done, "The screen-space antialiasing fixture completed.");
    }

    private static int Partial(Image image, bool alpha)
    {
        var count = 0;
        for (var y = 0; y < image.Height; y++)
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image.GetPixel(x, y);
                if ((alpha ? pixel.A : pixel.R) is > .01f and < .99f) count++;
            }
        return count;
    }
}
