using System.Diagnostics;
using Electron2D;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;
using AAction = Electron2D.AccessibilityServer.AccessibilityAction;
using Path = System.IO.Path;

internal static partial class AccessibilityFoundationTests
{
    private static int _checks;
    private static void Check(bool value, string message) { _checks++; if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        _checks++; try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private sealed class CustomButton : Button
    {
        internal int Updates, Invalidations;
        internal RID Selected = default;
        internal bool CompositeFocus;
        private static Node Create() => new CustomButton();
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationAccessibilityUpdate)
            {
                Updates++;
                if (CompositeFocus && !AccessibilityServer.HasElement(Selected))
                {
                    Selected = AccessibilityServer.CreateSubElement(GetAccessibilityElement(), Role.Button);
                    AccessibilityServer.UpdateSetName(Selected, "Composite focus target");
                }
            }
            if (what == NotificationAccessibilityInvalidate) Invalidations++;
        }
        protected override string[] OnGetAccessibilityConfigurationWarnings() => ["Custom accessible warning"];
        protected override RID OnGetFocusedAccessibilityElement() => Selected.IsValid() ? Selected : base.OnGetFocusedAccessibilityElement();
    }
    [System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles("The framework-dependent scene fixture can relaunch its managed assembly.")]
    internal static void RunManaged()
    {
        Check(!AccessibilityServer.IsSupported() && !AccessibilityServer.HasElement(default), "No simulated service outside a native display.");
        Reject<NotSupportedException>(() => AccessibilityServer.CreateElement(0, Role.Button));
        Reject<ArgumentOutOfRangeException>(() => ProjectSettings.Set(ProjectSettings.AccessibilitySupport, (AccessibilitySupport)3));
        Check(ProjectSettings.Get(ProjectSettings.AccessibilitySupport) == AccessibilitySupport.Auto, "Built-in default is registered.");
        using (var button = new CustomButton { AccessibilityName = "Action", AccessibilityDescription = "Details" })
        {
            Check(!button.GetAccessibilityElement().IsValid(), "Detached semantic identity is empty.");
            Check(button.GetConfigurationWarnings() is ["Custom accessible warning"], "Accessibility warning extension reaches the existing authoring query.");
            button.QueueAccessibilityUpdate(); button.QueueAccessibilityUpdate();
            using var copy = (CustomButton)button.Duplicate();
            Check(copy.AccessibilityName == "Action" && copy.AccessibilityDescription == "Details" && !copy.GetAccessibilityElement().IsValid(), "Duplicate retains authored metadata without native identity.");
            Reject<ArgumentNullException>(() => button.AccessibilityName = null!);
            Reject<ArgumentNullException>(() => button.AccessibilityDescription = null!);
        }
        using (var empty = new Button())
        {
            Check(empty.GetConfigurationWarnings().Length == 1, "An unnamed action exposes an authoring warning.");
            empty.AccessibilityName = "Named action"; Check(empty.GetConfigurationWarnings().Length == 0, "An authored name resolves the warning.");
        }
        using (var region = new VBoxContainer { AccessibilityRegion = true })
        {
            Check(region.GetConfigurationWarnings().Length == 1, "An unnamed landmark exposes an authoring warning.");
            region.AccessibilityName = "Named region"; using var copy = (VBoxContainer)region.Duplicate();
            Check(copy.AccessibilityRegion && copy.AccessibilityName == "Named region" && copy.GetConfigurationWarnings().Length == 0, "Derived container metadata and landmark policy duplicate together.");
        }
        using (var tree = new SceneTree(new Control()))
        {
            var action = new Button(); var region = new VBoxContainer(); tree.Root.AddChild(action); tree.Root.AddChild(region); tree.EditedSceneRoot = tree.Root;
            var changes = 0; tree.NodeConfigurationWarningChanged += (_, _) => changes++;
            action.AccessibilityName = "Named"; action.AccessibilityName = "Named"; region.AccessibilityRegion = true;
            Check(changes == 2, "Name/region writes refresh edited-scene warnings; equal names stay silent.");
            action.Text = "Caption"; Check(changes == 3, "Caption writes refresh unnamed-action warnings.");
        }
        var folder = Path.Combine(Path.GetTempPath(), "electron2d-accessibility-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            using var root = new Control { Name = "Root", AccessibilityName = "Root name", AccessibilityDescription = "Root description", AccessibilityLive = AccessibilityServer.AccessibilityLiveMode.Polite };
            var child = new Button("Caption") { Name = "Action", AccessibilityName = "Authored action", AccessibilityDescription = "Action details" }; root.AddChild(child); child.Owner = root;
            var region = new PanelContainer { Name = "Region", AccessibilityRegion = true, AccessibilityName = "Scene region" }; root.AddChild(region); region.Owner = root;
            var line = new LineEdit { Name = "Line", ContextMenuEnabled = false, AccessibilityLive = AccessibilityServer.AccessibilityLiveMode.Assertive }; root.AddChild(line); line.Owner = root;
            var path = Path.Combine(folder, "semantic.e2dscene"); ResourceSaver.SaveScene(root, path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(AccessibilityFoundationTests).Assembly.Location);
            start.ArgumentList.Add("--accessibility-scene"); start.ArgumentList.Add(path);
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Accessibility archive reader timed out."); }
            Check(process.ExitCode == 0, output.GetAwaiter().GetResult() + errors.GetAwaiter().GetResult());
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine($"PASS {_checks} managed accessibility, detached metadata and fresh-process scene contracts.");
    }
    internal static void ReadScene(string path)
    {
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep); using var root = (Control)scene.Instantiate();
        var child = root.GetNode<Button>("Action");
        Check(root.AccessibilityLive == AccessibilityServer.AccessibilityLiveMode.Polite && root.GetNode<LineEdit>("Line") is { ContextMenuEnabled: false, AccessibilityLive: AccessibilityServer.AccessibilityLiveMode.Assertive }, "Live and context-menu policies survive a fresh process.");
        Check(root.AccessibilityName == "Root name" && root.AccessibilityDescription == "Root description" && child.Text == "Caption" && child.AccessibilityName == "Authored action" && child.AccessibilityDescription == "Action details", "Scene preserves both metadata properties across process boundaries.");
        Check(root.GetNode<PanelContainer>("Region").AccessibilityRegion && root.GetNode<PanelContainer>("Region").AccessibilityName == "Scene region", "Landmark metadata survives a fresh process.");
        Check(!root.GetAccessibilityElement().IsValid() && !child.GetAccessibilityElement().IsValid(), "Scene archives have no live native identities.");
    }
    private static void ServerEdges(int windowID, RID borrowed)
    {
        var root = AccessibilityServer.GetWindowRoot(windowID);
        Reject<InvalidOperationException>(() => AccessibilityServer.FreeElement(root));
        Reject<InvalidOperationException>(() => AccessibilityServer.FreeElement(borrowed));
        Reject<KeyNotFoundException>(() => AccessibilityServer.FreeElement(default));
        using (var material = new CanvasItemMaterial()) Reject<KeyNotFoundException>(() => AccessibilityServer.UpdateSetName(material.GetRID(), "wrong kind"));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.CreateElement(windowID, (Role)48));
        foreach (var role in Enum.GetValues<Role>()) { var item = AccessibilityServer.CreateElement(windowID, role); Check(AccessibilityServer.HasElement(item), "Every typed role creates a real native element."); AccessibilityServer.FreeElement(item); }
        var parent = AccessibilityServer.CreateElement(windowID, Role.Container);
        var owned = AccessibilityServer.CreateSubElement(parent, Role.Button);
        var nested = AccessibilityServer.CreateSubElement(owned, Role.StaticText);
        var independent = AccessibilityServer.CreateElement(windowID, Role.Panel);
        AccessibilityServer.UpdateAddChild(parent, independent);
        Reject<ArgumentException>(() => AccessibilityServer.UpdateAddChild(owned, parent));
        Reject<InvalidOperationException>(() => AccessibilityServer.UpdateAddChild(parent, root));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.CreateSubElement(parent, Role.Button, -2));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.CreateSubElement(parent, Role.Button, 4));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetFlag(parent, (Flag)10, true));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateAddAction(parent, (AAction)22, () => { }));
        Reject<ArgumentNullException>(() => AccessibilityServer.UpdateAddAction(parent, AAction.Click, (Action)null!));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetBounds(parent, new(0, 0, -1, 4)));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetBounds(parent, new(float.NaN, 0, 1, 1)));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetTransform(parent, new(float.PositiveInfinity, 0, 0, 1, 0, 0)));
        AccessibilityServer.UpdateSetBounds(parent, new(0, 0, 2, 2));
        Reject<ArgumentOutOfRangeException>(() => AccessibilityServer.UpdateSetTransform(parent, new(float.MaxValue, 0, 0, float.MaxValue, 0, 0)));
        AccessibilityServer.FreeElement(parent);
        Check(!AccessibilityServer.HasElement(parent) && !AccessibilityServer.HasElement(owned) && !AccessibilityServer.HasElement(nested) && AccessibilityServer.HasElement(independent), "Parent frees owned descendants and detaches independent elements.");
        Reject<KeyNotFoundException>(() => AccessibilityServer.UpdateSetFocus(owned)); AccessibilityServer.FreeElement(independent);
        Reject<InvalidOperationException>(() => Task.Run(() => AccessibilityServer.UpdateSetName(borrowed, "wrong thread")).GetAwaiter().GetResult());
    }
}
