using System.Diagnostics;
using System.Globalization;
using Electron2D;

internal static partial class GUITranslationContextTests
{
    private const string Domain = "gui-context-check";
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 400, 200); }
    private sealed class Probe : Control
    {
        internal int TranslationNotifications;
        protected override void OnNotification(int what)
        { base.OnNotification(what); if (what == NotificationTranslationChanged) TranslationNotifications++; }
    }
    private static void Register()
    {
        var culture = CultureInfo.GetCultureInfo("en");
        TranslationServer.Culture = culture;
        TranslationServer.AddTranslation(culture, Domain, "Token", "plain");
        TranslationServer.AddTranslation(culture, Domain, "Token", "AAAA", "action");
        TranslationServer.AddTranslation(culture, Domain, "Token", "BBBBBB", "noun");
        TranslationServer.AddTranslation(culture, Domain, "Token", "explicit", "override");
        TranslationServer.AddTranslation(CultureInfo.GetCultureInfo("fr"), Domain, "Token", "CCCCCCCC", "noun");
        TranslationServer.AddPluralTranslation(culture, Domain, "one", "many", n => n == 1 ? "A" : "AA", "action");
        TranslationServer.AddPluralTranslation(culture, Domain, "one", "many", _ => "O", "override");
    }
    internal static void Run()
    {
        var previous = TranslationServer.Culture;
        try
        {
            NodeLocalizationTests.Run();
            Register();
            using var parent = new Control { TranslationContext = "noun", TranslationDomain = Domain };
            var control = new Probe(); parent.AddChild(control);
            Node node = control; ElectronObject obj = control;
            Check(control.TranslationContext == "" && node.Atr("Token") == "plain", "Context is local, while domain is inherited.");
            var descriptor = control.GetPropertyList().Single(p => p.Name == nameof(Control.TranslationContext));
            Check(descriptor.IsStored && !descriptor.IsReadOnly, "Context is a writable stored descriptor.");
            control.TranslationNotifications = 0; control.TranslationContext = "action";
            Check(control.TranslationNotifications == 0, "Context setter does not notify or invalidate captions.");
            Check(node.Atr("Token") == "AAAA" && node.Atr("Token", "") == "AAAA" && node.Atr("Token", "override") == "explicit", "Automatic default and explicit override work through a Node reference.");
            Check(obj.Tr("Token") == "plain" && obj.Tr("Token", "override") == "explicit", "Explicit object translation keeps its own context.");
            Check(node.AtrN("one", "many", 1) == "A" && node.AtrN("one", "many", 2, "") == "AA" && node.AtrN("one", "many", 2, "override") == "O", "Automatic plural context follows the same rule.");
            control.AutoTranslateMode = NodeAutoTranslateMode.Disabled;
            Check(node.Atr("Token") == "Token" && node.AtrN("one", "many", 2) == "many" && obj.Tr("Token") == "plain", "Automatic disablement leaves explicit translation enabled.");
            control.AutoTranslateMode = NodeAutoTranslateMode.Always; control.CanTranslateMessages = false;
            Check(node.Atr("Token") == "Token", "Object-level disablement still applies."); control.CanTranslateMessages = true;
            descriptor.Revert(control); Check(control.TranslationContext == "", "Stored descriptor reverts to empty context.");
            Throws<ArgumentNullException>(() => control.TranslationContext = null!);
            control.TranslationContext = "action";
            for (var i = 0; i < 128; i++) Check(node.Atr("Token") == "AAAA", "Warm context lookup.");
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1024; i++) { control.TranslationContext = "action"; Check(node.Atr("Token") == "AAAA", "Context result."); }
            Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed context writes and direct lookups allocate zero bytes.");
            Consumers(); Persistence();
            using var root = new TestViewport(); var attached = new Control(); root.AddChild(attached); using var tree = new SceneTree(root);
            Throws<InvalidOperationException>(() => Task.Run(() => attached.TranslationContext = "worker").GetAwaiter().GetResult());
            Throws<InvalidOperationException>(() => Task.Run(() => _ = attached.TranslationContext).GetAwaiter().GetResult());
            tree.Dispose(); Throws<ObjectDisposedException>(() => _ = attached.TranslationContext);
            Throws<ObjectDisposedException>(() => attached.TranslationContext = "disposed");
        }
        finally { TranslationServer.Culture = previous; TranslationServer.RemoveDomain(Domain); }
        Console.WriteLine("GUI context singular/plural, policy, cached captions, storage, fresh process, lifetime and zero-byte checks passed.");
    }
    private static void Consumers()
    {
        using var label = new Label { TranslationDomain = Domain, TranslationContext = "action", Text = "Token" };
        Check(label.Text == "Token" && label.GetTotalCharacterCount() == 4, "Label preserves source while preparing contextual text.");
        label.TranslationContext = "noun"; Check(label.GetTotalCharacterCount() == 4, "Setter alone preserves cached label.");
        label.Notify(Node.NotificationTranslationChanged); Check(label.GetTotalCharacterCount() == 6, "Explicit translation notification refreshes label.");
        using var rich = new RichTextLabel { TranslationDomain = Domain, TranslationContext = "action", Text = "Token" };
        Check(rich.GetParsedText() == "AAAA", "Rich text consumes automatic context.");
        rich.TranslationContext = "noun"; rich.Notify(Node.NotificationTranslationChanged);
        Check(rich.GetParsedText() == "BBBBBB", "Rich text notification refreshes context.");
        using var button = new Button { TranslationDomain = Domain, TranslationContext = "action", Text = "Token" };
        Check(button.TranslateButtonText(button.Text) == "AAAA", "Button caption consumes automatic context.");
        using var option = new OptionButton { TranslationDomain = Domain, TranslationContext = "action" }; option.AddItem("Token");
        Check(option.TranslateButtonText(option.Text) == "AAAA", "Option caption consumes automatic context.");
        option.SetItemAutoTranslateMode(0, NodeAutoTranslateMode.Always);
        Check(option.TranslateButtonText(option.Text) == "plain", "Always item policy uses explicit translation without automatic context.");
        option.SetItemAutoTranslateMode(0, NodeAutoTranslateMode.Disabled);
        Check(option.TranslateButtonText(option.Text) == "Token", "Disabled item policy preserves source.");
        using var tabs = new TabBar { TranslationDomain = Domain, TranslationContext = "action", Size = new(300, 40) }; tabs.AddTab("Token");
        using var expectedTabs = new TabBar { AutoTranslateMode = NodeAutoTranslateMode.Disabled, Size = new(300, 40) }; expectedTabs.AddTab("AAAA");
        Check(tabs.GetTabRect(0).Size.X == expectedTabs.GetTabRect(0).Size.X, "Tab title layout consumes contextual text.");
        using var items = new ItemList { TranslationDomain = Domain, TranslationContext = "action", Size = new(300, 100) }; items.AddItem("Token");
        using var expectedItems = new ItemList { AutoTranslateMode = NodeAutoTranslateMode.Disabled, Size = new(300, 100) }; expectedItems.AddItem("AAAA");
        Check(items.GetItemRect(0, false).Size == expectedItems.GetItemRect(0, false).Size, "Item layout consumes contextual text.");
        using var line = new LineEdit { TranslationDomain = Domain, TranslationContext = "action", PlaceholderText = "Token" };
        using var expectedLine = new LineEdit { AutoTranslateMode = NodeAutoTranslateMode.Disabled, PlaceholderText = "AAAA" };
        Check(line.GetMinimumSize() == expectedLine.GetMinimumSize(), "Empty entry placeholder consumes contextual text.");
        line.Text = "Token"; Check(line.Text == "Token", "Authored entry text stays literal.");
    }
    private static void Persistence()
    {
        using var source = new Label { TranslationDomain = Domain, TranslationContext = "action", Text = "Token" };
        using var packed = new PackedScene(); packed.Pack(source);
        using var copy = (Label)packed.Instantiate();
        Check(copy.TranslationContext == "action" && copy.Atr("Token") == "AAAA", "Packed scene stores context independently.");
        copy.TranslationContext = "noun"; Check(source.TranslationContext == "action", "Scene instances own independent contexts.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-gui-context-" + Guid.NewGuid().ToString("N") + ".e2dscene");
        try
        {
            ResourceSaver.Save(packed, path);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true };
            if (System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet") start.ArgumentList.Add(typeof(GUITranslationContextTests).Assembly.Location);
            start.Environment["ELECTRON2D_TEST_GUI_CONTEXT_CHILD"] = path;
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Fresh context scene process."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh GUI context scene passed"), "Fresh file process: " + error.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunChild(string path)
    {
        Register(); using var loaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore);
        var label = (Label)loaded.Instantiate(); using var tree = new SceneTree(label);
        Check(label.TranslationContext == "action" && label.Text == "Token" && label.GetTotalCharacterCount() == 4, "Fresh scene resolves contextual caption after entry.");
        label.TranslationContext = "noun"; label.Notify(Node.NotificationTranslationChanged);
        Check(label.GetTotalCharacterCount() == 6, "Fresh scene executes contextual retranslation.");
        Console.WriteLine("Fresh GUI context scene passed");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
