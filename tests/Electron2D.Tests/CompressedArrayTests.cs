using System.Buffers.Binary;
using System.Text.Json;
using Electron2D;
using Path = System.IO.Path;

internal static partial class CompressedArrayTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }

    internal static void RunManaged()
    {
        Check(typeof(CompressedTextureLayered).BaseType == typeof(TextureLayered) && typeof(CompressedTextureArray).BaseType == typeof(CompressedTextureLayered), "Compressed array inheritance.");
        using var empty = new CompressedTextureArray();
        Check(empty.LoadPath == "" && empty.GetWidth() == 0 && empty.GetHeight() == 0 && empty.GetLayers() == 0 && empty.GetFormat() == Image.Format.L8 && !empty.HasMIPMaps(), "Empty metadata.");
        Reject<ArgumentException>(() => empty.Load("")); Reject<ArgumentOutOfRangeException>(() => empty.GetLayerData(0));
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-compressed-array-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "layers.CTEXARRAY");
            using var first = Source(0); using var second = Source(1);
            foreach (var mode in new uint[] { 0, 1, 2 })
            {
                var bytes = Container([(first, mode, null), (second, mode, null)]); File.WriteAllBytes(path, bytes);
                using var array = new CompressedTextureArray(); var identity = array.GetRID(); var notifications = new List<string>();
                array.PropertyListChanged += _ => notifications.Add("properties"); array.Changed += _ => notifications.Add("changed");
                array.LoadPath = path;
                Check(array.GetRID() == identity && array.LoadPath == path && array.GetLayers() == 2 && array.GetWidth() == 7 && array.GetHeight() == 5 && array.HasMIPMaps(), "Loaded dimensions, complete mips, path and resource identity.");
                Check(notifications.SequenceEqual(new[] { "properties", "changed" }), "Post-publication event order.");
                using var pixels = array.GetLayerData(0)!; Check(pixels.GetData().AsSpan().SequenceEqual(first.GetData()), "Raw/PNG/WebP mip parity."); pixels.Fill(Colors.White);
                using var retained = array.GetLayerData(0)!; Check(retained.GetData().AsSpan().SequenceEqual(first.GetData()), "Layer outputs are independent.");
                Failure(array, path, bytes, first.GetData());
            }
            Basis(directory);
            InvalidLayouts(directory, first, second);
            using (var blocks = Source(0))
            {
                blocks.Compress(Image.CompressMode.S3tc); File.WriteAllBytes(path, Container([(blocks, 0, null)]));
                empty.Load(path); using var layer = empty.GetLayerData(0)!;
                Check(layer.IsCompressed && layer.GetData().AsSpan().SequenceEqual(blocks.GetData()), "BC source and every mip retained.");
            }
            File.WriteAllBytes(path, Container([(first, 0, null), (second, 1, null)]));
            using var cached = ResourceLoader.Load<CompressedTextureArray>(path); var rid = cached.GetRID();
            Check(ReferenceEquals(cached, ResourceLoader.Load<CompressedTextureLayered>(path)) && ResourceLoader.GetRecognizedExtensionsForType<CompressedTextureLayered>().Contains("ctexarray"), "Typed resource discovery and shared cache.");
            File.WriteAllBytes(path, Container([(second, 0, null)]));
            Check(ReferenceEquals(cached, ResourceLoader.Load<CompressedTextureArray>(path, ResourceLoader.CacheMode.Replace)) && cached.GetRID() == rid && cached.GetLayers() == 1, "Replacement keeps resource identity and replaces state.");
            File.WriteAllBytes(path, [1, 2, 3]);
            Reject<InvalidDataException>(() => ResourceLoader.Load<CompressedTextureArray>(path, ResourceLoader.CacheMode.Replace));
            Check(cached.GetRID() == rid && cached.GetLayers() == 1 && ReferenceEquals(cached, ResourceLoader.Load<CompressedTextureArray>(path)), "Failed cache replacement retains the previous resource.");
            File.WriteAllBytes(path, Container([(second, 0, null)]));
            ResourceLoader.LoadThreadedRequest<CompressedTextureArray>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            using var threaded = ResourceLoader.LoadThreadedGet<CompressedTextureArray>(path);
            Check(!ReferenceEquals(threaded, cached) && threaded.GetLayers() == 1, "Worker loading prepares an independent resource.");
            using var copy = (CompressedTextureArray)cached.Duplicate(true);
            Check(copy.LoadPath == path && copy.GetRID() != rid && copy.GetLayers() == 1, "Duplication retains source data with independent identity.");
            var archive = Path.Combine(directory, "array.e2dres"); ResourceSaver.Save(cached, archive); File.Delete(path);
            using var restored = ResourceLoader.Load<CompressedTextureArray>(archive, ResourceLoader.CacheMode.Ignore);
            using var restoredPixels = restored.GetLayerData(0)!;
            Check(restored.LoadPath == path && restoredPixels.GetData().AsSpan().SequenceEqual(second.GetData()), "Archive embeds pixels without reopening the original file.");
            var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(CompressedArrayTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_COMPRESSED_ARRAY"] = "restore";
            start.ArgumentList.Add(archive); start.ArgumentList.Add(path); start.ArgumentList.Add(Convert.ToBase64String(second.GetData()));
            using (var child = System.Diagnostics.Process.Start(start)!)
            {
                var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
                if (!child.WaitForExit(60000)) { child.Kill(true); throw new TimeoutException("Fresh array archive process did not finish."); }
                Check(child.ExitCode == 0, "Fresh array archive: " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
            }
            var before = GC.GetAllocatedBytesForCurrentThread(); var sum = 0;
            for (var i = 0; i < 1000; ++i) sum += restored.GetWidth() + restored.GetHeight() + restored.GetLayers() + (restored.HasMIPMaps() ? 1 : 0);
            Check(GC.GetAllocatedBytesForCurrentThread() == before && sum == 14000, "Prepared metadata reads allocate no managed bytes.");
            restored.Dispose(); Reject<ObjectDisposedException>(() => restored.Load(path)); Reject<ObjectDisposedException>(() => _ = restored.LoadPath);
        }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("Compressed arrays: raw/PNG/WebP/Basis LDR/HDR, complete mips, channel roles, atomic failures, events, cache/worker loading, duplication, archive and warmed metadata passed.");
    }

    internal static void Restore(string archive, string original, string expected)
    {
        Check(!File.Exists(original), "The original imported asset is absent in the fresh process.");
        using var restored = ResourceLoader.Load<CompressedTextureArray>(archive);
        using var pixels = restored.GetLayerData(0)!;
        Check(restored.LoadPath == original && restored.GetLayers() == 1 && restored.HasMIPMaps() && pixels.GetData().AsSpan().SequenceEqual(Convert.FromBase64String(expected)), "Fresh process restores path and embedded source mips.");
    }

    private static void InvalidLayouts(string directory, Image first, Image second)
    {
        var path = Path.Combine(directory, "invalid.ctexarray"); var valid = Container([(first, 0, null), (second, 0, null)]);
        using var array = new CompressedTextureArray(); File.WriteAllBytes(path, valid); array.Load(path); var rid = array.GetRID();
        for (var length = 0; length < valid.Length; length++)
        {
            File.WriteAllBytes(path, valid.AsSpan(0, length).ToArray()); Reject<InvalidDataException>(() => array.Load(path));
        }
        File.WriteAllBytes(path, [.. valid, 0]); Reject<InvalidDataException>(() => array.Load(path));
        using var small = Image.CreateEmpty(3, 5, true, Image.Format.Rgba8);
        using var noMips = Image.CreateEmpty(7, 5, false, Image.Format.Rgba8);
        using var otherFormat = Image.CreateEmpty(7, 5, true, Image.Format.Rgb8);
        foreach (var different in new[] { small, noMips, otherFormat })
        {
            File.WriteAllBytes(path, Container([(first, 0, null), (different, 0, null)])); Reject<InvalidDataException>(() => array.Load(path));
        }
        foreach (var offset in new[] { 40, 44, 48 })
        {
            var altered = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(altered.AsSpan(offset), offset == 40 ? 0xffff_ffff : 1u);
            File.WriteAllBytes(path, altered); Reject<InvalidDataException>(() => array.Load(path));
        }
        using var retained = array.GetLayerData(0)!;
        Check(array.GetRID() == rid && array.GetLayers() == 2 && retained.GetData().AsSpan().SequenceEqual(first.GetData()), "All truncations, trailing data and heterogeneous layers preserve the prior snapshot.");
    }

    private static void Failure(CompressedTextureArray array, string path, byte[] valid, byte[] expected)
    {
        var rid = array.GetRID(); var previousPath = array.LoadPath;
        foreach (var length in new[] { 0, 4, 35, 36, 51, valid.Length - 1 })
        {
            File.WriteAllBytes(path, valid.AsSpan(0, length).ToArray()); Reject<InvalidDataException>(() => array.Load(path));
        }
        foreach (var offset in new[] { 0, 4, 8, 36, 44 })
        {
            var bytes = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), uint.MaxValue);
            File.WriteAllBytes(path, bytes); Reject<InvalidDataException>(() => array.Load(path));
        }
        var cube = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(cube.AsSpan(12), 1); File.WriteAllBytes(path, cube);
        Reject<NotSupportedException>(() => array.Load(path));
        using var pixels = array.GetLayerData(0)!;
        Check(array.GetRID() == rid && array.LoadPath == previousPath && pixels.GetData().AsSpan().SequenceEqual(expected), "Failed load preserves complete state.");
        File.WriteAllBytes(path, valid);
        Action<Resource> failed = _ => throw new ArithmeticException("observer"); array.Changed += failed;
        Reject<ArithmeticException>(() => array.Load(path)); array.Changed -= failed;
        Check(array.LoadPath == path && array.GetRID() == rid, "Observer failure follows publication.");
    }

    private static void Basis(string directory)
    {
        foreach (var resource in new[] { "TestCompressedArray.basis-oracle.json", "TestKTX.oracle.json" })
        {
            using var stream = typeof(CompressedArrayTests).Assembly.GetManifestResourceStream(resource)!;
            using var document = JsonDocument.Parse(stream);
            foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray())
            {
                var name = fixture.GetProperty("name").GetString()!;
                if (resource == "TestKTX.oracle.json" && name.StartsWith("raw", StringComparison.Ordinal)) continue;
                var encoded = fixture.GetProperty("encoded").GetBytesFromBase64(); var original = fixture.GetProperty("pixels").GetBytesFromBase64();
                var hdr = fixture.GetProperty("format").GetString() == "Rgbh";
                foreach (var role in hdr ? new uint[] { 5 } : [0u, 1u, 2u, 3u, 4u])
                {
                    var outputFormat = role switch { 0 or 3 => Image.Format.Rg8, 1 => Image.Format.Rgb8, 2 => Image.Format.Rgba8, 4 => Image.Format.R8, _ => Image.Format.Rgbh };
                    var channels = role switch { 0 or 3 => 2, 1 => 3, 4 => 1, _ => 4 };
                    var expected = hdr ? original : Enumerable.Range(0, original.Length / 4).SelectMany(pixel => Enumerable.Range(0, channels).Select(channel => original[pixel * 4 + (role == 3 && channel == 1 ? 3 : channel)])).ToArray();
                    using var metadata = Image.CreateFromData(7, 5, true, outputFormat, expected);
                    var payload = new byte[encoded.Length + 4]; BinaryPrimitives.WriteUInt32LittleEndian(payload, role | (resource == "TestKTX.oracle.json" ? 0x80000000u : 0)); encoded.CopyTo(payload, 4);
                    var path = Path.Combine(directory, "basis.ctexarray"); File.WriteAllBytes(path, Container([(metadata, 3, payload)]));
                    using var texture = new CompressedTextureArray(); texture.Load(path); using var loaded = texture.GetLayerData(0)!;
                    Check(texture.GetFormat() == outputFormat && loaded.GetData().AsSpan().SequenceEqual(expected), "Independent Basis and channel-role parity: " + name + "/" + role);
                    if (hdr) Check(loaded.GetPixel(0, 0).B > 3, "Basis HDR source range.");
                    if (role == (hdr ? 5u : 0u))
                    {
                        foreach (var invalidRole in new[] { 6u, hdr ? 2u : 5u })
                        {
                            var invalid = Container([(metadata, 3, payload)]);
                            BinaryPrimitives.WriteUInt32LittleEndian(invalid.AsSpan(56), invalidRole | (resource == "TestKTX.oracle.json" ? 0x80000000u : 0));
                            File.WriteAllBytes(path, invalid); Reject<InvalidDataException>(() => texture.Load(path));
                            using var retained = texture.GetLayerData(0)!;
                            Check(retained.GetData().AsSpan().SequenceEqual(expected), "Invalid channel/HDR roles preserve prior Basis pixels.");
                        }
                    }
                }
            }
        }
    }

    internal static Image Source(int layer)
    {
        using var empty = Image.CreateEmpty(7, 5, true, Image.Format.Rgba8); var data = empty.GetData();
        for (var mip = 0; mip <= empty.MIPMapCount; ++mip)
        {
            using var level = Image.CreateEmpty(Math.Max(1, 7 >> mip), Math.Max(1, 5 >> mip), false, Image.Format.Rgba8);
            level.Fill(new(.15f + layer * .3f, .15f + mip * .25f, .4f, 1)); level.GetData().CopyTo(data, empty.GetMIPMapOffset(mip));
        }
        return Image.CreateFromData(7, 5, true, Image.Format.Rgba8, data);
    }

    internal static byte[] Container((Image Image, uint Mode, byte[]? Payload)[] layers)
    {
        using var output = new MemoryStream(); using var writer = new BinaryWriter(output);
        writer.Write("GSTL"u8); writer.Write(1u); writer.Write((uint)layers.Length); writer.Write(0u);
        for (var i = 0; i < 5; ++i) writer.Write(0u);
        foreach (var layer in layers)
        {
            var image = layer.Image; writer.Write(layer.Mode); writer.Write((ushort)image.Width); writer.Write((ushort)image.Height);
            writer.Write((uint)image.MIPMapCount); writer.Write((uint)image.PixelFormat);
            if (layer.Mode == 0) writer.Write(image.GetData());
            else if (layer.Mode == 3) { writer.Write(layer.Payload!.Length); writer.Write(layer.Payload); }
            else
            {
                var data = image.GetData();
                for (var mip = 0; mip <= image.MIPMapCount; ++mip)
                {
                    var offset = image.GetMIPMapOffset(mip); var end = mip == image.MIPMapCount ? data.Length : image.GetMIPMapOffset(mip + 1);
                    using var level = Image.CreateFromData(Math.Max(1, image.Width >> mip), Math.Max(1, image.Height >> mip), false, image.PixelFormat, data.AsSpan(offset, end - offset).ToArray());
                    var encoded = layer.Mode == 1 ? level.SavePNGToBuffer() : level.SaveWebPToBuffer(); writer.Write(encoded.Length); writer.Write(encoded);
                }
            }
        }
        return output.ToArray();
    }
}
