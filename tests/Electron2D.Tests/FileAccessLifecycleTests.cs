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
            VerifyMountedFileProtection(directory.FullName, project);
            VerifyMountedDirectoryWriteProtection(directory.FullName, project);
            VerifyPhysicalDirectoryRejection(project, user);
            VerifyInvalidWriteParent(project, user);
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

    private static void VerifyMountedFileProtection(string directory, string project)
    {
        const string name = "lifecycle-shadowed";
        const string child = name + "/child";
        const string resource = "res://" + name;
        const string attribute = "lifecycle-sentinel";
        const string value = "metadata";
        var physical = IOPath.Combine(project, name);
        File.WriteAllBytes(physical, TextBytes);
        var operations = new List<(string Name, Action Run)>
        {
            ("Open(Write)", () => { using var file = EFile.Open(resource, FileAccessModeFlags.Write); }),
            ("OpenCompressed(Write)", () => { using var file = EFile.OpenCompressed(resource, FileAccessModeFlags.Write, FileCompressionMode.Gzip); }),
            ("OpenEncrypted(Write)", () => { using var file = EFile.OpenEncrypted(resource, FileAccessModeFlags.Write, new byte[32]); }),
            ("OpenEncryptedWithPassword(Write)", () => { using var file = EFile.OpenEncryptedWithPassword(resource, FileAccessModeFlags.Write, "password"); }),
        };
        var metadataReads = new List<Action>
        {
            () => Check(EFile.GetAccessTime(resource) == 0, "A mounted file reports the default access time."),
            () => Check(EFile.GetModifiedTime(resource) == 0, "A mounted file reports the default modification time."),
        };
        var xattrs = new List<Action>();
        if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            EFile.SetExtendedAttributeString(physical, attribute, value);
            xattrs.AddRange([
                () => EFile.GetExtendedAttribute(resource, attribute),
                () => EFile.GetExtendedAttributeString(resource, attribute),
                () => EFile.GetExtendedAttributesList(resource),
                () => EFile.SetExtendedAttribute(resource, attribute, "wrong"u8),
                () => EFile.SetExtendedAttributeString(resource, attribute, "wrong"),
                () => EFile.RemoveExtendedAttribute(resource, attribute),
            ]);
        }
        var originalAttributes = File.GetAttributes(physical);
        var originalMode = !OperatingSystem.IsWindows() ? File.GetUnixFileMode(physical) : default;
        if (OperatingSystem.IsWindows())
        {
            operations.Add(("SetHidden", () => EFile.SetHidden(resource, (originalAttributes & FileAttributes.Hidden) == 0)));
            operations.Add(("SetReadOnly", () => EFile.SetReadOnly(resource, (originalAttributes & FileAttributes.ReadOnly) == 0)));
            metadataReads.Add(() => Check(!EFile.IsHidden(resource), "A mounted file reports the default hidden flag."));
            metadataReads.Add(() => Check(EFile.IsReadOnly(resource), "A mounted file reports the read-only flag."));
            Reject<PlatformNotSupportedException>(() => EFile.SetUnixPermissions(resource, UnixPermissionFlags.None));
        }
        else
        {
            var changedMode = originalMode ^ UnixFileMode.UserExecute;
            operations.Add(("SetUnixPermissions", () => EFile.SetUnixPermissions(resource, (UnixPermissionFlags)changedMode)));
            const UnixPermissionFlags readOnly = UnixPermissionFlags.ReadOwner | UnixPermissionFlags.ReadGroup | UnixPermissionFlags.ReadOther;
            metadataReads.Add(() => Check(EFile.GetUnixPermissions(resource) == readOnly, "A mounted file reports the default Unix mode."));
        }

        var filePack = IOPath.Combine(directory, "shadow-file.pck");
        using (var writer = new PCKPacker())
        {
            writer.PCKStart(filePack);
            writer.AddFileRemoval(child);
            writer.AddFileFromBuffer(name, "mounted"u8);
        }
        var directoryPack = IOPath.Combine(directory, "shadow-directory.pck");
        using (var writer = new PCKPacker())
        {
            writer.PCKStart(directoryPack);
            writer.AddFileRemoval(name);
            writer.AddFileFromBuffer(child, "child"u8);
        }

        Check(ProjectSettings.LoadResourcePack(filePack), "Mount a file over the physical destination.");
        foreach (var operation in operations)
            Reject<UnauthorizedAccessException>(operation.Run);
        foreach (var read in metadataReads)
            read();
        foreach (var operation in xattrs)
            Reject<NotSupportedException>(operation);
        Check(ProjectSettings.LoadResourcePack(directoryPack), "Replace the mounted file with a directory.");
        foreach (var operation in operations)
            Reject<FileNotFoundException>(operation.Run);
        foreach (var read in metadataReads)
            Reject<FileNotFoundException>(read);
        foreach (var operation in xattrs)
            Reject<FileNotFoundException>(operation);

        var mounts = Task.Run(() =>
        {
            for (var attempt = 0; attempt < 512; attempt++)
            {
                Check(ProjectSettings.LoadResourcePack(filePack), "Replace the directory with a mounted file.");
                Check(ProjectSettings.LoadResourcePack(directoryPack), "Replace the file with a mounted directory.");
            }
        });
        try
        {
            do
            {
                foreach (var (operation, run) in operations)
                {
                    try
                    {
                        run();
                        throw new InvalidOperationException($"A PCK replacement exposed a masked physical file to {operation}.");
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (FileNotFoundException) { }
                }
                foreach (var read in metadataReads)
                {
                    try { read(); }
                    catch (FileNotFoundException) { }
                }
                foreach (var operation in xattrs)
                {
                    try
                    {
                        operation();
                        throw new InvalidOperationException("A PCK replacement exposed the masked physical extended attributes.");
                    }
                    catch (NotSupportedException ex) when (ex.GetType() == typeof(NotSupportedException)) { }
                    catch (FileNotFoundException) { }
                }
            }
            while (!mounts.IsCompleted);
        }
        finally
        {
            mounts.GetAwaiter().GetResult();
            if (OperatingSystem.IsWindows())
            {
                var attributes = File.GetAttributes(physical);
                if (attributes != originalAttributes)
                    File.SetAttributes(physical, originalAttributes);
                Check(attributes == originalAttributes, "PCK replacements must preserve the masked Windows file attributes.");
            }
            else
            {
                Check(File.GetUnixFileMode(physical) == originalMode, "PCK replacements must preserve the masked Unix permission bits.");
            }
            Check(File.ReadAllBytes(physical).SequenceEqual(TextBytes), "PCK replacements must preserve the masked physical bytes.");
            if (xattrs.Count != 0)
            {
                Check(EFile.GetExtendedAttributesList(physical).Contains(attribute) &&
                    EFile.GetExtendedAttributeString(physical, attribute) == value,
                    "PCK replacements must preserve the masked physical extended attributes.");
            }
        }
    }

    private static void VerifyMountedDirectoryWriteProtection(string directory, string project)
    {
        var zip = IOPath.Combine(directory, "mounted-directories.zip");
        using (var writer = new ZIPPacker())
        {
            writer.Open(zip);
            writer.AddDirectory("lifecycle-empty");
            writer.StartFile("lifecycle-derived/packed.txt");
            writer.WriteFile(TextBytes);
            writer.Close();
        }
        var files = new[] { "lifecycle-empty/notes.txt", "lifecycle-derived/nested/notes.txt" };
        foreach (var name in files)
        {
            var physical = IOPath.Combine(project, name);
            Directory.CreateDirectory(IOPath.GetDirectoryName(physical)!);
            File.WriteAllBytes(physical, TextBytes);
        }
        Check(ProjectSettings.LoadResourcePack(zip), "Mount the empty and derived ZIP directories.");
        var key = new byte[32];
        foreach (var name in files)
        {
            var physical = IOPath.Combine(project, name);
            var resource = "res://" + name;
            Check(EFile.GetFileAsString(resource) == Text, "A mounted directory preserves physical fallback for reads.");
            foreach (var mode in new[] { FileAccessModeFlags.Write, FileAccessModeFlags.WriteRead, FileAccessModeFlags.ReadWrite })
            {
                Reject<UnauthorizedAccessException>(() => { using var file = EFile.Open(resource, mode); });
                Reject<UnauthorizedAccessException>(() => { using var file = EFile.OpenCompressed(resource, mode, FileCompressionMode.Gzip); });
                Reject<UnauthorizedAccessException>(() => { using var file = EFile.OpenEncrypted(resource, mode, key); });
                Reject<UnauthorizedAccessException>(() => { using var file = EFile.OpenEncryptedWithPassword(resource, mode, "fixture password"); });
            }
            if (OperatingSystem.IsWindows())
            {
                var flags = File.GetAttributes(physical);
                Reject<UnauthorizedAccessException>(() => EFile.SetHidden(resource, true));
                Reject<UnauthorizedAccessException>(() => EFile.SetReadOnly(resource, true));
                Check(File.GetAttributes(physical) == flags, "Rejected setters preserve Windows attributes.");
            }
            else
            {
                var mode = File.GetUnixFileMode(physical);
                Reject<UnauthorizedAccessException>(() => EFile.SetUnixPermissions(resource, (UnixPermissionFlags)(mode ^ UnixFileMode.UserExecute)));
                Check(File.GetUnixFileMode(physical) == mode, "Rejected setters preserve Unix permissions.");
            }
            if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                const string attribute = "lifecycle-sentinel";
                EFile.SetExtendedAttributeString(physical, attribute, "original");
                Reject<UnauthorizedAccessException>(() => EFile.SetExtendedAttribute(resource, attribute, "changed"u8));
                Reject<UnauthorizedAccessException>(() => EFile.SetExtendedAttributeString(resource, attribute, "changed"));
                Reject<UnauthorizedAccessException>(() => EFile.RemoveExtendedAttribute(resource, attribute));
                Check(EFile.GetExtendedAttributeString(resource, attribute) == "original", "Rejected setters preserve xattrs and reads.");
            }
            Reject<UnauthorizedAccessException>(() => DirAccess.RemoveAbsolute(resource));
            Check(File.ReadAllBytes(physical).SequenceEqual(TextBytes), "Rejected writes preserve physical data under a mounted directory.");
        }
        Console.WriteLine("Mounted ZIP directories protect physical descendants from writable opens and metadata changes.");
    }

    private static void VerifyPhysicalDirectoryRejection(string project, string user)
    {
        const string name = "physical-directory";
        var projectDirectory = Directory.CreateDirectory(IOPath.Combine(project, name)).FullName;
        var userDirectory = Directory.CreateDirectory(IOPath.Combine(user, name)).FullName;
        var paths = new[]
        {
            (Path: projectDirectory, Physical: projectDirectory),
            (Path: "res://" + name, Physical: projectDirectory),
            (Path: "user://" + name, Physical: userDirectory),
        };
        var key = new byte[32];
        foreach (var (path, physical) in paths)
        {
            foreach (var mode in new[] { FileAccessModeFlags.Write, FileAccessModeFlags.WriteRead })
            {
                Reject<UnauthorizedAccessException>(() => EFile.Open(path, mode));
                Reject<UnauthorizedAccessException>(() => EFile.OpenCompressed(path, mode, FileCompressionMode.Gzip));
                Reject<UnauthorizedAccessException>(() => EFile.OpenEncrypted(path, mode, key));
                Reject<UnauthorizedAccessException>(() => EFile.OpenEncryptedWithPassword(path, mode, "fixture password"));
            }

            if (OperatingSystem.IsWindows())
            {
                var attributes = File.GetAttributes(physical);
                try
                {
                    Reject<UnauthorizedAccessException>(() => EFile.SetHidden(path, (attributes & FileAttributes.Hidden) == 0));
                    Reject<UnauthorizedAccessException>(() => EFile.SetReadOnly(path, (attributes & FileAttributes.ReadOnly) == 0));
                    Check(File.GetAttributes(physical) == attributes, "FileAccess must preserve directory attributes.");
                }
                finally
                {
                    if (File.GetAttributes(physical) != attributes)
                        File.SetAttributes(physical, attributes);
                }
            }
            else
            {
                var permissions = File.GetUnixFileMode(physical);
                Reject<UnauthorizedAccessException>(() => EFile.SetUnixPermissions(path, (UnixPermissionFlags)(permissions ^ UnixFileMode.OtherWrite)));
                Check(File.GetUnixFileMode(physical) == permissions, "FileAccess must preserve directory permissions.");
            }

            Reject<FileNotFoundException>(() => EFile.SetExtendedAttributeString(path, "test", "value"));
            Check(Directory.Exists(physical) && !Directory.EnumerateFileSystemEntries(physical).Any(),
                "Rejected opens must leave the directory intact and empty.");
        }
        Console.WriteLine("Writable FileAccess factories and metadata setters reject physical directories.");
    }

    private static void VerifyInvalidWriteParent(string project, string user)
    {
        const string name = "existing-file";
        var projectFile = IOPath.Combine(project, name);
        var userFile = IOPath.Combine(user, name);
        File.WriteAllBytes(projectFile, TextBytes);
        File.WriteAllBytes(userFile, TextBytes);
        var key = new byte[32];
        foreach (var path in new[]
        {
            IOPath.Combine(projectFile, "child"),
            "res://" + name + "/child",
            "user://" + name + "/nested/child",
        })
        {
            foreach (var mode in new[] { FileAccessModeFlags.Write, FileAccessModeFlags.WriteRead })
            {
                Reject<IOException>(() => EFile.Open(path, mode));
                Reject<DirectoryNotFoundException>(() => EFile.OpenCompressed(path, mode, FileCompressionMode.Gzip));
                Reject<DirectoryNotFoundException>(() => EFile.OpenEncrypted(path, mode, key));
                Reject<DirectoryNotFoundException>(() => EFile.OpenEncryptedWithPassword(path, mode, "fixture password"));
            }
        }
        Check(File.ReadAllBytes(projectFile).SequenceEqual(TextBytes) && File.ReadAllBytes(userFile).SequenceEqual(TextBytes),
            "Rejected opens must preserve files that block the destination.");
        Console.WriteLine("Writable FileAccess factories reject physical file ancestors at open.");
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
