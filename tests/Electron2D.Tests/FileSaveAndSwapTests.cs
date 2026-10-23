using Electron2D;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using EFile = Electron2D.FileAccess;
using NativeFile = System.IO.File;

internal static class FileSaveAndSwapTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static Exception Error(Action action) { try { action(); } catch (Exception error) { return error; } throw new InvalidOperationException("Expected operation failure"); }
    internal static void Run(string profile)
    {
        Check(!OS.UseFileAccessSaveAndSwap, "Process default is direct writing");
        var folder = System.IO.Directory.CreateTempSubdirectory("electron2d-save-swap-");
        try
        {
            if (profile == "managed")
            {
                Lifecycle(folder.FullName); PathsAndConcurrency(folder.FullName); Modes(folder.FullName); Links(folder.FullName); Faults(folder.FullName); Temporary(); Packs(folder.FullName); Warm(folder.FullName); Crash(folder.FullName); EncoderFailure(folder.FullName);
            }
            else
            {
                OS.SetUseFileAccessSaveAndSwap(true); MovieWriterTests.Native(profile, System.IO.Path.Combine(folder.FullName, "movies"));
            }
            Console.WriteLine(JsonSerializer.Serialize(new { profile = "file-save-and-swap", mode = profile, completed = true }));
        }
        finally { OS.SetUseFileAccessSaveAndSwap(false); folder.Delete(true); }
    }
    private static string PathIn(string root, string name) => System.IO.Path.Combine(root, name);
    private static void Lifecycle(string root)
    {
        var path = PathIn(root, "retained.bin"); NativeFile.WriteAllText(path, "old");
        Task.Run(() => OS.SetUseFileAccessSaveAndSwap(true)).GetAwaiter().GetResult();
        using var file = EFile.Open(path, FileAccessModeFlags.Write); var staging = file.AbsolutePath;
        Check(file.Path == path && staging != path && NativeFile.Exists(staging) && NativeFile.ReadAllText(path) == "old", "Open stages without truncating the old destination");
        file.WriteString("new"); file.Flush(); Check(NativeFile.ReadAllText(path) == "old" && new FileInfo(staging).Length == 3, "Flush persists staging without publication");
        OS.SetUseFileAccessSaveAndSwap(false); file.Close(); file.Close();
        Check(!file.IsOpen && file.AbsolutePath == staging && !NativeFile.Exists(staging) && NativeFile.ReadAllText(path) == "new", "Captured policy commits once and retains physical path observation");
        using (var direct = EFile.Open(path, FileAccessModeFlags.Write)) { Check(direct.AbsolutePath == path && new FileInfo(path).Length == 0, "Disabled policy truncates directly"); direct.WriteString("direct"); }
        OS.SetUseFileAccessSaveAndSwap(true);
        var absent = PathIn(root, "new-file.bin"); using (var fresh = EFile.Open(absent, FileAccessModeFlags.Write)) { fresh.WriteString("created"); Check(!NativeFile.Exists(absent), "New destination is absent until close"); }
        Check(NativeFile.ReadAllText(absent) == "created", "New target creation");
        using (var empty = EFile.Open(path, FileAccessModeFlags.Write)) { }
        Check(new FileInfo(path).Length == 0, "Closing an unwritten staged file intentionally saves empty output");
        NativeFile.WriteAllText(path, "before"); using var oldReader = EFile.Open(path, FileAccessModeFlags.Read);
        using (var replacement = EFile.Open(path, FileAccessModeFlags.Write)) replacement.WriteString("after");
        Check(oldReader.ReadAllText() == "before" && NativeFile.ReadAllText(path) == "after", "An existing reader retains the old inode while new readers see the replacement");
    }
    private static void PathsAndConcurrency(string root)
    {
        var project = ProjectSettings.ProjectRoot; var user = ProjectSettings.UserDataRoot;
        var userRoot = System.IO.Directory.CreateDirectory(PathIn(root, "virtual-user")).FullName;
        try
        {
            ProjectSettings.ConfigurePaths(root, userRoot);
            foreach (var virtualPath in new[] { "res://virtual.bin", "user://virtual.bin" })
            {
                var physical = ProjectSettings.GlobalizePath(virtualPath); NativeFile.WriteAllText(physical, "old");
                using var file = EFile.Open(virtualPath, FileAccessModeFlags.Write); file.WriteString("virtual");
                Check(file.Path == virtualPath && NativeFile.ReadAllText(physical) == "old", "Virtual input retains its original name and stages physical output");
                file.Close(); Check(NativeFile.ReadAllText(physical) == "virtual", "Virtual target publication");
            }
        }
        finally { ProjectSettings.ConfigurePaths(project, user); }
        var path = PathIn(root, "independent.bin"); NativeFile.WriteAllText(path, "initial");
        using var first = EFile.Open(path, FileAccessModeFlags.Write); using var second = EFile.Open(path, FileAccessModeFlags.Write);
        first.WriteString("first"); second.WriteString("second");
        Check(first.AbsolutePath != second.AbsolutePath && NativeFile.ReadAllText(path) == "initial", "Concurrent owners have independent siblings");
        second.Close(); Check(NativeFile.ReadAllText(path) == "second", "First completion publishes its own contents");
        first.Close(); Check(NativeFile.ReadAllText(path) == "first", "Last completion wins without combining data");
        var missing = PathIn(root, "absent-directory/output.bin");
        Check(Error(() => EFile.Open(missing, FileAccessModeFlags.Write)) is DirectoryNotFoundException && !System.IO.Directory.Exists(PathIn(root, "absent-directory")), "Open never creates parent directories");
        using var valid = EFile.Open(path, FileAccessModeFlags.Write); valid.WriteString("valid");
        _ = Error(() => valid.Resize(-1)); valid.Close(); Check(NativeFile.ReadAllText(path) == "valid", "Caller validation errors do not cancel successful output");
    }
    private static void Modes(string root)
    {
        foreach (var mode in new[] { FileAccessModeFlags.ReadWrite, FileAccessModeFlags.WriteRead })
        {
            var path = PathIn(root, "mode-" + mode); NativeFile.WriteAllText(path, "abc");
            using var file = EFile.Open(path, mode); Check(file.AbsolutePath == path && file.Length == (mode == FileAccessModeFlags.WriteRead ? 0 : 3), "Only exact Write stages");
            file.WriteString("modified"); file.Flush(); Check(new FileInfo(path).Length == 8, "Mixed-mode flush remains direct");
        }
        var path2 = PathIn(root, "compressed.bin"); NativeFile.WriteAllText(path2, "old");
        using (var compressed = EFile.OpenCompressed(path2, FileAccessModeFlags.Write, FileCompressionMode.Gzip))
        { Check(compressed.AbsolutePath == path2, "Transformed stream has independent atomic envelope ownership"); compressed.WriteString("compressed"); compressed.Flush(); using var read = EFile.OpenCompressed(path2, FileAccessModeFlags.Read, FileCompressionMode.Gzip); Check(read.ReadAllText() == "compressed", "Transformed Flush still publishes its envelope"); }
        using var settings = new ProjectSettingsRegistry(root, PathIn(root, "user")); var definition = new ProjectSetting<int>("save/test", 7); settings.Register(definition); settings.Set(definition, 11); settings.SaveCustom(PathIn(root, "project.e2d"));
        using var loaded = new ProjectSettingsRegistry(root, PathIn(root, "user")); loaded.Register(definition); loaded.LoadCustom(PathIn(root, "project.e2d")); Check(loaded.Get(definition) == 11, "Independent configuration persistence remains atomic");
    }
    private static void Links(string root)
    {
        var target = PathIn(root, "target.bin"); var link = PathIn(root, "link.bin"); NativeFile.WriteAllText(target, "old"); NativeFile.CreateSymbolicLink(link, "target.bin");
        if (!OperatingSystem.IsWindows()) NativeFile.SetUnixFileMode(target, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        using (var file = EFile.Open(link, FileAccessModeFlags.Write))
        {
            file.WriteString("linked"); if (!OperatingSystem.IsWindows()) { Check(file.UnixPermissions == (UnixPermissionFlags)384, "Existing Unix mode copied to staging"); file.UnixPermissions = (UnixPermissionFlags)416; }
        }
        Check(new FileInfo(link).LinkTarget == "target.bin" && NativeFile.ReadAllText(target) == "linked", "Relative symbolic link is preserved");
        if (!OperatingSystem.IsWindows()) Check((int)NativeFile.GetUnixFileMode(target) == 416, "Pending permission edits reach the replaced target");
        var dangling = PathIn(root, "dangling.bin"); var missing = PathIn(root, "missing-target.bin"); NativeFile.CreateSymbolicLink(dangling, "missing-target.bin");
        using (var file = EFile.Open(dangling, FileAccessModeFlags.Write)) file.WriteString("resolved");
        Check(new FileInfo(dangling).LinkTarget == "missing-target.bin" && NativeFile.ReadAllText(missing) == "resolved", "Dangling link creates its target without replacing the link");
        if (OperatingSystem.IsLinux())
        {
            var fifo = PathIn(root, "input.fifo"); Check(CreateFIFO(fifo, 384) == 0, "Owned FIFO fixture"); var before = System.IO.Directory.GetFiles(root).Length;
            Check(Error(() => EFile.Open(fifo, FileAccessModeFlags.Write)) is IOException && System.IO.Directory.GetFiles(root).Length == before, "Nonregular destination rejects without opening/blocking or creating staging"); NativeFile.Delete(fifo);
        }
        var alias = PathIn(root, "alias.bin"); NativeFile.CreateSymbolicLink(alias, "link.bin");
        using (var file = EFile.Open(alias, FileAccessModeFlags.Write)) file.WriteString("multi-hop");
        Check(new FileInfo(alias).LinkTarget == "link.bin" && NativeFile.ReadAllText(target) == "multi-hop", "Final target is resolved through multiple symbolic links");
        var cycle = PathIn(root, "cycle.bin"); NativeFile.CreateSymbolicLink(cycle, "cycle.bin");
        Check(Error(() => EFile.Open(cycle, FileAccessModeFlags.Write)) is IOException, "Cyclic symbolic link rejects before staging"); NativeFile.Delete(cycle);
        Check(Error(() => EFile.Open(root, FileAccessModeFlags.Write)) is IOException, "Directory is not a save destination");
    }
    private static void Faults(string root)
    {
        var path = PathIn(root, "failure.bin"); NativeFile.WriteAllText(path, "retained");
        foreach (var operation in new[] { "flush", "write", "resize" })
            using (var file = EFile.Open(path, FileAccessModeFlags.Write))
            {
                var staged = file.AbsolutePath; file.WriteString("partial"); file.Flush();
                var wrapper = (SaveAndSwapStream)file.BorrowStream;
                var physical = (FileStream)typeof(SaveAndSwapStream).GetField("_file", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(wrapper)!; physical.SafeFileHandle.Dispose();
                _ = Error(() => { if (operation == "flush") file.Flush(); else if (operation == "write") file.WriteByte(1); else file.Resize(32); }); _ = Error(file.Close);
                Check(!file.IsOpen && NativeFile.ReadAllText(path) == "retained" && !NativeFile.Exists(staged), "Underlying I/O fault aborts output and removes partial staging");
            }
        var blocked = PathIn(root, "blocked.bin"); string recovery;
        using (var file = EFile.Open(blocked, FileAccessModeFlags.Write))
        {
            recovery = file.AbsolutePath; file.WriteString("recover me"); System.IO.Directory.CreateDirectory(blocked);
            _ = Error(file.Close); Check(!file.IsOpen && NativeFile.ReadAllText(recovery) == "recover me" && System.IO.Directory.Exists(blocked), "Failed replacement retains completed output and external destination"); file.Close();
        }
        NativeFile.Delete(recovery); System.IO.Directory.Delete(blocked);
    }
    private static void Temporary()
    {
        string destination, staged;
        using (var file = EFile.CreateTemp(FileAccessModeFlags.Write, "save-swap", keep: false)) { destination = file.Path; staged = file.AbsolutePath; file.WriteString("temporary"); }
        Check(!NativeFile.Exists(destination) && !NativeFile.Exists(staged), "Delete-on-close removes both logical and physical temporary ownership");
        using (var file = EFile.CreateTemp(FileAccessModeFlags.Write, "save-swap", keep: true)) { destination = file.Path; staged = file.AbsolutePath; file.WriteString("kept"); }
        Check(!NativeFile.Exists(staged) && NativeFile.ReadAllText(destination) == "kept", "Kept temporary output publishes at its original requested name"); NativeFile.Delete(destination);
    }
    private static void Packs(string root)
    {
        var path = PathIn(root, "data.pck"); NativeFile.WriteAllText(path, "old pack");
        using (var pack = new PCKPacker()) { pack.PCKStart(path); pack.AddFileFromBuffer("save.txt", "saved"u8); Check(NativeFile.ReadAllText(path) == "old pack", "Package index authoring remains staged"); pack.Flush(); }
        Check(NativeFile.ReadAllBytes(path).AsSpan(0, 4).SequenceEqual("GDPC"u8), "Finalized package publishes complete output");
        NativeFile.WriteAllText(path, "keep pack");
        using (var pack = new PCKPacker())
        {
            pack.PCKStart(path); pack.AddFileFromBuffer("pending", "pending"u8); typeof(PCKPacker).GetField("_faulted", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(pack, true);
        }
        Check(NativeFile.ReadAllText(path) == "keep pack", "Faulted package never publishes staging");
    }
    private static void Warm(string root)
    {
        var block = new byte[4096]; using var file = EFile.Open(PathIn(root, "prepared.bin"), FileAccessModeFlags.Write);
        for (var i = 0; i < 64; i++) file.WriteBytes(block);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 1024; i++) file.WriteBytes(block);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0, "1024 prepared staged writes owner/all bytes " + ownerBytes + "/" + allBytes);
        Console.WriteLine(JsonSerializer.Serialize(new { probe = "prepared-staged-writes", writes = 1024, bytesPerWrite = block.Length, ownerBytes, allBytes }));
    }
    private static void Crash(string root)
    {
        var path = PathIn(root, "crash.bin"); var marker = PathIn(root, "crash.json"); NativeFile.WriteAllText(path, "old committed");
        var start = new ProcessStartInfo(Environment.ProcessPath!) { ArgumentList = { "--save-swap-child", path, marker }, UseShellExecute = false };
        if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Insert(0, typeof(FileSaveAndSwapTests).Assembly.Location);
        using var child = Process.Start(start)!;
        Check(child.WaitForExit(15000) && child.ExitCode == 23, "Child exits without disposal"); var staging = JsonSerializer.Deserialize<string>(NativeFile.ReadAllText(marker))!;
        Check(NativeFile.ReadAllText(path) == "old committed" && NativeFile.ReadAllText(staging) == "uncommitted", "Abrupt process exit preserves old file and leaves recoverable staging"); NativeFile.Delete(staging);
    }
    internal static void Child(string path, string marker)
    {
        OS.SetUseFileAccessSaveAndSwap(true); var file = EFile.Open(path, FileAccessModeFlags.Write); file.WriteString("uncommitted"); file.Flush(); NativeFile.WriteAllText(marker, JsonSerializer.Serialize(file.AbsolutePath)); Environment.Exit(23);
    }
    private delegate void BeginWriter(Vector2i size, int fps, string path);
    private delegate void FrameWriter(Image image, ReadOnlySpan<int> samples);
    private static void EncoderFailure(string root)
    {
        var path = PathIn(root, "failure.ogv"); NativeFile.WriteAllText(path, "old video"); using var writer = new OGVMovieWriter();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic; var begin = typeof(OGVMovieWriter).GetMethod("OnWriteBegin", flags)!.CreateDelegate<BeginWriter>(writer); var frame = typeof(OGVMovieWriter).GetMethod("OnWriteFrame", flags)!.CreateDelegate<FrameWriter>(writer); var end = typeof(OGVMovieWriter).GetMethod("OnWriteEnd", flags)!.CreateDelegate<Action>(writer);
        begin(new(16, 16), 60, path); using var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgb8); Check(Error(() => frame(image, Array.Empty<int>())) is IOException, "Native encoder rejects invalid frame"); end();
        Check(NativeFile.ReadAllText(path) == "old video", "Native codec failure cancels staged output");
    }
    [DllImport("libc", EntryPoint = "mkfifo", SetLastError = true)] private static extern int CreateFIFO(string path, uint mode);
}
