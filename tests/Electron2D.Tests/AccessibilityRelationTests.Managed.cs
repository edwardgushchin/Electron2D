using System.Diagnostics;
using Electron2D;
using Path = System.IO.Path;

internal static partial class AccessibilityRelationTests
{
    private static int _checks;
    private static void Check(bool condition, string message) { _checks++; if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    internal static void RunManaged()
    {
        using var root = new Control { Name = "Form" };
        var label = new Label { Name = "Caption", Text = "Related caption" }; root.AddChild(label); label.Owner = root;
        var button = new Button("") { Name = "Action", AccessibilityLabeledByNodes = ["../Caption"], AccessibilityDescribedByNodes = ["../Caption"], AccessibilityControlsNodes = [".."], AccessibilityFlowToNodes = ["../Caption", ""] }; root.AddChild(button); button.Owner = root;
        Check(button.GetConfigurationWarnings().Length == 0, "An authored resolved label names an otherwise unnamed action.");
        var input = new[] { "../Caption" }; button.AccessibilityLabeledByNodes = input; input[0] = "bad";
        var output = button.AccessibilityLabeledByNodes; output[0] = "bad";
        Check(button.AccessibilityLabeledByNodes[0] == "../Caption", "Path arrays are copied at both boundaries.");
        Reject<ArgumentNullException>(() => button.AccessibilityControlsNodes = null!);
        Reject<ArgumentException>(() => button.AccessibilityDescribedByNodes = [null!]);
        Reject<ArgumentException>(() => button.AccessibilityLabeledByNodes = ["../Caption::bad"]);
        using (var duplicate = (Control)root.Duplicate())
        {
            var copy = duplicate.GetNode<Button>("Action");
            Check(copy.AccessibilityLabeledByNodes.SequenceEqual(["../Caption"]) && copy.AccessibilityControlsNodes.SequenceEqual([".."]) && copy.AccessibilityFlowToNodes.SequenceEqual(["../Caption", ""]), "Duplication preserves authored paths.");
            copy.AccessibilityLabeledByNodes = []; Check(button.AccessibilityLabeledByNodes.Length == 1, "Duplicate metadata is independent.");
        }
        var folder = Directory.CreateTempSubdirectory("electron2d-relations-").FullName;
        try
        {
            var path = Path.Combine(folder, "relations.e2dscene"); ResourceSaver.SaveScene(root, path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
            start.ArgumentList.Add("--accessibility-relations-scene"); start.ArgumentList.Add(path);
            using var child = Process.Start(start)!; var text = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Related scene reader timed out."); }
            Check(child.ExitCode == 0, text.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
        var disposed = new Control(); disposed.Dispose(); Reject<ObjectDisposedException>(() => _ = disposed.AccessibilityLabeledByNodes);
        Console.WriteLine($"PASS {_checks} managed relation metadata, copy boundaries, warnings and fresh-process archive contracts.");
    }
    internal static void ReadScene(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var root = (Control)packed.Instantiate(); var action = root.GetNode<Button>("Action");
        Check(action.AccessibilityLabeledByNodes.SequenceEqual(["../Caption"]) && action.AccessibilityDescribedByNodes.SequenceEqual(["../Caption"]) && action.AccessibilityControlsNodes.SequenceEqual([".."]) && action.AccessibilityFlowToNodes.SequenceEqual(["../Caption", ""]), "All relation properties survive a fresh process.");
        Check(!root.GetAccessibilityElement().IsValid() && !action.GetAccessibilityElement().IsValid(), "Archives never retain process-local semantic references.");
    }
}
