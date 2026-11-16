using Electron2D;
using Path = System.IO.Path;

internal static class DirAccessTemporaryTests
{
    internal static void Run()
    {
        Reject<ArgumentNullException>(() => DirAccess.CreateTemp(null!));
        foreach (var prefix in new[] { ".", "..", "a/b", "a\\b", "a:b", "a\0b", "a\nb", "<", ">", "\"", "|", "?", "*" })
            Reject<ArgumentException>(() => DirAccess.CreateTemp(prefix));

        var owners = new DirAccess[32];
        try
        {
            Parallel.For(0, owners.Length, index => owners[index] = DirAccess.CreateTemp("parallel"));
            Check(owners.Select(owner => owner.GetCurrentDir()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == owners.Length,
                "Simultaneous owners have distinct physical roots");
        }
        finally
        {
            foreach (var owner in owners) owner?.Dispose();
        }

        foreach (var prefix in new[] { "", "plain", "with space", "кириллица", "a.b", "CON" })
        {
            var owner = prefix.Length == 0 ? DirAccess.CreateTemp() : DirAccess.CreateTemp(prefix);
            var root = owner.GetCurrentDir();
            Check(Path.IsPathFullyQualified(root) && Directory.Exists(root), "Factory creates a real directory");
            Check(Path.GetDirectoryName(root) == Path.TrimEndingDirectorySeparator(Path.GetTempPath()), "System temporary parent");
            if (prefix.Length != 0) Check(Path.GetFileName(root).StartsWith(prefix + "-", StringComparison.Ordinal), "Exact prefix spelling");
            owner.MakeDirRecursive("nested/leaf");
            var filePath = Path.Combine(root, "nested", "leaf", "данные.bin");
            using (var file = Electron2D.FileAccess.Open(filePath, FileAccessModeFlags.Write)) file.WriteBytes([0, 42, 255]);
            Check(Electron2D.FileAccess.GetFileAsBytes(filePath).SequenceEqual(new byte[] { 0, 42, 255 }), "Public file I/O");
            var snapshot = owner.GetDirectories();
            owner.ListDirBegin();
            Check(owner.GetNext() == "nested" && owner.CurrentIsDir(), "Live temporary listing");
            owner.Dispose();
            owner.Dispose();
            Check(!Directory.Exists(root) && snapshot.SequenceEqual(new[] { "nested" }), "Recursive cleanup and detached listing");
            Reject<ObjectDisposedException>(() => owner.GetNext());
            Reject<ObjectDisposedException>(() => owner.ListDirBegin());
        }

        var missing = DirAccess.CreateTemp("missing");
        Directory.Delete(missing.GetCurrentDir());
        missing.Dispose();
        missing.Dispose();
        Check(missing.IsDisposed, "Already removed root is harmless");

        if (OperatingSystem.IsWindows())
        {
            using var target = DirAccess.CreateTemp("cleanup-target");
            var targetPath = target.GetCurrentDir();
            var targetFile = Path.Combine(targetPath, "preserve");
            File.WriteAllText(targetFile, "external");
            foreach (var linked in new[] { false, true })
                foreach (var keep in new[] { false, true })
                {
                    var owner = DirAccess.CreateTemp("cleanup-failure", keep);
                    var root = owner.GetCurrentDir();
                    var file = Path.Combine(root, "readonly");
                    File.WriteAllText(file, "keep");
                    File.SetAttributes(file, FileAttributes.ReadOnly);
                    if (linked) DirAccessPackMetadataTests.CreateDirectoryLink(root, "external", targetPath);
                    try
                    {
                        if (keep) owner.Dispose();
                        else Reject<UnauthorizedAccessException>(() => owner.Dispose());
                        Check(owner.IsDisposed && File.Exists(file), "Cleanup failure leaves a terminal owner and recoverable content");
                        Check(File.ReadAllText(targetFile) == "external", "Cleanup failure preserves external content");
                        Reject<ObjectDisposedException>(() => owner.GetFiles());
                        owner.Dispose();
                    }
                    finally
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        if (linked && keep) Directory.Delete(Path.Combine(root, "external"), recursive: false);
                        Directory.Delete(root, recursive: true);
                    }
                }
        }
        using var outside = DirAccess.CreateTemp("outside");
        var outsideRoot = outside.GetCurrentDir();
        var marker = Path.Combine(outsideRoot, "preserve");
        File.WriteAllText(marker, "external");
        foreach (var linked in new[] { false, true })
            foreach (var keep in new[] { false, true })
                foreach (var location in new[] { ".", "nested", outsideRoot })
                {
                    var owner = DirAccess.CreateTemp("ownership", keep);
                    var root = owner.GetCurrentDir();
                    owner.MakeDirRecursive("nested/leaf");
                    File.WriteAllText(Path.Combine(root, "nested", "leaf", "payload"), "owned");
                    if (linked)
                    {
                        DirAccessPackMetadataTests.CreateDirectoryLink(root, "external", outsideRoot);
                        DirAccessPackMetadataTests.CreateDirectoryLink(root, "nested/external", outsideRoot);
                    }
                    owner.ChangeDir(location);
                    owner.ListDirBegin();
                    var observed = false;
                    owner.Disposed += _ =>
                    {
                        observed = true;
                        Check(owner.IsDisposed && Directory.Exists(root) == keep, "Observer sees completed cleanup");
                        owner.Dispose();
                        Reject<ObjectDisposedException>(() => owner.ChangeDir(root));
                        throw new InvalidOperationException("observer failure");
                    };
                    Reject<InvalidOperationException>(() => owner.Dispose());
                    owner.Dispose();
                    Check(observed && File.ReadAllText(marker) == "external", "Reentry/failure preserves external target");
                    if (keep)
                    {
                        if (linked)
                        {
                            Directory.Delete(Path.Combine(root, "external"), recursive: false);
                            Directory.Delete(Path.Combine(root, "nested", "external"), recursive: false);
                        }
                        Directory.Delete(root, recursive: true);
                    }
                }

        Console.WriteLine("DirAccess temporary ownership regression passed (32 concurrent owners, prefix, keep, cursor, reentry, cleanup and links).");
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
