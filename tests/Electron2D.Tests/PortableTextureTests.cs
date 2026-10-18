using Electron2D;
using Path = System.IO.Path;

internal static class PortableTextureTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static PropertyDescriptor<PortableCompressedTexture, byte[]> Data(PortableCompressedTexture texture) => (PropertyDescriptor<PortableCompressedTexture, byte[]>)texture.GetPropertyList().Single(p => p.Name == "portable_data");
    private static Image Source(bool mips = true, Image.Format format = Image.Format.Rgba8)
    { var image = Image.CreateEmpty(7, 5, false, format); image.Fill(new(.8f, .25f, .125f, .5f)); if (mips) image.GenerateMIPMaps(); return image; }
    internal static void Run(string profile)
    {
        if (profile == "managed") Managed(); else if (profile == "restore") Restore(Environment.GetEnvironmentVariable("ELECTRON2D_PORTABLE_ARCHIVE")!); else Native(profile);
        Console.WriteLine("Portable texture " + profile + " checks passed.");
    }
    private static void Managed()
    {
        var directory = Path.Combine(Path.GetTempPath(), "e2d-portable-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            using var source = Source(); using var texture = new PortableCompressedTexture(); var rid = texture.GetRID();
            Check(texture.GetSize() == Vector2.Zero && texture.SizeOverride == Vector2.Zero && !texture.KeepCompressedBuffer && !PortableCompressedTexture.IsKeepingAllCompressedBuffers() && !texture.ResourceLocalToScene && texture.GetCompressionMode() == 0 && texture.GetImage() is null && texture.IsPixelOpaque(0, 0), "Empty portable defaults.");
            texture.SizeOverride = new(8, 4); Check(texture.DrawingSize == Vector2.Zero && texture.GetSize() == Vector2.Zero, "An empty texture does not draw a placeholder when an override is set."); texture.SizeOverride = Vector2.Zero;
            ResourceSaver.Save(texture, Path.Combine(directory, "empty.e2dres"));
            foreach (var mode in Enum.GetValues<PortableCompressedTexture.CompressionMode>())
            {
                texture.KeepCompressedBuffer = true; var order = new List<string>();
                void Properties(ElectronObject _) => order.Add("properties"); void Changed(Resource _) => order.Add("changed");
                texture.PropertyListChanged += Properties; texture.Changed += Changed; texture.CreateFromImage(source, mode); texture.PropertyListChanged -= Properties; texture.Changed -= Changed;
                Check(order.SequenceEqual(new[] { "properties", "changed" }) && texture.GetRID() == rid && texture.GetSize() == new Vector2(7, 5) && texture.SizeOverride == new Vector2(7, 5) && texture.MIPMapCount == 2 && texture.HasMIPMaps && texture.GetCompressionMode() == mode, "Atomic complete-mip publication and stable identity: " + mode);
                using var pixels = texture.GetImage()!; using var decoded = (Image)pixels.Duplicate(); if (decoded.IsCompressed) decoded.Decompress();
                var pixel = decoded.GetPixel(2, 2); Check(MathF.Abs(pixel.R - .8f) < .12f && MathF.Abs(pixel.G - .25f) < .12f && MathF.Abs(pixel.B - .125f) < .12f && MathF.Abs(pixel.A - .5f) < .12f, "Independent constant-color oracle: " + mode + " " + pixel);
                Check(texture.HasAlpha == (pixels.PixelFormat is Image.Format.Rgba8 or Image.Format.La8), "Stored-format alpha reporting: " + mode);
                texture.SizeOverride = new(14.9f, 10); Check(texture.GetSize() == new Vector2(7, 5) && texture.DrawingSize == new Vector2(14, 10), "Override changes sampling dimensions, preserving original queries.");
                var path = Path.Combine(directory, mode + ".e2dres"); ResourceSaver.Save(texture, path, SaverFlags.Compress | SaverFlags.SaveBigEndian);
                using var loaded = ResourceLoader.Load<PortableCompressedTexture>(path, ResourceLoader.CacheMode.Ignore); using var stored = loaded.GetImage()!;
                Check(loaded.GetCompressionMode() == mode && loaded.SizeOverride == texture.SizeOverride && stored.GetData().AsSpan().SequenceEqual(pixels.GetData()), "Typed archive retains encoded payload, policy, override and mip bytes: " + mode);
                foreach (var deep in new[] { false, true })
                { using var copy = (PortableCompressedTexture)texture.Duplicate(deep); using var read = copy.GetImage()!; Check(copy.GetRID() != rid && copy.SizeOverride == texture.SizeOverride && read.GetData().AsSpan().SequenceEqual(pixels.GetData()), "Independent duplicate."); copy.CreateFromImage(source, PortableCompressedTexture.CompressionMode.Lossless); Check(texture.GetCompressionMode() == mode, "Duplicate replacement isolation."); }
            }
            using (var wide = Image.CreateEmpty(16384, 1, false, Image.Format.Rgba8))
            { wide.Fill(Colors.Blue); using var fallback = new PortableCompressedTexture(); fallback.CreateFromImage(wide, PortableCompressedTexture.CompressionMode.Lossless); using var read = fallback.GetImage()!; Check(read.Width == 16384 && read.GetPixel(16383, 0).B == 1, "Lossless PNG fallback outside WebP dimension range."); }
            using (var transparent = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8))
            {
                transparent.Fill(new(.8f, .25f, .125f, 0));
                foreach (var mode in Enum.GetValues<PortableCompressedTexture.CompressionMode>())
                { using var alphaTexture = new PortableCompressedTexture(); alphaTexture.CreateFromImage(transparent, mode); Check(!alphaTexture.IsPixelOpaque(int.MinValue, int.MaxValue), "Decoded alpha and clamped opacity: " + mode); }
            }
            using (var read = texture.GetImage()!) { if (read.IsCompressed) read.Decompress(); read.Fill(Colors.Blue); using var untouched = texture.GetImage()!; if (untouched.IsCompressed) untouched.Decompress(); Check(untouched.GetPixel(0, 0).R > .6f, "Image reads do not expose texture-owned storage."); }
            using (var rgb = Source(format: Image.Format.Rgb8))
            { using var portable = new PortableCompressedTexture { KeepCompressedBuffer = true }; portable.CreateFromImage(rgb, PortableCompressedTexture.CompressionMode.BasisUniversal); Check(!portable.HasAlpha, "Basis alpha flag follows source metadata, not the RGBA transcoder output."); using var copy = (PortableCompressedTexture)portable.Duplicate(); Check(!copy.HasAlpha, "Basis alpha metadata survives duplication."); var saved = Path.Combine(directory, "rgb-basis.e2dres"); ResourceSaver.Save(portable, saved); using var loaded = ResourceLoader.Load<PortableCompressedTexture>(saved, ResourceLoader.CacheMode.Ignore); Check(!loaded.HasAlpha, "Basis alpha metadata survives archives."); }
            var original = Data(texture).GetValue(texture); var format = texture.PixelFormat;
            foreach (var length in new[] { 0, 1, 4, 27, 31, original.Length - 1 })
            {
                if (length == 0) continue;
                Reject<InvalidDataException>(() => Data(texture).SetValue(texture, original[..length])); Check(texture.PixelFormat == format && texture.GetRID() == rid, "Corrupt envelope preserves resource.");
            }
            Reject<InvalidDataException>(() => Data(texture).SetValue(texture, [.. original, 0]));
            var oversized = (byte[])original.Clone(); System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(oversized.AsSpan(8), 16384); System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(oversized.AsSpan(20), 15); Reject<InvalidDataException>(() => Data(texture).SetValue(texture, oversized)); Check(texture.GetSize() == new Vector2(7, 5), "Malformed complete-mip metadata rejects without publication.");
            using (var observed = new PortableCompressedTexture())
            {
                var identity = observed.GetRID(); void Fail(Resource _) => throw new IOException("Portable observer"); observed.Changed += Fail;
                Reject<IOException>(() => observed.CreateFromImage(source, PortableCompressedTexture.CompressionMode.BasisUniversal)); observed.Changed -= Fail;
                Check(observed.GetRID() == identity && observed.GetCompressionMode() == PortableCompressedTexture.CompressionMode.BasisUniversal && observed.GetSize() == new Vector2(7, 5), "Observer failure retains published pixels, mode and identity."); observed.CreateFromImage(source, 0);
            }
            Reject<ArgumentOutOfRangeException>(() => texture.CreateFromImage(source, (PortableCompressedTexture.CompressionMode)99)); Reject<ArgumentOutOfRangeException>(() => texture.CreateFromImage(source, 0, lossyQuality: float.NaN)); Reject<ArgumentOutOfRangeException>(() => texture.SizeOverride = new(-1, 1)); Reject<ArgumentOutOfRangeException>(() => texture.SizeOverride = new(float.PositiveInfinity, 1));
            texture.SetBASISUCompressorParams(-1, -1); texture.SetBASISUCompressorParams(0, 1); texture.CreateFromImage(source, PortableCompressedTexture.CompressionMode.BasisUniversal, true);
            Reject<ArgumentOutOfRangeException>(() => texture.SetBASISUCompressorParams(5, 0)); Reject<ArgumentOutOfRangeException>(() => texture.SetBASISUCompressorParams(1, float.NaN));
            texture.KeepCompressedBuffer = false; Check(DataRetained(texture) == false, "Clearing retention discards encoded bytes."); texture.KeepCompressedBuffer = true; Check(!DataRetained(texture), "Retention cannot reconstruct discarded bytes.");
            using (var copy = (PortableCompressedTexture)texture.Duplicate()) Check(copy.GetSize() == texture.GetSize(), "Duplication does not require encoded payload.");
            var preserve = Path.Combine(directory, "preserve.e2dres"); File.WriteAllText(preserve, "original"); Reject<AggregateException>(() => ResourceSaver.Save(texture, preserve)); Check(File.ReadAllText(preserve) == "original", "Missing payload cannot replace existing file.");
            PortableCompressedTexture.SetKeepAllCompressedBuffers(true); texture.KeepCompressedBuffer = false; texture.CreateFromImage(source, 0); Check(DataRetained(texture) && !texture.KeepCompressedBuffer, "Global policy retains new payloads.");
            PortableCompressedTexture.SetKeepAllCompressedBuffers(false); Check(DataRetained(texture), "Global policy does not visit existing buffers."); texture.KeepCompressedBuffer = false; Check(!DataRetained(texture), "Explicit release also clears globally retained buffers.");
            using (var etc = (Image)source.Duplicate())
            { etc.Compress(Image.CompressMode.Etc); using var portable = new PortableCompressedTexture(); portable.CreateFromImage(etc, PortableCompressedTexture.CompressionMode.ETC2); using var read = portable.GetImage()!; Check(read.PixelFormat == Image.Format.Etc && read.GetData().AsSpan().SequenceEqual(etc.GetData()), "ETC1 is valid precompressed ETC-family input."); }
            using (var rgba = (Image)source.Duplicate())
            { rgba.CompressFromChannels(Image.CompressMode.S3tc, Image.UsedChannels.Rgba); using var ra = Image.CreateFromData(7, 5, true, Image.Format.Dxt5RaAsRg, rgba.GetData()); using var portable = new PortableCompressedTexture(); portable.CreateFromImage(ra, PortableCompressedTexture.CompressionMode.S3TC); using var read = portable.GetImage()!; Check(read.PixelFormat == Image.Format.Dxt5RaAsRg && read.GetData().AsSpan().SequenceEqual(ra.GetData()), "RA-as-RG is valid precompressed S3TC-family input."); }
            using var blocks = (Image)source.Duplicate(); blocks.Compress(Image.CompressMode.S3tc); texture.CreateFromImage(blocks, PortableCompressedTexture.CompressionMode.S3TC); using var copiedBlocks = texture.GetImage()!; Check(copiedBlocks.GetData().AsSpan().SequenceEqual(blocks.GetData()), "Matching blocks pass through exactly."); Reject<ArgumentException>(() => texture.CreateFromImage(blocks, PortableCompressedTexture.CompressionMode.ETC2));
            texture.KeepCompressedBuffer = true; texture.CreateFromImage(source, 0); texture.ResourceLocalToScene = true;
            using var root = new Node(); using var scene = new PackedScene(); var sprite = new Sprite { Texture = texture }; root.AddChild(sprite); sprite.Owner = root; scene.Pack(root);
            using var a = scene.Instantiate(); using var b = scene.Instantiate(); var ta = ((Sprite)a.GetChildren()[0]).Texture!; var tb = ((Sprite)b.GetChildren()[0]).Texture!; Check(!ReferenceEquals(ta, tb) && !ReferenceEquals(ta, texture), "Local scene copies own texture state.");
            var allocation = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) { _ = texture.GetSize(); _ = texture.GetRID(); _ = texture.IsPixelOpaque(0, 0); _ = texture.CapturePixels(); }
            Check(GC.GetAllocatedBytesForCurrentThread() == allocation, "Warm ordinary texture reads allocate no managed bytes.");
            var archive = Path.Combine(directory, "fresh.e2dres"); ResourceSaver.Save(texture, archive); var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(PortableTextureTests).Assembly.Location); start.Environment["ELECTRON2D_TEST_PORTABLE"] = "restore"; start.Environment["ELECTRON2D_PORTABLE_ARCHIVE"] = archive;
            using var worker = System.Diagnostics.Process.Start(start)!; var output = worker.StandardOutput.ReadToEnd(); var error = worker.StandardError.ReadToEnd(); Check(worker.WaitForExit(30000) && worker.ExitCode == 0, "Fresh-process archive: " + output + error);
            ImageEncoders(); texture.Dispose(); Reject<ObjectDisposedException>(() => texture.GetImage()); Reject<ObjectDisposedException>(() => texture.KeepCompressedBuffer = true);
        }
        finally { PortableCompressedTexture.SetKeepAllCompressedBuffers(false); Directory.Delete(directory, true); }
    }
    private static bool DataRetained(PortableCompressedTexture texture) { try { return Data(texture).GetValue(texture).Length > 0; } catch (InvalidOperationException) { return false; } }
    private static void Restore(string path)
    { using var texture = ResourceLoader.Load<PortableCompressedTexture>(path, ResourceLoader.CacheMode.Ignore); using var image = texture.GetImage()!; Check(image.Width == 7 && image.Height == 5 && image.MIPMapCount == 2 && texture.KeepCompressedBuffer, "Fresh process uses registered portable factory and complete encoded mip chain."); }
    private static void ImageEncoders()
    {
        foreach (var mode in new[] { Image.CompressMode.Etc, Image.CompressMode.Etc2, Image.CompressMode.Astc })
            foreach (var channels in Enum.GetValues<Image.UsedChannels>())
            {
                using var image = Source(); var notifications = 0; image.Changed += _ => notifications++; image.CompressFromChannels(mode, channels, Image.ASTCFormat.Format8X8);
                Check(image.IsCompressed && image.MIPMapCount == 2 && notifications == 1, "All channel roles encode complete mip chains."); image.Decompress(); var pixel = image.GetPixel(0, 0); Check(MathF.Abs(pixel.R - .8f) < .15f, "Native block red-channel oracle.");
                if (channels == Image.UsedChannels.RedGreen) Check(MathF.Abs(pixel.G - .25f) < .15f, "EAC green channel ordering.");
            }
        foreach (var mode in new[] { PortableCompressedTexture.CompressionMode.BasisUniversal, PortableCompressedTexture.CompressionMode.ASTC, PortableCompressedTexture.CompressionMode.BPTC })
            foreach (var sourceFormat in new[] { Image.Format.Rf, Image.Format.Rgf, Image.Format.Rgbf, Image.Format.Rgbaf, Image.Format.Rh, Image.Format.Rgh, Image.Format.Rgbh, Image.Format.Rgbah, Image.Format.Rgbe9995 })
            { using var hdr = Source(format: sourceFormat); hdr.Fill(new(2, .5f, .25f, 1)); using var texture = new PortableCompressedTexture(); texture.CreateFromImage(hdr, mode); using var image = texture.GetImage()!; if (image.IsCompressed) image.Decompress(); Check(MathF.Abs(image.GetPixel(0, 0).R - 2) < .15f && image.MIPMapCount == 2, "HDR values survive portable encoding: " + mode + " " + sourceFormat); }
        using var nan = Source(false, Image.Format.Rgbaf); nan.SetPixel(0, 0, new(float.NaN, 0, 0)); var prior = nan.GetData(); Reject<InvalidDataException>(() => nan.Compress(Image.CompressMode.Astc)); Check(nan.GetData().AsSpan().SequenceEqual(prior), "Encoder rejection preserves source pixels.");
        using var single = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); single.Fill(Colors.White); foreach (var mode in new[] { Image.CompressMode.Etc, Image.CompressMode.Etc2, Image.CompressMode.Astc }) { using var copy = (Image)single.Duplicate(); copy.Compress(mode); Check(copy.DataSize > 0 && copy.MIPMapCount == 0, "One-pixel encoder bounds."); }
    }
    private sealed class Painter(Texture texture, int index) : Entity
    { protected override void OnDraw() { texture.DrawRect(this, new(index * 24 + 2, 2, 20, 20), false); texture.DrawRectRegion(this, new(index * 24 + 2, 26, 20, 20), new(0, 0, 7, 5)); } }
    private sealed class OverridePainter(Texture texture) : Entity
    {
        protected override void OnDraw()
        {
            texture.DrawRectRegion(this, new(2, 50, 20, 20), new(0, 0, 4, 1), clipUV: false);
            texture.DrawRect(this, new(28, 50, 32, 8), true);
        }
    }
    private static void Native(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Source(); source.Fill(new(.8f, .25f, .125f, 1));
        var textures = new List<PortableCompressedTexture>(); var painters = new List<Painter>();
        try
        {
            var root = new Window { Size = new(176, 100), ContentScaleSize = new(176, 100) };
            foreach (var mode in Enum.GetValues<PortableCompressedTexture.CompressionMode>()) { var texture = new PortableCompressedTexture(); textures.Add(texture); texture.CreateFromImage(source, mode); var painter = new Painter(texture, textures.Count - 1); painters.Add(painter); root.AddChild(painter); }
            using var pattern = Image.CreateFromData(4, 1, false, Image.Format.Rgba8, [255, 0, 0, 255, 255, 0, 0, 255, 0, 255, 0, 255, 0, 255, 0, 255]);
            using var overridden = new PortableCompressedTexture(); overridden.CreateFromImage(pattern, 0); overridden.SizeOverride = new(8, 1); root.AddChild(new OverridePainter(overridden));
            using var empty = new PortableCompressedTexture { SizeOverride = new(8, 1) }; root.AddChild(new OverridePainter(empty));
            using var target = root.GetTexture(); var frames = 0; long before = 0, allocated = 0;
            root.Ready += _ =>
            {
                if (backend == "gpu")
                {
                    var device = RenderingServer.GetRenderingDevice()!;
                    foreach (var texture in textures)
                    {
                        var storage = RenderingServer.TextureGetRDTexture(texture.GetRID()); using var format = device.TextureGetFormat(storage); var pixels = texture.CapturePixels()!;
                        Check(format.Width == 7 && format.Height == 5 && format.MIPMaps == 3 && (device.TextureGetData(storage, 0).AsSpan().SequenceEqual(pixels.Upload.Data) || device.TextureGetData(storage, 0).AsSpan().SequenceEqual(pixels.Source.Data)), "Actual portable upload contains all native or decoded mip bytes.");
                    }
                }
                RenderingServer.SetDefaultClearColor(Colors.Black); Check(RenderingServer.GetCurrentRenderingMethod() == backend, "Requested renderer selected.");
                RenderingServer.FramePreDraw += () => { before = GC.GetAllocatedBytesForCurrentThread(); foreach (var painter in painters) painter.QueueRedraw(); };
                RenderingServer.FramePostDraw += () =>
                {
                    var delta = GC.GetAllocatedBytesForCurrentThread() - before; frames++; if (frames > 24) allocated += delta; if (frames == 88)
                    {
                        Check(allocated == 0, "Warm portable rendering allocated " + allocated); using var image = target.GetImage()!; for (var i = 0; i < textures.Count; i++) { var pixel = image.GetPixel(i * 24 + 10, 10); Check(MathF.Abs(pixel.R - .8f) < .12f && MathF.Abs(pixel.G - .25f) < .12f && MathF.Abs(pixel.B - .125f) < .12f, "Rendered portable color: " + backend + " " + textures[i].GetCompressionMode() + " " + pixel); }
                        Check(image.GetPixel(10, 60).R > .95f && image.GetPixel(10, 60).G < .05f && image.GetPixel(30, 54).R > .95f && image.GetPixel(34, 54).G > .95f, "Rendering override affects source-region and repeat pixel dimensions.");
                        if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory) { Directory.CreateDirectory(directory); image.SavePNG(Path.Combine(directory, "portable-" + backend + ".png")); }
                        root.Tree!.Quit();
                    }
                };
            };
            Engine.Run(root); Check(frames == 88, "Portable native checks reached completion.");
        }
        finally { foreach (var texture in textures) texture.Dispose(); }
    }
}
