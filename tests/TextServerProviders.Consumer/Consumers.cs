using Electron2D;

internal static class ProviderConsumers
{
    internal static void Run()
    {
        var builtin = TextServerManager.GetPrimaryInterface(); using var provider = new ConsumerProbe();
        TextServerManager.SetPrimaryInterface(provider);
        try
        {
            Case("items", theme => new ItemList { Theme = theme }, list =>
            {
                list.AddItem("keep"); list.AddItem("remove"); _ = list.GetItemRect(1); var freed = provider.Freed;
                list.RemoveItem(1); Check(provider.Freed > freed, "Item removal retires shaped content.");
                list.AddItem("shrink"); _ = list.GetItemRect(1); freed = provider.Freed; list.ItemCount = 1; Check(provider.Freed > freed, "Item count shrink retires both layouts."); list.Clear();
            });
            Case("tabs", theme => new TabBar { Theme = theme }, tabs =>
            {
                tabs.AddTab("keep"); tabs.AddTab("remove"); _ = tabs.GetTabRect(1); var freed = provider.Freed;
                tabs.RemoveTab(1); Check(provider.Freed > freed, "Tab removal retires intrinsic and display layouts."); tabs.AddTab("shrink"); _ = tabs.GetTabRect(1); tabs.TabCount = 1; tabs.ClearTabs();
            });
            Case("popup", theme => new PopupMenu { Theme = theme }, menu =>
            {
                menu.AddItem("keep"); menu.AddItem("remove"); _ = menu.GetContentsMinimumSize(); var freed = provider.Freed;
                menu.RemoveItem(1); Check(provider.Freed > freed, "Menu removal retires text and accelerator layouts."); menu.Clear();
            });
            Case("options", theme => new OptionButton { Theme = theme }, options =>
            {
                options.AddItem("keep"); options.AddItem("remove"); _ = options.GetMinimumSize(); var freed = provider.Freed;
                options.RemoveItem(1); Check(provider.Freed > freed, "Option removal retires longest-item measurement storage."); options.Clear();
            });
            Case("tree", theme => new Tree { Theme = theme, Columns = 2 }, tree =>
            {
                var root = tree.CreateItem(); root.SetText(0, "keep"); root.SetText(1, "remove"); tree.SetColumnTitle(1, "header"); _ = tree.GetItemAreaRect(root); var freed = provider.Freed;
                tree.Columns = 1; Check(provider.Freed > freed, "Column shrink retires header and cell storage.");
                var child = root.CreateChild(); child.SetText(0, "child"); _ = tree.GetItemAreaRect(child); child.Dispose();
            });
            Case("text-edit", theme => new TextEdit { Theme = theme, Text = "keep\nremove\nlast" }, edit =>
            {
                _ = edit.GetLineWidth(1); var freed = provider.Freed; edit.Text = "keep"; Check(provider.Freed > freed, "Document replacement retires deleted line layouts."); edit.Undo(); _ = edit.GetLineWidth(1); edit.Redo();
            });
            Case("code-edit", theme => new CodeEdit { Theme = theme, Text = "keep\nremove", GuttersDrawLineNumbers = true }, edit => { _ = edit.GetLineWidth(1); edit.Text = "keep"; });
            Case("foldable", theme => new FoldableContainer { Theme = theme, Title = "Title" }, foldable => _ = foldable.GetMinimumSize());
            Case("label", theme => new Label { Theme = theme, Text = "keep\nremove" }, label => { _ = label.GetMinimumSize(); var freed = provider.Freed; label.Text = "keep"; _ = label.GetMinimumSize(); Check(provider.Freed > freed, "Paragraph shrink retires removed label storage."); });
            Console.WriteLine("Nine public GUI consumer families retire removed records and release every provider buffer before primary replacement.");
        }
        finally { TextServerManager.SetPrimaryInterface(builtin); }

        void Case<T>(string name, Func<Theme, T> create, Action<T> exercise) where T : Node
        {
            using var font = new FontFile(); font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/Regular.ttf"));
            using var theme = new Theme { DefaultFont = font };
            using var root = new SubViewport { Size = new(500, 300) }; var control = create(theme); root.AddChild(control);
            if (control is Control visual) visual.Size = new(400, 200);
            using var tree = new SceneTree(root); tree.ProcessFrame(.01); exercise(control);
            control.Dispose(); tree.Dispose(); root.Dispose(); font.Dispose();
            Check(provider.Live.Count == 0, name + " retained " + provider.Live.Count + " provider buffers after explicit owner disposal.");
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class ConsumerProbe : Probe
    {
        internal readonly HashSet<RID> Live = [];
        internal int Freed;
        protected override RID OnCreateShapedText(TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
        { var rid = base.OnCreateShapedText(direction, orientation); Live.Add(rid); return rid; }
        protected override RID OnShapedTextSubstr(RID shaped, int start, int length)
        { var rid = base.OnShapedTextSubstr(shaped, start, length); Live.Add(rid); return rid; }
        protected override RID OnShapedTextDuplicate(RID shaped)
        { var rid = base.OnShapedTextDuplicate(shaped); Live.Add(rid); return rid; }
        protected override void OnFreeRID(RID rid) { if (Live.Remove(rid)) Freed++; base.OnFreeRID(rid); }
    }
}
