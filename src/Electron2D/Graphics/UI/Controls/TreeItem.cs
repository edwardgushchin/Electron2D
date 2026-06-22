/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

/// <summary>
/// Stores one row of data inside a <see cref="Tree"/> control.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>TreeItem</c> owns per-column text, icon, selection and selectable state.
/// Items are normally created through <see cref="Tree.CreateItem(TreeItem?, int)"/>
/// so they can participate in tree selection and visible traversal.
/// </para>
/// <para>
/// A standalone item created with the public constructor can store data, but it
/// has no owning tree until a tree creates or attaches it internally.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate tree items on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Tree"/>
public class TreeItem : Object
{
    private readonly List<TreeItem> children = new();
    private readonly Dictionary<int, TreeItemCell> cells = new();
    private Tree? tree;
    private TreeItem? parent;
    private bool collapsed;

    /// <summary>
    /// Initializes a new standalone instance of the <see cref="TreeItem"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// Standalone items can store per-column data but are not visible in a
    /// <see cref="Tree"/> until created by that tree.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene
    /// thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Tree.CreateItem(TreeItem?, int)"/>
    public TreeItem()
    {
    }

    internal TreeItem(Tree owner, TreeItem? parentItem)
    {
        tree = owner;
        parent = parentItem;
    }

    /// <summary>
    /// Gets the tree that owns this item.
    /// </summary>
    ///
    /// <returns>
    /// The owning <see cref="Tree"/>, or <c>null</c> for standalone items.
    /// </returns>
    ///
    /// <remarks>
    /// The value is assigned when an item is created by a tree.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Tree"/>
    public Tree? GetTree()
    {
        ThrowIfFreed();
        return tree;
    }

    /// <summary>
    /// Gets the parent item.
    /// </summary>
    ///
    /// <returns>
    /// The parent item, or <c>null</c> when this item is the root or standalone.
    /// </returns>
    ///
    /// <remarks>
    /// Parentage is managed by <see cref="Tree.CreateItem(TreeItem?, int)"/>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetChild(int)"/>
    public TreeItem? GetParent()
    {
        ThrowIfFreed();
        return parent;
    }

    /// <summary>
    /// Gets a direct child item.
    /// </summary>
    ///
    /// <param name="index">The child index.</param>
    ///
    /// <returns>
    /// The child item, or <c>null</c> when <paramref name="index"/> is outside
    /// the child list.
    /// </returns>
    ///
    /// <remarks>
    /// Negative indices are not normalized in the preview implementation.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetChildCount"/>
    public TreeItem? GetChild(int index)
    {
        ThrowIfFreed();
        return index >= 0 && index < children.Count ? children[index] : null;
    }

    /// <summary>
    /// Gets the number of direct child items.
    /// </summary>
    ///
    /// <returns>
    /// The direct child count.
    /// </returns>
    ///
    /// <remarks>
    /// Collapsed state does not affect this count.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetChild(int)"/>
    public int GetChildCount()
    {
        ThrowIfFreed();
        return children.Count;
    }

    /// <summary>
    /// Gets the next sibling item.
    /// </summary>
    ///
    /// <returns>
    /// The next sibling, or <c>null</c> when this item is the last sibling.
    /// </returns>
    ///
    /// <remarks>
    /// This method does not descend into child items.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetPrevious"/>
    public TreeItem? GetNext()
    {
        ThrowIfFreed();
        return GetSibling(offset: 1);
    }

    /// <summary>
    /// Gets the previous sibling item.
    /// </summary>
    ///
    /// <returns>
    /// The previous sibling, or <c>null</c> when this item is the first sibling.
    /// </returns>
    ///
    /// <remarks>
    /// This method does not inspect parent or child items.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetNext"/>
    public TreeItem? GetPrevious()
    {
        ThrowIfFreed();
        return GetSibling(offset: -1);
    }

    /// <summary>
    /// Gets the next visible item in owning tree traversal order.
    /// </summary>
    ///
    /// <returns>
    /// The next visible item, or <c>null</c> when there is no owning tree or no
    /// later visible item.
    /// </returns>
    ///
    /// <remarks>
    /// Collapsed descendants and a hidden root are skipped by the owning tree.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Tree.GetItemAtPosition(Vector2)"/>
    public TreeItem? GetNextVisible()
    {
        ThrowIfFreed();
        return tree?.GetNextVisibleItem(this);
    }

    /// <summary>
    /// Sets text for a column.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    /// <param name="text">The text to store.</param>
    ///
    /// <remarks>
    /// Assigning text queues the owning tree for redraw when one exists.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is negative.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetText(int)"/>
    public void SetText(int column, string text)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentNullException.ThrowIfNull(text);
        GetOrCreateCell(column).Text = text;
        tree?.QueueRedraw();
    }

    /// <summary>
    /// Gets text from a column.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    ///
    /// <returns>
    /// The stored text, or an empty string when no text is stored.
    /// </returns>
    ///
    /// <remarks>
    /// Missing cells are treated as empty cells.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetText(int, string)"/>
    public string GetText(int column)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        return cells.TryGetValue(column, out var cell) ? cell.Text : string.Empty;
    }

    /// <summary>
    /// Sets an icon for a column.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    /// <param name="icon">The icon texture, or <c>null</c> to remove it.</param>
    ///
    /// <remarks>
    /// Icons are optional and do not affect selection.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetIcon(int)"/>
    public void SetIcon(int column, Texture2D? icon)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        GetOrCreateCell(column).Icon = icon;
        tree?.QueueRedraw();
    }

    /// <summary>
    /// Gets an icon from a column.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    ///
    /// <returns>
    /// The stored icon, or <c>null</c> when no icon is stored.
    /// </returns>
    ///
    /// <remarks>
    /// Missing cells are treated as cells without an icon.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetIcon(int, Texture2D?)"/>
    public Texture2D? GetIcon(int column)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        return cells.TryGetValue(column, out var cell) ? cell.Icon : null;
    }

    /// <summary>
    /// Sets whether a column can be selected.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    /// <param name="selectable"><c>true</c> to allow selection; otherwise, <c>false</c>.</param>
    ///
    /// <remarks>
    /// Making a selected column non-selectable clears its selected state.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="IsSelectable(int)"/>
    public void SetSelectable(int column, bool selectable)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        var cell = GetOrCreateCell(column);
        cell.Selectable = selectable;
        if (!selectable)
        {
            cell.Selected = false;
        }

        tree?.QueueRedraw();
    }

    /// <summary>
    /// Reports whether a column can be selected.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the column is selectable; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Missing cells are selectable by default.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetSelectable(int, bool)"/>
    public bool IsSelectable(int column)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        return !cells.TryGetValue(column, out var cell) || cell.Selectable;
    }

    /// <summary>
    /// Selects this item in its owning tree.
    /// </summary>
    ///
    /// <param name="column">The zero-based selected column.</param>
    /// <param name="setAsCursor">Whether this item should become the tree cursor.</param>
    ///
    /// <remarks>
    /// Standalone items update local selected state but cannot change a tree
    /// cursor or emit tree signals.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Deselect(int)"/>
    public void Select(int column, bool setAsCursor = true)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        if (!IsSelectable(column))
        {
            return;
        }

        if (tree is not null)
        {
            tree.SelectItem(this, column, setAsCursor, emitSignal: true);
            return;
        }

        GetOrCreateCell(column).Selected = true;
    }

    /// <summary>
    /// Clears selection for one column.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    ///
    /// <remarks>
    /// Calling this method for an unselected column is a no-op.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Select(int, bool)"/>
    public void Deselect(int column)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        if (cells.TryGetValue(column, out var cell))
        {
            cell.Selected = false;
            tree?.ClearSelectionIfMatches(this, column);
            tree?.QueueRedraw();
        }
    }

    /// <summary>
    /// Reports whether one column is selected.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the column is selected; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Missing cells are not selected.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Select(int, bool)"/>
    public bool IsSelected(int column)
    {
        ThrowIfFreed();
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        return cells.TryGetValue(column, out var cell) && cell.Selected;
    }

    /// <summary>
    /// Sets whether this item hides its descendants in visible traversal.
    /// </summary>
    ///
    /// <param name="enable"><c>true</c> to collapse descendants; otherwise, <c>false</c>.</param>
    ///
    /// <remarks>
    /// Changing collapsed state queues the owning tree for redraw and emits
    /// <c>item_collapsed</c> from that tree.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="IsCollapsed"/>
    public void SetCollapsed(bool enable)
    {
        ThrowIfFreed();
        if (collapsed == enable)
        {
            return;
        }

        collapsed = enable;
        tree?.NotifyItemCollapsed(this);
    }

    /// <summary>
    /// Reports whether this item is collapsed.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when descendants are hidden; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Collapsing an item does not remove descendants from the data tree.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetCollapsed(bool)"/>
    public bool IsCollapsed()
    {
        ThrowIfFreed();
        return collapsed;
    }

    internal IReadOnlyList<TreeItem> Children => children;

    internal bool IsCollapsedCore => collapsed;

    internal int IndexInParent => parent is null ? -1 : parent.children.IndexOf(this);

    internal void AttachTo(Tree owner, TreeItem? parentItem, int index)
    {
        tree = owner;
        parent = parentItem;
        foreach (var child in children)
        {
            child.AttachTo(owner, this, child.IndexInParent);
        }

        if (parentItem is not null && !parentItem.children.Contains(this))
        {
            var insertionIndex = index < 0 || index > parentItem.children.Count ? parentItem.children.Count : index;
            parentItem.children.Insert(insertionIndex, this);
        }
    }

    internal void ClearSelectedRecursive()
    {
        foreach (var cell in cells.Values)
        {
            cell.Selected = false;
        }

        foreach (var child in children)
        {
            child.ClearSelectedRecursive();
        }
    }

    internal void SetSelectedCore(int column, bool selected)
    {
        GetOrCreateCell(column).Selected = selected;
    }

    internal bool HasSelectableColumn(int column)
    {
        return IsSelectable(column);
    }

    private TreeItemCell GetOrCreateCell(int column)
    {
        if (!cells.TryGetValue(column, out var cell))
        {
            cell = new TreeItemCell();
            cells[column] = cell;
        }

        return cell;
    }

    private TreeItem? GetSibling(int offset)
    {
        if (parent is null)
        {
            return null;
        }

        var index = parent.children.IndexOf(this);
        var targetIndex = index + offset;
        return targetIndex >= 0 && targetIndex < parent.children.Count ? parent.children[targetIndex] : null;
    }

    private sealed class TreeItemCell
    {
        public string Text { get; set; } = string.Empty;

        public Texture2D? Icon { get; set; }

        public bool Selectable { get; set; } = true;

        public bool Selected { get; set; }
    }
}
