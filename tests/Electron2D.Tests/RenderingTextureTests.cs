using Electron2D;
using System.Runtime.InteropServices;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyTextureCursor()
    {
        using var display = DisplayServer.Open("Electron2D texture cursor verification", new Vector2i(96, 80));
        using var source = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        source.Fill(Colors.Yellow);
        using var texture = ImageTexture.CreateFromImage(source);
        texture.SetSizeOverride(new Vector2i(512, 512));
        display.CursorSetCustomImage(texture, hotspot: new Vector2(1.75f, 0.25f));
        var installed = SDL3.SDL.GetCursor();
        Check(installed != 0 && !texture.IsDisposed && !source.IsDisposed, "A texture cursor borrows its source and uses original image pixels.");
        Reject<ArgumentOutOfRangeException>(() => display.CursorSetCustomImage(texture, hotspot: new Vector2(2, 0)));
        Reject<ArgumentOutOfRangeException>(() => display.CursorSetCustomImage(texture, hotspot: new Vector2(float.NaN, 0)));
        Exception? wrongThread = null;
        var worker = new Thread(() => { try { display.CursorSetCustomImage(texture); } catch (Exception error) { wrongThread = error; } });
        worker.Start(); worker.Join();
        Check(wrongThread is InvalidOperationException, "Cursor conversion requires the display owner thread.");
        using var empty = new ImageTexture();
        Reject<ArgumentException>(() => display.CursorSetCustomImage(empty));
        using var unrelated = new Shader();
        Reject<ArgumentException>(() => display.CursorSetCustomImage(unrelated));
        Image? temporary = null;
        using var custom = new CursorTexture(() => temporary = source.GetRegion(new RectI(0, 0, 2, 2)));
        Reject<ArgumentOutOfRangeException>(() => display.CursorSetCustomImage(custom, hotspot: new Vector2(2, 0)));
        Check(temporary is { IsDisposed: true }, "A failed cursor conversion releases the custom texture's temporary image.");
        using var oversized = new CursorTexture(() => temporary = Image.CreateEmpty(257, 1, false, Image.Format.Rgba8));
        Reject<ArgumentException>(() => display.CursorSetCustomImage(oversized));
        Check(temporary is { IsDisposed: true }, "The pixel size limit releases the oversized temporary image.");
        using var failure = new CursorTexture(() => throw new InvalidOperationException("injected cursor image failure"));
        Reject<InvalidOperationException>(() => display.CursorSetCustomImage(failure));
        texture.Dispose(); source.Dispose();
        Reject<ObjectDisposedException>(() => display.CursorSetCustomImage(texture));
        Check(SDL3.SDL.GetCursor() == installed, "Failures and disposal preserve the copied native cursor.");
        using var customSuccess = new CursorTexture(() => temporary = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8));
        display.CursorSetCustomImage(customSuccess, DisplayServer.CursorShape.IBeam);
        Check(temporary is { IsDisposed: true } && !customSuccess.IsDisposed && SDL3.SDL.GetCursor() == installed,
            "Successful conversion releases only its temporary image and preserves other shape slots.");
        customSuccess.Dispose();
        display.CursorSetShape(DisplayServer.CursorShape.IBeam);
        Check(SDL3.SDL.GetCursor() != 0 && SDL3.SDL.GetCursor() != installed, "A custom texture cursor survives source disposal.");
        display.CursorSetShape(DisplayServer.CursorShape.Arrow);
        Check(SDL3.SDL.GetCursor() == installed, "Returning to the arrow restores its own texture cursor.");
        display.CursorSetCustomImage(null);
        Check(SDL3.SDL.GetCursor() != 0 && SDL3.SDL.GetCursor() != installed, "Null restores the system cursor without overload ambiguity.");
        using var closesDisplay = new CursorTexture(() =>
        {
            display.Dispose();
            return temporary = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        });
        Reject<ObjectDisposedException>(() => display.CursorSetCustomImage(closesDisplay));
        Check(temporary is { IsDisposed: true }, "Disposal inside custom image capture prevents native access and releases the captured image.");
        Console.WriteLine("Texture cursor ownership and failure checks passed.");
    }

    private sealed class CursorTexture(Func<Image?> read) : Texture
    {
        public override int GetWidth() => 2;
        public override int GetHeight() => 2;
        public override Image? GetImage() => read();
    }

    private static void VerifyTextureResources()
    {
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        image.Fill(Colors.Red);
        image.SetPixel(0, 0, new Color(1, 0, 0, 0));
        using var texture = ImageTexture.CreateFromImage(image);
        image.Fill(Colors.Blue);
        using var pixels = texture.GetImage()!;
        Pixel(pixels, 1, 1, Colors.Red);
        pixels.Fill(Colors.Green);
        using var independent = texture.GetImage()!;
        Pixel(independent, 1, 1, Colors.Red);
        Check(texture.GetWidth() == 2 && texture.GetHeight() == 2 && texture.GetSize() == new Vector2(2, 2) && texture.HasAlpha && !texture.HasMipmaps && texture.MipmapCount == 0,
            "Texture metadata describes its copied original pixels.");
        Check(!texture.IsPixelOpaque(0, 0) && texture.IsPixelOpaque(1, 1) && texture.IsPixelOpaque(int.MaxValue, int.MaxValue), "Opacity checks clamp safely.");
        texture.SetSizeOverride(new Vector2i(8, 0));
        Check(texture.GetSize() == new Vector2(8, 2) && !texture.IsPixelOpaque(3, 0) && texture.IsPixelOpaque(4, 0), "Logical texture sizes map to original pixel coordinates.");
        Reject<ArgumentOutOfRangeException>(() => texture.SetSizeOverride(new Vector2i(-1, 0)));
        using var wrongSize = Image.CreateEmpty(1, 2, false, Image.Format.Rgba8);
        Reject<ArgumentException>(() => texture.Update(wrongSize));
        using var wrongFormat = Image.CreateEmpty(2, 2, false, Image.Format.Rgb8);
        Reject<ArgumentException>(() => texture.Update(wrongFormat));
        using var wrongMips = Image.CreateEmpty(2, 2, true, Image.Format.Rgba8);
        Reject<ArgumentException>(() => texture.Update(wrongMips));
        var original = texture.CapturePixels()!;
        texture.Update(image);
        Check(texture.GetSize() == new Vector2(8, 2) && ReferenceEquals(texture.CapturePixels()!.Allocation, original.Allocation), "Update retains logical size and native allocation identity.");
        using var updated = texture.GetImage()!;
        Pixel(updated, 0, 0, Colors.Blue);
        texture.SetImage(image);
        Check(texture.GetSize() == new Vector2(2, 2) && !ReferenceEquals(texture.CapturePixels()!.Allocation, original.Allocation), "SetImage resets logical size and replaces allocation identity.");
        using var duplicate = (ImageTexture)texture.Duplicate();
        image.Fill(Colors.Yellow); texture.Update(image);
        using var copiedPixels = duplicate.GetImage()!;
        Pixel(copiedPixels, 1, 1, Colors.Blue);
        using var empty = new ImageTexture();
        Check(empty.GetImage() is null && empty.GetSize() == Vector2.Zero && !empty.HasAlpha && empty.IsPixelOpaque(0, 0), "Uninitialized texture metadata is safe.");
        Reject<InvalidOperationException>(() => empty.Update(image));
        using var emptyImage = new Image();
        Reject<ArgumentException>(() => texture.SetImage(emptyImage));
        using var tooLarge = Image.CreateEmpty(16385, 1, false, Image.Format.Rgba8);
        Reject<ArgumentException>(() => ImageTexture.CreateFromImage(tooLarge));
        using var hdr = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf);
        hdr.Fill(new Color(2, 0.5f, 0, 0.75f));
        using var hdrTexture = ImageTexture.CreateFromImage(hdr);
        Check(hdrTexture.HasAlpha && hdrTexture.PixelFormat == Image.Format.Rgbaf && hdrTexture.CapturePixels()!.Upload.Format == Image.Format.Rgbaf,
            "HDR image precision is retained for GPU sampling.");
        using var shader = LoadShader("TextureHlsl");
        using var material = new ShaderMaterial { Shader = shader };
        shader.SetDefaultTextureParameter("colorMap", texture);
        Check(shader.GetShaderUniformList().Single(p => p.Name == "colorMap") is PropertyDescriptor<ShaderMaterial, Texture?>,
            "Reflected texture parameters use the public Texture resource type.");
        material.SetShaderParameter("colorMap", texture);
        material.SetShaderParameter("detailMap", texture);
        Check(material.GetShaderParameter("colorMap") == texture && shader.GetDefaultTextureParameter("colorMap") == texture, "Textures are named borrowed parameters.");
        Reject<ArgumentException>(() => material.SetShaderParameter("tint", texture));
        Reject<ArgumentException>(() => material.SetShaderParameter("colorMap", 1f));
        Reject<ArgumentOutOfRangeException>(() => shader.SetDefaultTextureParameter("colorMap", texture, 1));
        using var graph = (ShaderMaterial)material.Duplicate(true);
        using var graphShader = graph.Shader!;
        using var graphTexture = graph.GetShaderParameter("colorMap")!;
        Check(graphTexture != texture && graph.GetShaderParameter("detailMap") == graphTexture && graphShader.GetDefaultTextureParameter("colorMap") == graphTexture,
            "Deep resource copying preserves aliases across material overrides and Shader defaults.");
        var scratch = new Texture?[16];
        material.SetShaderParameter("colorMap", (Texture?)null);
        material.GetCanvasState()!.CopyTextures(scratch);
        Check(scratch[0] == texture, "Null overrides resolve the Shader default.");
        shader.SetDefaultTextureParameter("colorMap", null);
        Reject<InvalidOperationException>(() => material.GetCanvasState()!.CopyTextures(scratch));
        texture.Dispose();
        Reject<ObjectDisposedException>(() => texture.GetImage());
        Reject<ObjectDisposedException>(() => material.SetShaderParameter("colorMap", texture));
        Reject<ObjectDisposedException>(() => shader.SetDefaultTextureParameter("colorMap", texture));
    }

    private static void VerifyImageDependency()
    {
        Check(SDL3.Image.Version() == 3_004_006, "The pinned SDL_image native package loads through the engine dependency.");
        Check(typeof(Image).Assembly.GetExportedTypes().All(t => t.Namespace != "SDL3"), "Image bindings remain inside the engine assembly.");
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DQAAAEgQGALFXOsAAAAABJRU5ErkJggg==");
        foreach (var valid in new[] { true, false, true })
        {
            var bytes = valid ? png : new byte[] { 0, 1, 2, 3 };
            var pinned = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                var stream = SDL3.SDL.IOFromConstMem(pinned.AddrOfPinnedObject(), (nuint)bytes.Length);
                Check(stream != 0, "The encoded image stream opens.");
                try
                {
                    var surface = SDL3.Image.LoadIO(stream, false);
                    try
                    {
                        if (!valid) Check(surface == 0, "Malformed encoded images fail explicitly.");
                        else
                        {
                            Check(surface != 0, "PNG decoding succeeds: " + SDL3.SDL.GetError());
                            var read = SDL3.SDL.ReadSurfacePixel(surface, 0, 0, out var r, out var g, out var b, out var a);
                            Check(read && r == 255 && g == 0 && b == 0 && a == 128,
                                $"PNG decoding retains color and alpha: read={read}, rgba={r},{g},{b},{a}; {SDL3.SDL.GetError()}");
                        }
                    }
                    finally { if (surface != 0) SDL3.SDL.DestroySurface(surface); }
                }
                finally { Check(SDL3.SDL.CloseIO(stream), "The encoded image stream closes."); }
            }
            finally { pinned.Free(); }
        }
    }

    private static Image MipImage(Color baseColor, Color middle, Color last)
    {
        var data = new byte[(16 + 4 + 1) * 4];
        var offset = 0;
        foreach (var pair in new[] { (baseColor, 16), (middle, 4), (last, 1) })
            for (var i = 0; i < pair.Item2; i++)
            {
                data[offset++] = (byte)(pair.Item1.R * 255); data[offset++] = (byte)(pair.Item1.G * 255);
                data[offset++] = (byte)(pair.Item1.B * 255); data[offset++] = (byte)(pair.Item1.A * 255);
            }
        return Image.CreateFromData(4, 4, true, Image.Format.Rgba8, data);
    }

    private static void VerifyTextureFrame(string fixture)
    {
        using var shader = LoadShader(fixture);
        using var material = new ShaderMaterial { Shader = shader };
        using var source = MipImage(Colors.Red, Colors.Green, Colors.Blue);
        using var texture = ImageTexture.CreateFromImage(source);
        using var black = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        black.Fill(new Color(0, 0, 0, 0));
        using var detail = ImageTexture.CreateFromImage(black);
        using var hdr = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf);
        hdr.Fill(new Color(2, 0, 0, 1));
        using var hdrTexture = ImageTexture.CreateFromImage(hdr);
        using var custom = new TestTexture(Colors.Cyan);
        shader.SetDefaultTextureParameter("colorMap", texture);
        material.SetShaderParameter("detailMap", detail);
        material.SetShaderParameter("tint", Colors.White);
        var window = new Window { Size = new Vector2i(96, 80) };
        var frames = 0;
        var node = new CanvasNode { DrawAction = n => n.DrawRect(new Rect(0, 0, 64, 64), Colors.White), Material = material };
        node.ReadyAction = n =>
        {
            var server = RenderingServer.Instance!;
            server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                frames++;
                using var frame = server.Readback();
                var expected = frames switch
                {
                    1 or 2 or 3 or 5 => Colors.Red,
                    4 => Colors.Green,
                    14 => Colors.Blue,
                    6 => new Color(0.5f, 0, 0, 1),
                    7 or 8 => Colors.Cyan,
                    9 or 10 or 12 => Colors.Yellow,
                    11 => Colors.Red,
                    _ => Colors.White
                };
                try { Pixel(frame, 16, 16, expected); Pixel(frame, 48, 48, expected); }
                catch (InvalidOperationException e) { throw new InvalidOperationException($"{fixture} texture frame {frames}: {e.Message}", e); }
                Check(node.Draws == 1, "Texture updates and bindings reuse retained geometry.");
                if (frames == 1) material.SetShaderParameter("lod", 1f);
                else if (frames == 2) material.SetShaderParameter("lod", 2f);
                else if (frames == 3)
                {
                    using var changed = MipImage(Colors.Green, Colors.Red, Colors.Yellow);
                    texture.Update(changed);
                }
                else if (frames == 4) texture.SetImage(source);
                else if (frames == 5)
                {
                    material.SetShaderParameter("colorMap", hdrTexture);
                    material.SetShaderParameter("tint", new Color(0.25f, 1, 1, 1));
                    material.SetShaderParameter("lod", 0f);
                }
                else if (frames == 6)
                {
                    material.SetShaderParameter("colorMap", custom);
                    material.SetShaderParameter("tint", Colors.White);
                }
                else if (frames == 7) Check(custom.Reads == 1, "Custom Texture supplies one copied snapshot.");
                else if (frames == 8)
                {
                    Check(custom.Reads == 1, "Unchanged custom pixels reuse the snapshot across frames.");
                    custom.Fill(Colors.Yellow);
                }
                else if (frames == 9)
                {
                    Check(custom.Reads == 2, "Changed refreshes custom texture pixels.");
                    using var reordered = LoadShader("TextureReordered");
                    shader.SetSPIRV(reordered.GetSPIRV());
                }
                else if (frames == 10) material.SetShaderParameter("colorMap", (Texture?)null);
                else if (frames == 11) shader.SetDefaultTextureParameter("colorMap", custom);
                else if (frames == 12)
                {
                    black.Fill(Colors.Blue); detail.Update(black);
                    material.SetShaderParameter("detailAmount", 1f);
                }
                else if (frames == 13)
                {
                    using var decoded = new Image();
                    decoded.LoadPNGFromBuffer(black.SavePNGToBuffer());
                    texture.SetImage(decoded);
                    shader.SetDefaultTextureParameter("colorMap", texture);
                    material.SetShaderParameter("detailAmount", 0f);
                }
                else n.Tree!.Quit();
            };
        };
        window.AddChild(node);
        Engine.Instance.Run(window);
        Released(window);
        Check(frames == 14 && !texture.IsDisposed && !detail.IsDisposed && !custom.IsDisposed,
            "Texture sampling, base-level LOD clamping, update/replacement, custom resources, binding reload and borrowed cleanup execute through Engine.Run.");
    }

    private static void VerifyNamedSamplerDefaults(string backend, string fixture)
    {
        using var shader = LoadShader(fixture);
        var original = shader.GetSPIRV();
        using var reordered = LoadShader("TextureReordered");
        using var source = MipImage(Colors.Red, Colors.Green, Colors.Yellow);
        for (var y = 0; y < 4; y++)
            for (var x = 2; x < 4; x++) source.SetPixel(x, y, Colors.Blue);
        using var texture = ImageTexture.CreateFromImage(source);
        using var detail = ImageTexture.CreateFromImage(source);
        shader.SetDefaultTextureParameter("colorMap", texture);
        shader.SetDefaultTextureParameter("detailMap", detail);
        using var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("tint", Colors.White);
        using var copy = (ShaderMaterial)material.Duplicate();
        copy.SetShaderParameter("colorMap", texture);
        var window = new Window { Size = new(96, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.NearestWithMipmaps, CanvasItemDefaultTextureRepeat = Viewport.DefaultCanvasItemTextureRepeat.Mirror };
        var parent = new Entity { TextureFilter = CanvasItem.TextureFilterEnum.Nearest, TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled };
        window.AddChild(parent);
        var nodes = new[]
        {
            new CanvasNode { Name = "first", Material = material },
            new CanvasNode { Name = "copy", Material = copy, Position = new(0, 16), TextureFilter = CanvasItem.TextureFilterEnum.LinearWithMipmapsAnisotropic, TextureRepeat = CanvasItem.TextureRepeatEnum.Mirror },
            new CanvasNode { Name = "shared", Material = material, Position = new(0, 32) }
        };
        foreach (var node in nodes) { node.DrawAction = n => n.DrawRect(new(0, 0, 96, 12), Colors.White); parent.AddChild(node); }
        var frames = 0; var before = 0L; var allocated = 0L;
        var settings = ProjectSettings.Instance; var nearestMip = settings.Get(ProjectSettings.UseNearestMipmapFilter);
        window.Ready += _ =>
        {
            var server = RenderingServer.Instance!;
            server.FramePreDraw += () => before = GC.GetAllocatedBytesForCurrentThread();
            server.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (++frames > 20) allocated += bytes;
                using var image = server.Readback();
                foreach (var y in new[] { 4, 20, 36 })
                {
                    // The base-level boundary lies at u=0.5; sample centers differ by 1/64.
                    Pixel(image, 31, y, new(.53125f, frames >= 3 ? 1 : 0, .46875f, 1));
                    Pixel(image, 32, y, new(.46875f, frames >= 3 ? 1 : 0, .53125f, 1));
                    Pixel(image, 80, y, frames >= 3 ? Colors.Cyan : Colors.Blue);
                }
                if (frames == 1)
                {
                    material.SetShaderParameter("lod", 2f); copy.SetShaderParameter("lod", 2f);
                    parent.TextureFilter = CanvasItem.TextureFilterEnum.NearestWithMipmapsAnisotropic;
                    window.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.LinearWithMipmaps;
                    window.CanvasItemDefaultTextureRepeat = Viewport.DefaultCanvasItemTextureRepeat.Enabled;
                    settings.Set(ProjectSettings.UseNearestMipmapFilter, !nearestMip);
                }
                if (frames == 2)
                {
                    for (var y = 0; y < 4; y++)
                        for (var x = 0; x < 4; x++) source.SetPixel(x, y, x < 2 ? Colors.Yellow : Colors.Cyan);
                    texture.Update(source); detail.Update(source);
                }
                if (frames == 3) shader.SetSPIRV(reordered.GetSPIRV());
                if (frames == 4)
                {
                    // Exercise the second named sampler, after its binding moves during reload.
                    material.SetShaderParameter("tint", new Color(0, 0, 0, 0)); copy.SetShaderParameter("tint", new Color(0, 0, 0, 0));
                    material.SetShaderParameter("detailAmount", 1f); copy.SetShaderParameter("detailAmount", 1f);
                    material.SetShaderParameter("lod", -2f); copy.SetShaderParameter("lod", -2f);
                }
                if (frames == 5) shader.SetSPIRV(original);
                if (frames == 40) window.Tree!.Quit();
            };
        };
        try
        {
            if (backend == "compatibility") Reject<NotSupportedException>(() => Engine.Instance.Run(window));
            else
            {
                Engine.Instance.Run(window);
                Check(frames == 40 && allocated == 0 && nodes[0].Draws == 2 && nodes[1].Draws == 1 && nodes[2].Draws == 2, $"Named sampler result: frames={frames}, allocated={allocated}, draws={string.Join(",", nodes.Select(n => n.Draws))}.");
            }
        }
        finally { settings.Set(ProjectSettings.UseNearestMipmapFilter, nearestMip); }
        Released(window);
        Check(!texture.IsDisposed && !detail.IsDisposed, "Named sampler teardown preserves borrowed texture resources.");
        Console.WriteLine($"Named sampler defaults passed: {backend}/{fixture}; {frames} frames; {allocated} warm rendering bytes.");
    }

    private static void VerifyTextureFailure(string backend)
    {
        using var shader = LoadShader("TextureGlsl");
        using var material = new ShaderMaterial { Shader = shader };
        var window = new Window();
        window.AddChild(new CanvasNode { Material = material, DrawAction = n => n.DrawRect(new Rect(0, 0, 10, 10), Colors.White) });
        if (backend == "compatibility") Reject<NotSupportedException>(() => Engine.Instance.Run(window));
        else Reject<InvalidOperationException>(() => Engine.Instance.Run(window));
        Released(window);
    }

    private sealed class TestTexture : Texture
    {
        private readonly Image _image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8);
        internal int Reads;
        internal TestTexture(Color color) { _image.Fill(color); }
        public override int GetWidth() { ThrowIfDisposed(); return _image.Width; }
        public override int GetHeight() { ThrowIfDisposed(); return _image.Height; }
        public override Image GetImage() { ThrowIfDisposed(); Reads++; return (Image)_image.Duplicate(); }
        internal void Fill(Color color) { ThrowIfDisposed(); _image.Fill(color); EmitChanged(); }
        protected override void Dispose(bool disposing) { if (disposing) _image.Dispose(); base.Dispose(disposing); }
    }
}
