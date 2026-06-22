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
/// Displays selectable <see cref="TreeItem"/> objects in a hierarchy.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>Tree</c> stores one root item and any number of descendant items. It can
/// hide the root row, select visible items by pointer input and expose the
/// current selected item through <see cref="GetSelected"/>.
/// </para>
/// <para>
/// The 0.1.0 Preview implementation focuses on runtime tree display and
/// selection. Drag-and-drop, editing controls and scrolling are planned for
/// later UI tasks.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate trees on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="TreeItem"/>
/// <seealso cref="Control"/>
public class Tree : Control
{
    private const int DefaultRowHeight = 24;
    private readonly Dictionary<int, string> columnTitles = new();
    private TreeItem? root;
    private TreeItem? selectedItem;
    private int selectedColumn = -1;
    private int columns = 1;

    /// <summary>
    /// Identifies how tree item selection is interpreted.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The preview stores all values and treats <see cref="Row"/> like
    /// <see cref="Single"/> for selection state, while preserving the selected
    /// column value.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SelectMode"/>
    public enum SelectModeEnum
    {
        /// <summary>
        /// Allows at most one selected cell.
        /// </summary>
        ///
        /// <remarks>
        /// Selecting a new item clears the previous selection.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="SelectModeEnum"/>
        Single = 0,

        /// <summary>
        /// Selects the whole visible row.
        /// </summary>
        ///
        /// <remarks>
        /// The preview records the selected column but highlights the row.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="SelectModeEnum"/>
        Row = 1,

        /// <summary>
        /// Allows multiple selected cells.
        /// </summary>
        ///
        /// <remarks>
        /// The preview emits <c>multi_selected</c> when a user-visible item is
        /// selected in this mode.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="SelectModeEnum"/>
        Multi = 2
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tree"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The constructor enables focus, clips hit-testing and registers
    /// <c>item_selected</c>, <c>multi_selected</c>, <c>item_activated</c> and
    /// <c>item_collapsed</c> signals.
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
    /// <seealso cref="Tree"/>
    public Tree()
    {
        FocusMode = FocusMode.All;
        ClipContents = true;
        AddUserSignal("item_selected");
        AddUserSignal("multi_selected");
        AddUserSignal("item_activated");
        AddUserSignal("item_collapsed");
    }

    /// <summary>
    /// Gets or sets whether selecting the current item again emits signals.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to allow repeated selection signals; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// This affects pointer and programmatic selection through
    /// <see cref="TreeItem.Select(int, bool)"/>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetSelected"/>
    public bool AllowReselect { get; set; }

    /// <summary>
    /// Gets or sets whether right mouse button input may select rows.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to allow right-click selection; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// Right-click selection is useful before opening a context menu.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="_GuiInput(InputEvent)"/>
    public bool AllowRmbSelect { get; set; }

    /// <summary>
    /// Gets or sets whether automatic tooltip lookup is enabled.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when item text may be used as tooltip fallback; otherwise,
    /// <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// The preview stores this flag. Full per-cell tooltip lookup is planned
    /// with richer editor UI tasks.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Control.TooltipText"/>
    public bool AutoTooltip { get; set; } = true;

    /// <summary>
    /// Gets or sets whether column title rows are visible.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to reserve a title row; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// Column titles are drawn only when this flag is enabled.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetColumnTitle(int, string)"/>
    public bool ColumnTitlesVisible { get; set; }

    /// <summary>
    /// Gets or sets the number of columns stored by the tree.
    /// </summary>
    ///
    /// <value>
    /// A positive column count. The default is <c>1</c>.
    /// </value>
    ///
    /// <remarks>
    /// Existing item cell data is preserved when reducing the column count,
    /// but drawing and hit-testing use the current value.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is less than <c>1</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetColumnTitle(int, string)"/>
    public int Columns
    {
        get => columns;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            columns = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether folding arrows are hidden.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to hide folding affordances; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// The preview stores this flag and keeps collapse state functional.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TreeItem.SetCollapsed(bool)"/>
    public bool HideFolding { get; set; }

    /// <summary>
    /// Gets or sets whether the root item is hidden from visible traversal.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to hide the root row; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// Hidden root items still own children and can be returned by
    /// <see cref="GetRoot"/>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetRoot"/>
    public bool HideRoot { get; set; }

    /// <summary>
    /// Gets or sets the current selection mode.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="SelectModeEnum"/>. The default is
    /// <see cref="SelectModeEnum.Single"/>.
    /// </value>
    ///
    /// <remarks>
    /// Switching away from multi selection clears extra selected cells.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="TreeItem.Select(int, bool)"/>
    public SelectModeEnum SelectMode { get; set; }

    /// <summary>
    /// Creates an item in this tree.
    /// </summary>
    ///
    /// <param name="parent">The parent item, or <c>null</c> to create or append under the root.</param>
    /// <param name="index">The insertion index, or <c>-1</c> to append.</param>
    ///
    /// <returns>
    /// The created <see cref="TreeItem"/>.
    /// </returns>
    ///
    /// <remarks>
    /// The first call with a <c>null</c> parent creates the root. Later calls
    /// with a <c>null</c> parent append a child under the root.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="parent"/> belongs to another tree.
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
    /// <seealso cref="GetRoot"/>
    public TreeItem CreateItem(TreeItem? parent = null, int index = -1)
    {
        ThrowIfFreed();
        if (parent is not null && !ReferenceEquals(parent.GetTree(), this))
        {
            throw new ArgumentException("Parent item must belong to this tree.", nameof(parent));
        }

        if (root is null && parent is null)
        {
            root = new TreeItem(this, parentItem: null);
            QueueRedraw();
            return root;
        }

        var actualParent = parent ?? root;
        if (actualParent is null)
        {
            throw new InvalidOperationException("Tree root was not created.");
        }

        var item = new TreeItem(this, actualParent);
        item.AttachTo(this, actualParent, index);
        QueueRedraw();
        return item;
    }

    /// <summary>
    /// Removes all items and clears selection.
    /// </summary>
    ///
    /// <remarks>
    /// Column titles and tree configuration are preserved.
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
    /// <seealso cref="CreateItem(TreeItem?, int)"/>
    public void Clear()
    {
        ThrowIfFreed();
        root = null;
        selectedItem = null;
        selectedColumn = -1;
        QueueRedraw();
    }

    /// <summary>
    /// Clears every selected item.
    /// </summary>
    ///
    /// <remarks>
    /// This method clears both tree cursor state and per-item selected flags.
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
    /// <seealso cref="GetSelected"/>
    public void DeselectAll()
    {
        ThrowIfFreed();
        root?.ClearSelectedRecursive();
        selectedItem = null;
        selectedColumn = -1;
        QueueRedraw();
    }

    /// <summary>
    /// Gets the title of one column.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    ///
    /// <returns>
    /// The column title, or an empty string when no title is stored.
    /// </returns>
    ///
    /// <remarks>
    /// Titles are drawn only when <see cref="ColumnTitlesVisible"/> is
    /// <c>true</c>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is outside the current column
    /// range.
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
    /// <seealso cref="SetColumnTitle(int, string)"/>
    public string GetColumnTitle(int column)
    {
        ThrowIfFreed();
        ValidateColumn(column);
        return columnTitles.TryGetValue(column, out var title) ? title : string.Empty;
    }

    /// <summary>
    /// Sets the title of one column.
    /// </summary>
    ///
    /// <param name="column">The zero-based column index.</param>
    /// <param name="title">The column title.</param>
    ///
    /// <remarks>
    /// Assigning a title queues a redraw.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="column"/> is outside the current column
    /// range.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="title"/> is <c>null</c>.
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
    /// <seealso cref="GetColumnTitle(int)"/>
    public void SetColumnTitle(int column, string title)
    {
        ThrowIfFreed();
        ValidateColumn(column);
        ArgumentNullException.ThrowIfNull(title);
        columnTitles[column] = title;
        QueueRedraw();
    }

    /// <summary>
    /// Gets the root item.
    /// </summary>
    ///
    /// <returns>
    /// The root item, or <c>null</c> when the tree is empty.
    /// </returns>
    ///
    /// <remarks>
    /// The root can be hidden from visible traversal by <see cref="HideRoot"/>.
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
    /// <seealso cref="CreateItem(TreeItem?, int)"/>
    public TreeItem? GetRoot()
    {
        ThrowIfFreed();
        return root;
    }

    /// <summary>
    /// Gets the currently selected item.
    /// </summary>
    ///
    /// <returns>
    /// The selected item, or <c>null</c> when nothing is selected.
    /// </returns>
    ///
    /// <remarks>
    /// In multi selection mode this returns the most recent cursor item.
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
    /// <seealso cref="GetSelectedColumn"/>
    public TreeItem? GetSelected()
    {
        ThrowIfFreed();
        return selectedItem;
    }

    /// <summary>
    /// Gets the selected column.
    /// </summary>
    ///
    /// <returns>
    /// The selected column, or <c>-1</c> when nothing is selected.
    /// </returns>
    ///
    /// <remarks>
    /// Row selection still records the column that received input.
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
    /// <seealso cref="GetSelected"/>
    public int GetSelectedColumn()
    {
        ThrowIfFreed();
        return selectedColumn;
    }

    /// <summary>
    /// Gets the visible item at a local position.
    /// </summary>
    ///
    /// <param name="position">The local tree position.</param>
    ///
    /// <returns>
    /// The visible item at the row, or <c>null</c> when no row exists there.
    /// </returns>
    ///
    /// <remarks>
    /// The root row is skipped when <see cref="HideRoot"/> is <c>true</c>.
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
    /// <seealso cref="TreeItem.GetNextVisible"/>
    public TreeItem? GetItemAtPosition(Vector2 position)
    {
        ThrowIfFreed();
        if (position.Y < 0f)
        {
            return null;
        }

        var rowOffset = ColumnTitlesVisible ? GetRowHeight() : 0;
        var adjustedY = position.Y - rowOffset;
        if (adjustedY < 0f)
        {
            return null;
        }

        var visibleItems = GetVisibleItems();
        var index = (int)MathF.Floor(adjustedY / GetRowHeight());
        return index >= 0 && index < visibleItems.Count ? visibleItems[index] : null;
    }

    /// <summary>
    /// Handles GUI input routed to this tree.
    /// </summary>
    ///
    /// <param name="inputEvent">The input event delivered by the viewport.</param>
    ///
    /// <remarks>
    /// Mouse and touch press select visible items. Double-click and activation
    /// keys emit <c>item_activated</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Control._GuiInput(InputEvent)"/>
    public override void _GuiInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton mouseButton when IsSelectableMouseButton(mouseButton):
                var item = GetItemAtPosition(mouseButton.GlobalPosition - GlobalPosition);
                if (item is not null)
                {
                    SelectItem(item, column: 0, setAsCursor: true, emitSignal: true);
                    if (mouseButton.DoubleClick)
                    {
                        EmitSignal("item_activated");
                    }

                    AcceptEvent();
                }

                break;
            case InputEventScreenTouch { Pressed: true, Canceled: false } touch:
                var touchItem = GetItemAtPosition(touch.Position - GlobalPosition);
                if (touchItem is not null)
                {
                    SelectItem(touchItem, column: 0, setAsCursor: true, emitSignal: true);
                    AcceptEvent();
                }

                break;
            case InputEventKey key when key.Pressed && IsActivationKey(key.Keycode) && selectedItem is not null:
                EmitSignal("item_activated");
                AcceptEvent();
                break;
        }
    }

    /// <summary>
    /// Draws the visible tree rows.
    /// </summary>
    ///
    /// <remarks>
    /// Text drawing is skipped when no theme font is available.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="CanvasItem.DrawRect(Rect2, Color, bool, float, bool)"/>
    public override void _Draw()
    {
        var rowHeight = GetRowHeight();
        var y = 0f;
        var font = GetThemeFont("font");
        var fontSize = GetThemeFontSize("font_size");
        if (ColumnTitlesVisible)
        {
            DrawRect(new Rect2(0f, 0f, Size.X, rowHeight), new Color(0.16f, 0.17f, 0.20f, 1f));
            if (font is not null)
            {
                DrawString(font, new Vector2(6f, MathF.Max(font.GetAscent(fontSize), ((rowHeight - font.GetHeight(fontSize)) * 0.5f) + font.GetAscent(fontSize))), GetColumnTitle(0), HorizontalAlignment.Left, Size.X - 12f, fontSize, Color.White);
            }

            y += rowHeight;
        }

        foreach (var item in GetVisibleItems())
        {
            var selected = ReferenceEquals(item, selectedItem);
            if (selected)
            {
                DrawRect(new Rect2(0f, y, Size.X, rowHeight), new Color(0.24f, 0.38f, 0.62f, 1f));
            }

            if (font is not null)
            {
                var depth = GetDepth(item);
                var text = item.GetText(0);
                DrawString(font, new Vector2(6f + (depth * 14f), y + MathF.Max(font.GetAscent(fontSize), ((rowHeight - font.GetHeight(fontSize)) * 0.5f) + font.GetAscent(fontSize))), text, HorizontalAlignment.Left, Size.X - 12f, fontSize, Color.White);
            }

            y += rowHeight;
        }
    }

    /// <summary>
    /// Gets the minimum size requested by this tree.
    /// </summary>
    ///
    /// <returns>
    /// A conservative size for the current visible row count.
    /// </returns>
    ///
    /// <remarks>
    /// The minimum height changes when items collapse or when the root is
    /// hidden.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Control.GetMinimumSize"/>
    public override Vector2 _GetMinimumSize()
    {
        var visibleRows = GetVisibleItems().Count + (ColumnTitlesVisible ? 1 : 0);
        return new Vector2(128f, visibleRows * GetRowHeight());
    }

    internal void SelectItem(TreeItem item, int column, bool setAsCursor, bool emitSignal)
    {
        if (!ReferenceEquals(item.GetTree(), this) || !item.HasSelectableColumn(column))
        {
            return;
        }

        var changed = !ReferenceEquals(selectedItem, item) || selectedColumn != column || !item.IsSelected(column);
        if (SelectMode != SelectModeEnum.Multi)
        {
            root?.ClearSelectedRecursive();
        }

        item.SetSelectedCore(column, true);
        if (setAsCursor)
        {
            selectedItem = item;
            selectedColumn = column;
        }

        if (emitSignal && (changed || AllowReselect))
        {
            EmitSignal("item_selected");
            if (SelectMode == SelectModeEnum.Multi)
            {
                EmitSignal("multi_selected", item, column, true);
            }
        }

        QueueRedraw();
    }

    internal void ClearSelectionIfMatches(TreeItem item, int column)
    {
        if (ReferenceEquals(selectedItem, item) && selectedColumn == column)
        {
            selectedItem = null;
            selectedColumn = -1;
        }
    }

    internal void NotifyItemCollapsed(TreeItem item)
    {
        QueueRedraw();
        EmitSignal("item_collapsed", item);
    }

    internal TreeItem? GetNextVisibleItem(TreeItem item)
    {
        var visibleItems = GetVisibleItems();
        var index = visibleItems.IndexOf(item);
        return index >= 0 && index + 1 < visibleItems.Count ? visibleItems[index + 1] : null;
    }

    private List<TreeItem> GetVisibleItems()
    {
        var result = new List<TreeItem>();
        if (root is null)
        {
            return result;
        }

        CollectVisible(root, result, includeSelf: !HideRoot);
        return result;
    }

    private void CollectVisible(TreeItem item, List<TreeItem> result, bool includeSelf)
    {
        if (includeSelf)
        {
            result.Add(item);
        }

        if (item.IsCollapsedCore)
        {
            return;
        }

        foreach (var child in item.Children)
        {
            CollectVisible(child, result, includeSelf: true);
        }
    }

    private int GetRowHeight()
    {
        return Math.Max(1, GetThemeConstantOrDefault("row_height", DefaultRowHeight));
    }

    private void ValidateColumn(int column)
    {
        if (column < 0 || column >= Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "Column is outside the tree column range.");
        }
    }

    private bool IsSelectableMouseButton(InputEventMouseButton mouseButton)
    {
        return mouseButton.Pressed &&
            (mouseButton.ButtonIndex == MouseButton.Left ||
            (AllowRmbSelect && mouseButton.ButtonIndex == MouseButton.Right));
    }

    private static bool IsActivationKey(Key key)
    {
        return key is Key.Enter or Key.KpEnter or Key.Space;
    }

    private static int GetDepth(TreeItem item)
    {
        var depth = 0;
        for (var parent = item.GetParent(); parent is not null; parent = parent.GetParent())
        {
            depth++;
        }

        return Math.Max(0, depth - 1);
    }
}
