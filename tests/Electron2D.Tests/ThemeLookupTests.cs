using Electron2D;

internal static class ThemeLookupTests
{
    internal static void Run()
    {
        VerifyPriorityAndVariations();
        VerifyBoundariesAndWindow();
        VerifyNotificationsAndOverrides();
        VerifyMembershipAndFailures();
        VerifyPackingAndLegacyProjections();
        VerifyFallbacksAndDefaults();
        VerifyCallbackReentryAndStoredSchema();
        VerifyWarmLookups();
        Console.WriteLine("Control/window theme priority, deferred caches, override lifetime, packing and warm lookup checks passed.");
    }

    private static void VerifyPriorityAndVariations()
    {
        using var outerTheme = new Theme(); using var innerTheme = new Theme();
        outerTheme.SetConstant("priority", nameof(LookupControl), 11); innerTheme.SetConstant("priority", nameof(Control), 22);
        var outer = new Control { Theme = outerTheme }; var inner = new Control { Name = "Inner", Theme = innerTheme }; var leaf = new LookupControl { Name = "Leaf" };
        outer.AddChild(inner); inner.AddChild(leaf); using var tree = new SceneTree(outer); tree.ProcessFrame(0);
        Check(leaf.GetThemeConstant("priority") == 22, "A nearer theme's base-class entry wins over an outer theme's more specific native entry.");
        leaf.AddThemeConstantOverride("priority", 33);
        Check(leaf.GetThemeConstant("priority") == 33 && leaf.GetThemeConstant("priority", nameof(LookupControl)) == 33 && leaf.GetThemeConstant("priority", nameof(Control)) == 22,
            "Local overrides apply to the owner/default type while an explicit different type bypasses them.");
        leaf.RemoveThemeConstantOverride("priority");
        innerTheme.SetConstant("native", nameof(CanvasItem), 9); tree.FlushDeferred(); Check(leaf.GetThemeConstant("native") == 9, "Native lookup follows the full real class hierarchy.");
        innerTheme.SetTypeVariation("Special", "Middle"); innerTheme.SetTypeVariation("Middle", nameof(LookupControl));
        innerTheme.SetConstant("varied", "Special", 70); innerTheme.SetConstant("varied", "Middle", 60); innerTheme.SetConstant("varied", nameof(Control), 50);
        leaf.ThemeTypeVariation = "Special"; tree.FlushDeferred();
        Check(leaf.GetThemeConstant("varied") == 70, "The most specific variation precedes its base variation and native types.");
        innerTheme.ClearConstant("varied", "Special"); tree.FlushDeferred(); Check(leaf.GetThemeConstant("varied") == 60, "Variation lookup falls through to its variation base.");
        innerTheme.ClearConstant("varied", "Middle"); tree.FlushDeferred(); Check(leaf.GetThemeConstant("varied") == 50, "Variation lookup eventually falls through to the native hierarchy.");
        outerTheme.SetTypeVariation("SharedVariation", nameof(Control)); innerTheme.SetConstant("shared", "SharedVariation", 88); leaf.ThemeTypeVariation = "SharedVariation"; tree.FlushDeferred();
        Check(leaf.GetThemeConstant("shared") == 88, "An outer theme can define the variation chain consumed by a nearer theme's values.");
        innerTheme.SetTypeVariation("CycleA", "CycleB"); innerTheme.SetTypeVariation("CycleB", "CycleA"); leaf.ThemeTypeVariation = "CycleA";
        Reject<InvalidOperationException>(() => leaf.GetThemeConstant("cycle_missing")); Reject<InvalidOperationException>(() => leaf.HasThemeConstant("cycle_missing"));
        leaf.ThemeTypeVariation = "";
        using var style = new StyleBoxEmpty(); outerTheme.SetStyleBox("skin", nameof(Control), style); innerTheme.SetStyleBox("skin", nameof(Control), null); tree.FlushDeferred();
        Check(ReferenceEquals(leaf.GetThemeStyleBox("skin"), style), "A nearer null placeholder allows an outer theme's nonnull resource to participate.");
        Reject<ArgumentNullException>(() => leaf.ThemeTypeVariation = null!); Reject<ArgumentException>(() => leaf.AddThemeConstantOverride("bad\0key", 1));
        Reject<InvalidOperationException>(() => Task.Run(() => leaf.GetThemeConstant("priority")).GetAwaiter().GetResult());
    }

    private static void VerifyBoundariesAndWindow()
    {
        using var theme = new Theme(); theme.SetConstant("branch", nameof(Control), 41); theme.SetConstant("window", nameof(Window), 17);
        var root = new Control { Theme = theme }; var direct = new Control { Name = "Direct" }; var neutral = new Node { Name = "Neutral" }; var isolated = new Control { Name = "Isolated" };
        using var window = new Window { Name = "Window", Theme = theme }; var windowChild = new Control { Name = "WindowChild" }; window.AddChild(windowChild);
        root.AddChild(direct); root.AddChild(neutral); neutral.AddChild(isolated);
        using var tree = new SceneTree(root); tree.ProcessFrame(0);
        Check(direct.GetThemeConstant("branch") == 41 && windowChild.GetThemeConstant("branch") == 41 && window.GetThemeConstant("window") == 17, "A detached Window supplies its own theme to its Control descendants without requiring a native host.");
        Check(isolated.GetThemeConstant("branch") == 0 && !isolated.HasThemeConstant("branch"), "An ordinary Node breaks theme inheritance.");
        var isolatedEvents = 0; var windowEvents = 0; isolated.ThemeChanged += () => isolatedEvents++; window.ThemeChanged += () => windowEvents++; windowChild.ThemeChanged += () => windowEvents++;
        theme.SetConstant("branch", nameof(Control), 42); tree.FlushDeferred();
        Check(isolatedEvents == 0 && direct.GetThemeConstant("branch") == 42 && windowChild.GetThemeConstant("branch") == 42 && windowEvents == 0, "Live propagation stops at neutral nodes while detached Window inheritance stays fresh without scene notifications.");
        using var windowTheme = new Theme { DefaultBaseScale = 1.5f, DefaultFontSize = 23 }; windowTheme.SetConstant("branch", nameof(Control), 55); windowTheme.SetConstant("window", nameof(Window), 29);
        window.Theme = windowTheme;
        Check(window.GetThemeConstant("window") == 29 && windowChild.GetThemeConstant("branch") == 55 && window.GetThemeDefaultBaseScale() == 1.5f && windowChild.GetThemeDefaultFontSize() == 23,
            "A window-owned theme supplies values and defaults to its own branch.");
        window.AddThemeConstantOverride("window", 99); Check(window.GetThemeConstant("window") == 99 && windowChild.GetThemeConstant("branch") == 55, "Window local overrides do not become descendant overrides.");
        direct.Reparent(neutral, keepGlobalTransform: false); Check(direct.GetThemeConstant("branch") == 0, "Reparenting across a neutral boundary invalidates an inherited cached value.");

        using var detachedTheme = new Theme(); detachedTheme.SetConstant("value", nameof(Control), 1);
        using var detached = new Control { Theme = detachedTheme }; var child = new Control { Name = "Child" }; detached.AddChild(child); var detachedEvents = 0;
        detached.ThemeChanged += () => detachedEvents++; child.ThemeChanged += () => detachedEvents++;
        Check(child.GetThemeConstant("value") == 1, "Constructed detached descendants can query inherited appearance.");
        detachedTheme.SetConstant("value", nameof(Control), 2);
        Check(child.GetThemeConstant("value") == 2 && detachedEvents == 0, "Detached inherited lookups remain fresh without emitting scene theme notifications.");
        detached.BeginBulkThemeOverride(); detached.AddThemeConstantOverride("local", 3); detached.EndBulkThemeOverride();
        Check(detached.GetThemeConstant("local") == 3 && detachedEvents == 0, "Detached local edits remain usable and notification-free.");
    }

    private static void VerifyNotificationsAndOverrides()
    {
        using var theme = new Theme(); theme.SetConstant("value", nameof(Control), 1);
        var root = new Control { Theme = theme }; var child = new Control { Name = "Child" }; root.AddChild(child); using var tree = new SceneTree(root); tree.ProcessFrame(0);
        Check(root.GetThemeConstant("value") == 1 && child.GetThemeConstant("value") == 1, "Prime branch value caches.");
        var seen = new List<int>(); var events = 0; child.ThemeChanged += () => { events++; seen.Add(child.GetThemeConstant("value")); };
        theme.SetConstant("value", nameof(Control), 2);
        Check(events == 0 && child.GetThemeConstant("value") == 1, "Theme resource changes preserve cached values until their deferred scene notification.");
        tree.FlushDeferred(); Check(events == 1 && seen.SequenceEqual(new[] { 1 }) && child.GetThemeConstant("value") == 2, "ThemeChanged observes the old cache, followed by required invalidation and fresh values.");
        using var style = new StyleBoxLine(); child.AddThemeStyleBoxOverride("first", style); child.AddThemeStyleBoxOverride("second", style); events = 0;
        style.Thickness = 2; Check(events == 1, "A same-thread override resource change refreshes its owner synchronously once for shared aliases.");
        child.RemoveThemeStyleBoxOverride("first"); events = 0; style.Thickness = 3; Check(events == 1, "One remaining override alias keeps the shared change subscription.");
        events = 0; Task.Run(() => style.Thickness = 4).GetAwaiter().GetResult(); Check(events == 0, "A background override-resource update marshals notification to the scene owner.");
        tree.FlushDeferred(); Check(events == 1, "The background resource update reaches the owner through its deferred callback.");
        child.RemoveThemeStyleBoxOverride("second"); events = 0; style.Thickness = 5; Check(events == 0, "Removing the last override alias releases its subscription.");
        child.BeginBulkThemeOverride(); child.BeginBulkThemeOverride(); child.AddThemeColorOverride("tint", Colors.Red); child.AddThemeConstantOverride("number", 7); child.AddThemeFontSizeOverride("size", -3);
        Check(events == 0 && child.GetThemeConstant("number") == 7 && child.GetThemeFontSize("size") == -3, "Bulk override edits commit immediately while suppressing notifications; signed sizes are retained.");
        child.EndBulkThemeOverride(); Check(events == 1, "Repeated Begin calls form a boolean batch rather than nested batches.");
        Reject<InvalidOperationException>(child.EndBulkThemeOverride); child.BeginBulkThemeOverride(); child.EndBulkThemeOverride(); Check(events == 2, "Ending an empty attached batch still emits one refresh.");
        child.AddThemeStyleBoxOverride("dead", style); events = 0; style.Dispose();
        Check(events == 1 && child.HasThemeStyleBoxOverride("dead") && ReferenceEquals(child.GetThemeStyleBox("dead"), style), "Disposal refreshes an override owner while preserving the borrowed identity.");
        Reject<ObjectDisposedException>(() => child.AddThemeStyleBoxOverride("invalid", style)); Reject<ArgumentNullException>(() => child.AddThemeStyleBoxOverride("null", null!));
    }

    private static void VerifyMembershipAndFailures()
    {
        using var theme = new Theme(); theme.SetConstant("value", nameof(Control), 1);
        var oldRoot = new Control(); var branch = new Control { Name = "Branch", Theme = theme }; var leaf = new Control { Name = "Leaf" }; oldRoot.AddChild(branch); branch.AddChild(leaf);
        using var oldTree = new SceneTree(oldRoot); using var newTree = new SceneTree(new Control()); oldTree.ProcessFrame(0); newTree.ProcessFrame(0);
        var events = 0; branch.ThemeChanged += () => events++; theme.SetConstant("value", nameof(Control), 2);
        oldRoot.RemoveChild(branch); newTree.Root.AddChild(branch); events = 0; oldTree.FlushDeferred();
        Check(events == 0 && leaf.GetThemeConstant("value") == 2, "Deferred callbacks from the old tree membership cannot notify a reattached branch.");
        theme.SetConstant("value", nameof(Control), 3); newTree.FlushDeferred(); Check(events == 1 && leaf.GetThemeConstant("value") == 3, "The current membership receives new deferred changes after stale work is ignored.");
        theme.SetConstant("value", nameof(Control), 4); newTree.Root.RemoveChild(branch); newTree.Root.AddChild(branch); events = 0; newTree.FlushDeferred();
        Check(events == 0 && leaf.GetThemeConstant("value") == 4, "Same-tree removal and reentry invalidate earlier queued generations.");
        _ = branch.GetThemeConstant("value"); _ = leaf.GetThemeConstant("value"); var leafEvents = 0; leaf.ThemeChanged += () => leafEvents++;
        Action failure = () => throw new ApplicationException("expected theme observer failure"); branch.ThemeChanged += failure;
        theme.SetConstant("value", nameof(Control), 5); Reject<AggregateException>(newTree.FlushDeferred); branch.ThemeChanged -= failure;
        Check(branch.GetThemeConstant("value") == 5 && leaf.GetThemeConstant("value") == 5 && leafEvents == 1, "A failed parent notification still clears its cache and refreshes descendants.");
        theme.Dispose(); newTree.FlushDeferred();
        Check(ReferenceEquals(branch.Theme, theme) && leaf.GetThemeConstant("value") == 0, "A disposed borrowed theme keeps its assigned identity while subsequent lookup skips its unavailable values.");
        Reject<ObjectDisposedException>(() => branch.Theme = theme);
    }

    private static void VerifyPackingAndLegacyProjections()
    {
        using var theme = new Theme(); using var style = new StyleBoxLine(); using var inherited = new StyleBoxEmpty();
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); using var icon = ImageTexture.CreateFromImage(image);
        theme.SetConstant("number", nameof(Control), 2); theme.SetStyleBox("skin", nameof(Control), inherited); theme.SetTypeVariation("PackedVariation", nameof(Control));
        using var control = new Control { Theme = theme, ThemeTypeVariation = "PackedVariation" };
        control.AddThemeColorOverride("tint", Colors.Red); control.AddThemeConstantOverride("number", 9); control.AddThemeFontSizeOverride("size", 21);
        control.AddThemeIconOverride("icon", icon); control.AddThemeStyleBoxOverride("skin", style);
        using var packed = new PackedScene(); packed.Pack(control); using var copy = (Control)packed.Instantiate();
        Check(ReferenceEquals(copy.Theme, theme) && copy.ThemeTypeVariation == "PackedVariation" && copy.GetThemeColor("tint") == Colors.Red && copy.GetThemeConstant("number") == 9 && copy.GetThemeFontSize("size") == 21 &&
            ReferenceEquals(copy.GetThemeIcon("icon"), icon) && ReferenceEquals(copy.GetThemeStyleBox("skin"), style), "Packing restores the theme, variation and all five typed override families.");
        var properties = copy.GetPropertyList();
        Check(properties.Single(property => property.Name == "ThemeColorOverride/tint") is PropertyDescriptor<Control, Color?> &&
            properties.Single(property => property.Name == "ThemeConstantOverride/number") is PropertyDescriptor<Control, int?> &&
            properties.Single(property => property.Name == "ThemeFontSizeOverride/size") is PropertyDescriptor<Control, int?> &&
            properties.Single(property => property.Name == "ThemeIconOverride/icon") is PropertyDescriptor<Control, Texture?> &&
            properties.Single(property => property.Name == "ThemeStyleBoxOverride/skin") is PropertyDescriptor<Control, StyleBox?>, "Override descriptors retain their nullable typed removal representation.");
        foreach (var property in properties.Where(property => property.Name.StartsWith("Theme", StringComparison.Ordinal) && property.Name.Contains("Override/", StringComparison.Ordinal))) copy.RevertProperty(property);
        Check(!copy.HasThemeColorOverride("tint") && !copy.HasThemeConstantOverride("number") && !copy.HasThemeFontSizeOverride("size") && !copy.HasThemeIconOverride("icon") && !copy.HasThemeStyleBoxOverride("skin") &&
            copy.GetThemeConstant("number") == 2 && ReferenceEquals(copy.GetThemeStyleBox("skin"), inherited), "Reverting a nullable override removes its entry and resumes inherited lookup.");
        copy.Dispose(); Check(!theme.IsDisposed && !style.IsDisposed && !icon.IsDisposed, "Disposing a consumer retains its borrowed theme and override resources.");
        using var window = new Window { Theme = theme }; window.AddThemeConstantOverride("number", 8); packed.Pack(window); using var windowCopy = (Window)packed.Instantiate();
        var windowOverride = windowCopy.GetPropertyList().Single(property => property.Name == "ThemeConstantOverride/number");
        Check(windowOverride is PropertyDescriptor<Window, int?> && windowCopy.GetThemeConstant("number") == 8, "Window packing uses equivalent typed nullable override descriptors.");
        windowCopy.RevertProperty(windowOverride); Check(!windowCopy.HasThemeConstantOverride("number"), "Window override reversion also removes the entry.");

        theme.SetConstant("separation", nameof(BoxContainer), 9); using var box = new HBoxContainer { Theme = theme };
        var legacy = box.GetPropertyList().Single(property => property.Name == nameof(BoxContainer.Separation));
        Check(box.Separation == 9 && !legacy.IsStored, "Legacy separation projects inheritance and is not serialized as an independent fixed value.");
        packed.Pack(box); using var boxCopy = (HBoxContainer)packed.Instantiate(); theme.SetConstant("separation", nameof(BoxContainer), 10);
        Check(boxCopy.Separation == 10 && !boxCopy.HasThemeConstantOverride("separation"), "Packing an inherited separation does not freeze the value.");
        box.AddThemeConstantOverride("separation", 10); var explicitOverride = box.GetPropertyList().Single(property => property.Name == "ThemeConstantOverride/separation");
        Check(!box.PropertyCanRevert(legacy) && box.PropertyCanRevert(explicitOverride), "An override equal to inheritance is invisible to legacy value reversion but remains explicitly removable through its nullable descriptor.");
        box.RevertProperty(explicitOverride); Check(!box.HasThemeConstantOverride("separation") && box.Separation == 10, "Nullable reversion removes the equal-valued override without changing the effective value.");
        box.Separation = 13; box.RevertProperty(legacy); Check(box.Separation == 10 && !box.HasThemeConstantOverride("separation"), "Legacy reversion restores inheritance when its effective value differs.");
        theme.SetConstant("h_separation", nameof(GridContainer), 6); theme.SetConstant("v_separation", nameof(GridContainer), 8);
        using var grid = new GridContainer { Theme = theme }; packed.Pack(grid); using var gridCopy = (GridContainer)packed.Instantiate();
        theme.SetConstant("h_separation", nameof(GridContainer), 7); theme.SetConstant("v_separation", nameof(GridContainer), 9);
        Check(gridCopy.HSeparation == 7 && gridCopy.VSeparation == 9 && !gridCopy.HasThemeConstantOverride("h_separation") && !gridCopy.HasThemeConstantOverride("v_separation"), "Both grid separation projections retain inheritance across packing.");
    }

    private static void VerifyFallbacksAndDefaults()
    {
        var database = ThemeDB.Service; var defaults = ThemeDB.GetDefaultTheme();
        var baseScale = ThemeDB.FallbackBaseScale; var fontSize = ThemeDB.FallbackFontSize; var icon = ThemeDB.FallbackIcon; var style = ThemeDB.FallbackStyleBox;
        var defaultScale = defaults.DefaultBaseScale; var defaultSize = defaults.DefaultFontSize;
        using var replacement = new StyleBoxEmpty(); using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); using var replacementIcon = ImageTexture.CreateFromImage(image);
        try
        {
            defaults.DefaultBaseScale = 0; defaults.DefaultFontSize = -1; ThemeDB.FallbackBaseScale = 2.5f; ThemeDB.FallbackFontSize = 31;
            ThemeDB.FallbackIcon = replacementIcon; ThemeDB.FallbackStyleBox = replacement;
            var control = new Control(); using var tree = new SceneTree(control); tree.ProcessFrame(0);
            Check(control.GetThemeDefaultBaseScale() == 2.5f && control.GetThemeDefaultFontSize() == 31 && control.GetThemeFontSize("theme_lookup_missing") == 31 &&
                ReferenceEquals(control.GetThemeIcon("theme_lookup_missing"), replacementIcon) && ReferenceEquals(control.GetThemeStyleBox("theme_lookup_missing"), replacement) && !control.HasThemeStyleBox("theme_lookup_missing"),
                "Universal fallbacks participate after missing local and built-in defaults without making Has report an item.");
            var oldSeen = false; control.ThemeChanged += () => oldSeen |= ReferenceEquals(control.GetThemeStyleBox("theme_lookup_missing"), replacement);
            ThemeDB.FallbackStyleBox = null; Check(ReferenceEquals(control.GetThemeStyleBox("theme_lookup_missing"), replacement), "Changing a universal fallback preserves attached cached getters until deferred delivery.");
            tree.FlushDeferred(); Check(oldSeen && control.GetThemeStyleBox("theme_lookup_missing") is null, "Fallback notification preserves old-cache event ordering and then refreshes the result.");
            using var theme = new Theme { DefaultBaseScale = 1.25f, DefaultFontSize = 19 }; control.Theme = theme;
            Check(control.GetThemeDefaultBaseScale() == 1.25f && control.GetThemeDefaultFontSize() == 19 && control.GetThemeFontSize("missing") == 19, "Positive branch defaults override universal fallbacks.");
        }
        finally
        {
            defaults.DefaultBaseScale = defaultScale; defaults.DefaultFontSize = defaultSize; ThemeDB.FallbackBaseScale = baseScale; ThemeDB.FallbackFontSize = fontSize;
            ThemeDB.FallbackIcon = icon; ThemeDB.FallbackStyleBox = style;
        }
    }

    private static void VerifyCallbackReentryAndStoredSchema()
    {
        var control = new Control(); using var tree = new SceneTree(control); tree.ProcessFrame(0);
        var calls = 0; var depth = 0; var maximumDepth = 0;
        Action settle = () =>
        {
            depth++; maximumDepth = Math.Max(maximumDepth, depth); calls++;
            if (calls == 1) control.AddThemeConstantOverride("during_callback", 42);
            depth--;
        };
        control.ThemeChanged += settle; control.AddThemeConstantOverride("trigger", 1); control.ThemeChanged -= settle;
        Check(calls == 2 && maximumDepth == 1 && control.GetThemeConstant("during_callback") == 42, "Reentrant override changes request another notification pass without recursively entering ThemeChanged.");
        var feedbackCalls = 0;
        Action feedback = () => { feedbackCalls++; control.AddThemeConstantOverride("feedback", feedbackCalls); };
        Exception? failure;
        control.ThemeChanged += feedback;
        try { failure = Capture(() => control.AddThemeConstantOverride("trigger", 2)); }
        finally { control.ThemeChanged -= feedback; }
        Check(failure is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Any(error => error is InvalidOperationException && error.Message.Contains("64", StringComparison.Ordinal)) &&
            feedbackCalls == 64 && control.GetThemeConstant("feedback") == 64, "Nonsettling notification feedback stops after 64 committed passes with an explicit error.");
        var recovered = 0; Action recovery = () => recovered++;
        control.ThemeChanged += recovery; control.AddThemeConstantOverride("after_feedback", 123); control.ThemeChanged -= recovery;
        Check(recovered == 1 && control.GetThemeConstant("after_feedback") == 123, "A feedback error releases the notification guard so ordinary later edits recover.");

        control.AddThemeColorOverride("validated", Colors.Red);
        var color = control.GetPropertyList().OfType<PropertyDescriptor<Control, Color?>>().Single(property => property.Name == "ThemeColorOverride/validated");
        Reject<ArgumentException>(() => color.SetValue(control, new Color(float.NaN, 0, 0)));
        Check(control.GetThemeColor("validated") == Colors.Red && control.HasThemeColorOverride("validated"), "The stored descriptor setter applies the same finite-color guard before mutating the override.");
        using var malformed = new MalformedSchemaControl { IncludeMalformed = true }; using var packed = new PackedScene(); packed.Pack(malformed);
        MalformedSchemaControl.LastCreated = null;
        try
        {
            Reject<InvalidOperationException>(() => packed.Instantiate());
            Check(MalformedSchemaControl.LastCreated is { IsDisposed: true }, "A reserved color-override prefix with an integer schema is rejected during reconstruction and its provisional node is disposed.");
        }
        finally { MalformedSchemaControl.LastCreated = null; }
    }

    private static void VerifyWarmLookups()
    {
        using var theme = new Theme(); theme.SetConstant("warm", nameof(Control), 0); var root = new Control { Theme = theme }; var child = new Control { Name = "Child" }; root.AddChild(child);
        using var tree = new SceneTree(root); var events = 0; child.ThemeChanged += () => events++; tree.ProcessFrame(0);
        void Frame(int pass) { theme.SetConstant("warm", nameof(Control), pass + 1); tree.ProcessFrame(0); Check(child.GetThemeConstant("warm") == pass + 1 && child.HasThemeConstant("warm"), "Prepared theme propagation executes actual inherited lookups."); }
        for (var pass = 0; pass < 64; pass++) Frame(pass);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Frame(pass);
        Check(GC.GetAllocatedBytesForCurrentThread() == before && events >= 128, "Warmed theme mutation, deferred descendant refresh and lookups allocate zero managed bytes.");
        before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 1000; pass++) _ = child.GetThemeConstant("warm");
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Repeated cached inherited lookups allocate zero managed bytes.");
    }

    private sealed class LookupControl : Control { }
    private sealed class MalformedSchemaControl : Control
    {
        private static readonly PropertyDescriptor<MalformedSchemaControl, int> WrongColor = new("ThemeColorOverride/invalid", _ => 7, (_, _) => { }, stored: true);
        internal bool IncludeMalformed;
        internal static MalformedSchemaControl? LastCreated;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => IncludeMalformed ? base.GetPropertyDescriptors().Append(WrongColor) : base.GetPropertyDescriptors();
        protected override Func<Node> CreateSceneInstanceFactory() => CreateControl;
        private static Node CreateControl() => LastCreated = new MalformedSchemaControl();
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
