using System.Globalization;
using System.Text.Json;
using Electron2D;
using EOS = Electron2D.OS;

internal static class KeyCodeNamesTests
{
    private static Key Chord(Key code, KeyModifierMask modifiers) => (Key)((int)code | (int)modifiers);
    internal static void Run()
    {
        using var data = JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "KeyCodeNames.json")));
        var table = data.RootElement.GetProperty(OperatingSystem.IsMacOS() ? "macos" : OperatingSystem.IsWindows() ? "windows" : "linux");
        var checkedCodes = new HashSet<int>();
        foreach (var row in table.GetProperty("named").EnumerateArray())
        {
            var raw = row[0].GetInt32(); var name = row[1].GetString()!; var key = (Key)raw;
            Check(EOS.GetKeycodeString(key) == name, "Pinned named key " + raw + ": " + EOS.GetKeycodeString(key) + " != " + name);
            Check(EOS.FindKeycodeFromString(name) == key && EOS.FindKeycodeFromString(name.ToLowerInvariant()) == key, "Named key parsing and case: " + name);
            Check(EOS.IsKeycodeUnicode(raw) == row[2].GetBoolean(), "Character capability: " + name);
            using var input = new InputEventKey { Keycode = key }; Check(input.AsTextKeycode() == name, "InputEventKey shares the exact formatter: " + name);
            checkedCodes.Add(raw);
        }
        foreach (var row in table.GetProperty("samples").EnumerateArray()) Check(EOS.IsKeycodeUnicode(row[0].GetInt32()) == row[1].GetBoolean(), "Classifier boundaries: " + row[0]);
        Check(EOS.GetKeycodeString(Key.None) == "" && EOS.FindKeycodeFromString("") == Key.None && EOS.FindKeycodeFromString("NoSuchKey") == Key.None, "Unset and unknown names.");
        Check(EOS.GetKeycodeString(Key.Unknown) == "Unknown" && EOS.FindKeycodeFromString("Unknown") == Key.Unknown, "Unknown is the recognized key sentinel.");
        Check(EOS.GetKeycodeString((Key)0x1F680) == "🚀" && EOS.FindKeycodeFromString("🚀") == Key.None, "Display of an unnamed Unicode scalar does not expand the named parser.");
        Check(EOS.FindKeycodeFromString("Shift+Tab") == Chord(Key.Tab, KeyModifierMask.Shift), "Documented modifier combination.");
        Check(EOS.FindKeycodeFromString("junk+Shift+Tab") == Chord(Key.Tab, KeyModifierMask.Shift) && EOS.FindKeycodeFromString(" Shift+Tab") == Key.Tab, "Unknown modifier prefixes are ignored without trimming.");
        Check(EOS.FindKeycodeFromString("Ctrl+") == Chord(Key.None, KeyModifierMask.Control) && EOS.FindKeycodeFromString("Ctrl+missing") == Chord(Key.None, KeyModifierMask.Control), "Known prefix survives an unknown or empty final key.");
        Check(EOS.FindKeycodeFromString("+") == Key.None && EOS.FindKeycodeFromString("Plus") == Key.Plus && EOS.FindKeycodeFromString(" Ctrl") == Key.None, "Delimiter and named punctuation stay distinct.");
        Check(EOS.FindKeycodeFromString("Ctrl+Ctrl+A") == Chord(Key.A, KeyModifierMask.Control), "Repeated modifiers combine once.");
        var mask = KeyModifierMask.Control | KeyModifierMask.Alt | KeyModifierMask.Shift | KeyModifierMask.Meta;
        var full = Chord(Key.A, mask); Check(EOS.FindKeycodeFromString(EOS.GetKeycodeString(full)) == full, "All concrete modifiers preserve their typed identity.");
        var expected = OperatingSystem.IsMacOS() ? "Ctrl+Option+Shift+Command+A" : OperatingSystem.IsWindows() ? "Ctrl+Alt+Shift+Windows+A" : "Ctrl+Alt+Shift+Meta+A";
        Check(EOS.GetKeycodeString(full) == expected, "Canonical modifier order.");
        var logical = Chord(Key.A, KeyModifierMask.CommandOrControl); var concrete = Chord(Key.A, OperatingSystem.IsMacOS() ? KeyModifierMask.Meta : KeyModifierMask.Control);
        Check(EOS.GetKeycodeString(logical) == EOS.GetKeycodeString(concrete) && EOS.FindKeycodeFromString(EOS.GetKeycodeString(logical)) == concrete, "Logical modifier display resolves to the concrete platform key.");
        using var modified = new InputEventKey { Keycode = full }; Check(modified.AsTextKeycode() == EOS.GetKeycodeString(full), "Embedded modifiers share formatting.");
        var culture = CultureInfo.CurrentCulture; try { CultureInfo.CurrentCulture = new("tr-TR"); Check(EOS.FindKeycodeFromString("insert") == Key.Insert, "Names are independent of current culture."); } finally { CultureInfo.CurrentCulture = culture; }
        Reject<ArgumentNullException>(() => EOS.FindKeycodeFromString(null!));
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        for (var i = 0; i < 256; i++) _ = EOS.FindKeycodeFromString("Ctrl+Shift+Space");
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1024; i++) Check(EOS.FindKeycodeFromString("Ctrl+Shift+Space") == Chord(Key.Space, KeyModifierMask.Control | KeyModifierMask.Shift), "Warmed lookup.");
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed parsing allocates zero caller bytes.");
        Parallel.For(0, 64, _ => Check(EOS.FindKeycodeFromString("Shift+Tab") == Chord(Key.Tab, KeyModifierMask.Shift), "Independent callers share immutable names."));
        Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Pure key operations require no native host.");
        Console.WriteLine("Key names: " + checkedCodes.Count + " pinned identities, modifiers, boundaries, culture, parallel access and 1024 zero-byte lookups passed.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException(typeof(T).Name + " expected."); }
}
