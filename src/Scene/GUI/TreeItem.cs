namespace Electron2D;

/// <summary>Owns a row of typed cells and child items within a Tree control.</summary>
/// <remarks>Items are created by Tree.CreateItem or CreateChild. Removing a child detaches its branch without
/// disposal; attached branches belong to their Tree and follow its owner thread. Icons, fonts and styles are borrowed.</remarks>
public sealed partial class TreeItem : ElectronObject
{
    /// <summary>Selects the executable presentation and editing policy of one cell.</summary>
    public enum TreeCellMode
    {
        /// <summary>Shaped text, optionally editable.</summary>
        String = 0,
        /// <summary>Text with a checkbox.</summary>
        Check = 1,
        /// <summary>A numeric value or text-defined choice.</summary>
        Range = 2,
        /// <summary>Borrowed icon.</summary>
        Icon = 3,
        /// <summary>Typed custom drawing or popup interaction.</summary>
        Custom = 4
    }
    internal sealed class Cell
    {
        internal TreeCellMode Mode;
        internal string Text = "", Description = "", Suffix = "", Language = "", Tooltip = "";
        internal Texture? Icon, Overlay;
        internal Font? Font;
        internal int FontSize = -1, IconMaxWidth;
        internal StyleBox? Style;
        internal Rect2 IconRegion, Rect, IconRect, TextRect;
        internal Color IconModulate = Colors.White, Color, Background;
        internal bool Merged, HasColor, HasBackground, BackgroundOutline, Multiline, Checked, Indeterminate, Editable, Selected, CustomButton, ExpandRight;
        internal bool Selectable = true;
        internal double Min, Max = 100, Step = 1, Value;
        internal bool Exponential;
        internal HorizontalAlignment Alignment;
        internal TextDirection Direction = TextDirection.Inherited;
        internal TextAutowrapMode Autowrap;
        internal TextLineBreakFlags Trim = TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces;
        internal TextOverrunBehavior Overrun = TextOverrunBehavior.TrimEllipsis;
        internal StructuredTextParser Parser;
        internal string[] ParserOptions = [];
        internal NodeAutoTranslateMode AutoTranslate = NodeAutoTranslateMode.Inherit;
        internal object? Metadata;
        internal Action<TreeItem, Rect2>? CustomDraw;
        internal readonly List<CellButton> Buttons = [];
        internal readonly TextLayout Layout = new();
        internal readonly List<TextBIDIRange> Contexts = [];
        internal string Display = "";
        internal int Size;
        internal Font? ResolvedFont;
    }
    internal sealed class CellButton
    {
        internal Texture? Texture;
        internal int ID;
        internal bool Disabled;
        internal string Tooltip = "", Description = "";
        internal Color Color = Colors.White;
        internal Rect2 Rect;
    }
    internal Tree? Owner;
    internal TreeItem? ParentItem;
    internal readonly List<TreeItem> Children = [];
    internal readonly List<Cell> Cells = [];
    internal bool IsRoot;
    internal int LayoutDepth;
    internal bool LayoutVisible, LayoutUnfolded;
    private bool _collapsed, _disableFolding, _visible = true, _acceptChildren = true;
    private int _minimumHeight;
    internal TreeItem(Tree? tree, int columns) { Owner = tree; ResizeCells(columns); }
    internal void ResizeCells(int count) { if (Cells.Count > count) Cells.RemoveRange(count, Cells.Count - count); while (Cells.Count < count) Cells.Add(new()); }
    private static void CollectException(ref List<Exception>? errors, Exception error) { Node.CollectException(ref errors, error); }
    private static void ThrowCollected(string message, List<Exception>? errors) { Node.ThrowCollected(message, errors); }
    internal void CheckItem() { ThrowIfDisposed(); Owner?.CheckTree(); }
    internal void MutableItem() { CheckItem(); Owner?.EnsureTreeMutable(); }
    internal Cell At(int column) { CheckItem(); if ((uint)column >= Cells.Count) throw new ArgumentOutOfRangeException(nameof(column)); return Cells[column]; }
    private void Changed() { Owner?.InvalidateTree(); }
    /// <summary>Gets or sets whether this branch's children are folded.</summary><value>False initially.</value>
    public bool Collapsed { get { CheckItem(); return _collapsed; } set { MutableItem(); if (_collapsed == value) return; _collapsed = value; Changed(); Owner?.PublishCollapsed(this); } }
    /// <summary>Gets or sets whether the row has its own folding control.</summary><value>False initially.</value>
    public bool DisableFolding { get { CheckItem(); return _disableFolding; } set { MutableItem(); _disableFolding = value; Changed(); } }
    /// <summary>Gets or sets local branch visibility.</summary><value>True initially; hidden ancestors hide descendants.</value>
    public bool Visible { get { CheckItem(); return _visible; } set { MutableItem(); if (_visible == value) return; _visible = value; Changed(); } }
    /// <summary>Gets or sets a nonnegative row minimum height in pixels.</summary><value>Zero initially.</value>
    public int CustomMinimumHeight { get { CheckItem(); return _minimumHeight; } set { MutableItem(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _minimumHeight = value; Changed(); } }
    /// <summary>Returns the current borrowed owning control or null when detached.</summary><returns>The owning Tree.</returns>
    public Tree? GetTree() { CheckItem(); return Owner; }
    /// <summary>Returns the parent item or null for a root/detached branch.</summary><returns>Borrowed parent.</returns>
    public TreeItem? GetParent() { CheckItem(); return ParentItem; }
    /// <summary>Returns an independent array of borrowed direct children.</summary><returns>Child order.</returns>
    public TreeItem[] GetChildren() { CheckItem(); return Children.ToArray(); }
    /// <summary>Returns the number of direct children.</summary><returns>Child count.</returns>
    public int GetChildCount() { CheckItem(); return Children.Count; }
    /// <summary>Returns one direct child, accepting negative indices from the end.</summary><param name="index">Child index.</param><returns>Borrowed item.</returns>
    public TreeItem GetChild(int index) { CheckItem(); if (index < 0) index += Children.Count; if ((uint)index >= Children.Count) throw new ArgumentOutOfRangeException(nameof(index)); return Children[index]; }
    /// <summary>Returns the first direct child or null.</summary><returns>Borrowed child.</returns>
    public TreeItem? GetFirstChild() { CheckItem(); return Children.Count > 0 ? Children[0] : null; }
    /// <summary>Returns this item's sibling index.</summary><returns>Zero for a root/detached item.</returns>
    public int GetIndex() { CheckItem(); return ParentItem?.Children.IndexOf(this) ?? 0; }
    /// <summary>Returns the next sibling or null.</summary><returns>Borrowed sibling.</returns>
    public TreeItem? GetNext() { CheckItem(); var index = GetIndex(); return ParentItem is { } parent && index + 1 < parent.Children.Count ? parent.Children[index + 1] : null; }
    /// <summary>Returns the preceding sibling or null.</summary><returns>Borrowed sibling.</returns>
    public TreeItem? GetPrev() { CheckItem(); var index = GetIndex(); return ParentItem is { } parent && index > 0 ? parent.Children[index - 1] : null; }
    /// <summary>Returns whether this row and its ancestors are locally visible.</summary><returns>Visibility independent of folding.</returns>
    public bool IsVisibleInTree() { CheckItem(); for (var item = this; item != null; item = item.ParentItem) if (!item._visible) return false; return true; }
    /// <summary>Creates a child at a clamped index, or appends for negative indices.</summary><param name="index">Insertion index or -1.</param><returns>New owned child.</returns>
    public TreeItem CreateChild(int index = -1) { MutableItem(); var item = new TreeItem(Owner, Cells.Count) { ParentItem = this }; Children.Insert(index < 0 ? Children.Count : Math.Min(index, Children.Count), item); Changed(); return item; }
    private bool HasAncestor(TreeItem item) { for (var at = ParentItem; at != null; at = at.ParentItem) if (at == item) return true; return false; }
    internal void BindTree(Tree? tree)
    {
        var old = Owner; if (old == tree) return; old?.ItemDetached(this); Owner = tree; if (tree != null) ResizeCells(tree.Columns); foreach (var child in Children) child.BindTree(tree); old?.InvalidateTree(); tree?.InvalidateTree();
    }
    /// <summary>Attaches an unowned detached branch as the final child.</summary><param name="child">Live detached item; cycles are rejected.</param>
    public void AddChild(TreeItem child) { MutableItem(); ArgumentNullException.ThrowIfNull(child); child.CheckItem(); if (child.Owner != null || child.ParentItem != null || child == this || HasAncestor(child)) throw new InvalidOperationException("The branch is attached or would create a cycle."); child.BindTree(Owner); child.ParentItem = this; Children.Add(child); Changed(); }
    /// <summary>Detaches a direct branch, retaining its cells and children.</summary><param name="child">Direct child.</param>
    public void RemoveChild(TreeItem child) { MutableItem(); ArgumentNullException.ThrowIfNull(child); if (child.ParentItem != this) throw new InvalidOperationException("The item is not a direct child."); Children.Remove(child); child.ParentItem = null; child.BindTree(null); Changed(); }
    /// <summary>Moves this complete branch before another nonroot item.</summary><param name="item">Destination sibling.</param>
    public void MoveBefore(TreeItem item) => Move(item, false);
    /// <summary>Moves this complete branch after another nonroot item.</summary><param name="item">Destination sibling.</param>
    public void MoveAfter(TreeItem item) => Move(item, true);
    private void Move(TreeItem item, bool after)
    {
        MutableItem(); ArgumentNullException.ThrowIfNull(item); item.MutableItem(); if (item == this) return; if (IsRoot || item.ParentItem == null || item.HasAncestor(this)) throw new InvalidOperationException("Cannot move a branch to a root or descendant.");
        var old = Owner; ParentItem?.Children.Remove(this); if (IsRoot) { old?.ItemDetached(this); IsRoot = false; }
        BindTree(item.Owner); ParentItem = item.ParentItem; ParentItem.Children.Insert(ParentItem.Children.IndexOf(item) + (after ? 1 : 0), this); old?.InvalidateTree(); Changed();
    }
    internal TreeItem BranchRoot() { var item = this; while (item.ParentItem != null) item = item.ParentItem; return item; }
    internal TreeItem? WalkNext(bool fold, bool invisible)
    {
        if ((!fold || !_collapsed) && Children.Count > 0) return Children[0]; for (var item = this; item != null; item = item.ParentItem) { var next = item.GetNext(); if (next != null) return next; }
        return null;
    }
    private TreeItem? Traverse(bool forward, bool wrap, bool folded)
    {
        CheckItem(); var root = BranchRoot(); var current = this;
        while (true)
        {
            TreeItem? next; if (forward) next = current.WalkNext(folded, false); else { next = current.GetPrev(); if (next == null) next = current.ParentItem; else while ((!folded || !next._collapsed) && next.Children.Count > 0) next = next.Children[^1]; }
            if (next == null && wrap) { next = root; if (!forward) while ((!folded || !next._collapsed) && next.Children.Count > 0) next = next.Children[^1]; }
            if (next == null || next == this) return next; current = next; if (current.IsVisibleInTree()) return current;
        }
    }
    /// <summary>Returns the following visible-in-hierarchy item in depth-first order, ignoring folding.</summary><param name="wrap">Wraps to the branch start.</param><returns>Borrowed item or null.</returns>
    public TreeItem? GetNextInTree(bool wrap = false) => Traverse(true, wrap, false);
    /// <summary>Returns the preceding visible-in-hierarchy item, ignoring folding.</summary><param name="wrap">Wraps to the branch end.</param><returns>Borrowed item or null.</returns>
    public TreeItem? GetPrevInTree(bool wrap = false) => Traverse(false, wrap, false);
    /// <summary>Returns the following presented row, respecting folding.</summary><param name="wrap">Wraps to the branch start.</param><returns>Borrowed item or null.</returns>
    public TreeItem? GetNextVisible(bool wrap = false) => Traverse(true, wrap, true);
    /// <summary>Returns the preceding presented row, respecting folding.</summary><param name="wrap">Wraps to the branch end.</param><returns>Borrowed item or null.</returns>
    public TreeItem? GetPrevVisible(bool wrap = false) => Traverse(false, wrap, true);
    /// <summary>Applies a typed callback to a captured depth-first subtree, continuing after callback errors.</summary><param name="callback">An action borrowing each still-live item.</param>
    public void CallRecursive(Action<TreeItem> callback) { MutableItem(); ArgumentNullException.ThrowIfNull(callback); var snapshot = new List<TreeItem>(); for (var item = this; item != null; item = item.WalkNext(false, true)) { if (item != this && !item.HasAncestor(this)) break; snapshot.Add(item); } List<Exception>? errors = null; foreach (var item in snapshot) if (!item.IsDisposed) try { callback(item); } catch (Exception error) { CollectException(ref errors, error); } ThrowCollected("TreeItem recursive callbacks failed.", errors); }
    /// <summary>Sets folding for this whole branch.</summary><param name="enable">Collapsed state.</param>
    public void SetCollapsedRecursive(bool enable) => CallRecursive(item => item.Collapsed = enable);
    /// <summary>Reports whether this branch contains a collapsed item.</summary><param name="onlyVisible">Omits locally hidden branches.</param><returns>Collapse presence.</returns>
    public bool IsAnyCollapsed(bool onlyVisible = false) { CheckItem(); for (var item = this; item != null; item = item.WalkNext(false, true)) { if (item != this && !item.HasAncestor(this)) break; if (item._collapsed && (!onlyVisible || item.IsVisibleInTree())) return true; } return false; }
    /// <summary>Unfolds this item and every ancestor.</summary>
    public void UncollapseTree() { MutableItem(); for (var item = this; item != null; item = item.ParentItem) item.Collapsed = false; }
    /// <summary>Sets whether drop presentation treats this row as accepting children.</summary><param name="allowed">Child-drop policy.</param>
    public void SetAcceptChildren(bool allowed) { MutableItem(); _acceptChildren = allowed; Changed(); }
    /// <summary>Reports the child-drop policy.</summary><returns>True initially.</returns>
    public bool IsAcceptingChildren() { CheckItem(); return _acceptChildren; }
    /// <inheritdoc />
    protected override void ValidateDisposal() { Owner?.EnsureTreeMutable(); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null; if (disposing) { var old = Owner; ParentItem?.Children.Remove(this); ParentItem = null; old?.ItemDetached(this); Owner = null; foreach (var child in Children.ToArray()) try { child.Dispose(); } catch (Exception error) { CollectException(ref errors, error); } Children.Clear(); Cells.Clear(); old?.InvalidateTree(); }
        try { base.Dispose(disposing); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("TreeItem cleanup failed.", errors);
    }
}
