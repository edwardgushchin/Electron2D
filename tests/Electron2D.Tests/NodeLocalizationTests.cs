using System.Globalization;
using Electron2D;

internal static class NodeLocalizationTests
{
    internal static void Run()
    {
        var culture = TranslationServer.Culture;
        var enabled = TranslationServer.Enabled;
        try
        {
            TranslationServer.Clear();
            TranslationServer.Enabled = true;
            TranslationServer.Culture = CultureInfo.GetCultureInfo("fr");
            TranslationServer.AddTranslation(TranslationServer.Culture, "game", "Hello", "Bonjour");
            TranslationServer.AddTranslation(TranslationServer.Culture, "other", "Hello", "Salut");
            TranslationServer.AddPluralTranslation(TranslationServer.Culture, "game", "apple", "apples",
                count => count == 1 ? "pomme" : "pommes");

            using var root = new ProbeNode { Name = "Root", TranslationDomain = "game" };
            var child = new ProbeNode { Name = "Child" };
            var grandchild = new ProbeNode { Name = "Grandchild" };
            var explicitChild = new ProbeNode { Name = "Explicit", TranslationDomain = "other" };
            root.AddChild(child);
            child.AddChild(grandchild);
            root.AddChild(explicitChild);
            Check(child.TranslationDomain == "game" && grandchild.Tr("Hello") == "Bonjour" &&
                child.Atr("Hello") == "Bonjour" && child.AtrN("apple", "apples", 2) == "pommes" &&
                explicitChild.Atr("Hello") == "Salut", "Parent domain inheritance and automatic singular/plural lookup.");

            child.AutoTranslateMode = NodeAutoTranslateMode.Disabled;
            Check(!child.CanAutoTranslate() && !grandchild.CanAutoTranslate() &&
                child.Atr("Hello") == "Hello" && grandchild.AtrN("apple", "apples", 2) == "apples" &&
                child.Tr("Hello") == "Bonjour", "Automatic policy leaves explicit translation available.");
            grandchild.AutoTranslateMode = NodeAutoTranslateMode.Always;
            Check(grandchild.CanAutoTranslate() && grandchild.Atr("Hello") == "Bonjour", "Descendant mode overrides inherited disablement.");
            child.AutoTranslateMode = NodeAutoTranslateMode.Inherit;
            grandchild.AutoTranslateMode = NodeAutoTranslateMode.Inherit;
            child.CanTranslateMessages = false;
            Check(child.Atr("Hello") == "Hello" && grandchild.Atr("Hello") == "Bonjour",
                "Per-object message enablement stays independent of descendant policy.");
            child.CanTranslateMessages = true;
            Reject<ArgumentOutOfRangeException>(() => child.AutoTranslateMode = (NodeAutoTranslateMode)10);
            Reject<ArgumentNullException>(() => child.Atr(null!));
            Reject<ArgumentNullException>(() => child.AtrN("apple", null!, 2));
            using (var orphan = new Node { TranslationDomain = "game" })
            {
                orphan.SetTranslationDomainInherited();
                Check(orphan.TranslationDomain == string.Empty,
                    "A parentless node restored to inheritance uses the main domain.");
            }

            using (var tree = new SceneTree(root))
            {
                Check(root.AutoTranslateMode == NodeAutoTranslateMode.Always,
                    "The default project setting selects Always for an inherited scene root.");
                Reject<InvalidOperationException>(() => root.AutoTranslateMode = NodeAutoTranslateMode.Inherit);
                Check(root.TranslationNotifications > 0 && child.TranslationNotifications > 0,
                    "Tree entry notifies automatically translating nodes.");
                var childNotifications = child.TranslationNotifications;
                var grandchildNotifications = grandchild.TranslationNotifications;
                var explicitNotifications = explicitChild.TranslationNotifications;
                root.TranslationDomain = "other";
                Check(child.TranslationDomain == "other" && child.Atr("Hello") == "Salut" &&
                    child.TranslationNotifications == childNotifications + 1 &&
                    grandchild.TranslationNotifications == grandchildNotifications + 1 &&
                    explicitChild.TranslationNotifications == explicitNotifications,
                    "Changing a parent domain notifies only affected inheriting descendants.");
                child.TranslationDomain = string.Empty;
                Check(child.TranslationDomain == string.Empty && child.Atr("Hello") == "Hello",
                    "An explicit empty domain stops inheritance.");
                child.SetTranslationDomainInherited();
                Check(child.TranslationDomain == "other" && child.Atr("Hello") == "Salut",
                    "Restoring inheritance immediately observes the current parent domain.");
                root.AutoTranslateMode = NodeAutoTranslateMode.Disabled;
                Check(!child.CanAutoTranslate() && child.Atr("Hello") == "Hello" &&
                    explicitChild.Atr("Hello") == "Hello", "The root mode controls inheriting descendants.");
                Reject<InvalidOperationException>(() => Task.Run(child.CanAutoTranslate).GetAwaiter().GetResult());
                Reject<InvalidOperationException>(() => Task.Run(() => child.TranslationDomain).GetAwaiter().GetResult());
                root.AutoTranslateMode = NodeAutoTranslateMode.Always;
                child.ThrowOnTranslation = true;
                grandchildNotifications = grandchild.TranslationNotifications;
                Reject<AggregateException>(() => root.TranslationDomain = "game");
                Check(grandchild.TranslationNotifications == grandchildNotifications + 1 &&
                    child.TranslationDomain == "game", "A failing translation callback does not skip inheriting descendants or roll back the domain.");
            }

            using var packedRoot = new Node { Name = "PackedRoot", TranslationDomain = "game" };
            var packedInherited = new Node { Name = "Inherited", AutoTranslateMode = NodeAutoTranslateMode.Disabled };
            var packedExplicit = new Node { Name = "Explicit", TranslationDomain = string.Empty };
            packedRoot.AddChild(packedInherited);
            packedRoot.AddChild(packedExplicit);
            packedInherited.Owner = packedRoot;
            packedExplicit.Owner = packedRoot;
            using var scene = new PackedScene();
            scene.Pack(packedRoot);
            using var copy = scene.Instantiate();
            Check(copy.GetChild(0).TranslationDomain == "game" &&
                copy.GetChild(0).AutoTranslateMode == NodeAutoTranslateMode.Disabled &&
                !copy.GetChild(0).CanAutoTranslate() &&
                copy.GetChild(1).TranslationDomain == string.Empty,
                "Scene storage preserves auto-translation modes and inherited versus explicit empty domains.");
            copy.TranslationDomain = "other";
            Check(copy.GetChild(0).TranslationDomain == "other" &&
                copy.GetChild(1).TranslationDomain == string.Empty,
                "Instantiated inherited domains remain live after parent changes.");

            var settings = ProjectSettings.Service;
            var previousRootMode = ProjectSettings.Get(ProjectSettings.RootNodeAutoTranslate);
            try
            {
                ProjectSettings.Set(ProjectSettings.RootNodeAutoTranslate, false);
                using var disabledRoot = new Node();
                using var disabledTree = new SceneTree(disabledRoot);
                Check(disabledRoot.AutoTranslateMode == NodeAutoTranslateMode.Disabled &&
                    !disabledRoot.CanAutoTranslate(), "The project setting disables automatic root translation at tree construction.");
                ProjectSettings.Set(ProjectSettings.RootNodeAutoTranslate, true);
                Check(!disabledRoot.CanAutoTranslate(), "An existing tree retains its sampled root mode.");
            }
            finally { ProjectSettings.Set(ProjectSettings.RootNodeAutoTranslate, previousRootMode); }
            Console.WriteLine("Node localization checks passed.");
        }
        finally
        {
            TranslationServer.Clear();
            TranslationServer.Culture = culture;
            TranslationServer.Enabled = enabled;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Reject<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new Exception($"Expected {typeof(TException).Name}.");
    }

    private sealed class ProbeNode : Node
    {
        internal int TranslationNotifications { get; private set; }

        internal bool ThrowOnTranslation { get; set; }

        protected override void OnNotification(int what)
        {
            if (what == NotificationTranslationChanged) TranslationNotifications++;
            if (what == NotificationTranslationChanged && ThrowOnTranslation)
                throw new InvalidOperationException("Expected translation notification failure.");
            base.OnNotification(what);
        }
    }
}
