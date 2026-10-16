using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using Electron2D;
using Path = System.IO.Path;

internal static partial class CompressedTextureTests
{
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }

    internal static byte[] Container(Image image, uint mode = 0, byte[]? payload = null, int width = 14, int height = 10)
    {
        using var output = new MemoryStream(); using var writer = new BinaryWriter(output);
        writer.Write("GST2"u8); writer.Write(1u); writer.Write(width); writer.Write(height);
        writer.Write((1u << 22) | (1u << 23)); writer.Write(uint.MaxValue);
        for (var i = 0; i < 3; ++i) writer.Write(0u);
        writer.Write(CompressedArrayTests.Container([(image, mode, payload)]).AsSpan(36));
        return output.ToArray();
    }

    internal static void RunManaged()
    {
        Check(typeof(CompressedTexture).BaseType == typeof(Texture), "Imported textures preserve the ordinary texture role.");
        using var empty = new CompressedTexture(); var emptyRID = empty.GetRID();
        Check(empty.GetSize() == Vector2.Zero && empty.GetImage() is null && empty.LoadPath == "" && !empty.HasAlpha && !empty.HasMIPMaps && empty.MIPMapCount == 0 && empty.PixelFormat == Image.Format.L8 && empty.IsPixelOpaque(int.MinValue, int.MaxValue), "Empty texture state.");
        Reject<ArgumentException>(() => empty.Load("")); Reject<ArgumentNullException>(() => empty.Load(null!));
        var directory = Path.Combine(Path.GetTempPath(), "electron2d-compressed-texture-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "image.CTEX"); using var source = CompressedArrayTests.Source(0);
            var data = source.GetData(); data[3] = 25; data[7] = 26;
            using var alpha = Image.CreateFromData(7, 5, true, Image.Format.Rgba8, data);
            foreach (var mode in new uint[] { 0, 1, 2 })
            {
                var valid = Container(alpha, mode); File.WriteAllBytes(path, valid);
                var order = new List<string>();
                void Properties(ElectronObject _) { Check(empty.GetWidth() == 14, "Metadata published before observers."); order.Add("properties"); }
                void Changed(Resource _) => order.Add("changed");
                empty.PropertyListChanged += Properties; empty.Changed += Changed; empty.LoadPath = path;
                empty.PropertyListChanged -= Properties; empty.Changed -= Changed;
                Check(empty.GetRID() == emptyRID && empty.LoadPath == path && empty.GetSize() == new Vector2(14, 10) && empty.PixelFormat == Image.Format.Rgba8 && empty.HasMIPMaps && empty.MIPMapCount == 2 && !empty.HasAlpha, "Logical size, format, mips, alpha flag and stable identity.");
                Check(order.SequenceEqual(new[] { "properties", "changed" }), "Post-publication notification order.");
                using var pixels = empty.GetImage()!;
                Check(pixels.Width == 7 && pixels.Height == 5 && pixels.GetData().AsSpan().SequenceEqual(data), "Raw/PNG/WebP retains physical pixels and every mip.");
                pixels.Fill(Colors.White); using var retained = empty.GetImage()!;
                Check(retained.GetData().AsSpan().SequenceEqual(data) && !empty.IsPixelOpaque(int.MinValue, int.MinValue) && !empty.IsPixelOpaque(1, 0) && empty.IsPixelOpaque(2, 0) && empty.IsPixelOpaque(int.MaxValue, int.MaxValue), "Copied outputs and clamped logical alpha threshold.");
                InvalidInputs(empty, path, valid, data);
            }
            Basis(directory); Blocks(directory);
            File.WriteAllBytes(path, Container(alpha));
            Check(ResourceLoader.Exists<CompressedTexture>(path) && ResourceLoader.GetRecognizedExtensionsForType<Texture>().Contains("ctex") && !ResourceLoader.GetRecognizedExtensionsForType<CompressedTextureArray>().Contains("ctex"), "Typed loader discovery preserves texture roles.");
            using var cached = ResourceLoader.Load<CompressedTexture>(path); var identity = cached.GetRID();
            Check(ReferenceEquals(cached, ResourceLoader.Load<Texture>(path)), "Base-type reuse returns the cached resource.");
            File.WriteAllBytes(path, Container(source, 2, width: 21, height: 15));
            Check(ReferenceEquals(cached, ResourceLoader.Load<CompressedTexture>(path, ResourceLoader.CacheMode.Replace)) && cached.GetRID() == identity && cached.GetSize() == new Vector2(21, 15), "Replace keeps identity and publishes pixels and logical size.");
            File.WriteAllBytes(path, [1, 2, 3]); Reject<InvalidDataException>(() => ResourceLoader.Load<CompressedTexture>(path, ResourceLoader.CacheMode.Replace));
            Check(cached.GetWidth() == 21 && cached.GetRID() == identity, "Failed cache replacement retains prior state.");
            File.WriteAllBytes(path, Container(alpha));
            ResourceLoader.LoadThreadedRequest<Texture>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            using var worker = ResourceLoader.LoadThreadedGet<Texture>(path);
            Check(worker is CompressedTexture loaded && loaded.LoadPath == path && loaded.GetWidth() == 14 && worker != cached, "Worker prepares a typed independent texture.");
            cached.ResourceLocalToScene = true;
            var archive = Path.Combine(directory, "texture.e2dres"); ResourceSaver.Save(cached, archive);
            using var root = new Node { Name = "Imported" };
            var first = new Sprite { Name = "First", Texture = cached }; var second = new TextureRect { Name = "Second", Texture = cached };
            root.AddChild(first); root.AddChild(second); first.Owner = root; second.Owner = root;
            using var scene = new PackedScene(); scene.Pack(root); ResourceSaver.Save(scene, Path.Combine(directory, "scene.e2dscene"));
            File.Delete(path);
            foreach (var deep in new[] { false, true })
            {
                using var copy = (CompressedTexture)cached.Duplicate(deep);
                using var copiedPixels = copy.GetImage()!;
                Check(copy.LoadPath == path && copy.ResourcePath == "" && copy.GetRID() != identity && copy.GetSize() == new Vector2(21, 15) && copy.ResourceLocalToScene && copiedPixels.GetData().AsSpan().SequenceEqual(source.GetData()), "Duplication does not reopen the source and has independent identity.");
            }
            using var restored = ResourceLoader.Load<CompressedTexture>(archive, ResourceLoader.CacheMode.Ignore);
            Check(restored.GetWidth() == 21 && restored.LoadPath == path && restored.ResourceLocalToScene, "Archive embeds source and logical metadata.");
            using var savedScene = ResourceLoader.Load<PackedScene>(Path.Combine(directory, "scene.e2dscene"), ResourceLoader.CacheMode.Ignore);
            using var a = savedScene.Instantiate(); using var b = savedScene.Instantiate();
            var aTexture = (CompressedTexture)a.GetNode<Sprite>("First").Texture!; var bTexture = (CompressedTexture)b.GetNode<Sprite>("First").Texture!;
            Check(aTexture != bTexture && aTexture != cached && aTexture == a.GetNode<TextureRect>("Second").Texture && aTexture.GetWidth() == 21 && aTexture.GetLocalScene() == a, "Scene-local duplication preserves sharing within each independent scene.");
            FreshArchive(archive, path, Convert.ToBase64String(source.GetData()));
            aTexture.Dispose(); bTexture.Dispose();
            var before = GC.GetAllocatedBytesForCurrentThread(); var sum = 0;
            for (var i = 0; i < 1000; ++i) sum += restored.GetWidth() + restored.GetHeight() + restored.MIPMapCount + (restored.IsPixelOpaque(i, i) ? 1 : 0);
            Check(GC.GetAllocatedBytesForCurrentThread() == before && sum == 39000, "Prepared metadata and opacity reads allocate zero managed bytes.");
            using var emptyArchive = new CompressedTexture(); ResourceSaver.Save(emptyArchive, Path.Combine(directory, "empty.e2dres"));
            using var restoredEmpty = ResourceLoader.Load<CompressedTexture>(Path.Combine(directory, "empty.e2dres"), ResourceLoader.CacheMode.Ignore);
            Check(restoredEmpty.GetImage() is null && restoredEmpty.GetSize() == Vector2.Zero, "Empty archive round trip.");
            foreach (var flags in new[] { SaverFlags.Compress | SaverFlags.SaveBigEndian, SaverFlags.BundleResources | SaverFlags.ReplaceSubresourcePaths })
            {
                var alternate = Path.Combine(directory, "alternate.e2dres"); ResourceSaver.Save(cached, alternate, flags);
                using var roundTrip = ResourceLoader.Load<CompressedTexture>(alternate, ResourceLoader.CacheMode.Ignore); using var pixels = roundTrip.GetImage()!;
                Check(roundTrip.GetSize() == new Vector2(21, 15) && pixels.GetData().AsSpan().SequenceEqual(source.GetData()), "Compressed/big-endian/bundled archive keeps imported state without source I/O.");
            }
            CorruptArchives(directory, emptyArchive, cached);
            restored.Dispose(); Reject<ObjectDisposedException>(() => restored.Load(path)); Reject<ObjectDisposedException>(() => _ = restored.LoadPath); Reject<ObjectDisposedException>(() => restored.GetSize()); Reject<ObjectDisposedException>(() => _ = restored.HasAlpha); Reject<ObjectDisposedException>(() => restored.GetImage()); Reject<ObjectDisposedException>(() => restored.IsPixelOpaque(0, 0));
        }
        finally { Directory.Delete(directory, true); }
        Console.WriteLine("Compressed textures: logical/physical sizes, raw/PNG/WebP/Basis and block mips, alpha, atomic failures, cache/worker, copies, local scenes, fresh archives and zero-allocation reads passed.");
    }

    private static void InvalidInputs(CompressedTexture texture, string path, byte[] valid, byte[] expected)
    {
        var identity = texture.GetRID(); var events = 0; void Changed(Resource _) => events++; texture.Changed += Changed;
        for (var length = 0; length < valid.Length; length++) { File.WriteAllBytes(path, valid.AsSpan(0, length).ToArray()); Reject<InvalidDataException>(() => texture.Load(path)); }
        File.WriteAllBytes(path, [.. valid, 0]); Reject<InvalidDataException>(() => texture.Load(path));
        foreach (var offset in new[] { 0, 4, 8, 12, 36, 44, 48 })
        {
            var bytes = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), uint.MaxValue);
            File.WriteAllBytes(path, bytes); Reject<InvalidDataException>(() => texture.Load(path));
        }
        foreach (var offset in new[] { 8, 12, 40 })
        {
            var bytes = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), 0);
            File.WriteAllBytes(path, bytes); Reject<InvalidDataException>(() => texture.Load(path));
        }
        if (BinaryPrimitives.ReadUInt32LittleEndian(valid.AsSpan(36)) != 0)
        {
            var bytes = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(52), uint.MaxValue);
            File.WriteAllBytes(path, bytes); Reject<InvalidDataException>(() => texture.Load(path));
        }
        using (var file = new FileStream(path, FileMode.Create)) file.SetLength(64 * 1024 * 1024 + 1);
        Reject<InvalidDataException>(() => texture.Load(path));
        using var retained = texture.GetImage()!;
        Check(events == 0 && texture.GetRID() == identity && texture.GetWidth() == 14 && texture.LoadPath == path && retained.GetData().AsSpan().SequenceEqual(expected), "Malformed input preserves the complete state without notifications.");
        texture.Changed -= Changed;
        var versionZero = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(versionZero.AsSpan(4), 0);
        File.WriteAllBytes(path, versionZero); texture.Load(path);
        File.WriteAllBytes(path, valid); Action<Resource> failed = _ => throw new ArithmeticException("observer"); texture.Changed += failed;
        Reject<ArithmeticException>(() => texture.Load(path)); texture.Changed -= failed;
        Check(texture.LoadPath == path && texture.GetRID() == identity, "Observer failure follows publication.");
        Action<ElectronObject> failedProperties = _ => throw new ArithmeticException("property observer"); texture.PropertyListChanged += failedProperties;
        Reject<ArithmeticException>(() => texture.Load(path)); texture.PropertyListChanged -= failedProperties;
        using var afterObserver = texture.GetImage()!;
        Check(afterObserver.GetData().AsSpan().SequenceEqual(expected) && texture.GetRID() == identity, "Property observer failure retains committed source state.");
    }

    private static void Basis(string directory)
    {
        foreach (var resource in new[] { "TestCompressedArray.basis-oracle.json", "TestKTX.oracle.json" })
        {
            using var stream = typeof(CompressedTextureTests).Assembly.GetManifestResourceStream(resource)!; using var oracle = JsonDocument.Parse(stream);
            foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
            {
                if (entry.GetProperty("name").GetString()!.StartsWith("raw", StringComparison.Ordinal)) continue;
                var hdr = entry.GetProperty("format").GetString() == "Rgbh"; var expected = entry.GetProperty("pixels").GetBytesFromBase64();
                using var metadata = Image.CreateFromData(7, 5, true, hdr ? Image.Format.Rgbh : Image.Format.Rgba8, expected);
                var encoded = entry.GetProperty("encoded").GetBytesFromBase64(); var payload = new byte[encoded.Length + 4]; encoded.CopyTo(payload, 4);
                BinaryPrimitives.WriteUInt32LittleEndian(payload, (hdr ? 5u : 2u) | (resource == "TestKTX.oracle.json" ? 0x80000000u : 0));
                var path = Path.Combine(directory, "basis.ctex"); File.WriteAllBytes(path, Container(metadata, 3, payload));
                using var texture = ResourceLoader.Load<CompressedTexture>(path, ResourceLoader.CacheMode.Ignore); using var image = texture.GetImage()!;
                Check(image.PixelFormat == metadata.PixelFormat && image.GetData().AsSpan().SequenceEqual(expected) && texture.GetWidth() == 14, "Independent Basis LDR/HDR image and logical metadata.");
            }
        }
    }

    private static void Blocks(string directory)
    {
        using var stream = typeof(CompressedTextureTests).Assembly.GetManifestResourceStream("TestBlocks.oracle.json")!; using var oracle = JsonDocument.Parse(stream);
        foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            using var image = Image.CreateFromData(7, 5, true, Enum.Parse<Image.Format>(entry.GetProperty("source_format").GetString()!), entry.GetProperty("encoded").GetBytesFromBase64());
            var path = Path.Combine(directory, "blocks.ctex"); File.WriteAllBytes(path, Container(image));
            using var texture = new CompressedTexture(); texture.Load(path); using var retained = texture.GetImage()!;
            Check(retained.PixelFormat == image.PixelFormat && retained.GetData().AsSpan().SequenceEqual(image.GetData()), "Imported source blocks remain exact.");
            using var expected = Image.CreateFromData(7, 5, true, Enum.Parse<Image.Format>(entry.GetProperty("format").GetString()!), entry.GetProperty("pixels").GetBytesFromBase64());
            for (var y = 0; y < 10; y++) for (var x = 0; x < 14; x++) Check(texture.IsPixelOpaque(x, y) == (expected.GetPixel(x / 2, y / 2).A > .1f), "Decoded block opacity uses logical coordinates.");
        }
        using var integer = Image.CreateEmpty(1, 1, false, Image.Format.Rgba16I); var integerPath = Path.Combine(directory, "integer.ctex"); File.WriteAllBytes(integerPath, Container(integer));
        using var unsupported = new CompressedTexture(); Reject<NotSupportedException>(() => unsupported.Load(integerPath)); Check(unsupported.GetWidth() == 0, "Integer sampler dependency fails before publication.");
        using var prior = CompressedArrayTests.Source(0); File.WriteAllBytes(integerPath, Container(prior)); unsupported.Load(integerPath); var identity = unsupported.GetRID();
        using var blocks = Image.CreateEmpty(7, 5, true, Image.Format.Etc); var invalidData = blocks.GetData(); invalidData[blocks.GetMIPMapOffset(1)] = 4; invalidData[blocks.GetMIPMapOffset(1) + 3] = 2;
        using var invalid = Image.CreateFromData(7, 5, true, Image.Format.Etc, invalidData); File.WriteAllBytes(integerPath, Container(invalid)); Reject<InvalidDataException>(() => unsupported.Load(integerPath));
        using var overBudget = Image.CreateEmpty(2049, 2049, false, Image.Format.Etc); File.WriteAllBytes(integerPath, Container(overBudget)); Reject<InvalidOperationException>(() => unsupported.Load(integerPath));
        using var retainedPrior = unsupported.GetImage()!;
        Check(unsupported.GetRID() == identity && retainedPrior.GetData().AsSpan().SequenceEqual(prior.GetData()), "Later mip decoder failure and codec budget preserve the previous complete texture.");
        File.WriteAllBytes(integerPath, Container(prior, width: int.MaxValue, height: int.MaxValue)); unsupported.Load(integerPath);
        Check(unsupported.GetWidth() == int.MaxValue && unsupported.IsPixelOpaque(int.MaxValue, int.MaxValue), "Large logical dimensions do not multiply pixel storage or overflow opacity mapping.");
    }

    private static void CorruptArchives(string directory, CompressedTexture empty, CompressedTexture loaded)
    {
        foreach (var resource in new[] { empty, loaded })
        {
            var path = Path.Combine(directory, "corrupt.e2dres"); ResourceSaver.Save(resource, path); var valid = File.ReadAllBytes(path);
            var text = System.Text.Encoding.Unicode.GetBytes(resource.LoadPath);
            var custom = resource.LoadPath.Length == 0 ? -1 : valid.AsSpan().LastIndexOf(text) + text.Length;
            if (custom < 0) continue;
            foreach (var offset in new[] { custom, custom + 4, custom + 8 })
            {
                var bytes = (byte[])valid.Clone();
                if (offset == custom + 8) bytes[offset] = 2; else BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), -1);
                File.WriteAllBytes(path, bytes); Reject<InvalidDataException>(() => ResourceLoader.Load<CompressedTexture>(path, ResourceLoader.CacheMode.Ignore));
            }
        }
    }

    private static void FreshArchive(string archive, string source, string expected)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
        if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(CompressedTextureTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_COMPRESSED_TEXTURE"] = "restore";
        foreach (var argument in new[] { archive, source, expected }) start.ArgumentList.Add(argument);
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(60000)) { child.Kill(true); throw new TimeoutException("Fresh texture archive process did not finish."); }
        Check(child.ExitCode == 0, "Fresh texture archive: " + output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult());
    }

    internal static void Restore(string archive, string source, string expected)
    {
        Check(!File.Exists(source), "Original source is absent in the fresh process.");
        using var texture = ResourceLoader.Load<CompressedTexture>(archive); using var image = texture.GetImage()!;
        Check(texture.LoadPath == source && texture.GetSize() == new Vector2(21, 15) && image.GetData().AsSpan().SequenceEqual(Convert.FromBase64String(expected)), "Fresh process restores physical mips and logical size without source I/O.");
    }
}
