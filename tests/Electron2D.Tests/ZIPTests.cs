using Electron2D;
using System.IO.Compression;
using System.Text;
using Path = System.IO.Path;

internal static class ZIPTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunManaged()
    {
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-zip-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "assets.zip"); var payload = Encoding.UTF8.GetBytes("binary\0data\r\nкириллица 😀");
            using var writer = new ZIPPacker(); using var reader = new ZIPReader();
            Check(writer.CompressionLevel == -1 && (int)ZIPAppend.Create == 0 && (int)ZIPAppend.CreateAfter == 1 && (int)ZIPAppend.AddInZIP == 2 && (int)ZIPCompressionLevel.Best == 9, "Public defaults and enum values.");
            Reject<InvalidOperationException>(() => writer.WriteFile(payload)); Reject<InvalidOperationException>(() => reader.GetFiles());
            Reject<ArgumentOutOfRangeException>(() => writer.CompressionLevel = 10); Reject<ArgumentOutOfRangeException>(() => writer.CompressionLevel = -2);
            writer.Open(path); writer.AddDirectory("assets"); Reject<IOException>(() => writer.AddDirectory("assets/"));
            const ulong time = 1735787046;
            writer.StartFile("assets/Данные😀.bin", (UnixPermissionFlags)416, time); writer.WriteFile(payload.AsSpan(0, 4)); writer.WriteFile(payload.AsSpan(4));
            writer.StartFile("empty", UnixPermissionFlags.None, time); writer.CloseFile(); Reject<InvalidOperationException>(writer.CloseFile);
            for (var level = -1; level <= 9; level++) { writer.CompressionLevel = level; writer.StartFile("level-" + level); writer.WriteFile(payload); writer.CloseFile(); }
            writer.StartFile("duplicate"); writer.WriteFile([1]); writer.StartFile("duplicate"); writer.WriteFile([2]); writer.Close(); writer.Close();
            using (var independent = ZipFile.OpenRead(path))
            {
                Check(independent.Entries.Count == 16, "Independent ZIP reader sees directory, files, levels and duplicates.");
                var entry = independent.GetEntry("assets/Данные😀.bin")!; using var contents = entry.Open(); using var output = new MemoryStream(); contents.CopyTo(output);
                Check(output.ToArray().SequenceEqual(payload) && (entry.ExternalAttributes >> 16 & 0xffff) == (0x8000 | 416), "Independent decode and Unix permissions.");
                var expected = DateTimeOffset.FromUnixTimeSeconds((long)time).LocalDateTime; Check(Math.Abs((entry.LastWriteTime.DateTime - expected).TotalSeconds) < 2, "UTC input becomes local two-second DOS time.");
            }
            reader.Open(path); Check(reader.ReadFile("ASSETS/данные😀.BIN", false).SequenceEqual(payload) && !reader.FileExists("ASSETS/данные😀.BIN"), "Ordinal exact/Unicode insensitive lookup.");
            Check(reader.ReadFile("empty").Length == 0 && reader.ReadFile("assets/").Length == 0 && reader.ReadFile("duplicate").SequenceEqual(new byte[] { 1 }), "Empty entries and first duplicate.");
            Check(reader.GetCompressionLevel("missing") == -1 && !reader.FileExists("missing"), "Missing queries."); Reject<FileNotFoundException>(() => reader.ReadFile("missing"));
            foreach (var level in Enumerable.Range(-1, 11)) Check(reader.GetCompressionLevel("level-" + level) == (level switch { 1 => 1, 2 => 2, 8 or 9 => 9, _ => 6 }), "Compression hint " + level);
            var names = reader.GetFiles(); names[0] = "mutated"; Check(reader.GetFiles()[0] == "assets/", "Owned name snapshots.");
            var invalid = Path.Combine(folder, "invalid.zip"); File.WriteAllBytes(invalid, [1, 2, 3]); Reject<InvalidDataException>(() => reader.Open(invalid)); Check(reader.ReadFile("empty").Length == 0, "Invalid replacement preserves prior read session."); reader.Close(); reader.Close();
            writer.Open(path, ZIPAppend.AddInZIP); writer.StartFile("appended"); writer.WriteFile([4, 5]); writer.Close();
            reader.Open(path); Check(reader.GetFiles().Length == 17 && reader.ReadFile("appended").SequenceEqual(new byte[] { 4, 5 }) && reader.ReadFile("duplicate")[0] == 1, "Append preserves old payloads, order and duplicates."); reader.Close();
            PrefixAndComments(folder); Corruption(folder); SparseZIP64(folder); VirtualPaths();
            var disposed = new ZIPReader(); disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.Open(path)); var disposedWriter = new ZIPPacker(); disposedWriter.Dispose(); Reject<ObjectDisposedException>(() => disposedWriter.CompressionLevel = 0);
            var automatic = Path.Combine(folder, "automatic.zip"); using (var finalizer = new ZIPPacker()) { finalizer.Open(automatic); finalizer.StartFile("a"); finalizer.WriteFile([7]); }
            reader.Open(automatic); Check(reader.ReadFile("a")[0] == 7, "Deterministic disposal finalizes the active entry and archive.");
            Console.WriteLine("ZIP managed: all compression levels, independent decode, Unicode/duplicates/empty entries, permissions/time, append/prefix/comments/ZIP64, CRC/failure/lifetime and virtual paths passed.");
        }
        finally { Directory.Delete(folder, true); }
    }
    private static void PrefixAndComments(string folder)
    {
        var path = Path.Combine(folder, "prefix.zip"); var prefix = Encoding.UTF8.GetBytes("preserved executable prefix"); File.WriteAllBytes(path, prefix);
        using (var writer = new ZIPPacker()) { writer.Open(path, ZIPAppend.CreateAfter); writer.StartFile("first"); writer.WriteFile([3]); }
        Check(File.ReadAllBytes(path).AsSpan(0, prefix.Length).SequenceEqual(prefix), "CreateAfter preserves prefix.");
        using (var reader = new ZIPReader()) { reader.Open(path); Check(reader.ReadFile("first")[0] == 3, "Prefixed archive offsets."); }
        using (var writer = new ZIPPacker()) { writer.Open(path, ZIPAppend.AddInZIP); writer.StartFile("second"); writer.WriteFile([4]); }
        using (var reader = new ZIPReader()) { reader.Open(path); Check(reader.ReadFile("first")[0] == 3 && reader.ReadFile("second")[0] == 4, "Append a relative-offset prefixed archive."); }
        var ordinary = Path.Combine(folder, "comment.zip");
        using (var archive = ZipFile.Open(ordinary, ZipArchiveMode.Create)) { archive.Comment = "archive comment 😀"; var entry = archive.CreateEntry("external"); using var stream = entry.Open(); stream.Write([8]); }
        using (var writer = new ZIPPacker()) { writer.Open(ordinary, ZIPAppend.AddInZIP); writer.StartFile("new"); writer.WriteFile([9]); }
        using var verify = ZipFile.OpenRead(ordinary); Check(verify.Comment == "archive comment 😀" && verify.Entries.Count == 2, "Append preserves external central records and archive comment.");
    }
    private static void Corruption(string folder)
    {
        var path = Path.Combine(folder, "corrupt.zip"); var bytes = Encoding.ASCII.GetBytes("unique uncompressed payload");
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create)) { var entry = archive.CreateEntry("bad", System.IO.Compression.CompressionLevel.NoCompression); using var stream = entry.Open(); stream.Write(bytes); }
        var archiveBytes = File.ReadAllBytes(path); var offset = archiveBytes.AsSpan().IndexOf(bytes); Check(offset >= 0, "Independent stored payload found."); archiveBytes[offset] ^= 64; File.WriteAllBytes(path, archiveBytes);
        using var reader = new ZIPReader(); reader.Open(path); Reject<InvalidDataException>(() => reader.ReadFile("bad")); Check(reader.FileExists("bad"), "CRC failure cannot poison directory queries.");
        reader.Close(); archiveBytes = new byte[22]; System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(archiveBytes, 0x06054b50); System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(archiveBytes.AsSpan(8), 65534); System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(archiveBytes.AsSpan(10), 65534); File.WriteAllBytes(path, archiveBytes); Reject<InvalidDataException>(() => reader.Open(path));
    }
    private static void SparseZIP64(string folder)
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) return;
        var path = Path.Combine(folder, "sparse64.zip");
        using (var file = new FileStream(path, FileMode.Create, System.IO.FileAccess.ReadWrite))
        {
            file.Position = (long)uint.MaxValue + 4096;
            using var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: true); var entry = archive.CreateEntry("large-offset"); using var stream = entry.Open(); stream.Write([11]);
        }
        using (var reader = new ZIPReader()) { reader.Open(path); Check(reader.ReadFile("large-offset")[0] == 11, "Independent ZIP64 offsets beyond four GiB."); }
        using (var writer = new ZIPPacker()) { writer.Open(path, ZIPAppend.AddInZIP); writer.StartFile("appended64"); writer.WriteFile([12]); }
        using var independent = ZipFile.OpenRead(path); Check(independent.Entries.Count == 2, "Independent ZIP64 append directory.");
        using var result = new ZIPReader(); result.Open(path); Check(result.ReadFile("large-offset")[0] == 11 && result.ReadFile("appended64")[0] == 12, "ZIP64 append preserves old offsets and emits new offset/end records.");
    }
    private static void VirtualPaths()
    {
        var root = "res://bin/zip-tests-" + Guid.NewGuid().ToString("N"); var absolute = ProjectSettings.GlobalizePath(root); Directory.CreateDirectory(absolute);
        try { using (var writer = new ZIPPacker()) { writer.Open(root + "/a.zip"); writer.StartFile("v"); writer.WriteFile([13]); } using var reader = new ZIPReader(); reader.Open(root + "/a.zip"); Check(reader.ReadFile("v")[0] == 13, "Directory-backed res path integration."); }
        finally { Directory.Delete(absolute, true); }
    }
    internal static void RunNative(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-zip-native-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var scenePath = Path.Combine(folder, "scene.e2dscene");
            using (var authored = new Window { Size = new(40, 40) })
            using (var image = Image.CreateEmpty(12, 12, false, Image.Format.Rgba8))
            {
                image.Fill(Colors.Lime); using var texture = ImageTexture.CreateFromImage(image); var sprite = new Sprite { Name = "Sprite", Texture = texture, Position = new(20, 20) }; authored.AddChild(sprite); sprite.Owner = authored;
                using var scene = new PackedScene(); scene.Pack(authored); ResourceSaver.Save(scene, scenePath, SaverFlags.BundleResources);
            }
            var path = Path.Combine(folder, "game.zip"); using (var writer = new ZIPPacker()) { writer.Open(path); writer.StartFile("game/scene.e2dscene"); writer.WriteFile(File.ReadAllBytes(scenePath)); }
            using (var reader = new ZIPReader()) { reader.Open(path); File.WriteAllBytes(scenePath, reader.ReadFile("game/scene.e2dscene")); }
            using var loaded = ResourceLoader.Load<PackedScene>(scenePath, ResourceLoader.CacheMode.Ignore); var window = (Window)loaded.Instantiate(); using var target = window.GetTexture(); var frame = 0; var passed = false;
            window.Ready += _ => RenderingServer.FramePostDraw += Capture;
            void Capture()
            {
                if (++frame != 4) return; using var pixels = target.GetImage()!; var pixel = pixels.GetPixel(20, 20); Check(pixel.G > .98f && pixel.R < .02f && pixel.B < .02f, "Bundled ZIP scene renders its restored texture."); if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } capture) { Directory.CreateDirectory(capture); pixels.SavePNG(Path.Combine(capture, "zip-" + backend + ".png")); }
                passed = true; RenderingServer.FramePostDraw -= Capture; window.Tree!.Quit();
            }
            Check(Engine.Run(window) == 0 && passed, "ZIP native resource workflow completed."); Console.WriteLine("ZIP " + backend + ": bundled scene/resource archive, CRC-checked read, load and real texture rendering passed.");
        }
        finally { Directory.Delete(folder, true); }
    }
}
