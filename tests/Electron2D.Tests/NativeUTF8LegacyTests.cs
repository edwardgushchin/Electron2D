using Electron2D;
using System.Runtime.InteropServices;
using EngineFile = Electron2D.FileAccess;
using Path = System.IO.Path;

internal static partial class NativeUTF8LegacyTests
{
    [LibraryImport("libElectron2DENet.so", EntryPoint = "e2d_test_fail_after")]
    private static partial void FailAfter(uint count);
    internal static void Run()
    {
        var root = Environment.GetEnvironmentVariable("ELECTRON2D_UTF8_TEST_DIRECTORY") ?? throw new InvalidOperationException("An owned fixture directory is required.");
        if (!Path.IsPathFullyQualified(root) || Directory.Exists(root)) throw new InvalidOperationException("Use a fresh absolute fixture directory.");
        Directory.CreateDirectory(root);
        var folder = root;
        for (var i = 0; i < 4; i++) { folder = Path.Combine(folder, new string('я', 60) + i); Directory.CreateDirectory(folder); }
        var path = Path.Combine(folder, "payload.bin"); File.WriteAllBytes(path, []);
        var payload = Enumerable.Range(0, 8193).Select(i => (byte)(i * 31)).ToArray();
        EngineFile.SetExtendedAttribute(path, "данные", payload);
        Check(EngineFile.GetExtendedAttribute(path, "данные").AsSpan().SequenceEqual(payload), "The real extended-attribute output buffer contains all 8193 native bytes.");
        Check(EngineFile.GetExtendedAttributesList(path).Contains("данные"), "The native name list retains non-ASCII names.");
        EngineFile.SetExtendedAttribute(path, "empty", []);
        Check(EngineFile.GetExtendedAttribute(path, "empty").Length == 0, "A null data pointer with size zero retains empty-attribute semantics.");
        var baseline = NativeAllocation.Snapshot().Current;
        var bad = new string('n', 300);
        for (uint failure = 1; failure <= 2; failure++)
        {
            FailAfter(failure); var rejected = false;
            try { EngineFile.SetExtendedAttribute(path, bad, payload); }
            catch (OutOfMemoryException) { rejected = true; }
            finally { FailAfter(0); }
            Check(rejected && NativeAllocation.Snapshot().Current == baseline && EngineFile.GetExtendedAttribute(path, "данные").AsSpan().SequenceEqual(payload),
                "Either path/name preparation refusal preserves the old attribute and retires all temporary inputs.");
        }
        var invalid = false;
        try { EngineFile.SetExtendedAttribute(path, bad, payload); } catch (IOException) { invalid = true; }
        Check(invalid && NativeAllocation.Snapshot().Current == baseline, "The real native name-limit error survives UTF8 cleanup.");
        EngineFile.RemoveExtendedAttribute(path, "empty"); EngineFile.RemoveExtendedAttribute(path, "данные");
        Check(EngineFile.GetExtendedAttributesList(path).Count == 0, "Native attribute removal drains the list.");
        var missing = false;
        try { _ = EngineFile.GetExtendedAttribute(path, "missing"); } catch (IOException) { missing = true; }
        Check(missing && NativeAllocation.Snapshot().Current == baseline, "Missing-attribute errno is preserved after argument cleanup.");
        for (uint failure = 1; failure <= 3; failure++)
        {
            FailAfter(failure); var rejected = false;
            try { _ = OS.CreateProcess("/bin/echo", ["first", "second"]); }
            catch (OutOfMemoryException) { rejected = true; }
            finally { FailAfter(0); }
            Check(rejected && NativeAllocation.Snapshot().Current == baseline, "Each argv refusal releases preceding strings before creating a process.");
        }
        var output = new List<string>(); var text = "Аргумент 😀 " + new string('q', 4096);
        Check(OS.Execute("/usr/bin/printf", ["%s", text], output) == 0 && output.Count == 1 && output[0] == text,
            "Healthy argv retry delivers complete UTF8 bytes to a real child process and captures its output.");
        var message = LinuxPortalDBus.NewMethodCall("org.Electron2D.Memory", "/memory", "org.Electron2D.Memory", "Check");
        Check(message != 0, "The migrated DBus factory creates a real message.");
        LinuxPortalDBus.MessageUnref(message);
        FailAfter(1); var refused = false;
        try { _ = LinuxPortalDBus.NewMethodCall("org.Electron2D.Memory", "/" + new string('x', 400), "org.Electron2D.Memory", "Check"); }
        catch (OutOfMemoryException) { refused = true; }
        finally { FailAfter(0); }
        Check(refused, "A DBus path refusal rejects before message creation.");
        var trashFile = Path.Combine(folder, "trash.txt"); File.WriteAllText(trashFile, "owned fixture");
        FailAfter(1); refused = false;
        try { OS.MoveToTrash(trashFile); }
        catch (OutOfMemoryException) { refused = true; }
        finally { FailAfter(0); }
        Check(refused && File.Exists(trashFile), "Refused dynamic GIO path storage preserves the source.");
        OS.MoveToTrash(trashFile);
        var trash = Path.Combine(Environment.GetEnvironmentVariable("XDG_DATA_HOME")!, "Trash", "files", "trash.txt");
        Check(!File.Exists(trashFile) && File.ReadAllText(trash) == "owned fixture", "The dynamic GIO adapter retains recoverable trash behavior in the private data directory.");
        Console.WriteLine("Legacy UTF8 inputs: real long-path/8193-byte/empty xattrs, name/errno failures, argv refusal/retry/child bytes, DBus and recoverable private GIO trash passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
