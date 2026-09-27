using Electron2D;

internal static class LabelSettingsTests
{
    internal static void Run()
    {
        VerifyDefaultsAndChanges();
        VerifyStackOperations();
        VerifyGuardsAndDescriptors();
        VerifyFontCopiesAndLifetime();
        VerifyConcurrencyAndAllocations();
        Console.WriteLine("Label settings defaults, stacked effects, typed storage, font ownership and allocation checks passed.");
    }

    private static void VerifyDefaultsAndChanges()
    {
        using var settings = new LabelSettings(); var changes = 0; var lists = 0;
        settings.Changed += _ => changes++; settings.PropertyListChanged += _ => lists++;
        Check(settings.LineSpacing == 3 && settings.ParagraphSpacing == 0 && settings.Font is null && settings.FontSize == 16 && settings.FontColor == Colors.White &&
            settings.OutlineSize == 0 && settings.OutlineColor == Colors.White && settings.ShadowSize == 1 && settings.ShadowColor == Colors.Transparent && settings.ShadowOffset == Vector2.One &&
            settings.StackedOutlineCount == 0 && settings.StackedShadowCount == 0, "Label settings expose the complete pinned scalar and empty-stack defaults.");
        settings.LineSpacing = 3; settings.ParagraphSpacing = 0; settings.Font = null; settings.FontSize = 16; settings.FontColor = Colors.White;
        settings.OutlineSize = 0; settings.OutlineColor = Colors.White; settings.ShadowSize = 1; settings.ShadowColor = Colors.Transparent; settings.ShadowOffset = Vector2.One;
        settings.StackedOutlineCount = 0; settings.StackedShadowCount = 0;
        Check(changes == 0 && lists == 0, "Every scalar and count setter suppresses equal assignments.");
        settings.LineSpacing = -3.5f; settings.ParagraphSpacing = -2.5f; settings.FontSize = -10; settings.FontColor = Colors.Red;
        settings.OutlineSize = -4; settings.OutlineColor = Colors.Blue; settings.ShadowSize = -6; settings.ShadowColor = Colors.Green; settings.ShadowOffset = new(-2, 3);
        Check(changes == 9 && lists == 0 && settings.FontSize == -10 && settings.OutlineSize == -4 && settings.ShadowSize == -6 && settings.LineSpacing == -3.5f,
            "Signed sizes and spacing are stored without editor-hint clamping, and ordinary edits only emit Changed.");
        var order = new List<string>(); settings.PropertyListChanged += _ => order.Add("list"); settings.Changed += _ => order.Add("changed");
        settings.AddStackedOutline(); Check(order.SequenceEqual(new[] { "list", "changed" }), "Structural changes notify the property list before Changed.");
        order.Clear(); settings.SetStackedOutlineSize(0, 5); Check(order.SequenceEqual(new[] { "changed" }), "Layer value edits do not rebuild the property schema.");
        order.Clear(); settings.SetStackedOutlineSize(0, 5); settings.SetStackedOutlineColor(0, Colors.Black); Check(order.Count == 0, "Equal layer assignments are silent.");
    }

    private static void VerifyStackOperations()
    {
        using var settings = new LabelSettings { StackedOutlineCount = 3, StackedShadowCount = 3 };
        for (var index = 0; index < 3; index++)
        {
            Check(settings.GetStackedOutlineSize(index) == 0 && settings.GetStackedOutlineColor(index) == Colors.Black && settings.GetStackedShadowColor(index) == Colors.Black &&
                settings.GetStackedShadowOffset(index) == Vector2.One && settings.GetStackedShadowOutlineSize(index) == 0, "New stack elements use opaque black, zero sizes and one-unit shadow offsets.");
            settings.SetStackedOutlineSize(index, (index + 1) * 11); settings.SetStackedShadowOffset(index, new((index + 1) * 10, index + 1));
            settings.SetStackedShadowOutlineSize(index, index + 1);
        }
        settings.MoveStackedOutline(0, 3);
        Check(OutlineSizes(settings).SequenceEqual(new[] { 22, 33, 11 }), "An insertion position equal to Count moves an outline to the end.");
        settings.MoveStackedOutline(2, 0); Check(OutlineSizes(settings).SequenceEqual(new[] { 11, 22, 33 }), "Moving to an earlier insertion position retains layer order.");
        var changes = 0; var lists = 0; settings.Changed += _ => changes++; settings.PropertyListChanged += _ => lists++;
        settings.MoveStackedOutline(1, 1); settings.MoveStackedOutline(1, 2);
        Check(OutlineSizes(settings).SequenceEqual(new[] { 11, 22, 33 }) && changes == 2 && lists == 2, "Same and immediately following insertion positions preserve order but still emit structural events.");
        settings.MoveStackedShadow(0, 2);
        Check(settings.GetStackedShadowOffset(0).X == 20 && settings.GetStackedShadowOffset(1).X == 10 && settings.GetStackedShadowOutlineSize(1) == 1,
            "Shadow moves use pre-removal insertion positions and move all fields together.");
        settings.MoveStackedShadow(2, 0); Check(settings.GetStackedShadowOffset(0).X == 30 && settings.GetStackedShadowOffset(2).X == 10, "Shadow moves also support earlier destinations.");
        settings.StackedOutlineCount = 1; settings.StackedOutlineCount = 3;
        Check(OutlineSizes(settings).SequenceEqual(new[] { 11, 0, 0 }), "Shrinking discards removed outline values instead of resurrecting capacity contents.");
        settings.StackedShadowCount = 0; settings.AddStackedShadow(-20); settings.AddStackedShadow(int.MinValue);
        Check(settings.StackedShadowCount == 2 && settings.GetStackedShadowOffset(0) == Vector2.One && settings.GetStackedShadowColor(1) == Colors.Black, "Every negative insertion index appends a fresh default shadow.");
        settings.AddStackedOutline(0); Check(settings.GetStackedOutlineSize(0) == 0 && settings.GetStackedOutlineSize(1) == 11, "Explicit insertion shifts later outlines.");
        settings.RemoveStackedOutline(0); settings.RemoveStackedShadow(1); settings.SetStackedOutlineSize(0, -7); settings.SetStackedShadowOutlineSize(0, -9);
        Check(settings.StackedOutlineCount == 3 && settings.StackedShadowCount == 1 && settings.GetStackedOutlineSize(0) == -7 && settings.GetStackedShadowOutlineSize(0) == -9,
            "Removal and signed layer sizes remain executable.");
    }

    private static void VerifyGuardsAndDescriptors()
    {
        using var settings = new LabelSettings { StackedOutlineCount = 1, StackedShadowCount = 1 }; var changes = 0; settings.Changed += _ => changes++;
        Reject<ArgumentOutOfRangeException>(() => settings.StackedOutlineCount = -1); Reject<ArgumentOutOfRangeException>(() => settings.StackedShadowCount = -1);
        Reject<ArgumentOutOfRangeException>(() => settings.AddStackedOutline(2)); Reject<ArgumentOutOfRangeException>(() => settings.AddStackedShadow(2));
        Reject<ArgumentOutOfRangeException>(() => settings.MoveStackedOutline(-1, 0)); Reject<ArgumentOutOfRangeException>(() => settings.MoveStackedOutline(0, 2));
        Reject<ArgumentOutOfRangeException>(() => settings.MoveStackedShadow(0, -1)); Reject<ArgumentOutOfRangeException>(() => settings.RemoveStackedShadow(1));
        Reject<ArgumentOutOfRangeException>(() => settings.GetStackedOutlineColor(1)); Reject<ArgumentOutOfRangeException>(() => settings.SetStackedShadowOutlineSize(1, 3));
        Reject<ArgumentException>(() => settings.LineSpacing = float.NaN); Reject<ArgumentException>(() => settings.ParagraphSpacing = float.PositiveInfinity);
        Reject<ArgumentException>(() => settings.FontColor = new(float.NaN, 0, 0)); Reject<ArgumentException>(() => settings.OutlineColor = new(0, float.NaN, 0));
        Reject<ArgumentException>(() => settings.ShadowColor = new(0, 0, float.NaN)); Reject<ArgumentException>(() => settings.ShadowOffset = new(float.NaN, 0));
        Reject<ArgumentException>(() => settings.SetStackedOutlineColor(0, new(0, 0, float.PositiveInfinity)));
        Reject<ArgumentException>(() => settings.SetStackedShadowColor(0, new(float.NaN, 0, 0)));
        Reject<ArgumentException>(() => settings.SetStackedShadowOffset(0, new(0, float.NegativeInfinity)));
        Check(changes == 0 && settings.LineSpacing == 3 && settings.StackedOutlineCount == 1 && settings.StackedShadowCount == 1, "Invalid edits preserve state and notifications.");
        var properties = settings.GetPropertyList();
        var outline = properties.OfType<PropertyDescriptor<LabelSettings, Color>>().Single(property => property.Name == "StackedOutline[0].Color");
        var shadow = properties.OfType<PropertyDescriptor<LabelSettings, Vector2>>().Single(property => property.Name == "StackedShadow[0].Offset");
        outline.SetValue(settings, Colors.Red); shadow.SetValue(settings, new(4, 5));
        Check(settings.PropertyCanRevert(outline) && settings.PropertyCanRevert(shadow), "Typed indexed layer descriptors support field-specific defaults.");
        settings.RevertProperty(outline); settings.RevertProperty(shadow);
        Check(settings.GetStackedOutlineColor(0) == Colors.Black && settings.GetStackedShadowOffset(0) == Vector2.One, "Indexed reversion restores the actual layer defaults.");
        Action<ElectronObject> fail = _ => throw new ApplicationException("expected settings property-list failure"); settings.PropertyListChanged += fail; changes = 0;
        Reject<ApplicationException>(() => settings.AddStackedOutline()); settings.PropertyListChanged -= fail;
        Check(changes == 1 && settings.StackedOutlineCount == 2, "A property-list observer failure still publishes Changed for committed structural state.");
        settings.Dispose(); Reject<ObjectDisposedException>(() => _ = settings.FontSize); Reject<ObjectDisposedException>(() => settings.FontSize = 7);
        Reject<ObjectDisposedException>(() => settings.AddStackedShadow()); Reject<ObjectDisposedException>(() => settings.GetStackedOutlineSize(0));
    }

    private static void VerifyFontCopiesAndLifetime()
    {
        using var firstFont = new FontFile(); using var secondFont = new FontFile();
        using var settings = new LabelSettings(); var changes = 0; settings.Changed += _ => changes++;
        settings.Font = firstFont; settings.Font = firstFont; Check(changes == 1, "Equal font assignments do not duplicate subscriptions or notifications.");
        firstFont.EmitChanged(); Check(changes == 2, "Borrowed font changes forward to settings.");
        settings.Font = secondFont; changes = 0; firstFont.EmitChanged(); Check(changes == 0, "Replacing a font releases its old subscription.");
        secondFont.Dispose(); Check(changes == 1 && ReferenceEquals(settings.Font, secondFont), "Font disposal forwards one change while retaining its borrowed identity.");
        Reject<ObjectDisposedException>(() => settings.Font = secondFont); settings.Font = firstFont;
        settings.LineSpacing = -2; settings.ParagraphSpacing = 4; settings.FontSize = 21; settings.FontColor = Colors.Red; settings.OutlineSize = 2; settings.OutlineColor = Colors.Blue;
        settings.ShadowSize = 3; settings.ShadowColor = Colors.Green; settings.ShadowOffset = new(-3, 5); settings.StackedOutlineCount = 1; settings.StackedShadowCount = 1;
        settings.SetStackedOutlineSize(0, 8); settings.SetStackedOutlineColor(0, Colors.Yellow); settings.SetStackedShadowOffset(0, new(9, 10)); settings.SetStackedShadowOutlineSize(0, 4);
        using var shallow = (LabelSettings)settings.Duplicate();
        Check(ReferenceEquals(shallow.Font, firstFont) && shallow.LineSpacing == -2 && shallow.ParagraphSpacing == 4 && shallow.FontSize == 21 && shallow.FontColor == Colors.Red &&
            shallow.OutlineSize == 2 && shallow.OutlineColor == Colors.Blue && shallow.ShadowSize == 3 && shallow.ShadowColor == Colors.Green && shallow.ShadowOffset == new Vector2(-3, 5) &&
            shallow.GetStackedOutlineSize(0) == 8 && shallow.GetStackedOutlineColor(0) == Colors.Yellow && shallow.GetStackedShadowOffset(0) == new Vector2(9, 10) && shallow.GetStackedShadowOutlineSize(0) == 4,
            "Shallow copying preserves every settings family and borrows its font.");
        shallow.SetStackedOutlineSize(0, 99); Check(settings.GetStackedOutlineSize(0) == 8, "Layer containers are independently mutable after shallow copying.");
        using var deep = (LabelSettings)settings.Duplicate(true); using var deepFont = deep.Font!;
        Check(deepFont.GetType() == typeof(FontFile) && !ReferenceEquals(deepFont, firstFont), "Deep settings duplication follows the actual font resource graph.");
        using var copied = new LabelSettings(); var copyChanges = 0; copied.Changed += _ => copyChanges++; copied.CopyFromResource(settings);
        Check(copyChanges == 1 && ReferenceEquals(copied.Font, firstFont) && copied.GetStackedOutlineSize(0) == 8 && copied.GetStackedShadowOutlineSize(0) == 4, "CopyFromResource replaces complete settings in one Changed batch.");
        settings.ResourceLocalToScene = true; shallow.ResourceLocalToScene = true;
        using var consumer = new SettingsConsumer { First = settings, Second = shallow }; using var packed = new PackedScene(); packed.Pack(consumer);
        var instance = (SettingsConsumer)packed.Instantiate(); var localFirst = instance.First!; var localSecond = instance.Second!; var localFont = localFirst.Font!;
        Check(!ReferenceEquals(localFirst, settings) && !ReferenceEquals(localSecond, shallow) && !ReferenceEquals(localFont, firstFont) && ReferenceEquals(localFont, localSecond.Font),
            "Scene-local settings duplicate independently while a common built-in font preserves alias identity within the scene graph.");
        instance.Dispose(); Check(localFirst.IsDisposed && localSecond.IsDisposed && localFont.IsDisposed && !settings.IsDisposed && !firstFont.IsDisposed, "Scene ownership releases localized settings/font copies without owning source resources.");
        settings.Dispose(); Check(!firstFont.IsDisposed, "Disposing settings never disposes a borrowed font.");
    }

    private static void VerifyConcurrencyAndAllocations()
    {
        using var settings = new LabelSettings { StackedOutlineCount = 2, StackedShadowCount = 2 };
        Action<Resource> outsideLock = _ => Check(Task.Run(() => settings.FontSize).Wait(TimeSpan.FromSeconds(2)), "Settings observers execute outside the resource lock.");
        settings.Changed += outsideLock; settings.FontSize = 17; settings.Changed -= outsideLock;
        Parallel.For(0, 256, index => { settings.FontSize = index; settings.SetStackedOutlineSize(index % 2, index); Check(settings.GetStackedOutlineSize(index % 2) >= 0, "Concurrent indexed edits remain valid."); });
        void Update(int value)
        {
            settings.LineSpacing = value % 2; settings.ParagraphSpacing = -value; settings.FontSize = value; settings.FontColor = value % 2 == 0 ? Colors.Red : Colors.Blue;
            settings.SetStackedOutlineSize(0, value); settings.SetStackedShadowOffset(0, new(value, -value)); settings.SetStackedShadowColor(0, value % 2 == 0 ? Colors.Green : Colors.White);
            settings.MoveStackedOutline(0, 2); settings.MoveStackedShadow(0, 2);
        }
        for (var pass = 0; pass < 64; pass++) Update(pass);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Update(pass);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed scalar/layer edits and moves allocate zero managed bytes.");
    }

    private static int[] OutlineSizes(LabelSettings settings) => Enumerable.Range(0, settings.StackedOutlineCount).Select(settings.GetStackedOutlineSize).ToArray();
    private sealed class SettingsConsumer : Node
    {
        private static readonly PropertyDescriptor[] Properties =
        [
            new PropertyDescriptor<SettingsConsumer, LabelSettings?>(nameof(First), node => node.First, (node, value) => node.First = value, _ => null, stored: true),
            new PropertyDescriptor<SettingsConsumer, LabelSettings?>(nameof(Second), node => node.Second, (node, value) => node.Second = value, _ => null, stored: true)
        ];
        internal LabelSettings? First { get; set; }
        internal LabelSettings? Second { get; set; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateConsumer;
        private static Node CreateConsumer() => new SettingsConsumer();
    }
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
