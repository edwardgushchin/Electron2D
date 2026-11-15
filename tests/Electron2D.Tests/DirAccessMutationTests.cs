using Electron2D;
using IOPath = System.IO.Path;

internal static class DirAccessMutationTests
{
    internal static void Run()
    {
        using var fixture = DirAccess.CreateTemp("dir-mutations");
        var root = fixture.GetCurrentDir();
        var project = IOPath.Combine(root, "project");
        var user = IOPath.Combine(root, "user");
        var nextProject = IOPath.Combine(root, "next-project");
        var nextUser = IOPath.Combine(root, "next-user");
        var oldProject = ProjectSettings.ProjectRoot;
        var oldUser = ProjectSettings.UserDataRoot;
        try
        {
            foreach (var path in new[] { project, user, nextProject, nextUser })
                DirAccess.MakeDirAbsolute(path);
            ProjectSettings.ConfigurePaths(project, user);
            using var resources = DirAccess.Open("res://");
            using var data = DirAccess.Open("user://");

            resources.MakeDir("single");
            Reject<IOException>(() => DirAccess.MakeDirAbsolute("res://single"));
            Reject<DirectoryNotFoundException>(() => resources.MakeDir("missing/only"));
            Check(!Directory.Exists(IOPath.Combine(project, "missing")), "Single mkdir leaves parents absent");
            resources.MakeDirRecursive("nested/leaf");
            DirAccess.MakeDirRecursiveAbsolute("res://nested/leaf");
            resources.ChangeDir("nested");
            using (var file = Electron2D.FileAccess.Open("res://nested/source", FileAccessModeFlags.Write))
                file.WriteBytes([1, 2, 3]);
            var source = IOPath.Combine(project, "nested", "source");
            var copy = IOPath.Combine(project, "nested", "copy");
            File.WriteAllBytes(copy, [9, 8, 7, 6]);
            resources.Copy("./source", "copy");
            Check(File.ReadAllBytes(copy).SequenceEqual(new byte[] { 1, 2, 3 }), "Copy overwrites and truncates");
            Reject<ArgumentException>(() => resources.Copy("source", "leaf/../source"));
            Reject<DirectoryNotFoundException>(() => resources.Copy("source", "missing/out"));
            Reject<FileNotFoundException>(() => resources.Copy("leaf", "out"));
            Reject<FileNotFoundException>(() => resources.Copy("source/", "out"));
            Reject<UnauthorizedAccessException>(() => resources.Copy("source", "copy/"));
            Reject<FileNotFoundException>(() => resources.Copy("missing", "out"));
            Reject<IOException>(() => resources.MakeDirRecursive("source/child"));
            Reject<ArgumentException>(() => DirAccess.CopyAbsolute("source", copy));
            Reject<InvalidOperationException>(() => resources.Rename("source", "user://out"));
            Reject<UnauthorizedAccessException>(() => resources.Copy("source", "../../out"));
            Reject<UnauthorizedAccessException>(() => data.Remove("user://../project/single"));
            Reject<NotSupportedException>(() => DirAccess.MakeDirAbsolute("other://folder"));
            Reject<ArgumentNullException>(() => DirAccess.RemoveAbsolute(null!));

            const UnixPermissionFlags mode = UnixPermissionFlags.ReadOwner | UnixPermissionFlags.WriteOwner;
            if (OperatingSystem.IsWindows())
            {
                Reject<PlatformNotSupportedException>(() => resources.Copy("source", "copy", mode));
                File.SetAttributes(copy, FileAttributes.ReadOnly);
                try { Reject<UnauthorizedAccessException>(() => DirAccess.CopyAbsolute(source, copy)); }
                finally { File.SetAttributes(copy, FileAttributes.Normal); }
                Reject<ArgumentException>(() => DirAccess.CopyAbsolute(source, source.ToUpperInvariant()));
                DirAccess.RenameAbsolute(copy, IOPath.Combine(project, "nested", "COPY"));
                Check(Directory.EnumerateFiles(IOPath.GetDirectoryName(copy)!).Any(path => IOPath.GetFileName(path) == "COPY"),
                    "Case-only rename updates the name");
                DirAccess.RenameAbsolute(IOPath.Combine(project, "nested", "COPY"), copy);
            }
            else
            {
                Reject<ArgumentOutOfRangeException>(() => resources.Copy("source", "copy", (UnixPermissionFlags)4096));
                resources.Copy("source", "copy", mode);
                Check(File.GetUnixFileMode(copy) == (UnixFileMode)(int)mode, "Explicit mode");
                resources.Copy("source", "copy");
                Check(File.GetUnixFileMode(copy) == (UnixFileMode)(int)mode, "Default keeps existing mode");
                File.CreateSymbolicLink(IOPath.Combine(project, "nested", "alias"), "source");
                Reject<ArgumentException>(() => resources.Copy("source", "alias"));
                resources.Rename("alias", "renamed-link");
                Check(resources.ReadLink("renamed-link") == "source", "Rename preserves the relative link payload");
                resources.Remove("renamed-link");
                File.CreateSymbolicLink(IOPath.Combine(project, "nested", "dangling"), "missing");
                resources.Remove("dangling");
                Check(File.ReadAllBytes(source).SequenceEqual(new byte[] { 1, 2, 3 }), "Link operations preserve the referent");
                if (OperatingSystem.IsLinux())
                {
                    var readOnlyPath = IOPath.Combine("/sys", Guid.NewGuid().ToString("N"));
                    Reject<UnauthorizedAccessException>(() => DirAccess.MakeDirAbsolute(readOnlyPath));
                    Reject<UnauthorizedAccessException>(() => DirAccess.CopyAbsolute(source, readOnlyPath));
                }
            }

            resources.Rename("copy", "source");
            Check(!File.Exists(copy) && File.ReadAllBytes(source).SequenceEqual(new byte[] { 1, 2, 3 }), "Rename replaces a file");
            resources.MakeDir("empty");
            File.WriteAllText(IOPath.Combine(project, "nested", "leaf", "keep"), "keep");
            resources.Rename("leaf", "empty");
            var nonempty = IOPath.Combine(project, "nested", "empty");
            Reject<IOException>(() => resources.Remove("empty"));
            resources.MakeDir("other");
            Reject<IOException>(() => resources.Rename("other", "empty"));
            Reject<IOException>(() => resources.Rename("other", "source"));
            Check(File.Exists(IOPath.Combine(nonempty, "keep")) && resources.DirExists("other"), "Rejected mutations preserve entries");
            DirAccessPackMetadataTests.CreateDirectoryLink(IOPath.Combine(project, "nested"), "directory-link", "empty");
            resources.Remove("directory-link/");
            Check(File.Exists(IOPath.Combine(nonempty, "keep")), "Removing a directory link preserves its target");
            DirAccess.RemoveAbsolute("res://nested/other");
            Reject<FileNotFoundException>(() => resources.Remove("other"));
            Reject<FileNotFoundException>(() => resources.Rename("other", "empty"));

            var pck = IOPath.Combine(root, "mutation.pck");
            using (var writer = new PCKPacker())
            {
                writer.PCKStart(pck);
                writer.AddFileFromBuffer("mounted.bin", [42]);
            }
            var zip = IOPath.Combine(root, "mutation.zip");
            using (var archive = System.IO.Compression.ZipFile.Open(zip, System.IO.Compression.ZipArchiveMode.Create))
            {
                archive.CreateEntry("mounted-dir/");
                using var member = archive.CreateEntry("mounted-zip.bin").Open();
                member.WriteByte(43);
            }
            File.WriteAllText(IOPath.Combine(project, "mounted.bin"), "physical shadow");
            Check(ProjectSettings.LoadResourcePack(pck) && ProjectSettings.LoadResourcePack(zip), "Mount PCK and ZIP");
            DirAccess.CopyAbsolute("res://mounted.bin", "user://export");
            DirAccess.CopyAbsolute("res://mounted-zip.bin", "user://zip-export");
            Check(File.ReadAllBytes(IOPath.Combine(user, "export")).SequenceEqual(new byte[] { 42 }) &&
                File.ReadAllBytes(IOPath.Combine(user, "zip-export")).SequenceEqual(new byte[] { 43 }),
                "PCK and ZIP copy-out read mounted bytes");
            Reject<UnauthorizedAccessException>(() => DirAccess.CopyAbsolute(source, "res://mounted.bin"));
            Reject<UnauthorizedAccessException>(() => DirAccess.CopyAbsolute(source, "res://mounted-dir/out"));
            Reject<UnauthorizedAccessException>(() => DirAccess.RenameAbsolute("res://mounted.bin", "user://moved"));
            Reject<UnauthorizedAccessException>(() => DirAccess.RenameAbsolute(source, "res://mounted-dir"));
            Reject<UnauthorizedAccessException>(() => DirAccess.MakeDirAbsolute("res://mounted-dir"));
            Reject<UnauthorizedAccessException>(() => DirAccess.MakeDirRecursiveAbsolute("res://mounted-dir"));
            Reject<UnauthorizedAccessException>(() => DirAccess.MakeDirRecursiveAbsolute("res://mounted.bin/under"));
            Reject<UnauthorizedAccessException>(() => DirAccess.RemoveAbsolute("res://mounted-dir"));
            Reject<UnauthorizedAccessException>(() => DirAccess.RemoveAbsolute("res://mounted.bin"));
            DirAccess.CopyAbsolute(IOPath.Combine(project, "mounted.bin"), "user://physical-copy");

            ProjectSettings.ConfigurePaths(nextProject, nextUser);
            resources.Copy("source", "from-old-root");
            data.Rename("export", "renamed");
            Reject<FileNotFoundException>(() => DirAccess.CopyAbsolute("user://renamed", "res://unused"));
            DirAccess.CopyAbsolute(IOPath.Combine(user, "renamed"), "res://from-absolute");
            Check(File.Exists(IOPath.Combine(project, "nested", "from-old-root")) &&
                File.ReadAllText(IOPath.Combine(user, "physical-copy")) == "physical shadow" &&
                File.ReadAllBytes(IOPath.Combine(nextProject, "from-absolute")).SequenceEqual(new byte[] { 42 }),
                "Open owners retain their roots; absolute helpers use current roots");
        }
        finally
        {
            ProjectSettings.ConfigurePaths(oldProject, oldUser);
        }

        var otherVolume = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DIR_MUTATIONS_OTHER_VOLUME");
        if (otherVolume is not null)
        {
            var source = IOPath.Combine(root, "cross-volume");
            var destination = IOPath.Combine(otherVolume, Guid.NewGuid().ToString("N"));
            File.WriteAllText(source, "keep");
            try
            {
                Reject<IOException>(() => DirAccess.RenameAbsolute(source, destination));
                Check(File.ReadAllText(source) == "keep" && !File.Exists(destination), "Cross-volume rename preserves source");
                DirAccess.CopyAbsolute(source, destination);
                Check(File.ReadAllText(destination) == "keep", "Copy works across volumes");
                DirAccess.RemoveAbsolute(destination);
                var sourceDirectory = IOPath.Combine(root, "cross-volume-dir");
                DirAccess.MakeDirAbsolute(sourceDirectory);
                DirAccess.MakeDirAbsolute(destination);
                Reject<IOException>(() => DirAccess.RenameAbsolute(sourceDirectory, destination));
                Check(Directory.Exists(sourceDirectory) && Directory.Exists(destination), "Both cross-volume directories survive");
                DirAccess.RemoveAbsolute(destination);
            }
            finally
            {
                if (Directory.Exists(destination))
                    Directory.Delete(destination);
                else
                    File.Delete(destination);
            }
        }

        fixture.Dispose();
        Reject<ObjectDisposedException>(() => fixture.MakeDir("disposed"));
        Reject<ObjectDisposedException>(() => fixture.MakeDirRecursive("disposed"));
        Reject<ObjectDisposedException>(() => fixture.Copy("disposed", "other"));
        Reject<ObjectDisposedException>(() => fixture.Rename("disposed", "other"));
        Reject<ObjectDisposedException>(() => fixture.Remove("disposed"));
        Check(!Directory.Exists(root), "Temporary ownership cleans up the fixture");
        Console.WriteLine($"DirAccess mutation regression passed ({(otherVolume is null ? "no second volume" : "second volume verified")}).");
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
