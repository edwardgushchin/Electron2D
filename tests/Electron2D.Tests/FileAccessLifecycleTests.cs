using Electron2D;
using System.Security.Cryptography;
using System.Text;
using EFile = Electron2D.FileAccess;
using IOPath = System.IO.Path;

internal static class FileAccessLifecycleTests
{
    private static readonly byte[] TextBytes = Convert.FromHexString("EFBBBF68C3A90D0A00");
    private const string Text = "\uFEFFh\u00E9\r\n\0";
    private const string MD5 = "e108c76afd55e379788e3396a193a21f";
    private const string SHA256 = "5258653d0ccfe80762da70d699dc373bfcfbe144e5583237a8fd6df0ef153d52";

    internal static void Run()
    {
        var originalProject = ProjectSettings.ProjectRoot;
        var originalUser = ProjectSettings.UserDataRoot;
        var directory = Directory.CreateTempSubdirectory("electron2d-file-lifecycle-");
        try
        {
            var project = directory.CreateSubdirectory("project").FullName;
            var user = directory.CreateSubdirectory("user").FullName;
            var other = directory.CreateSubdirectory("other").FullName;
            ProjectSettings.ConfigurePaths(project, user);
            VerifySnapshots(directory.FullName, project, user);
            if (OperatingSystem.IsLinux())
                VerifyLinkedSize(project);
            VerifyTransformed(project, user, other);
            VerifyTemporaryOwnership();
            Console.WriteLine("FileAccess lifecycle and snapshots passed (physical, res, user, PCK, ZIP).");
        }
        finally
        {
            ProjectSettings.ConfigurePaths(originalProject, originalUser);
            directory.Delete(recursive: true);
        }
    }

    private static void VerifyLinkedSize(string project)
    {
        var link = IOPath.Combine(project, "link.txt");
        File.CreateSymbolicLink(link, "text");
        Check(EFile.GetSize(link) == TextBytes.Length && EFile.GetSize("res://link.txt") == TextBytes.Length,
            "GetSize follows the same symbolic-link target as the snapshot reader.");
        File.Delete(IOPath.Combine(project, "text"));
        Reject<FileNotFoundException>(() => EFile.GetSize(link));
    }

    private static void VerifySnapshots(string directory, string project, string user)
    {
        var pck = IOPath.Combine(directory, "fixture.pck");
        var zip = IOPath.Combine(directory, "fixture.zip");
        using (var writer = new PCKPacker())
        {
            writer.PCKStart(pck);
            writer.AddFileFromBuffer("lifecycle/pck.txt", TextBytes);
            writer.AddFileFromBuffer("lifecycle/pck-bad.txt", [0xC3]);
            writer.Flush();
        }
        using (var writer = new ZIPPacker())
        {
            writer.Open(zip);
            foreach (var (name, bytes) in new[] { ("zip.txt", TextBytes), ("zip-bad.txt", new byte[] { 0xC3 }) })
            {
                writer.StartFile("lifecycle/" + name);
                writer.WriteFile(bytes);
            }
            writer.Close();
        }
        var physical = IOPath.Combine(directory, "physical");
        foreach (var file in new[] { physical, IOPath.Combine(project, "text"), IOPath.Combine(user, "text") })
        {
            File.WriteAllBytes(file, TextBytes);
            File.WriteAllBytes(file + ".bad", [0xC3]);
        }

        Check(ProjectSettings.LoadResourcePack(pck), "Mount the PCK fixture.");
        const string mountedPath = "res://lifecycle/pck.txt";
        using var old = EFile.Open(mountedPath, FileAccessModeFlags.Read);
        Check(ProjectSettings.LoadResourcePack(zip), "Mount the ZIP fixture.");
        foreach (var (path, badPath, identity) in new[]
        {
            (physical, physical + ".bad", physical),
            ("res://text", "res://text.bad", IOPath.Combine(project, "text")),
            ("user://text", "user://text.bad", IOPath.Combine(user, "text")),
            ("res://lifecycle/./pck.txt", "res://lifecycle/pck-bad.txt", mountedPath),
            ("res://lifecycle/./zip.txt", "res://lifecycle/zip-bad.txt", "res://lifecycle/zip.txt")
        })
        {
            using var file = EFile.Open(path, FileAccessModeFlags.Read);
            Check(file.IsOpen && file.Path == path && file.AbsolutePath == identity && file.Length == TextBytes.Length, path);
            file.Seek(2);
            Check(file.ReadAllText() == Text && file.Position == 2, "The snapshot preserves the cursor.");
            Reject<InvalidOperationException>(file.Flush);
            Check(file.IsOpen, "Read-only Flush leaves the stream open.");
            file.Close();
            file.Close();
            Check(!file.IsOpen && file.Path == path && file.AbsolutePath == identity, "Close preserves captured identity.");
            Reject<InvalidOperationException>(() => file.ReadByte());
            Reject<DecoderFallbackException>(() => EFile.GetFileAsString(badPath));
            Parallel.For(0, 4, _ =>
            {
                var snapshot = EFile.GetFileAsBytes(path);
                snapshot[0] = 0;
                Check(EFile.FileExists(path) && EFile.GetSize(path) == TextBytes.Length &&
                      EFile.GetFileAsString(path) == Text && EFile.GetMD5(path) == MD5 && EFile.GetSHA256(path) == SHA256, path);
            });
        }

        Check(!EFile.FileExists("res://lifecycle"), "A mounted directory is not a file.");
        Reject<FileNotFoundException>(() => EFile.GetSize("res://lifecycle"));
        Reject<FileNotFoundException>(() => EFile.GetFileAsString("user://missing"));
        Reject<FileNotFoundException>(() => EFile.GetMD5("user://missing"));
        Reject<FileNotFoundException>(() => EFile.GetSHA256("user://missing"));

        var patch = IOPath.Combine(directory, "overlay.pck");
        using (var writer = new PCKPacker())
        {
            writer.PCKStart(patch);
            writer.AddFileFromBuffer("lifecycle/pck.txt", "new"u8);
            writer.Flush();
        }
        Check(ProjectSettings.LoadResourcePack(patch), "Publish a replacement PCK entry.");
        Check(old.ReadAllText() == Text && EFile.GetFileAsString(mountedPath) == "new", "Existing reader retains its entry.");
    }

    private static void VerifyTransformed(string project, string user, string other)
    {
        var key = new byte[32];
        const string path = "user://save";
        var destination = IOPath.Combine(user, "save");
        using (var initial = EFile.OpenEncrypted(path, FileAccessModeFlags.Write, key))
        {
            initial.WriteString("original");
            initial.Flush();
            Check(initial.IsOpen, "Flush leaves the encrypted writer open.");
        }
        var previous = File.ReadAllBytes(destination);
        Check(EFile.GetSize(path) == previous.Length, "Static size reads the encoded container.");
        using var edit = EFile.OpenEncrypted(path, FileAccessModeFlags.ReadWrite, key);
        Check(edit.ReadAllText() == "original", "ReadWrite preserves the authenticated plaintext.");
        edit.WriteString("changed!");
        ProjectSettings.ConfigurePaths(project, other);
        Check(!EFile.FileExists(path) && edit.AbsolutePath == destination, "New lookups switch roots. Open owners keep their destination.");
        if (OperatingSystem.IsWindows())
        {
            using var pin = File.Open(destination, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
            RejectCommit(edit.Flush);
            Check(edit.IsOpen && edit.Position == 8 && File.ReadAllBytes(destination).SequenceEqual(previous),
                "A failed Flush retains plaintext, cursor and the previous file.");
        }
        edit.Flush();
        Check(!File.ReadAllBytes(destination).SequenceEqual(previous) && !EFile.FileExists(path), "Retry publishes under the captured root.");
        edit.Close();
        using var read = EFile.OpenEncrypted(destination, FileAccessModeFlags.Read, key);
        Check(read.ReadAllText() == "changed!", "Retry published the expected authenticated plaintext.");

        using (var alternate = EFile.OpenEncrypted(path, FileAccessModeFlags.Write, key))
            alternate.WriteString("alternate");
        using (var stop = new CancellationTokenSource())
        {
            var switchRoots = Task.Run(() =>
            {
                while (!stop.IsCancellationRequested)
                {
                    ProjectSettings.ConfigurePaths(project, user);
                    ProjectSettings.ConfigurePaths(project, other);
                }
            });
            try
            {
                for (var attempt = 0; attempt < 128; attempt++)
                {
                    using var candidate = EFile.OpenEncrypted(path, FileAccessModeFlags.ReadWrite, key);
                    var expected = candidate.AbsolutePath == destination ? "changed!" : "alternate";
                    Check(candidate.ReadAllText() == expected, "ReadWrite reads from its captured destination.");
                }
            }
            finally
            {
                stop.Cancel();
                switchRoots.GetAwaiter().GetResult();
            }
        }

        const string password = "fixture password";
        using var fresh = EFile.OpenEncryptedWithPassword(path, FileAccessModeFlags.WriteRead, password);
        Check(fresh.Length == 0, "WriteRead starts an empty transformed file.");
        fresh.WriteString("password");
        fresh.Close();
        Reject<InvalidDataException>(() => EFile.OpenEncrypted(path, FileAccessModeFlags.Read, key));
        Reject<CryptographicException>(() => EFile.OpenEncryptedWithPassword(path, FileAccessModeFlags.Read, "wrong password"));
        using var passwordRead = EFile.OpenEncryptedWithPassword(path, FileAccessModeFlags.ReadWrite, password);
        Check(passwordRead.ReadAllText() == "password", "ReadWrite authenticates an existing password container.");
        if (OperatingSystem.IsWindows())
        {
            var passwordPath = IOPath.Combine(other, "save");
            var before = File.ReadAllBytes(passwordPath);
            using var pin = File.Open(passwordPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
            passwordRead.WriteString("rejected");
            RejectCommit(passwordRead.Close);
            Check(!passwordRead.IsOpen && File.ReadAllBytes(passwordPath).SequenceEqual(before), "Failed Close releases ownership and preserves the file.");
            passwordRead.Close();
            Check(Directory.GetFiles(other).Length == 1, "Failed commits remove temporary files.");
        }
    }

    private static void VerifyTemporaryOwnership()
    {
        using var temporary = EFile.CreateTemp();
        using var kept = EFile.CreateTemp(prefix: "life", extension: ".txt", keep: true);
        var path = kept.AbsolutePath;
        try
        {
            temporary.WriteString("temp");
            Check(temporary.ReadAllText() == "temp" && temporary.Path != path, "Default ReadWrite and independent names.");
            temporary.Close();
            temporary.Close();
            Check(!EFile.FileExists(temporary.Path), "Close deletes the default temporary file.");
            kept.WriteString("kept");
            kept.Dispose();
            Check(IOPath.GetFileName(path).StartsWith("life-", StringComparison.Ordinal) &&
                  path.EndsWith(".txt", StringComparison.Ordinal) && EFile.GetFileAsString(path) == "kept",
                "Kept temporary files retain their prefix, extension and bytes.");
            Reject<ObjectDisposedException>(kept.Close);
        }
        finally
        {
            kept.Dispose();
            File.Delete(path);
        }
    }

    private static void Reject<T>(Action operation) where T : Exception
    {
        try { operation(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void RejectCommit(Action operation)
    {
        try { operation(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return; }
        throw new InvalidOperationException("Expected the filesystem to refuse replacement.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
