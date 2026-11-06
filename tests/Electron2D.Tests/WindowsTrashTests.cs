using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Electron2D;
using IOPath = System.IO.Path;

internal static partial class WindowsTrashTests
{
    private static string _oracle = "", _root = "";
    private static int _checks;
    private static readonly HashSet<string> Attempted = new(StringComparer.OrdinalIgnoreCase);
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static string[] Oracle(string mode, string path, string? payload = null)
    {
        var start = new ProcessStartInfo(_oracle) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        start.ArgumentList.Add(mode); start.ArgumentList.Add(path); if (payload is not null) start.ArgumentList.Add(payload);
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Windows Shell oracle timed out."); }
        if (process.ExitCode != 0) throw new InvalidOperationException("Windows Shell oracle: " + error.GetAwaiter().GetResult());
        return output.GetAwaiter().GetResult().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }
    private static void Trash(string path)
    {
        var absolute = IOPath.GetFullPath(ProjectSettings.GlobalizePath(path));
        if (!absolute.StartsWith(_root + IOPath.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Fixture escape.");
        lock (Attempted) Attempted.Add(absolute); OS.MoveToTrash(path);
    }
    private static string FileAt(string name, string value)
    { var path = IOPath.Combine(_root, name); Directory.CreateDirectory(IOPath.GetDirectoryName(path)!); File.WriteAllText(path, value, Encoding.UTF8); return path; }
    private static void Restore(string source, string payload)
    {
        Check(Oracle("restore", source, payload).Length == 1, "The native Shell restores the selected recycle identity.");
        Check(!Oracle("inspect", source).Contains(payload, StringComparer.OrdinalIgnoreCase), "Restoration removes the matching recycle metadata.");
    }
    internal static void Run(string oracle, string workspace)
    {
        if (!OperatingSystem.IsWindows()) throw new NotSupportedException("This profile qualifies Windows recycling.");
        _oracle = IOPath.GetFullPath(oracle); _root = IOPath.GetFullPath(IOPath.Combine(workspace, "windows-trash-" + Guid.NewGuid().ToString("N"))); Directory.CreateDirectory(_root);
        var passed = false;
        try
        {
            if (Environment.GetEnvironmentVariable("ELECTRON2D_WINDOWS_TRASH_PRECEDING") == "1")
            {
                var file = FileAt("preceding.txt", "preserved"); Reject<NotSupportedException>(() => Trash(file)); Check(File.ReadAllText(file) == "preserved" && Oracle("inspect", file).Length == 0, "The preceding engine refuses without moving the source.");
                passed = true; Console.WriteLine($"Windows preceding control PASS {_checks}"); return;
            }
            Files(); Directories(); Failures(); Apartments(); VirtualAndConcurrent(); Browser(); passed = true;
            Console.WriteLine($"Windows trash PASS {_checks}; real Shell recycle metadata, native restoration and browser flow");
        }
        finally
        {
            foreach (var path in Attempted) foreach (var payload in Oracle("inspect", path))
                {
                    if (File.Exists(path) || Directory.Exists(path)) throw new InvalidOperationException("Preserve conflicting recovery fixtures: " + _root);
                    Restore(path, payload);
                }
            if (passed) Directory.Delete(_root, true); else Console.Error.WriteLine("Preserved controlled fixtures: " + _root);
        }
    }
    private static void Files()
    {
        foreach (var name in new[] { "Ёж 🦔 & ' $ (literal).txt", "readonly.txt", new string('x', 55) + ".txt" })
        {
            var source = FileAt(name, name); if (name == "readonly.txt") File.SetAttributes(source, FileAttributes.ReadOnly);
            Trash(source); Check(!File.Exists(source), "Successful recycling removes the original file entry."); var records = Oracle("inspect", source); Check(records.Length == 1 && File.ReadAllText(records[0]) == name, "Independent $I metadata identifies the exact path and recoverable bytes.");
            Restore(source, records[0]); Check(File.ReadAllText(source) == name, "Native restoration preserves Unicode and exact bytes."); File.SetAttributes(source, FileAttributes.Normal);
        }
        var duplicate = FileAt("same-name.txt", "first"); Trash(duplicate); File.WriteAllText(duplicate, "second"); Trash(duplicate); var versions = Oracle("inspect", duplicate);
        Check(versions.Length == 2 && versions.Select(File.ReadAllText).Order().SequenceEqual(new[] { "first", "second" }), "Same-name recycling retains independent versions without overwriting earlier data.");
        foreach (var payload in versions) { var content = File.ReadAllText(payload); Restore(duplicate, payload); Check(File.ReadAllText(duplicate) == content, "A selected native recycle identity restores its own version."); File.Move(duplicate, IOPath.Combine(_root, content + ".restored")); }
        var relative = FileAt("relative.txt", "relative"); var previous = Environment.CurrentDirectory;
        try { Environment.CurrentDirectory = _root; Trash("relative.txt"); } finally { Environment.CurrentDirectory = previous; }
        Restore(relative, Oracle("inspect", relative).Single());
        var binary = IOPath.Combine(_root, "binary.bin"); var bytes = Enumerable.Range(0, 4097).Select(i => (byte)(i * 137)).ToArray(); File.WriteAllBytes(binary, bytes); Trash(binary); var binaryRecord = Oracle("inspect", binary).Single(); Check(File.ReadAllBytes(binaryRecord).SequenceEqual(bytes), "The native recycle payload preserves every arbitrary byte."); Restore(binary, binaryRecord); Check(File.ReadAllBytes(binary).SequenceEqual(bytes), "Native restoration preserves exact binary contents.");
        var deep = IOPath.Combine(new string('d', 100), new string('e', 100), "длинный путь", "long.txt"); var longPath = FileAt(deep, "long");
        Attempted.Add(longPath); var native = Oracle("guard", longPath); foreach (var record in Oracle("inspect", longPath)) Restore(longPath, record);
        Check(File.ReadAllText(longPath) == "long", "The independent guarded native reference preserves or restores its controlled source.");
        if (native.Any(line => line.StartsWith("flags=", StringComparison.Ordinal) && (Convert.ToUInt32(line[6..], 16) & 0x80) == 0) || native.Any(line => line.StartsWith("result=8", StringComparison.Ordinal)))
        {
            Reject<IOException>(() => Trash(longPath)); Check(File.ReadAllText(longPath) == "long" && Oracle("inspect", longPath).Length == 0, "When the native Shell offers permanent deletion for a long path, recycling refuses before mutation.");
        }
        else { Trash(longPath); Restore(longPath, Oracle("inspect", longPath).Single()); Check(File.ReadAllText(longPath) == "long", "Supported long absolute paths preserve recoverable bytes."); }
    }
    private static void Directories()
    {
        var directory = IOPath.Combine(_root, "nested"); FileAt("nested/inner/data.txt", "nested bytes"); Trash(directory); Check(!Directory.Exists(directory), "A populated directory is recycled as one identity."); var payload = Oracle("inspect", directory).Single(); Check(File.ReadAllText(IOPath.Combine(payload, "inner/data.txt")) == "nested bytes", "The recycled directory retains its children."); Restore(directory, payload);
        var target = IOPath.Combine(_root, "junction target"); FileAt("junction target/protected.txt", "preserved target"); var link = IOPath.Combine(_root, "junction link"); Oracle("junction", link, target);
        Trash(link); Check(!Directory.Exists(link) && File.ReadAllText(IOPath.Combine(target, "protected.txt")) == "preserved target", "Recycling an NTFS junction preserves its target."); Restore(link, Oracle("inspect", link).Single()); Check((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0, "Native restore preserves the junction identity."); Directory.Delete(link);
    }
    private static void Failures()
    {
        Reject<ArgumentNullException>(() => OS.MoveToTrash(null!)); Reject<ArgumentException>(() => OS.MoveToTrash("")); Reject<ArgumentException>(() => OS.MoveToTrash("invalid\0suffix"));
        var absent = IOPath.Combine(_root, "absent"); Reject<IOException>(() => Trash(absent)); Check(Oracle("inspect", absent).Length == 0, "A missing path creates no recycle entry.");
        var source = FileAt("locked.txt", "must survive");
        using (var locked = File.Open(source, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None)) Reject<IOException>(() => Trash(source));
        Check(File.ReadAllText(source) == "must survive" && Oracle("inspect", source).Length == 0, "A sharing failure preserves the source and creates no partial recycle entry.");
        Trash(source); Restore(source, Oracle("inspect", source).Single()); Check(File.ReadAllText(source) == "must survive", "Retry succeeds after the sharing failure is removed.");
    }
    [SupportedOSPlatform("windows")]
    private static void Apartments()
    {
        foreach (var apartment in new[] { ApartmentState.MTA, ApartmentState.STA })
        {
            var source = FileAt(apartment + ".txt", "apartment"); ExceptionDispatchInfo? failure = null;
            var worker = new System.Threading.Thread(() =>
            {
                try
                {
                    var initialized = CoInitializeEx(0, apartment == ApartmentState.STA ? 2u : 0u); if (initialized < 0) Marshal.ThrowExceptionForHR(initialized);
                    try { Trash(source); if (System.Threading.Thread.CurrentThread.GetApartmentState() != apartment) throw new Exception("Caller apartment changed."); } finally { CoUninitialize(); }
                }
                catch (Exception error) { failure = ExceptionDispatchInfo.Capture(error); }
            });
            worker.SetApartmentState(apartment); worker.Start(); worker.Join(); failure?.Throw(); Restore(source, Oracle("inspect", source).Single()); Check(File.ReadAllText(source) == "apartment", "STA and MTA callers retain their own apartment and restored data.");
        }
    }
    private static void VirtualAndConcurrent()
    {
        var previousProject = ProjectSettings.ProjectRoot; var previousUser = ProjectSettings.UserDataRoot;
        string resource = FileAt("resource.txt", "resource"), user = FileAt("user/user.txt", "user");
        try { ProjectSettings.ConfigurePaths(_root, IOPath.Combine(_root, "user")); Trash("res://resource.txt"); Trash("user://user.txt"); }
        finally { ProjectSettings.ConfigurePaths(previousProject, previousUser); }
        Restore(resource, Oracle("inspect", resource).Single()); Restore(user, Oracle("inspect", user).Single()); Check(File.ReadAllText(resource) == "resource" && File.ReadAllText(user) == "user", "Resource/user paths reach the exact native filesystem identities.");
        var sources = Enumerable.Range(0, 3).Select(i => FileAt("concurrent-" + i + ".txt", "parallel " + i)).ToArray();
        Task.WaitAll(sources.Select(path => Task.Run(() => Trash(path))).ToArray());
        for (var i = 0; i < sources.Length; i++) { Restore(sources[i], Oracle("inspect", sources[i]).Single()); Check(File.ReadAllText(sources[i]) == "parallel " + i, "Concurrent callers retain independent native operations and payloads."); }
    }
    [LibraryImport("ole32.dll")] private static partial int CoInitializeEx(nint reserved, uint mode);
    [LibraryImport("ole32.dll")] private static partial void CoUninitialize();
}
