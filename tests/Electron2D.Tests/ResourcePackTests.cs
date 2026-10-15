using Electron2D;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Path = System.IO.Path;
using EFile = Electron2D.FileAccess;

internal static class ResourcePackTests
{
    private static int _checks;
    private static void Check(bool condition, string message) { _checks++; if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void Run()
    {
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-pck-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder); var oldProject = ProjectSettings.ProjectRoot; var oldUser = ProjectSettings.UserDataRoot;
        try
        {
            ProjectSettings.ConfigurePaths(folder, Path.Combine(folder, "user"));
            var path = Path.Combine(folder, "base.pck"); var bytes = Encoding.UTF8.GetBytes("Кириллица 😀\0binary\n");
            Directory.CreateDirectory(Path.Combine(folder, "assets")); File.WriteAllText(Path.Combine(folder, "assets", "physical.txt"), "physical");
            File.WriteAllText(Path.Combine(folder, "assets", "replace.txt"), "fallback");
            File.WriteAllBytes(Path.Combine(folder, "source.bin"), bytes);
            using (var p = new PCKPacker())
            {
                Reject<InvalidOperationException>(() => p.AddFileFromBuffer("a", [])); Reject<InvalidOperationException>(() => p.Flush());
                Reject<ArgumentOutOfRangeException>(() => p.PCKStart(path, 0)); Reject<ArgumentException>(() => p.PCKStart(path, key: "xyz"));
                Check(!File.Exists(path), "Invalid options have no destination side effects.");
                p.PCKStart(path, 17); p.AddFile("res://assets/текст😀.bin", Path.Combine(folder, "source.bin"));
                p.AddFileFromBuffer("assets/./sub/../replace.txt", "pack"u8); p.AddFileFromBuffer("assets/empty", []);
                p.AddFileFromBuffer("assets/.hidden", [7]); p.AddFileFromBuffer("assets/Case", [8]); p.AddFileFromBuffer("assets/case", [9]);
                Reject<UnauthorizedAccessException>(() => p.AddFileFromBuffer("../escape", [1])); Reject<ArgumentException>(() => p.AddFileRemoval("user://invalid"));
                p.AddFileFromBuffer("assets/owned", bytes); bytes[0] ^= 1;
                p.Flush(); Reject<InvalidOperationException>(() => p.AddFileRemoval("a")); Reject<InvalidOperationException>(() => p.Flush());
            }
            bytes[0] ^= 1;
            Check(ProjectSettings.LoadResourcePack(path), "Mount ordinary aligned PCK.");
            Check(EFile.GetFileAsBytes("res://assets/текст😀.bin").SequenceEqual(bytes) && EFile.GetFileAsBytes("res://assets/owned").SequenceEqual(bytes), "Immediate source and borrowed buffer capture.");
            Check(EFile.GetFileAsString("res://assets/replace.txt") == "pack" && File.ReadAllText(Path.Combine(folder, "assets", "replace.txt")) == "fallback", "Virtual overlay and physical bypass.");
            Check(EFile.GetSize("res://assets/empty") == 0 && EFile.GetFileAsBytes("res://assets/empty").Length == 0, "Empty file.");
            Check(EFile.GetModifiedTime("res://assets/owned") == 0 && EFile.GetAccessTime("res://assets/owned") == 0 && EFile.IsReadOnly("res://assets/owned"), "Packed metadata.");
            Check(EFile.GetMD5("res://assets/owned") == Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant(), "Hash packed bytes.");
            using var first = EFile.Open("res://assets/owned", FileAccessModeFlags.Read); using var second = EFile.Open("res://assets/owned", FileAccessModeFlags.Read);
            first.Seek(3); Check(first.ReadByte() == bytes[3] && second.ReadByte() == bytes[0], "Independent bounded cursors.");
            first.SeekEnd(); Check(first.ReadBytes(4).Length == 0 && first.EOFReached, "Bounded EOF does not read next entry.");
            Reject<UnauthorizedAccessException>(() => EFile.Open("res://assets/owned", FileAccessModeFlags.Write));
            Reject<NotSupportedException>(() => EFile.GetExtendedAttribute("res://assets/owned", "test"));
            using (var directory = DirAccess.Open("res://assets"))
            {
                var names = directory.GetFiles(); Check(names.Contains("physical.txt") && names.Contains("текст😀.bin") && !names.Contains(".hidden") && names.Contains("Case") && names.Contains("case"), "Merged ordinal directory listing.");
                directory.IncludeHidden = true; Check(directory.GetFiles().Contains(".hidden"), "Packed hidden listing.");
                Check(directory.FileExists("owned") && directory.DirExists(".") && directory.GetCurrentDir() == "res://assets", "Relative access remains resource scoped.");
                directory.Copy("owned", "copied"); Check(File.ReadAllBytes(Path.Combine(folder, "assets", "copied")).SequenceEqual(bytes), "Copy packed source out.");
                Reject<UnauthorizedAccessException>(() => directory.Remove("owned")); Reject<UnauthorizedAccessException>(() => directory.Rename("owned", "renamed"));
                Reject<UnauthorizedAccessException>(() => directory.Remove("res://assets"));
            }
            var patch = Path.Combine(folder, "patch.pck");
            using (var p = new PCKPacker()) { p.PCKStart(patch); p.AddFileFromBuffer("assets/owned", [44]); p.AddFileRemoval("assets/replace.txt"); p.AddFileFromBuffer("only/in/pack/deep.bin", [10]); }
            Check(ProjectSettings.LoadResourcePack(patch, replaceFiles: false) && EFile.GetSize("res://assets/owned") == bytes.Length, "Nonreplacement mount preserves the first overlay.");
            Check(EFile.GetFileAsString("res://assets/replace.txt") == "fallback", "Removal reveals physical fallback without deleting it.");
            Check(ProjectSettings.LoadResourcePack(patch) && EFile.GetFileAsBytes("res://assets/owned").SequenceEqual(new byte[] { 44 }), "Replacement overlay.");
            Check(first.ReadOnly, "Opened packed metadata remains read-only after later mounts.");
            Reject<UnauthorizedAccessException>(() => first.ReadOnly = false);
            second.Seek(0); Check(second.ReadBytes(bytes.Length).SequenceEqual(bytes), "Already open stream retains old entry.");
            using (var only = DirAccess.Open("res://only")) { Check(only.GetDirectories().SequenceEqual(new[] { "in" }), "Synthetic directory."); only.ChangeDir("in/pack"); Check(only.FileExists("deep.bin"), "Synthetic navigation."); }
            var collisionPack = Path.Combine(folder, "shadow.pck");
            Directory.CreateDirectory(Path.Combine(folder, "shadow", "physical-dir")); File.WriteAllBytes(Path.Combine(folder, "shadow", "physical-dir", "child"), [3]);
            using (var p = new PCKPacker()) { p.PCKStart(collisionPack); p.AddFileFromBuffer("shadow/physical-dir", [2]); }
            Check(ProjectSettings.LoadResourcePack(collisionPack) && !EFile.FileExists("res://shadow/physical-dir/child"), "Mounted file hides a physical directory subtree.");
            Reject<FileNotFoundException>(() => EFile.GetFileAsBytes("res://shadow/physical-dir/child"));
            Reject<FileNotFoundException>(() => EFile.GetSize("res://shadow/physical-dir/child"));
            Reject<FileNotFoundException>(() => EFile.GetModifiedTime("res://shadow/physical-dir/child"));
            Check(!DirAccess.DirExistsAbsolute("res://shadow/physical-dir") && EFile.FileExists("res://shadow/physical-dir"), "File/directory collision is consistent.");
            using (var directory = DirAccess.Open("res://assets"))
            {
                Directory.CreateDirectory(Path.Combine(folder, "other-project"));
                ProjectSettings.ConfigurePaths(Path.Combine(folder, "other-project"), Path.Combine(folder, "user"));
                Check(directory.FileExists("physical.txt") && directory.GetFiles().Contains("physical.txt"), "An open accessor retains its physical root snapshot.");
                ProjectSettings.ConfigurePaths(folder, Path.Combine(folder, "user"));
            }
            var compressed = Path.Combine(folder, "compressed.bin");
            using (var file = EFile.OpenCompressed(compressed, FileAccessModeFlags.Write)) file.WriteBytes(bytes);
            var transformed = Path.Combine(folder, "transformed.pck"); using (var p = new PCKPacker()) { p.PCKStart(transformed); p.AddFile("transformed/compressed", compressed, true); }
            Check(ProjectSettings.LoadResourcePack(transformed), "Mount an encrypted transformed file.");
            using (var file = EFile.OpenCompressed("res://transformed/compressed", FileAccessModeFlags.Read)) Check(file.ReadOnly && file.AbsolutePath == "res://transformed/compressed" && file.ReadBytes(bytes.Length).SequenceEqual(bytes), "Compressed FileAccess reads and metadata use the selected packed identity.");
            var authenticated = Path.Combine(folder, "authenticated.bin"); var passwordFile = Path.Combine(folder, "password.bin"); var fileKey = new byte[32];
            using (var file = EFile.OpenEncrypted(authenticated, FileAccessModeFlags.Write, fileKey)) file.WriteBytes(bytes);
            using (var file = EFile.OpenEncryptedWithPassword(passwordFile, FileAccessModeFlags.Write, "test password")) file.WriteBytes(bytes);
            var layered = Path.Combine(folder, "layered.pck"); using (var p = new PCKPacker()) { p.PCKStart(layered); p.AddFile("transformed/authenticated", authenticated); p.AddFile("transformed/password", passwordFile); }
            Check(ProjectSettings.LoadResourcePack(layered), "Mount authenticated raw/password containers.");
            using (var file = EFile.OpenEncrypted("res://transformed/authenticated", FileAccessModeFlags.Read, fileKey)) Check(file.ReadOnly && file.AbsolutePath == "res://transformed/authenticated" && file.ReadBytes(bytes.Length).SequenceEqual(bytes), "Raw-key transformed stream retains packed identity.");
            using (var file = EFile.OpenEncryptedWithPassword("res://transformed/password", FileAccessModeFlags.Read, "test password")) Check(file.ReadOnly && file.AbsolutePath == "res://transformed/password" && file.ReadBytes(bytes.Length).SequenceEqual(bytes), "Password transformed stream retains packed identity.");
            Crypto(folder); Invalid(folder); ZIP(folder); VersionsAndOffsets(folder); Resources(folder);
            var lifecycle = new PCKPacker(); var lifetimePath = Path.Combine(folder, "lifetime.pck"); lifecycle.PCKStart(lifetimePath);
            lifecycle.AddFileFromBuffer("lifetime/entry", [1]); Reject<ArgumentException>(() => lifecycle.PCKStart(lifetimePath, key: "wrong"));
            var previousOutput = Console.Out; using var report = new StringWriter();
            try { Console.SetOut(report); lifecycle.Flush(true); } finally { Console.SetOut(previousOutput); }
            Check(report.ToString().Contains("res://lifetime/entry"), "Verbose flush reports the recorded entry."); lifecycle.Dispose(); lifecycle.Dispose();
            Reject<ObjectDisposedException>(() => lifecycle.Flush());
            Check(ProjectSettings.LoadResourcePack(lifetimePath), "Invalid replacement options preserve a live session and deterministic flush.");
            Check(!ProjectSettings.LoadResourcePack(Path.Combine(folder, "missing")), "Missing pack returns false.");
            Reject<ArgumentOutOfRangeException>(() => ProjectSettings.LoadResourcePack(path, offset: -1));
            Console.WriteLine($"Resource packs: {_checks} assertions passed.");
        }
        finally { ProjectSettings.ConfigurePaths(oldProject, oldUser); Directory.Delete(folder, recursive: true); }
    }

    private static void Crypto(string folder)
    {
        var key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(); var original = key.ToArray(); var pack = Path.Combine(folder, "encrypted.pck");
        using (var p = new PCKPacker())
        {
            p.PCKStart(pack, key: Convert.ToHexString(key), encryptDirectory: true);
            for (var i = 0; i < 34; i++) p.AddFileFromBuffer("encrypted/" + i, Enumerable.Range(0, i).Select(x => (byte)x).ToArray(), true);
            p.AddFileFromBuffer("encrypted/plain", [66]);
        }
        Check(!ProjectSettings.LoadResourcePack(pack), "Wrong directory key fails transactionally.");
        Check(!EFile.FileExists("res://encrypted/plain"), "Rejected directory publishes no names.");
        Check(ProjectSettings.LoadResourcePack(pack, key), "Explicit directory key."); Array.Fill(key, (byte)0);
        for (var i = 0; i < 34; i++) Check(EFile.GetFileAsBytes("res://encrypted/" + i).SequenceEqual(Enumerable.Range(0, i).Select(x => (byte)x)), "Encrypted length " + i);
        Check(EFile.GetFileAsBytes("res://encrypted/plain").SequenceEqual(new byte[] { 66 }), "Mixed plain and encrypted entries, copied key.");
        var raw = File.ReadAllBytes(pack); raw[checked((int)BinaryPrimitives.ReadInt64LittleEndian(raw.AsSpan(32))) + 44] ^= 1; File.WriteAllBytes(Path.Combine(folder, "corrupt.pck"), raw);
        Check(!ProjectSettings.LoadResourcePack(Path.Combine(folder, "corrupt.pck"), original), "Encrypted directory corruption rejects.");
        using (var p = new PCKPacker()) { p.PCKStart(pack); p.AddFileFromBuffer("zero-key", [1, 2, 3], true); }
        Check(ProjectSettings.LoadResourcePack(pack) && EFile.GetFileAsBytes("res://zero-key").SequenceEqual(new byte[] { 1, 2, 3 }), "Default key encrypted entry.");
        raw = File.ReadAllBytes(pack); raw[168] ^= 1; File.WriteAllBytes(pack, raw);
        Reject<InvalidDataException>(() => EFile.GetFileAsBytes("res://zero-key"));
    }

    private static void Invalid(string folder)
    {
        var pack = Path.Combine(folder, "invalid.pck");
        using (var p = new PCKPacker()) { p.PCKStart(pack); p.AddFileFromBuffer("transaction/first", [1]); p.AddFileFromBuffer("transaction/last", [2]); }
        var valid = File.ReadAllBytes(pack); var dir = (int)BinaryPrimitives.ReadInt64LittleEndian(valid.AsSpan(32));
        foreach (var cut in new[] { 0, 4, 20, 103, dir, valid.Length - 1 })
        { File.WriteAllBytes(pack, valid[..cut]); Check(!ProjectSettings.LoadResourcePack(pack) && !EFile.FileExists("res://transaction/first"), "Truncation is atomic at " + cut); }
        var flags = valid.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(flags.AsSpan(20), 4); File.WriteAllBytes(pack, flags); Check(!ProjectSettings.LoadResourcePack(pack), "Sparse bundle flags reject.");
        flags = valid.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(flags.AsSpan(valid.Length - 4), 4); File.WriteAllBytes(pack, flags); Check(!ProjectSettings.LoadResourcePack(pack) && !EFile.FileExists("res://transaction/first"), "Late unsupported delta entry rejects whole pack.");
        flags = valid.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(flags.AsSpan(dir), uint.MaxValue); File.WriteAllBytes(pack, flags); Check(!ProjectSettings.LoadResourcePack(pack), "Unbounded count rejects.");
        using (var p = new PCKPacker()) { p.PCKStart(pack); p.AddFileFromBuffer("collision", [1]); p.AddFileFromBuffer("collision/child", [2]); }
        Check(!ProjectSettings.LoadResourcePack(pack) && !EFile.FileExists("res://collision"), "Namespace collision rejects atomically.");
    }

    private static void ZIP(string folder)
    {
        var pack = Path.Combine(folder, "assets.zip");
        using (var p = new ZIPPacker()) { p.Open(pack); p.StartFile("zip/hello.txt"); p.WriteFile("hello"u8); p.Close(); }
        Check(ProjectSettings.LoadResourcePack(pack) && EFile.GetFileAsString("res://zip/hello.txt") == "hello", "ZIP resource mount.");
        Check(!ProjectSettings.LoadResourcePack(pack, offset: 1), "ZIP cannot mount at a PCK offset.");
    }

    private static void VersionsAndOffsets(string folder)
    {
        foreach (var version in new[] { 2, 3, 4 })
        {
            var pack = Path.Combine(folder, "v" + version); var name = "version/" + version; var nameBytes = Encoding.UTF8.GetBytes(name); var header = version == 2 ? 96 : 104;
            var start = version == 2 ? header + 4 + 4 + nameBytes.Length + 8 + 8 + 16 + 4 : header;
            using (var stream = File.Create(pack))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(0x43504447u); w.Write(version); w.Write(4); w.Write(0); w.Write(0); w.Write(2u); w.Write((long)start);
                if (version > 2) w.Write((long)start + 1);
                for (var i = 0; i < 16; i++) w.Write(0);
                if (version > 2) w.Write((byte)version);
                w.Write(1); w.Write(nameBytes.Length); w.Write(nameBytes); w.Write(0L); w.Write(1L); w.Write(MD5.HashData(new[] { (byte)version })); w.Write(0u);
                if (version == 2) w.Write((byte)version);
            }
            Check(ProjectSettings.LoadResourcePack(pack) && EFile.GetFileAsBytes("res://" + name).SequenceEqual(new[] { (byte)version }), "Read independent version " + version);
            var contents = File.ReadAllBytes(pack); using (var stream = File.Create(pack)) { stream.Write(new byte[11]); stream.Write(contents); using var w = new BinaryWriter(stream, Encoding.UTF8, true); w.Write((long)contents.Length); w.Write(0x43504447u); }
            Check(ProjectSettings.LoadResourcePack(pack) && ProjectSettings.LoadResourcePack(pack, offset: 11), "Appended trailer and explicit offset v" + version);
        }
        var sparse = Path.Combine(folder, "large-offset.pck");
        using (var stream = File.Create(sparse))
        using (var w = new BinaryWriter(stream))
        {
            var start = (1L << 32) + 32; w.Write(0x43504447u); w.Write(4); w.Write(4); w.Write(6); w.Write(0); w.Write(2u); w.Write(start); w.Write(104L); for (var i = 0; i < 16; i++) w.Write(0);
            w.Write(1); var name = Encoding.UTF8.GetBytes("large/entry"); w.Write(name.Length); w.Write(name); w.Write(0L); w.Write(1L); w.Write(MD5.HashData(new byte[] { 99 })); w.Write(0);
            stream.Position = start; w.Write((byte)99);
        }
        Check(ProjectSettings.LoadResourcePack(sparse) && EFile.GetFileAsBytes("res://large/entry").SequenceEqual(new byte[] { 99 }), "64-bit sparse physical offset without whole pack allocation.");
    }

    private static void Resources(string folder)
    {
        using var root = new Node { Name = "PackedRoot" }; using var scene = new PackedScene(); scene.Pack(root);
        var resource = Path.Combine(folder, "scene.e2dscene"); ResourceSaver.Save(scene, resource);
        var pack = Path.Combine(folder, "scene.pck"); using (var p = new PCKPacker()) { p.PCKStart(pack); p.AddFile("packed/scene.e2dscene", resource); }
        File.Delete(resource); Check(ProjectSettings.LoadResourcePack(pack), "Mount scene archive after source removal.");
        using var loaded = ResourceLoader.Load<PackedScene>("res://packed/scene.e2dscene", ResourceLoader.CacheMode.Ignore);
        using var instance = loaded.Instantiate(); Check(instance.Name == "PackedRoot", "Executable scene loading from PCK bytes.");
        Check(ResourceLoader.ListDirectory("res://packed").SequenceEqual(new[] { "scene.e2dscene" }), "Resource directory recognition.");
    }
}
