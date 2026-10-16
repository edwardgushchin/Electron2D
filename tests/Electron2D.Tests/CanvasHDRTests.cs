using Electron2D;
using static TextureArrayTests;

internal static partial class CanvasHDRTests
{
    private sealed class ColorPainter(Texture linear, Texture encoded) : Entity
    {
        protected override void OnDraw()
        {
            DrawRect(new(0, 0, 16, 16), new(.5f, .25f, .75f, 1));
            DrawTextureRect(linear, new(20, 0, 16, 16), false);
            DrawTextureRect(encoded, new(4, 24, 33, 12), false);
        }
    }
    private static float Linear(float value) => value <= .04045f ? value / 12.92f : MathF.Pow((value + .055f) / 1.055f, 2.4f);
    internal static void RunColor()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf); image.Fill(new(2.5f, .125f, .25f, 1));
        using var floating = ImageTexture.CreateFromImage(image);
        using var checker = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, [0, 0, 0, 255, 255, 255, 255, 255]);
        using var encoded = ImageTexture.CreateFromImage(checker);
        var root = new Window { Size = new(128, 64), UseHDR2D = true };
        var painter = new ColorPainter(floating, encoded) { TextureFilter = TextureFilter.Linear }; root.AddChild(painter);
        using var texture = root.GetTexture(); var frames = 0; long before = 0, allocated = 0;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!; var storage = RenderingServer.TextureGetRDTexture(encoded.GetRID());
            root.Tree!.ProcessFrameStarted += _ => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePreDraw += painter.QueueRedraw;
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                ++frames;
                if (frames <= 3 || frames == 88)
                {
                    using var pixels = texture.GetImage()!;
                    Check(MathF.Abs(pixels.GetPixel(4, 4).R - Linear(.5f)) < .001f, $"HDR vertex colors must be linear: {pixels.GetPixel(4, 4)}.");
                    Check(MathF.Abs(pixels.GetPixel(24, 4).R - 2.5f) < .001f, "Floating-point source values remain linear and unclamped.");
                    var expected = frames == 2 ? Linear(128f / 255) : .5f;
                    Check(MathF.Abs(pixels.GetPixel(20, 30).R - expected) < .002f, $"Encoded source filtering follows native writes and linearization: {pixels.GetPixel(20, 30)} != {expected}.");
                    using var screen = RenderingServer.Service!.Readback();
                    Check(screen.PixelFormat == Image.Format.Rgba8 && MathF.Abs(screen.GetPixel(4, 4).R - .5f) < .01f, "Actual SDR presentation applies the inverse transfer function.");
                }
                if (frames == 1)
                {
                    device.TextureUpdate(storage, 0, [128, 128, 128, 255, 128, 128, 128, 255]);
                    using var authored = encoded.GetImage()!;
                    Check(authored.GetData().AsSpan().SequenceEqual(checker.GetData()), "Native writes preserve authored color bytes.");
                }
                if (frames == 2) encoded.Update(checker);
                if (frames == 16) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
                if (frames == 88) { Check(allocated == 0, $"HDR color replay allocated {allocated} owner-thread bytes."); root.Tree!.Quit(); }
            };
        };
        Check(Engine.Run(root) == 0 && frames == 88, "HDR color host.");
        Console.WriteLine($"HDR color: authored colors, raw float sources, linear byte filtering, native device writes/source restoration, SDR presentation; 24 warm + 64 active redraw frames, {allocated} owner-thread bytes.");
    }

    internal static void RunManaged()
    {
        using var view = new SubViewport(); using var texture = view.GetTexture(); using var alias = (ViewportTexture)texture.Duplicate();
        Check(!view.UseHDR2D && texture.PixelFormat == Image.Format.Rgba8, "Default canvas storage remains normalized bytes.");
        var identity = texture.GetRID(); var notices = 0; alias.Changed += _ => notices++;
        Action<Resource> failed = _ => throw new ArithmeticException("HDR observer"); texture.Changed += failed;
        Reject<AggregateException>(() => view.UseHDR2D = true); texture.Changed -= failed;
        Check(view.UseHDR2D && texture.PixelFormat == Image.Format.Rgbah && texture.GetRID() == identity && notices == 1, "HDR observer failure preserves committed state and notifies every texture view.");
        view.UseHDR2D = true; Check(notices == 1, "Unchanged authored HDR state keeps metadata quiet.");
        using var scene = new PackedScene(); scene.Pack(view); using var copied = (SubViewport)scene.Instantiate();
        Check(copied.UseHDR2D && copied.GetTexture().PixelFormat == Image.Format.Rgbah && copied.RenderingOwner is null, "Packed scenes preserve authored HDR without native identities.");
        var previous = ProjectSettings.Get(ProjectSettings.HDR2D);
        try
        {
            ProjectSettings.Set(ProjectSettings.HDR2D, true); using var root = new Window(); root.ApplyProjectHDR();
            Check(root.UseHDR2D, "An unassigned root uses the typed project HDR setting.");
            root.UseHDR2D = false; root.ApplyProjectHDR(); Check(!root.UseHDR2D, "Explicit root selection retains precedence over project HDR.");
            using var child = new SubViewport(); Check(!child.UseHDR2D, "Offscreen canvases retain their independent HDR selection.");
        }
        finally { ProjectSettings.Set(ProjectSettings.HDR2D, previous); }
        Reject<InvalidOperationException>(() => RenderingServer.ViewportSetUseHDR2D(default, true));
        FileStorage();
        view.Dispose(); Reject<ObjectDisposedException>(() => view.UseHDR2D = false);
        Console.WriteLine("HDR viewport state: defaults, typed project precedence, scene storage, view metadata/identity, committed observer failures and guards passed.");
    }

    internal static void RunStorage(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf); image.Fill(new(2.5f, .125f, .25f, 1));
        using var source = ImageTexture.CreateFromImage(image);
        var root = new Window { Size = new(128, 64), UseHDR2D = true, CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        root.AddChild(new Sprite { Texture = source, Centered = false, Scale = new(16, 16) });
        root.AddChild(new HDRGuard(root));
        using var texture = root.GetTexture(); var identity = texture.GetRID(); var frames = 0; RID storage = default; RenderingDevice device = null!;
        if (backend == "compatibility")
        {
            Reject<NotSupportedException>(() => Engine.Run(root));
            Check(root.IsDisposed && RenderingServer.Service is null, "Unsupported HDR closes the host cleanly.");
            Console.WriteLine("Compatibility HDR target rejection passed."); return;
        }
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!;
            Reject<InvalidOperationException>(() => Task.Run(() => root.UseHDR2D = false).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => texture.PixelFormat).GetAwaiter().GetResult());
            using (var foreign = new SubViewport()) Reject<InvalidOperationException>(() => RenderingServer.ViewportSetUseHDR2D(foreign.GetViewportRID(), true));
            RenderingServer.FramePostDraw += () =>
            {
                ++frames; using var pixels = texture.GetImage()!; var hdr = frames is 1 or 3;
                Check(pixels.PixelFormat == (hdr ? Image.Format.Rgbah : Image.Format.Rgba8), "Completed readback retains actual target format.");
                Check(MathF.Abs(pixels.GetPixel(4, 4).R - (hdr ? 2.5f : 1f)) < .001f, "Actual half-float storage preserves shader values above one.");
                var next = RenderingServer.TextureGetRDTexture(identity); using var format = device.TextureGetFormat(next);
                Check(format.Format == (hdr ? RenderingDevice.DataFormat.R16G16B16A16SFLOAT : RenderingDevice.DataFormat.R8G8B8A8UNORM), "Borrowed RD identity describes actual canvas storage.");
                var nativeBytes = device.TextureGetData(next, 0);
                Check(format.Width == pixels.Width && format.Height == pixels.Height && nativeBytes.Length == checked(pixels.Width * pixels.Height * (hdr ? 8 : 4)), $"Borrowed canvas readback uses the actual pixel stride: {format.Width}x{format.Height}, image {pixels.Width}x{pixels.Height}, {nativeBytes.Length} bytes.");
                if (storage.IsValid()) Check(next != storage && !device.TextureIsValid(storage), "Format replacement expires prior native aliases.");
                storage = next; Check(texture.GetRID() == identity, "HDR transitions retain the logical texture RID.");
                if (frames == 1) root.UseHDR2D = false;
                if (frames == 2) { RenderingServer.ViewportSetUseHDR2D(root.GetViewportRID(), true); Check(!root.UseHDR2D, "Server HDR override preserves authored state."); }
                if (frames == 3) root.UseHDR2D = root.UseHDR2D;
                if (frames == 4) root.Tree!.Quit();
            };
        };
        Check(Engine.Run(root) == 0 && frames == 4 && root.IsDisposed && device.IsDisposed && RenderingServer.Service is null, "HDR target lifecycle.");
        Console.WriteLine("HDR storage smoke: real half-float values, byte fallback, server overrides/republish, exact RD format/stride, alias expiration and cleanup passed.");
    }
}
