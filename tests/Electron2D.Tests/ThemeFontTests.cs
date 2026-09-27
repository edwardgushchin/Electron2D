using Electron2D;

internal static class ThemeFontTests
{
    internal static void Run()
    {
        VerifyStorageAndCopies();
        VerifyLookupAndLifetime();
        VerifyPacking();
        Console.WriteLine("Theme fonts, defaults, copies, inheritance, overrides, deferred invalidation and packing passed; warm lookup=0 B.");
    }

    private static void VerifyStorageAndCopies()
    {
        using var font = new FontFile(); using var other = new FontFile(); using var theme = new Theme();
        var changes = 0; var lists = 0; theme.Changed += _ => changes++; theme.PropertyListChanged += _ => lists++;
        Check(theme.DefaultFont is null && !theme.HasDefaultFont() && !theme.HasFont("font", "Type"), "Empty themes have no font default or defined font.");
        theme.SetFont("font", "Type", null); theme.SetFont("font", "Type", null);
        Check(changes == 2 && lists == 2 && theme.GetFontList("Type").SequenceEqual(new[] { "font" }) && !theme.HasFont("font", "Type"), "Null slots remain enumerable and repeated placeholder writes notify schema changes.");
        theme.DefaultFont = font; theme.DefaultFont = font;
        Check(changes == 3 && lists == 2 && theme.HasDefaultFont() && theme.HasThemeItem(Theme.DataType.Font, "missing", "Other") && ReferenceEquals(theme.GetFont("font", "Type"), font),
            "A nonnull default satisfies any font query, while equal default assignment is silent.");
        theme.SetFont("font", "Type", other); theme.SetFont("alias", "Type", other);
        Check(ReferenceEquals(theme.GetFont("font", "Type"), other), "Explicit fonts precede the local default.");
        changes = lists = 0; other.EmitChanged(); Check(changes == 1 && lists == 0, "Aliased font slots use one change subscription.");
        theme.RenameThemeItem(Theme.DataType.Font, "alias", "renamed", "Type"); theme.ClearThemeItem(Theme.DataType.Font, "renamed", "Type");
        Check(theme.GetThemeItemList(Theme.DataType.Font, "Type").SequenceEqual(new[] { "font" }) && theme.GetThemeItemTypeList(Theme.DataType.Font).Contains("Type"), "Generic operations execute the font category.");
        theme.SetFont("null", "Type", null); theme.SetFont("same_default", "Type", font);
        var placeholder = theme.GetPropertyList().OfType<PropertyDescriptor<Theme, Font?>>().Single(property => property.Name == "Type/fonts/null");
        Check(placeholder.GetValue(theme) is null, "Stored font descriptors preserve null rather than serializing the resolved default.");
        using var shallow = (Theme)theme.Duplicate(); using var deep = (Theme)theme.Duplicate(true);
        using var deepDefault = deep.DefaultFont!; using var deepOther = deep.GetFont("font", "Type")!;
        Check(ReferenceEquals(shallow.DefaultFont, font) && ReferenceEquals(shallow.GetFont("font", "Type"), other), "Shallow copies borrow both default and item fonts.");
        Check(!ReferenceEquals(deepDefault, font) && !ReferenceEquals(deepOther, other) && ReferenceEquals(deepDefault, deep.GetFont("same_default", "Type")),
            "Deep copies preserve aliases between the default and font slots within an independent resource graph.");
        using var overlay = new Theme(); overlay.SetFont("font", "Type", null); theme.MergeWith(overlay);
        Check(ReferenceEquals(theme.DefaultFont, font) && ReferenceEquals(theme.GetFont("font", "Type"), font), "Merge preserves an absent source default while null slots overwrite explicit fonts.");
        overlay.DefaultFont = other; theme.MergeWith(overlay); Check(ReferenceEquals(theme.DefaultFont, other), "Merge replaces a defined default font.");
        theme.Clear(); changes = 0; other.EmitChanged();
        Check(changes == 1 && theme.HasDefaultFont() && theme.GetFontTypeList().Length == 0 && !other.IsDisposed, "Clear releases slots but preserves default identity and its live subscription.");
        theme.DefaultFont = null; changes = 0; other.EmitChanged(); Check(changes == 0, "Clearing the default releases its final subscription.");
        using var dead = new FontFile(); dead.Dispose(); Reject<ObjectDisposedException>(() => theme.DefaultFont = dead);
        Reject<ObjectDisposedException>(() => theme.SetFont("dead", "NeverCreated", dead)); Check(!theme.GetFontTypeList().Contains("NeverCreated"), "Rejected disposed fonts do not create category records.");
        theme.DefaultFont = font; theme.SetFont("borrowed", "Type", font); changes = 0; font.Dispose();
        Check(changes == 1 && ReferenceEquals(theme.DefaultFont, font) && ReferenceEquals(theme.GetFont("borrowed", "Type"), font), "Disposed borrowed fonts retain identity and emit one invalidation for all aliases.");
        Reject<ObjectDisposedException>(() => theme.Duplicate());
    }

    private static void VerifyLookupAndLifetime()
    {
        using var inherited = new FontFile(); using var assigned = new FontFile(); using var overridden = new FontFile();
        using var theme = new Theme { DefaultFont = inherited }; using var local = new Theme(); local.SetFont("font", nameof(Control), assigned);
        var root = new Control { Theme = theme }; var child = new Control { Name = "Child", Theme = local }; root.AddChild(child);
        using var tree = new SceneTree(root); tree.ProcessFrame(0);
        Check(ReferenceEquals(child.GetThemeFont("font"), assigned) && ReferenceEquals(child.GetThemeFont("missing"), inherited) &&
            ReferenceEquals(child.GetThemeDefaultFont(), inherited) && child.HasThemeFont("missing"), "Font lookup and default lookup follow the consecutive themed branch.");
        local.DefaultFont = assigned; tree.FlushDeferred();
        Check(ReferenceEquals(child.GetThemeFont("missing", nameof(Window)), assigned) && ReferenceEquals(child.GetThemeDefaultFont(), assigned), "The nearest default font participates even in explicit type queries.");
        var events = 0; child.ThemeChanged += () => events++;
        child.AddThemeFontOverride("font", overridden); Check(events == 1 && child.HasThemeFontOverride("font") && ReferenceEquals(child.GetThemeFont("font"), overridden), "Attached font overrides refresh synchronously.");
        Check(ReferenceEquals(child.GetThemeFont("font", nameof(Window)), assigned), "Local font overrides do not affect another explicit native type.");
        Reject<ArgumentNullException>(() => child.AddThemeFontOverride("font", null!));
        child.BeginBulkThemeOverride(); events = 0; Task.Run(overridden.EmitChanged).GetAwaiter().GetResult(); tree.FlushDeferred();
        Check(events == 0, "A background overridden font mutation respects bulk notification suppression at delivery.");
        child.EndBulkThemeOverride(); Check(events == 1, "Ending the bulk font override emits one refresh.");
        child.RemoveThemeFontOverride("font"); local.DefaultFont = null; local.ClearFont("font", nameof(Control)); tree.FlushDeferred();
        Check(ReferenceEquals(child.GetThemeFont("font"), inherited), "Removing overrides and local entries restores the inherited default.");
        _ = child.GetThemeFont("font"); theme.DefaultFont = assigned;
        Check(ReferenceEquals(child.GetThemeFont("font"), inherited), "Attached font lookup retains its cached value until the resource notification is delivered.");
        tree.FlushDeferred(); Check(ReferenceEquals(child.GetThemeFont("font"), assigned), "Deferred theme notifications invalidate cached font identities.");
        Action fail = () => throw new ApplicationException("expected font theme notification"); child.ThemeChanged += fail; theme.DefaultFont = inherited;
        Reject<AggregateException>(tree.FlushDeferred); child.ThemeChanged -= fail;
        Check(ReferenceEquals(child.GetThemeFont("font"), inherited), "A failing theme observer cannot strand the old font cache.");
        Check(Task.Run(() => Capture(() => child.GetThemeFont("font"))).Result is InvalidOperationException, "Attached font lookups retain owner-thread guards.");
        void Cycle() { inherited.EmitChanged(); tree.FlushDeferred(); _ = child.GetThemeFont("font"); }
        for (var pass = 0; pass < 64; pass++) Cycle();
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Cycle(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Warmed font mutation, deferred propagation and lookup allocated {allocated} bytes.");
        using var window = new Window { Theme = theme }; var windowChild = new Control(); window.AddChild(windowChild);
        window.AddThemeFontOverride("font", overridden);
        Check(ReferenceEquals(window.GetThemeFont("font"), overridden) && ReferenceEquals(window.GetThemeDefaultFont(), inherited) && ReferenceEquals(windowChild.GetThemeFont("font"), inherited),
            "Window font overrides stay local while its theme defaults reach child controls.");
        using var neutral = new Node(); var isolated = new Control(); neutral.AddChild(isolated); root.AddChild(neutral);
        Check(!ReferenceEquals(isolated.GetThemeDefaultFont(), inherited), "An ordinary Node breaks font-theme inheritance.");
    }

    private static void VerifyPacking()
    {
        using var font = new FontFile(); using var theme = new Theme { DefaultFont = font };
        using var control = new Control { Theme = theme }; control.AddThemeFontOverride("font", font);
        using var packed = new PackedScene(); packed.Pack(control); using var copy = (Control)packed.Instantiate();
        Check(ReferenceEquals(copy.Theme, theme) && ReferenceEquals(copy.GetThemeFont("font"), font), "Packing preserves borrowed theme fonts and local font overrides.");
        var descriptor = copy.GetPropertyList().Single(property => property.Name == "ThemeFontOverride/font");
        Check(descriptor is PropertyDescriptor<Control, Font?>, "Font override descriptors retain their typed nullable removal representation.");
        copy.RevertProperty(descriptor); Check(!copy.HasThemeFontOverride("font") && ReferenceEquals(copy.GetThemeFont("font"), font), "Reverting a font override removes the local slot and restores the theme default.");
        using var window = new Window(); window.AddThemeFontOverride("font", font); packed.Pack(window); using var windowCopy = (Window)packed.Instantiate();
        Check(windowCopy.HasThemeFontOverride("font") && ReferenceEquals(windowCopy.GetThemeFont("font"), font), "Window packing restores the sixth typed resource category.");
    }

    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
