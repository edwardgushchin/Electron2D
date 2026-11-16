using Electron2D;
using EFile = Electron2D.FileAccess;

internal static class FileAccessModeTests
{
    internal static void Run(string directory)
    {
        Check(Enum.GetValues<FileAccessModeFlags>().Select(value => (int)value).SequenceEqual(new[] { 1, 2, 3, 7 }), "Exact access identities.");
        Check(Enum.GetValues<FileCompressionMode>().Select(value => (int)value).SequenceEqual(new[] { 0, 1, 2, 3, 4 }), "Exact codec identities.");
        var key = new byte[32];
        var factories = new List<Func<string, FileAccessModeFlags, EFile>>
        {
            EFile.Open,
            (path, mode) => EFile.OpenEncrypted(path, mode, key),
            (path, mode) => EFile.OpenEncryptedWithPassword(path, mode, "mode-matrix")
        };
        foreach (var codec in Enum.GetValues<FileCompressionMode>())
            factories.Add((path, mode) => EFile.OpenCompressed(path, mode, codec));
        foreach (var open in factories)
        {
            foreach (var mode in Enum.GetValues<FileAccessModeFlags>())
            foreach (var exists in new[] { false, true })
            foreach (var dispose in new[] { false, true })
            {
                var path = System.IO.Path.Combine(directory, "mode-matrix");
                File.Delete(path);
                if (exists)
                    using (var seed = open(path, FileAccessModeFlags.Write)) seed.WriteBytes([11, 22, 33]);
                var truncates = mode is FileAccessModeFlags.Write or FileAccessModeFlags.WriteRead;
                if (!exists && !truncates)
                {
                    Reject<FileNotFoundException>(() => open(path, mode).Dispose());
                    Check(!File.Exists(path), "Failed existing-file open has no side effects.");
                    continue;
                }
                using var file = open(path, mode);
                Check(file.Position == 0 && file.Length == (truncates ? 0 : 3), "Initial cursor and truncation.");
                if (mode == FileAccessModeFlags.Read)
                {
                    Reject<InvalidOperationException>(() => file.WriteByte(99));
                    Check(file.Position == 0 && file.ReadByte() == 11, "Read permission and cursor.");
                }
                else
                {
                    if (mode == FileAccessModeFlags.Write)
                        Reject<InvalidOperationException>(() => file.ReadByte());
                    file.WriteByte(99);
                    Check(file.Position == 1, "Write cursor.");
                    if (mode != FileAccessModeFlags.Write)
                    {
                        file.Seek(0);
                        Check(file.ReadByte() == 99, "Mixed mode can read its write.");
                    }
                }
                if (dispose) file.Dispose();
                else { file.Close(); file.Close(); }
                if (!dispose)
                {
                    Check(!file.IsOpen, "Close retires owner.");
                    Reject<InvalidOperationException>(() => file.Seek(0));
                }
                file.Dispose();
                Reject<ObjectDisposedException>(() => file.Seek(0));
                using var read = open(path, FileAccessModeFlags.Read);
                var expected = mode == FileAccessModeFlags.Read ? new byte[] { 11, 22, 33 }
                    : truncates ? new byte[] { 99 } : new byte[] { 99, 22, 33 };
                Check(read.ReadBytes(9).SequenceEqual(expected), "Reopened committed content.");
            }
            foreach (var raw in new[] { -1, 0, 4, 5, 6, 8, int.MaxValue })
            {
                var path = System.IO.Path.Combine(directory, "invalid-mode");
                File.WriteAllBytes(path, [17]);
                Reject<ArgumentOutOfRangeException>(() => open(path, (FileAccessModeFlags)raw).Dispose());
                Check(File.ReadAllBytes(path).SequenceEqual(new byte[] { 17 }), "Invalid mode preserves destination.");
            }
        }
        foreach (var mode in Enum.GetValues<FileAccessModeFlags>())
        {
            using var temporary = EFile.CreateTemp(mode);
            Check(temporary.Length == 0 && temporary.Position == 0, "Temporary factory creates before applying access mode.");
        }
        Console.WriteLine("Access modes: ordinary, five compressed codecs, two encrypted factories and temporary owners passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
