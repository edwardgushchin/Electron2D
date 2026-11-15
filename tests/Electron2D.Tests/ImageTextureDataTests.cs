using Electron2D;
using Path = System.IO.Path;

internal static class ImageTextureDataTests
{
    private sealed class CustomTexture(Image pixels) : Texture
    {
        private Image _pixels = pixels;

        public override int GetWidth() => 8;
        public override int GetHeight() => 6;
        public override Image GetImage() => Image.CreateFromData(_pixels.Width, _pixels.Height, _pixels.HasMIPMaps, _pixels.PixelFormat, _pixels.GetData());

        internal void Replace(Image replacement)
        {
            _pixels = replacement;
            EmitChanged();
        }
    }

    internal static void RunManaged()
    {
        using var source = Image.CreateFromData(2, 1, true, Image.Format.La8, [255, 0, 128, 255, 64, 127]);
        using var replacement = Image.CreateFromData(2, 1, true, Image.Format.La8, [90, 255, 80, 0, 70, 255]);
        using var rgb = Image.CreateFromData(2, 1, false, Image.Format.Rgb8, [0, 0, 255, 0, 0, 255]);
        using var empty = new ImageTexture();
        Check(!empty.ResourceLocalToScene && empty.GetSize() == Vector2.Zero && empty.PixelFormat == Image.Format.L8 &&
            !empty.HasAlpha && !empty.HasMIPMaps && empty.MIPMapCount == 0 && empty.GetImage() is null &&
            empty.IsPixelOpaque(0, 0) && empty.GetRID().IsValid(), "Empty texture defaults");
        Reject<InvalidOperationException>(() => empty.Update(source));
        using var missing = new Image();
        Reject<ArgumentException>(() => ImageTexture.CreateFromImage(missing));
        Reject<ArgumentNullException>(() => ImageTexture.CreateFromImage(null!));

        using var texture = ImageTexture.CreateFromImage(source);
        var rid = texture.GetRID();
        var changes = new List<(Vector2 Size, Image.Format Format, byte[] Data)>();
        texture.Changed += _ =>
        {
            using var committed = texture.GetImage()!;
            changes.Add((texture.GetSize(), texture.PixelFormat, committed.GetData()));
        };
        var original = source.GetData();
        source.Fill(Colors.White);
        texture.SetSizeOverride(new(8, 6));
        texture.SetSizeOverride(Vector2i.Zero);
        Check(changes.Count == 1 && changes[0].Size == new Vector2(8, 6), "Size override publishes once, after commitment");
        Check(texture.GetWidth() == 8 && texture.GetHeight() == 6 && !texture.IsPixelOpaque(int.MinValue, 0) &&
            texture.IsPixelOpaque(int.MaxValue, 0), "Opacity uses logical coordinates and clamps to physical edge texels");
        VerifyImage(texture, Image.Format.La8, true, original);
        using (var copy = texture.GetImage()!) copy.Fill(Colors.White);
        VerifyImage(texture, Image.Format.La8, true, original);

        Reject<ArgumentException>(() => texture.SetImage(missing));
        Reject<ArgumentOutOfRangeException>(() => texture.SetSizeOverride(new(0, -1)));
        using var wrongSize = Image.CreateEmpty(8, 6, true, Image.Format.La8);
        using var wrongMips = Image.CreateEmpty(2, 1, false, Image.Format.La8);
        using var wrongFormat = Image.CreateEmpty(2, 1, true, Image.Format.Rgba8);
        Reject<ArgumentException>(() => texture.Update(wrongSize));
        Reject<ArgumentException>(() => texture.Update(wrongMips));
        Reject<ArgumentException>(() => texture.Update(wrongFormat));
        Check(changes.Count == 1 && texture.GetRID() == rid && texture.GetSize() == new Vector2(8, 6), "Invalid mutation preserves identity, dimensions and event count");
        VerifyImage(texture, Image.Format.La8, true, original);

        texture.Update(replacement);
        Check(changes.Count == 2 && changes[1].Size == new Vector2(8, 6) &&
            changes[1].Data.SequenceEqual(replacement.GetData()) && texture.GetRID() == rid &&
            texture.IsPixelOpaque(0, 0) && !texture.IsPixelOpaque(7, 0), "Update publishes physical pixels and retains logical dimensions and RID");
        VerifyScene(texture, replacement, original);
        texture.SetImage(rgb);
        Check(changes.Count == 3 && changes[2].Size == new Vector2(2, 1) &&
            changes[2].Format == Image.Format.Rgb8 && texture.GetRID() == rid, "Replacement resets the logical size and publishes the new format under the same RID");
        VerifyImage(texture, Image.Format.Rgb8, false, rgb.GetData());
        texture.Dispose();
        Reject<ObjectDisposedException>(() => texture.GetImage());
        Reject<ObjectDisposedException>(() => texture.GetRID());

        using var customImage = Image.CreateFromData(2, 1, true, Image.Format.La8, original);
        using Texture custom = new CustomTexture(customImage);
        Check(custom.GetSize() == new Vector2(8, 6) && !custom.IsPixelOpaque(0, 0), "Custom dimensions and opacity dispatch through Texture");
        VerifyImage(custom, Image.Format.La8, true, original);
        using (var copy = custom.GetImage()!) copy.Fill(Colors.White);
        VerifyImage(custom, Image.Format.La8, true, original);
        ((CustomTexture)custom).Replace(rgb);
        VerifyImage(custom, Image.Format.Rgb8, false, rgb.GetData());
        Check(custom.IsPixelOpaque(0, 0), "Custom Changed refreshes default pixel metadata and opacity");
        Console.WriteLine("Texture data: empty defaults, physical mips, copies, custom dispatch, opacity, Changed, rollback, RID and scene-local round trip passed.");
    }

    private static void VerifyScene(ImageTexture texture, Image replacement, byte[] original)
    {
        var folder = Directory.CreateTempSubdirectory("e2d-texture-data-");
        try
        {
            texture.ResourceLocalToScene = true;
            using var root = new Sprite { Texture = texture };
            using var packed = new PackedScene();
            packed.Pack(root);
            var file = Path.Combine(folder.FullName, "texture.e2dscene");
            ResourceSaver.Save(packed, file);
            using var loaded = ResourceLoader.Load<PackedScene>(file, ResourceLoader.CacheMode.Ignore);
            using var first = (Sprite)loaded.Instantiate();
            using var second = (Sprite)loaded.Instantiate();
            loaded.Dispose();
            var a = (ImageTexture)first.Texture!;
            var b = (ImageTexture)second.Texture!;
            Check(a.ResourceLocalToScene && b.ResourceLocalToScene && a.GetLocalScene() == first &&
                b.GetLocalScene() == second && a.GetRID() != b.GetRID() && b.GetRID() != texture.GetRID() &&
                a.GetSize() == new Vector2(8, 6) && b.GetSize() == a.GetSize(), "Loaded scene textures have independent lifetimes, identities and logical dimensions");
            using var changed = Image.CreateFromData(2, 1, true, Image.Format.La8, original);
            a.Update(changed);
            VerifyImage(a, Image.Format.La8, true, original);
            first.Dispose();
            Check(a.IsDisposed && !b.IsDisposed, "Disposing one instance releases only its local texture");
            VerifyImage(b, Image.Format.La8, true, replacement.GetData());
            VerifyImage(texture, Image.Format.La8, true, replacement.GetData());
        }
        finally { folder.Delete(recursive: true); }
    }

    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Image.CreateFromData(2, 1, true, Image.Format.Rgba8,
            [255, 0, 0, 255, 0, 255, 0, 255, 22, 33, 44, 255]);
        using var revised = Image.CreateFromData(2, 1, true, Image.Format.Rgba8,
            [0, 255, 0, 255, 255, 0, 0, 255, 44, 33, 22, 255]);
        using var replacement = Image.CreateFromData(1, 1, false, Image.Format.Rgb8, [0, 0, 255]);
        using var texture = ImageTexture.CreateFromImage(source);
        texture.SetSizeOverride(new(4, 2));
        var rid = texture.GetRID();
        var host = new Window { Size = new(64, 64) };
        host.AddChild(new Sprite { Texture = texture, Centered = false, Scale = new(12, 12), TextureFilter = TextureFilter.Nearest });
        using var target = host.GetTexture();
        var frames = 0;
        RID borrowedStorage = default;
        var driver = "";
        host.Ready += _ =>
        {
            driver = RenderingServer.GetCurrentRenderingDriverName();
            Check(RenderingServer.GetCurrentRenderingMethod() == backend && driver != "software", "Selected native backend is active");
            RenderingServer.FramePostDraw += ReadFrame;
        };
        Check(Engine.Run(host) == 0 && frames == 9 && host.IsDisposed, "Native texture route renders and releases its host");
        Console.WriteLine($"Texture data: {backend}/{driver} rendered three revisions, preserved borrowed RID rules and released native storage.");

        void ReadFrame()
        {
            frames++;
            if (frames % 3 != 0)
                return;
            using var frame = target.GetImage()!;
            Check(texture.GetRID() == rid && RenderingServer.TextureGetNativeHandle(rid) != 0, "Live native storage under stable resource RID");
            var device = RenderingServer.GetRenderingDevice();
            if (frames == 3)
            {
                CheckPixel(frame, 6, Colors.Red);
                CheckPixel(frame, 30, Colors.Lime);
                Reject<InvalidOperationException>(() => RenderingServer.FreeRID(rid));
                borrowedStorage = RenderingServer.TextureGetRDTexture(rid);
                if (backend == "gpu")
                    Check(device!.TextureIsValid(borrowedStorage), "GPU exposes current borrowed storage");
                Reject<ArgumentException>(() => texture.Update(replacement));
                texture.Update(revised);
            }
            else if (frames == 6)
            {
                CheckPixel(frame, 6, Colors.Lime);
                CheckPixel(frame, 30, Colors.Red);
                if (backend == "gpu")
                    Check(RenderingServer.TextureGetRDTexture(rid) == borrowedStorage, "Compatible update retains GPU storage");
                texture.SetImage(replacement);
                if (backend == "gpu")
                    Check(!device!.TextureIsValid(borrowedStorage), "Replacement invalidates the old borrowed GPU storage");
            }
            else
            {
                CheckPixel(frame, 6, Colors.Blue);
                var lastStorage = RenderingServer.TextureGetRDTexture(rid);
                texture.Dispose();
                Reject<ArgumentException>(() => RenderingServer.TextureGetNativeHandle(rid));
                if (backend == "gpu")
                    Check(lastStorage != borrowedStorage && !device!.TextureIsValid(lastStorage), "Disposal invalidates the replacement GPU storage");
                RenderingServer.FramePostDraw -= ReadFrame;
                host.Tree!.Quit();
            }
        }
    }

    private static void VerifyImage(Texture texture, Image.Format format, bool mipmaps, byte[] data)
    {
        Check(texture.PixelFormat == format && texture.HasAlpha == (format == Image.Format.La8) &&
            texture.HasMIPMaps == mipmaps && texture.MIPMapCount == (mipmaps ? 1 : 0), "Texture reports source format, alpha and mip metadata");
        using var snapshot = texture.GetImage()!;
        Check(snapshot.Width == 2 && snapshot.Height == 1 && snapshot.PixelFormat == format &&
            snapshot.HasMIPMaps == mipmaps && snapshot.GetData().SequenceEqual(data), "Image snapshot preserves physical dimensions and all source bytes");
    }

    private static void CheckPixel(Image frame, int x, Color expected) =>
        Check(frame.GetPixel(x, 6).ToRGBA32() == expected.ToRGBA32(), $"Rendered pixel at x={x} matches {expected}");

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }
}
