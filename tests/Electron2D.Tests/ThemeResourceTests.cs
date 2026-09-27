using Electron2D;

internal static class ThemeResourceTests
{
    internal static void Run()
    {
        VerifyValuesAndPlaceholders();
        VerifyResourceSubscriptions();
        VerifyTypesAndVariations();
        VerifyCopiesAndMerge();
        VerifyErrorsConcurrencyAndAllocations();
        Console.WriteLine("Typed theme values, placeholders, aliases, variations, merge, resource graphs and allocation checks passed.");
    }

    private static void VerifyValuesAndPlaceholders()
    {
        using var theme = new Theme(); var order = new List<string>();
        theme.PropertyListChanged += _ => order.Add("list"); theme.Changed += _ => order.Add("changed");
        Check(theme.DefaultBaseScale == 0 && theme.DefaultFontSize == -1 && !theme.HasDefaultBaseScale() && !theme.HasDefaultFontSize() && theme.GetTypeList().Length == 0,
            "Empty theme defaults do not supply scale or font size overrides.");
        Check(theme.GetColor("missing", "Type") == Colors.Black && theme.GetConstant("missing", "Type") == 0 && !theme.HasColor("missing", "Type") && !theme.HasFontSize("missing", "Type"), "Missing values and presence checks follow category defaults.");
        Check(ReferenceEquals(theme.GetIcon("missing", "Type"), ThemeDB.Instance.FallbackIcon) && ReferenceEquals(theme.GetStyleBox("missing", "Type"), ThemeDB.Instance.FallbackStyleBox) &&
            theme.GetFontSize("missing", "Type") == ThemeDB.Instance.FallbackFontSize, "Missing resources and sizes use live theme-database fallbacks.");
        theme.DefaultBaseScale = 0; theme.DefaultFontSize = -1; Check(order.Count == 0, "Equal default assignments are silent.");
        theme.DefaultBaseScale = -2; theme.DefaultFontSize = 18;
        Check(theme.DefaultBaseScale == -2 && !theme.HasDefaultBaseScale() && theme.HasDefaultFontSize() && theme.HasFontSize("missing", "Type") && theme.GetFontSize("missing", "Type") == 18, "Signed defaults are retained and only positive values participate in fallback.");
        theme.SetFontSize("size", "Type", 0); Check(theme.GetFontSize("size", "Type") == 18 && theme.GetFontSizeList("Type").SequenceEqual(new[] { "size" }), "Nonpositive font-size slots remain listed and resolve through local defaults.");
        theme.DefaultFontSize = -1; Check(!theme.HasFontSize("size", "Type") && theme.GetFontSize("size", "Type") == ThemeDB.Instance.FallbackFontSize, "Nonpositive size placeholders do not count as present without a local default.");
        theme.SetFontSize("size", "Type", 22); Check(theme.GetFontSize("size", "Type") == 22 && theme.HasFontSize("size", "Type"), "Positive item size overrides all fallbacks.");
        order.Clear(); theme.SetColor("9_item", "", Colors.Red); Check(order.SequenceEqual(new[] { "list", "changed" }), "A new scalar item publishes property-list changes before Changed.");
        order.Clear(); theme.SetColor("9_item", "", Colors.Red); Check(order.SequenceEqual(new[] { "changed" }), "Equal existing scalar writes still emit Changed without list changes.");
        theme.SetConstant("signed", "Type", -17); Check(theme.GetConstant("signed", "Type") == -17, "Constants retain signed values.");
        order.Clear(); theme.SetIcon("icon", "Type", null); theme.SetIcon("icon", "Type", null);
        Check(order.SequenceEqual(new[] { "list", "changed", "list", "changed" }) && !theme.HasIcon("icon", "Type") && theme.GetIconList("Type").Contains("icon"), "Repeated null resource writes publish list changes and retain a listed absent placeholder.");
        using var style = new StyleBoxEmpty(); theme.SetStyleBox("panel", "Type", null); order.Clear(); theme.SetStyleBox("panel", "Type", style);
        Check(order.SequenceEqual(new[] { "list", "changed" }) && theme.HasStyleBox("panel", "Type"), "Replacing a null style placeholder is a list-changing write.");
        order.Clear(); theme.SetStyleBox("panel", "Type", style); Check(order.SequenceEqual(new[] { "changed" }), "Equal nonnull resource replacement still emits Changed once.");
        order.Clear(); theme.SetStyleBox("panel", "Type", null); Check(order.SequenceEqual(new[] { "changed" }), "Replacing a nonnull resource with null retains the existing slot without a list notification.");
        theme.RenameStyleBox("panel", "renamed", "Type"); Check(theme.GetStyleBoxList("Type").SequenceEqual(new[] { "renamed" }), "Renaming works for null placeholders.");
        theme.ClearStyleBox("renamed", "Type"); Check(theme.GetStyleBoxList("Type").Length == 0 && theme.GetStyleBoxTypeList().Contains("Type"), "Clearing the last item retains its empty category type.");
        foreach (var dataType in new[] { Theme.DataType.Color, Theme.DataType.Constant, Theme.DataType.FontSize, Theme.DataType.Icon, Theme.DataType.StyleBox })
        {
            Check(theme.GetThemeItemTypeList(dataType) is not null, "Every supported typed category executes its list query.");
            Check(theme.GetThemeItemList(dataType, "missing").Length == 0, "Absent category types return empty item snapshots.");
        }
        theme.RenameThemeItem(Theme.DataType.Color, "9_item", "new_color", ""); Check(theme.HasThemeItem(Theme.DataType.Color, "new_color", ""), "Generic rename and presence dispatch to a real typed family.");
        theme.ClearThemeItem(Theme.DataType.Color, "new_color", ""); Check(!theme.HasColor("new_color", ""), "Generic clear removes the actual typed entry.");
    }

    private static void VerifyResourceSubscriptions()
    {
        using var theme = new Theme(); using var style = new StyleBoxLine(); var changes = 0; theme.Changed += _ => changes++;
        theme.SetStyleBox("first", "Type", style); theme.SetStyleBox("second", "Type", style); changes = 0;
        style.Thickness = 2; Check(changes == 1, "One shared resource change forwards once even when stored under multiple keys.");
        theme.ClearStyleBox("first", "Type"); changes = 0; style.Thickness = 3; Check(changes == 1, "Removing one alias preserves the remaining subscription.");
        theme.RenameStyleBox("second", "renamed", "Type"); changes = 0; style.Thickness = 4; Check(changes == 1, "Renaming preserves one resource subscription.");
        theme.ClearStyleBox("renamed", "Type"); changes = 0; style.Thickness = 5; Check(changes == 0, "Removing the final alias releases the subscription.");
        using var image = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); using var texture = ImageTexture.CreateFromImage(image);
        theme.SetIcon("a", "Type", texture); theme.SetIcon("b", "Other", texture); changes = 0;
        image.Fill(Colors.Red); texture.Update(image); Check(changes == 1, "Icon aliases also share one forwarded texture change.");
        theme.SetStyleBox("dead", "Type", style); changes = 0; style.Dispose();
        Check(changes == 1 && theme.HasStyleBox("dead", "Type") && ReferenceEquals(theme.GetStyleBox("dead", "Type"), style), "Explicit disposal notifies observers while retaining the borrowed identity.");
        Reject<ObjectDisposedException>(() => theme.SetStyleBox("invalid", "NewType", style)); Check(!theme.GetStyleBoxTypeList().Contains("NewType"), "Rejected disposed-resource insertion creates no empty type.");
        theme.Dispose(); changes = 0; image.Fill(Colors.Blue); texture.Update(image);
        Check(changes == 0 && !texture.IsDisposed, "Theme disposal releases subscriptions without disposing borrowed resources.");
    }

    private static void VerifyTypesAndVariations()
    {
        using var theme = new Theme(); var changes = 0; var lists = 0; theme.Changed += _ => changes++; theme.PropertyListChanged += _ => lists++;
        theme.AddType("Empty"); theme.AddType("Empty");
        Check(changes == 2 && lists == 2 && theme.GetTypeList().Count(name => name == "Empty") == 1 && theme.GetIconTypeList().Contains("Empty") && theme.GetColorTypeList().Contains("Empty") && theme.GetFontSizeTypeList().Contains("Empty"), "AddType creates every supported category record and notifies even for an existing type.");
        changes = lists = 0; theme.RemoveType("Empty"); Check(changes == 3 && lists == 3 && theme.GetTypeList().Length == 0, "Removing a complete supported type preserves separate icon, style and final notification phases.");
        theme.SetTypeVariation("Dense", "Control"); theme.SetTypeVariation("Deeper", "Dense"); theme.SetTypeVariation("Peer", "Control");
        Check(theme.GetTypeVariationList("Control").SequenceEqual(new[] { "Dense", "Deeper", "Peer" }) && theme.IsTypeVariation("Deeper", "Dense") && !theme.IsTypeVariation("Deeper", "Control"), "Variation lists recurse in child order while direct relationship tests do not follow ancestors.");
        theme.SetTypeVariation("Dense", "Control"); Check(theme.GetTypeVariationList("Control").SequenceEqual(new[] { "Peer", "Dense", "Deeper" }), "Equal variation replacement moves its link to the end of its base's child order.");
        theme.SetTypeVariation("CycleA", "CycleB"); theme.SetTypeVariation("CycleB", "CycleA"); theme.SetTypeVariation("Self", "Self");
        Check(theme.GetTypeVariationList("CycleA").SequenceEqual(new[] { "CycleB", "CycleA" }) && theme.GetTypeVariationList("Self").SequenceEqual(new[] { "Self" }), "Stored cycles and self-links have bounded unique variation-list traversal.");
        theme.ClearTypeVariation("CycleA"); Check(theme.GetTypeVariationBase("CycleA") == "" && theme.IsTypeVariation("CycleB", "CycleA"), "Clearing a variation removes only its direct link.");
        theme.SetColor("a", "Old", Colors.Red); theme.SetConstant("x", "Old", 1); theme.SetColor("b", "New", Colors.Blue);
        theme.SetTypeVariation("Child", "Old"); theme.SetTypeVariation("Grandchild", "Child"); theme.SetColor("local", "Child", Colors.Green);
        theme.RenameType("Old", "New");
        Check(theme.HasColor("a", "Old") && theme.HasColor("b", "New") && !theme.HasConstant("x", "Old") && theme.GetConstant("x", "New") == 1 &&
            theme.IsTypeVariation("Child", "New") && theme.IsTypeVariation("Grandchild", "New"), "Type rename skips category collisions and flattens descendant variation links onto the new base.");
        theme.RemoveType("New"); Check(theme.GetTypeVariationBase("Child") == "" && theme.GetTypeVariationBase("Grandchild") == "" && theme.GetColor("local", "Child") == Colors.Green && theme.GetColor("a", "Old") == Colors.Red, "Removing a base clears descendant links while preserving their independently stored items and collision-surviving records.");
        theme.SetTypeVariation("Custom", "Control"); theme.RenameType("Custom", "Control"); Check(theme.GetTypeVariationBase("Custom") == "" && theme.GetTypeVariationBase("Control") == "", "Renaming a variation onto a native type removes the invalid variation relationship.");
        Reject<ArgumentException>(() => theme.SetTypeVariation("Control", "Other")); Reject<ArgumentException>(() => theme.SetTypeVariation("Custom", ""));
    }

    private static void VerifyCopiesAndMerge()
    {
        using var source = new Theme { DefaultBaseScale = -2, DefaultFontSize = -3 }; using var style = new StyleBoxLine { Thickness = 7 };
        source.AddType("Empty"); source.SetFontSize("raw", "Type", -8); source.SetStyleBox("a", "Type", style); source.SetStyleBox("b", "Type", style); source.SetIcon("null_icon", "Type", null);
        source.SetTypeVariation("First", "Control"); source.SetTypeVariation("Second", "Control"); source.SetTypeVariation("First", "Control");
        using var shallow = (Theme)source.Duplicate();
        Check(ReferenceEquals(shallow.GetStyleBox("a", "Type"), style) && shallow.GetTypeList().Contains("Empty") && shallow.DefaultBaseScale == -2 && shallow.DefaultFontSize == -3 &&
            shallow.GetTypeVariationList("Control").SequenceEqual(new[] { "Second", "First" }), "Shallow duplication copies empty records, signed defaults and variation order while borrowing resources.");
        using var deep = (Theme)source.Duplicate(true); using var deepStyle = deep.GetStyleBox("a", "Type")!;
        Check(!ReferenceEquals(deepStyle, style) && ReferenceEquals(deepStyle, deep.GetStyleBox("b", "Type")) && deepStyle is StyleBoxLine { Thickness: 7 }, "Deep theme copying preserves resource alias identity within an independent graph.");
        var raw = source.GetPropertyList().OfType<PropertyDescriptor<Theme, int>>().Single(property => property.Name == "Type/font_sizes/raw");
        Check(raw.GetValue(source) == -8, "Typed theme descriptors preserve raw nonpositive font-size entries rather than resolved fallbacks.");
        var icon = source.GetPropertyList().OfType<PropertyDescriptor<Theme, Texture?>>().Single(property => property.Name == "Type/icons/null_icon");
        Check(icon.GetValue(source) is null, "Typed resource descriptors preserve explicit null placeholders.");
        using var target = new Theme { DefaultBaseScale = 2, DefaultFontSize = 24 }; target.SetStyleBox("a", "Type", style); target.SetColor("keep", "Type", Colors.Blue);
        using var overlay = new Theme { DefaultBaseScale = -1, DefaultFontSize = 0 }; overlay.AddType("OnlyEmpty"); overlay.SetStyleBox("a", "Type", null); overlay.SetConstant("new", "Type", 9);
        var changes = 0; var lists = 0; target.Changed += _ => changes++; target.PropertyListChanged += _ => lists++;
        target.MergeWith(null); Check(changes == 0 && lists == 0, "Null merge is silent."); target.MergeWith(overlay);
        Check(changes == 1 && lists == 1 && target.DefaultBaseScale == 2 && target.DefaultFontSize == 24 && !target.HasStyleBox("a", "Type") && target.GetConstant("new", "Type") == 9 && target.HasColor("keep", "Type") && !target.GetTypeList().Contains("OnlyEmpty"), "Merge overlays placeholders/items in one batch, ignores empty category types and keeps defaults when the source override is nonpositive.");
        changes = 0; target.CopyFromResource(source); Check(changes == 1 && target.DefaultBaseScale == -2 && target.DefaultFontSize == -3 && target.GetTypeList().Contains("Empty") && !target.HasColor("keep", "Type"), "Resource copy replaces the complete theme and coalesces Changed.");
        changes = lists = 0; target.MergeWith(target); Check(changes == 1 && lists == 1, "Self-merge uses a finite snapshot and one notification batch.");
        target.DefaultBaseScale = 3; target.DefaultFontSize = 20; target.Clear(); Check(target.GetTypeList().Length == 0 && target.DefaultBaseScale == 3 && target.DefaultFontSize == 20 && !style.IsDisposed, "Clear removes maps/variations while preserving defaults and borrowed resources.");
        source.ResourceLocalToScene = true; using var consumer = new ThemeConsumer { Theme = source }; using var packed = new PackedScene(); packed.Pack(consumer);
        var instance = (ThemeConsumer)packed.Instantiate(); var local = instance.Theme!; var localStyle = local.GetStyleBox("a", "Type")!;
        Check(!ReferenceEquals(local, source) && !ReferenceEquals(localStyle, style) && ReferenceEquals(localStyle, local.GetStyleBox("b", "Type")), "Packed scenes localize a theme and its built-in borrowed graph while preserving aliases.");
        instance.Dispose(); Check(local.IsDisposed && localStyle.IsDisposed && !source.IsDisposed && !style.IsDisposed, "The instance owns localized copies and leaves source resources alive.");
    }

    private static void VerifyErrorsConcurrencyAndAllocations()
    {
        using var theme = new Theme();
        Reject<ArgumentException>(() => theme.SetColor("", "Type", Colors.Red)); Reject<ArgumentException>(() => theme.SetColor("dash-name", "Type", Colors.Red));
        Reject<ArgumentException>(() => theme.SetConstant("item", "Тема", 1)); Reject<ArgumentException>(() => theme.SetColor("item", "Type", new(float.NaN, 0, 0)));
        Reject<ArgumentException>(() => theme.DefaultBaseScale = float.PositiveInfinity); Reject<ArgumentNullException>(() => theme.GetConstant(null!, "Type"));
        Reject<ArgumentException>(() => theme.ClearConstant("missing", "Type")); theme.SetConstant("item", "Type", 1);
        Reject<ArgumentException>(() => theme.RenameConstant("item", "item", "Type")); Reject<ArgumentException>(() => theme.RenameConstant("missing", "new", "Type"));
        Check(theme.GetColor("invalid-name", "invalid/type") == Colors.Black, "Queries accept arbitrary nonnull missing keys without mutation-name validation.");
        Reject<NotSupportedException>(() => theme.HasThemeItem(Theme.DataType.Font, "font", "Type")); Reject<NotSupportedException>(() => theme.GetThemeItemList(Theme.DataType.Font, "Type"));
        Reject<NotSupportedException>(() => theme.GetThemeItemTypeList(Theme.DataType.Font)); Reject<NotSupportedException>(() => theme.ClearThemeItem(Theme.DataType.Font, "font", "Type"));
        Reject<NotSupportedException>(() => theme.RenameThemeItem(Theme.DataType.Font, "old", "new", "Type")); Reject<ArgumentOutOfRangeException>(() => theme.GetThemeItemTypeList(Theme.DataType.Max));
        var delivered = 0; theme.Changed += _ => delivered++;
        Action<ElectronObject> failedList = _ => throw new ApplicationException("expected theme list failure"); theme.PropertyListChanged += failedList;
        Reject<ApplicationException>(() => theme.SetColor("new", "Type", Colors.Red)); theme.PropertyListChanged -= failedList;
        Check(delivered == 1 && theme.GetColor("new", "Type") == Colors.Red, "A property-list observer failure does not skip Changed or roll back committed state.");
        Action<Resource> outsideLock = _ => Check(Task.Run(() => theme.GetConstant("item", "Type")).Wait(TimeSpan.FromSeconds(2)), "Theme callbacks run outside the data lock.");
        theme.Changed += outsideLock; theme.SetConstant("item", "Type", 2); theme.Changed -= outsideLock;
        Parallel.For(0, 256, index => { theme.SetConstant("item", "Type", index); var value = theme.GetConstant("item", "Type"); Check(value is >= 0 and < 256, "Concurrent typed updates and reads remain valid."); });
        using var style = new StyleBoxLine(); theme.SetStyleBox("style", "Type", style); theme.SetColor("color", "Type", Colors.Red); theme.SetFontSize("size", "Type", 10);
        void Update(int value)
        {
            theme.SetConstant("item", "Type", value); theme.SetColor("color", "Type", value % 2 == 0 ? Colors.Red : Colors.Blue);
            theme.SetFontSize("size", "Type", value + 1); theme.SetStyleBox("style", "Type", style); style.Thickness = value % 2 + 1;
            Check(theme.GetConstant("item", "Type") == value && theme.HasStyleBox("style", "Type"), "Prepared updates execute all typed hot paths.");
        }
        for (var pass = 0; pass < 64; pass++) Update(pass);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Update(pass);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed existing-key updates, equal resource assignment and forwarded changes allocate zero managed bytes.");
        theme.Dispose(); Reject<ObjectDisposedException>(() => theme.GetColor("color", "Type")); Reject<ObjectDisposedException>(() => theme.SetConstant("item", "Type", 1)); Reject<ObjectDisposedException>(theme.Clear);
    }

    private sealed class ThemeConsumer : Node
    {
        private static readonly PropertyDescriptor<ThemeConsumer, Theme?> ThemeProperty = new(nameof(Theme), node => node.Theme, (node, value) => node.Theme = value, _ => null, stored: true);
        internal Theme? Theme { get; set; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(ThemeProperty);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateConsumer;
        private static Node CreateConsumer() => new ThemeConsumer();
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
