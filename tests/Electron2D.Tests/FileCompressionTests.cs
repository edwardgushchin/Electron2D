using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using FileAccess = Electron2D.FileAccess;
using Path = System.IO.Path;

internal static partial class FileCompressionTests
{
    internal static void Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"electron2d-compression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var previousProject = ProjectSettings.ProjectRoot; var previousUser = ProjectSettings.UserDataRoot;
            try
            {
                var user = Path.Combine(directory, "user"); Directory.CreateDirectory(user);
                ProjectSettings.ConfigurePaths(directory, user);
                using (var file = FileAccess.OpenCompressed("user://virtual.bin", FileAccessModeFlags.Write)) file.WriteBytes([17]);
                using var read = FileAccess.OpenCompressed("user://virtual.bin", FileAccessModeFlags.Read);
                Check(read.ReadByte() == 17, "Directory-backed virtual paths execute native compression.");
            }
            finally { ProjectSettings.ConfigurePaths(previousProject, previousUser); }
            var random = new Random(927);
            foreach (var mode in Enum.GetValues<FileCompressionMode>())
                foreach (var size in new[] { 0, 1, 2, 7, 15, 16, 17, 65535, 65536, 65537, 196619 })
                {
                    var data = new byte[size]; random.NextBytes(data);
                    RoundTrip(directory, mode, data);
                    Array.Fill(data, (byte)71); RoundTrip(directory, mode, data);
                }
            foreach (var mode in new[] { FileCompressionMode.FastLz, FileCompressionMode.Zstandard }) Corruption(directory, mode);
            BCLLengthMismatch(directory);
            var defaultPath = Path.Combine(directory, "default.bin");
            using (var file = FileAccess.OpenCompressed(defaultPath, FileAccessModeFlags.Write)) file.WriteBytes([3, 5, 7]);
            using (var file = FileAccess.OpenCompressed(defaultPath, FileAccessModeFlags.Read)) Check(file.ReadBytes(3).SequenceEqual(new byte[] { 3, 5, 7 }), "Default FastLZ executes.");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false };
            if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(FileCompressionTests).Assembly.Location);
            start.ArgumentList.Add("--file-compression-child"); start.ArgumentList.Add(defaultPath); start.ArgumentList.Add("FastLz");
            using var child = Process.Start(start)!;
            if (!child.WaitForExit(15000)) { child.Kill(true); throw new InvalidOperationException("Fresh file reader did not exit."); }
            Check(child.ExitCode == 0, "Independent process loads persisted compressed bytes.");
            Console.WriteLine("All five codecs: 110 empty/tiny/boundary/multi-block round trips, editing, corruption, atomic failure and fresh-process read passed.");
        }
        finally { Directory.Delete(directory, true); }
    }
    internal static void Child(string path, string mode)
    {
        using var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, Enum.Parse<FileCompressionMode>(mode));
        Check(file.ReadBytes(3).SequenceEqual(new byte[] { 3, 5, 7 }), "Fresh process payload.");
    }
    private static void RoundTrip(string directory, FileCompressionMode mode, byte[] data)
    {
        var path = Path.Combine(directory, $"{mode}.bin");
        using (var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.WriteRead, mode))
        {
            file.WriteBytes(data); file.Flush(); file.Seek(0);
            Check(file.ReadBytes(data.Length).SequenceEqual(data), "Flushed seekable view.");
        }
        using (var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, mode))
        {
            Check(file.Length == data.Length && file.ReadBytes(data.Length).SequenceEqual(data), "Reopened bytes.");
            Reject<InvalidOperationException>(() => file.WriteBytes([1]));
        }
        using (var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.ReadWrite, mode))
        {
            file.SeekEnd(); file.WriteBytes([11, 13]); file.Seek(0);
            if (data.Length != 0) { file.WriteBytes([29]); data[0] = 29; }
        }
        using (var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, mode)) Check(file.ReadBytes(data.Length + 2).SequenceEqual(data.Concat(new byte[] { 11, 13 })), "ReadWrite preserves and edits persisted payload.");
    }
    private static void BCLLengthMismatch(string directory)
    {
        foreach (var mode in new[] { FileCompressionMode.Deflate, FileCompressionMode.Gzip, FileCompressionMode.Brotli })
        {
            var path = Path.Combine(directory, "length-" + mode);
            using (var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Write, mode)) file.WriteBytes(new byte[4096]);
            var original = File.ReadAllBytes(path);
            foreach (var size in new long[] { 0, 4095, 4097, -1, (long)int.MaxValue + 1 })
            {
                var bytes = (byte[])original.Clone(); BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(8), size); File.WriteAllBytes(path, bytes);
                Reject<InvalidDataException>(() => { using var read = FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, mode); });
            }
        }
    }
    private static void Corruption(string directory, FileCompressionMode mode)
    {
        var path = Path.Combine(directory, $"corrupt-{mode}.bin");
        using (var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Write, mode)) file.WriteBytes(new byte[65537]);
        var original = File.ReadAllBytes(path);
        void Bad(byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
            Reject<InvalidDataException>(() => { using var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, mode); });
        }
        Bad(original[..15]); Bad(original[..^1]); Bad(original.Concat(new byte[] { 0 }).ToArray());
        var changed = (byte[])original.Clone(); changed[20] ^= 1; Bad(changed);
        changed = (byte[])original.Clone(); BinaryPrimitives.WriteInt32LittleEndian(changed.AsSpan(16), -1); Bad(changed);
        changed = (byte[])original.Clone(); BinaryPrimitives.WriteInt64LittleEndian(changed.AsSpan(8), int.MaxValue); Bad(changed);
        changed = (byte[])original.Clone(); changed[52] ^= 255; Bad(changed);
        File.WriteAllBytes(path, original);
        Reject<InvalidDataException>(() => { using var file = FileAccess.OpenCompressed(path, FileAccessModeFlags.Read, mode == FileCompressionMode.FastLz ? FileCompressionMode.Zstandard : FileCompressionMode.FastLz); });
        using var editing = FileAccess.OpenCompressed(path, FileAccessModeFlags.ReadWrite, mode);
        editing.WriteBytes([99]); File.Delete(path); Directory.CreateDirectory(path);
        try { Reject<IOException>(editing.Flush); Check(Directory.Exists(path), "Failed replace preserves destination."); }
        finally { Directory.Delete(path); File.WriteAllBytes(path, original); }
        editing.Close();
        Check(!Directory.EnumerateFiles(directory).Any(name => name.EndsWith(".tmp", StringComparison.Ordinal)), "Atomic commits retire temporary files.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
