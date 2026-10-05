using Electron2D;

internal static class ItemListTests
{
    internal static void Run()
    {
        using var list = new ItemList();
        Check(list.ChildCount == 0 && list.GetChildCount(includeInternal: true) == 2 &&
              list.GetVScrollBar().Parent == list && list.GetHScrollBar().Parent == list &&
              list.FocusMode == FocusMode.All && list.ClipContents,
            "A list owns two real internal bars and starts focused and clipped.");
        Check(list.AddItem("Alpha") == 0 && list.AddItem("Beta") == 1 &&
              list.AddIconItem(null, selectable: false) == 2 && list.ItemCount == 3,
            "Adding text and icon-only items returns their stable indices.");
        list.SetItemText(-1, "Gamma");
        Check(list.GetItemText(2) == "Gamma" && !list.IsItemSelectable(2),
            "Negative setter indices reach the final entry without changing its selection policy.");
        var marker = new object();
        list.SetItemMetadata(0, marker);
        list.SetItemMetadata(1, 42);
        Check(ReferenceEquals(list.GetItemMetadata<object>(0), marker) && list.GetItemMetadata<int>(1) == 42 &&
              !list.TryGetItemMetadata<string>(1, out _) && list.FindMetadata(42) == 1 && list.FindMetadata("42") == -1,
            "Typed metadata retains reference identity and compares only values with an exact stored type.");
        list.Select(1);
        Check(list.Current == 1 && list.GetSelectedItems().SequenceEqual([1]),
            "Single selection makes an eligible item current.");
        list.SelectionMode = ItemList.SelectMode.Multi;
        list.Select(0, single: false);
        Check(list.GetSelectedItems().SequenceEqual([0, 1]),
            "Multi mode retains independent selected items in display order.");
        list.SetItemDisabled(1, true);
        Check(list.IsSelected(1) && list.IsItemDisabled(1),
            "Disabling an item retains an existing selection.");
        list.DeselectAll();
        list.Select(1, single: false);
        Check(!list.IsAnythingSelected() && list.Current == -1,
            "A disabled item cannot become newly selected.");
        list.MoveItem(0, 2);
        Check(list.GetItemText(2) == "Alpha" && list.GetItemText(0) == "Beta",
            "Movement carries item data to its new index.");
        list.SortItemsByText();
        Check(list.GetItemText(0) == "Alpha" && list.GetItemText(1) == "Beta" && list.FindMetadata(marker) == 0,
            "Sorting by text carries typed metadata with the corresponding item.");
        list.ItemCount = 4;
        Check(list.GetItemText(3) == string.Empty && list.IsItemSelectable(3),
            "Resizing upward creates entries with the standard defaults.");
        list.RemoveItem(1);
        list.Clear();
        Check(list.ItemCount == 0 && list.Current == -1,
            "Removing and clearing entries leave an empty model.");
        VerifyVisibleRows();
        VerifyPointerAndKeyboard();
        VerifyColumns();
        VerifyPackedState();
        VerifyIncrementalSearch();
        VerifyMultiSelection();
        VerifyTheme();
        Console.WriteLine("ItemList model, internal bars, selection, mutation, measured rows, columns, input, search and packing passed.");
    }

    private static void VerifyVisibleRows()
    {
        var viewport = new TestViewport();
        var list = new ItemList { Size = new(120, 60) };
        for (var i = 0; i < 8; i++) list.AddItem($"Entry {i}");
        viewport.AddChild(list);
        using var tree = new SceneTree(viewport);
        tree.FlushDeferred();
        var first = list.GetItemRect(0);
        var second = list.GetItemRect(1);
        Check(first.Size.X > 0 && first.Size.Y > 0 && second.Position.Y > first.Position.Y &&
              list.GetVScrollBar().Visible && list.GetVScrollBar().Page > 0,
            "Text shaping gives each row a rectangle and overflow exposes the owned vertical bar.");
        list.SetItemTextDirection(0, TextDirection.RTL);
        list.SetItemLanguage(0, "ar");
        list.SetItemAutoTranslateMode(0, NodeAutoTranslateMode.Disabled);
        list.SetItemTooltip(0, "details");
        Check(list.GetItemTextDirection(0) == TextDirection.RTL && list.GetItemLanguage(0) == "ar" &&
              list.GetItemAutoTranslateMode(0) == NodeAutoTranslateMode.Disabled &&
              list.GetTooltip(first.Position + new Vector2(5, 5)) == "details",
            "Per-item shaping and tooltip settings remain observable on a laid-out list.");
        list.SetItemTooltipEnabled(0, false);
        Check(list.GetTooltip(first.Position + new Vector2(5, 5)) == string.Empty,
            "Disabling one item's tooltip suppresses both explicit and fallback text.");
        using var iconImage = Image.CreateFromData(4, 4, false, Image.Format.Rgba8,
            Enumerable.Repeat(new byte[] { 0, 0, 255, 255 }, 16).SelectMany(static pixel => pixel).ToArray());
        using var icon = ImageTexture.CreateFromImage(iconImage);
        list.SetItemIcon(0, icon);
        list.SetItemIconRegion(0, new(0, 0, 2, 4));
        list.SetItemIconTransposed(0, true);
        list.SetItemIconModulate(0, new(.5f, .5f, 1));
        list.SetItemCustomBGColor(0, new(.2f, .3f, .4f));
        list.SetItemCustomFGColor(0, Colors.Yellow);
        list.IconDisplayMode = ItemList.IconMode.Top;
        Check(ReferenceEquals(list.GetItemIcon(0), icon) && list.GetItemIconRegion(0) == new Rect2(0, 0, 2, 4) &&
              list.IsItemIconTransposed(0) && list.GetItemIconModulate(0) == new Color(.5f, .5f, 1) &&
              list.GetItemCustomBGColor(0) == new Color(.2f, .3f, .4f) && list.GetItemCustomFGColor(0) == Colors.Yellow &&
              list.GetItemRect(0).Size.Y > first.Size.Y,
            "Icon region, transpose, modulation, custom colors and top stacking affect the row model and size.");
        list.AddIconItem(icon);
        var priorIconHeight = list.GetItemRect(list.ItemCount - 1).Size.Y;
        icon.SetSizeOverride(new(4, 12));
        Check(list.GetItemRect(list.ItemCount - 1).Size.Y > priorIconHeight,
            "A shared borrowed icon invalidates every affected item when its logical size changes.");
        icon.Dispose();
        Check(list.GetItemIcon(0) is null && list.GetItemIcon(list.ItemCount - 1) is null,
            "Disposing a shared icon clears every borrowed reference.");
        list.GetVScrollBar().Value = 20;
        var firstAfterIcon = list.GetItemRect(0);
        Check(list.GetVScrollBar().Value == 20 && firstAfterIcon.Position.Y == first.Position.Y &&
              list.GetItemAtPosition(firstAfterIcon.Position + new Vector2(5, firstAfterIcon.Size.Y - 1), exact: true) != 0,
            "The public row rectangle stays in content coordinates while hit testing applies the scroll offset.");
    }

    private static void VerifyPointerAndKeyboard()
    {
        InputMap.LoadFromProjectSettings();
        var viewport = new TestViewport();
        var list = new ItemList { Position = new(10, 10), Size = new(120, 70) };
        for (var i = 0; i < 8; i++) list.AddItem($"Entry {i}");
        viewport.AddChild(list);
        using var tree = new SceneTree(viewport);
        tree.FlushDeferred();
        var selected = 0; var clicked = 0;
        list.ItemSelected += _ => selected++;
        list.ItemClicked += (_, _, _) => clicked++;
        var rect = list.GetItemRect(0);
        using (var press = new InputEventMouseButton { Position = list.Position + rect.Position + new Vector2(5, 5), ButtonIndex = MouseButton.Left, Pressed = true })
            viewport.PushInput(press, inLocalCoordinates: true);
        Check(list.Current == 0 && list.IsSelected(0) && selected == 1 && clicked == 1 && list.HasFocus(),
            "A pointer click selects a visible item, emits both GUI signals and focuses the list.");
        using (var down = new InputEventKey { Keycode = Key.Down, Pressed = true })
            viewport.PushInput(down, inLocalCoordinates: true);
        Check(list.Current == 1 && selected == 2,
            "A focused Down action advances the current selected row.");
        using (var page = new InputEventKey { Keycode = Key.PageDown, Pressed = true })
            viewport.PushInput(page, inLocalCoordinates: true);
        Check(list.Current == 5 && selected == 3,
            "The typed Page Down action advances four rows.");
        using (var wheel = new InputEventMouseButton { Position = new(20, 20), ButtonIndex = MouseButton.WheelDown, Pressed = true })
            viewport.PushInput(wheel, inLocalCoordinates: true);
        Check(list.GetVScrollBar().Value > 0,
            "Wheel input moves the actual internal vertical range.");
    }

    private static void VerifyColumns()
    {
        var viewport = new TestViewport();
        var list = new ItemList { Size = new(120, 80), FixedColumnWidth = 30, MaxColumns = 3 };
        for (var i = 0; i < 9; i++) list.AddItem("A");
        viewport.AddChild(list);
        using var tree = new SceneTree(viewport);
        tree.FlushDeferred();
        var first = list.GetItemRect(0, expand: false);
        var next = list.GetItemRect(1, expand: false);
        var nextRow = list.GetItemRect(3, expand: false);
        Check(next.Position.X > first.Position.X && next.Position.Y == first.Position.Y &&
              nextRow.Position.Y > first.Position.Y,
            "Three columns place adjacent items in one row and advance afterward.");
        list.Size = new(55, 80);
        tree.FlushDeferred();
        Check(list.GetItemRect(1, expand: false).Position.Y > list.GetItemRect(0, expand: false).Position.Y,
            "Narrow wraparound space reduces the row to one column.");
        list.WraparoundItems = false;
        list.ForceUpdateListSize();
        tree.FlushDeferred();
        Check(list.GetHScrollBar().Visible && list.GetHScrollBar().Page > 0,
            $"Disabling wraparound uses the real horizontal bar for wider content; visible={list.GetHScrollBar().Visible}, page={list.GetHScrollBar().Page}, max={list.GetHScrollBar().MaxValue}, size={list.Size}.");
        list.AutoWidth = true;
        list.ForceUpdateListSize();
        tree.FlushDeferred();
        Check(list.GetMinimumSize().X >= 90,
            "Automatic width reports the measured three-column content extent.");
        var rtl = new ItemList { Name = "RTL List", Size = new(120, 60), FixedColumnWidth = 30, MaxColumns = 3, LayoutDirection = LayoutDirection.RTL };
        for (var i = 0; i < 3; i++) rtl.AddItem($"RTL {i}");
        viewport.AddChild(rtl);
        tree.FlushDeferred();
        Check(rtl.GetItemAtPosition(new(100, 8), exact: true) == 0 &&
              rtl.GetItemAtPosition(new(10, 8), exact: true) == 2,
            "RTL hit testing mirrors the drawn three-column order.");
    }

    private static void VerifyPackedState()
    {
        using var image = Image.CreateFromData(1, 1, false, Image.Format.Rgba8, [0, 0, 255, 255]);
        using var icon = ImageTexture.CreateFromImage(image);
        using var source = new ItemList { IconDisplayMode = ItemList.IconMode.Top, MaxColumns = 2 };
        source.AddItem("First", icon);
        source.AddItem("Second", selectable: false);
        source.SetItemDisabled(1, true);
        source.SetItemMetadata(0, "runtime-only");
        source.Select(0);
        using var packed = new PackedScene();
        packed.Pack(source);
        using var copy = (ItemList)packed.Instantiate();
        Check(copy.ItemCount == 2 && copy.GetChildCount() == 0 && copy.GetChildCount(includeInternal: true) == 2 &&
              copy.IconDisplayMode == ItemList.IconMode.Top && copy.MaxColumns == 2 &&
              copy.GetItemText(0) == "First" && ReferenceEquals(copy.GetItemIcon(0), icon) &&
              !copy.IsItemSelectable(1) && copy.IsItemDisabled(1) && !copy.IsAnythingSelected() &&
              !copy.TryGetItemMetadata<string>(0, out _),
            "Typed scene packing restores item count before indexed fields, recreates bars and omits transient selection/metadata.");
    }

    private static void VerifyIncrementalSearch()
    {
        var viewport = new TestViewport();
        var list = new ItemList { Size = new(130, 90) };
        list.AddItem("Apple");
        list.AddItem("Banana");
        list.AddItem("Apricot");
        viewport.AddChild(list);
        using var tree = new SceneTree(viewport);
        list.GrabFocus();
        list.Current = 0;
        using (var key = new InputEventKey { Unicode = 'B', Pressed = true })
            viewport.PushInput(key, inLocalCoordinates: true);
        Check(list.Current == 1 && ProjectSettings.Get(ProjectSettings.IncrementalSearchMaxIntervalMsec) == 2000,
            "Typed Unicode search selects the next matching item with the pinned interval default.");
        list.AllowSearch = false;
        using (var key = new InputEventKey { Unicode = 'A', Pressed = true })
            viewport.PushInput(key, inLocalCoordinates: true);
        Check(list.Current == 1, "Disabling search leaves a focused list's current item unchanged.");
    }

    private static void VerifyMultiSelection()
    {
        var viewport = new TestViewport();
        var list = new ItemList { Position = new(10, 10), Size = new(130, 130), SelectionMode = ItemList.SelectMode.Multi };
        for (var i = 0; i < 5; i++) list.AddItem($"Choice {i}");
        viewport.AddChild(list);
        using var tree = new SceneTree(viewport);
        list.Current = 0;
        list.GrabFocus();
        void Click(int index, bool control = false, bool shift = false)
        {
            var point = list.Position + list.GetItemRect(index).Position + new Vector2(5, 5);
            using var press = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = true, CommandOrControlAutoremap = control, ShiftPressed = shift };
            using var release = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = false, CommandOrControlAutoremap = control, ShiftPressed = shift };
            viewport.PushInput(press, inLocalCoordinates: true);
            viewport.PushInput(release, inLocalCoordinates: true);
        }
        Click(1, control: true);
        Check(list.GetSelectedItems().SequenceEqual([1]) && list.Current == 0,
            "Command/Control-click adds a multi-selection without moving the current anchor.");
        Click(2);
        Check(list.GetSelectedItems().SequenceEqual([2]) && list.Current == 2,
            "A plain multi-mode click replaces the selected set and current item.");
        Click(4, shift: true);
        Check(list.GetSelectedItems().SequenceEqual([2, 3, 4]),
            "Shift-click extends selection through the current-to-clicked range.");
        Click(3, control: true);
        Check(list.GetSelectedItems().SequenceEqual([2, 4]),
            "Control-click on a selected item removes only that item.");
        using (var select = new InputEventKey { Keycode = Key.Space, Pressed = true })
            viewport.PushInput(select, inLocalCoordinates: true);
        Check(list.GetSelectedItems().SequenceEqual([4]),
            "The typed ui_select action toggles the current multi-selection.");
    }

    private static void VerifyTheme()
    {
        var theme = ThemeDB.GetDefaultTheme();
        Check(((StyleBoxFlat)theme.GetStyleBox("panel", "ItemList")!).BGColor == new Color(.1f, .1f, .1f, .6f) &&
              ReferenceEquals(theme.GetStyleBox("focus", "ItemList"), theme.GetStyleBox("focus", "Button")) &&
              ReferenceEquals(theme.GetStyleBox("cursor", "ItemList"), theme.GetStyleBox("focus", "ItemList")) &&
              ReferenceEquals(theme.GetStyleBox("cursor_unfocused", "ItemList"), theme.GetStyleBox("focus", "ItemList")) &&
              theme.GetConstant("h_separation", "ItemList") == 4 && theme.GetConstant("v_separation", "ItemList") == 4 &&
              theme.GetConstant("icon_margin", "ItemList") == 4 && theme.GetConstant("line_separation", "ItemList") == 2 &&
              theme.GetConstant("outline_size", "ItemList") == 0 &&
              theme.GetColor("font_color", "ItemList") == new Color(.65f, .65f, .65f) &&
              theme.GetColor("font_hovered_color", "ItemList") == new Color(.95f, .95f, .95f) &&
              theme.GetColor("font_selected_color", "ItemList") == Colors.White &&
              theme.GetColor("guide_color", "ItemList") == new Color(.7f, .7f, .7f, .25f) &&
              ReferenceEquals(theme.GetIcon("scroll_hint", "ItemList"), theme.GetIcon("scroll_hint_vertical", "ScrollContainer")),
            "The list theme uses the pinned panel, focus, selection text, spacing and shared scroll-hint defaults.");
    }

    private sealed class TestViewport : Viewport
    {
        public override Rect2 GetVisibleRect() => new(0, 0, 320, 240);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
