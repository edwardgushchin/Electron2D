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
/// Displays a selectable flat list of text and optional icon items.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>ItemList</c> is a runtime UI control for menus, inspectors and debug
/// panels that need a compact list of selectable rows.
/// </para>
/// <para>
/// The 0.1-preview implementation supports item storage, single and multi
/// selection, pointer activation, keyboard activation and simple canvas
/// drawing. Scrolling and text search are reserved for later UI tasks.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate item lists on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Control"/>
/// <seealso cref="PopupMenu"/>
public class ItemList : Control
{
    private const int DefaultRowHeight = 24;
    private readonly List<ItemListItem> items = new();
    private SelectModeEnum selectMode;
    private int maxColumns = 1;
    private int fixedColumnWidth;
    private Vector2I fixedIconSize;

    /// <summary>
    /// Identifies how many items can be selected at the same time.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The value is read by user input and by <see cref="Select(int, bool)"/>
    /// when deciding whether older selected items should be cleared.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SelectMode"/>
    public enum SelectModeEnum
    {
        /// <summary>
        /// Allows at most one selected item.
        /// </summary>
        ///
        /// <remarks>
        /// Selecting a new item clears previous selection.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SelectModeEnum"/>
        Single = 0,

        /// <summary>
        /// Allows multiple selected items.
        /// </summary>
        ///
        /// <remarks>
        /// User input keeps previous selected items when this value is active.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="SelectModeEnum"/>
        Multi = 1
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemList"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The constructor enables focus and registers <c>item_selected</c>,
    /// <c>multi_selected</c> and <c>item_activated</c> signals.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene
    /// thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="ItemList"/>
    public ItemList()
    {
        FocusMode = FocusMode.All;
        ClipContents = true;
        AddUserSignal("item_selected");
        AddUserSignal("multi_selected");
        AddUserSignal("item_activated");
    }

    /// <summary>
    /// Gets the number of items stored in this list.
    /// </summary>
    ///
    /// <value>
    /// The current item count.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The value changes through <see cref="AddItem(string, Texture2D?, bool)"/>,
    /// <see cref="AddIconItem(Texture2D, bool)"/> and <see cref="Clear"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Read it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetItemCount"/>
    public int ItemCount => items.Count;

    /// <summary>
    /// Gets or sets the selection mode used by this list.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="SelectModeEnum"/>. The default is
    /// <see cref="SelectModeEnum.Single"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Changing from multi selection to single selection keeps the first
    /// selected item and clears the rest.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Select(int, bool)"/>
    public SelectModeEnum SelectMode
    {
        get => selectMode;
        set
        {
            selectMode = value;
            if (selectMode == SelectModeEnum.Single)
            {
                KeepOnlyFirstSelected();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether selecting the current item again still emits signals.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to emit selection signals for already selected items;
    /// otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// This property affects user input and <see cref="Select(int, bool)"/>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Select(int, bool)"/>
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
    /// Right-click activation is useful for context menus. The preview still
    /// emits normal selection signals when selection succeeds.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="_GuiInput(InputEvent)"/>
    public bool AllowRmbSelect { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of visual columns requested by the list.
    /// </summary>
    ///
    /// <value>
    /// A positive number of columns. The default is <c>1</c>.
    /// </value>
    ///
    /// <remarks>
    /// The 0.1-preview stores this value for layout consumers. Pointer
    /// hit-testing remains row-based.
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
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="FixedColumnWidth"/>
    public int MaxColumns
    {
        get => maxColumns;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            maxColumns = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the requested fixed column width.
    /// </summary>
    ///
    /// <value>
    /// <c>0</c> for automatic width, or a positive width in UI units.
    /// </value>
    ///
    /// <remarks>
    /// The preview drawing path stores the value and keeps row hit-testing
    /// simple.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="MaxColumns"/>
    public int FixedColumnWidth
    {
        get => fixedColumnWidth;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            fixedColumnWidth = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the requested fixed icon size.
    /// </summary>
    ///
    /// <value>
    /// A non-negative icon size. <see cref="Vector2I.Zero"/> means icons use
    /// their resource size.
    /// </value>
    ///
    /// <remarks>
    /// The preview stores this value and uses it for minimum-size estimation.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any component is negative.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Texture2D.GetSize"/>
    public Vector2I FixedIconSize
    {
        get => fixedIconSize;
        set
        {
            ValidateNonNegative(value, nameof(FixedIconSize));
            fixedIconSize = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Adds an item with optional text and icon.
    /// </summary>
    ///
    /// <param name="text">The text displayed by the item.</param>
    /// <param name="icon">The optional icon texture.</param>
    /// <param name="selectable">Whether the item can be selected.</param>
    ///
    /// <returns>
    /// The zero-based index of the created item.
    /// </returns>
    ///
    /// <remarks>
    /// The item is enabled by default. Use <see cref="SetItemDisabled"/> to
    /// prevent user input from selecting it.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AddIconItem(Texture2D, bool)"/>
    public int AddItem(string text, Texture2D? icon = null, bool selectable = true)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(text);
        items.Add(new ItemListItem(text, icon, selectable));
        QueueRedraw();
        return items.Count - 1;
    }

    /// <summary>
    /// Adds an item that displays only an icon.
    /// </summary>
    ///
    /// <param name="icon">The icon texture to display.</param>
    /// <param name="selectable">Whether the item can be selected.</param>
    ///
    /// <returns>
    /// The zero-based index of the created item.
    /// </returns>
    ///
    /// <remarks>
    /// This is equivalent to adding an item with empty text and the specified
    /// icon.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="icon"/> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AddItem(string, Texture2D?, bool)"/>
    public int AddIconItem(Texture2D icon, bool selectable = true)
    {
        ArgumentNullException.ThrowIfNull(icon);
        return AddItem(string.Empty, icon, selectable);
    }

    /// <summary>
    /// Removes every item and clears selection.
    /// </summary>
    ///
    /// <remarks>
    /// The registered signals remain available after clearing.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="AddItem(string, Texture2D?, bool)"/>
    public void Clear()
    {
        ThrowIfFreed();
        items.Clear();
        QueueRedraw();
    }

    /// <summary>
    /// Gets the number of stored items.
    /// </summary>
    ///
    /// <returns>
    /// The same value as <see cref="ItemCount"/>.
    /// </returns>
    ///
    /// <remarks>
    /// This method mirrors the method-based API shape used by other list
    /// operations.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="ItemCount"/>
    public int GetItemCount()
    {
        ThrowIfFreed();
        return items.Count;
    }

    /// <summary>
    /// Sets the text displayed by an item.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    /// <param name="text">The new item text.</param>
    ///
    /// <remarks>
    /// Assigning text queues a redraw. Empty text is valid.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
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
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetItemText(int)"/>
    public void SetItemText(int idx, string text)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(text);
        GetItem(idx).Text = text;
        QueueRedraw();
    }

    /// <summary>
    /// Gets the text displayed by an item.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    ///
    /// <returns>
    /// The item text.
    /// </returns>
    ///
    /// <remarks>
    /// The returned string is never <c>null</c>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetItemText(int, string)"/>
    public string GetItemText(int idx)
    {
        ThrowIfFreed();
        return GetItem(idx).Text;
    }

    /// <summary>
    /// Sets the icon displayed by an item.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    /// <param name="icon">The icon texture, or <c>null</c> to remove it.</param>
    ///
    /// <remarks>
    /// Icons are optional and do not affect selection state.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetItemIcon(int)"/>
    public void SetItemIcon(int idx, Texture2D? icon)
    {
        ThrowIfFreed();
        GetItem(idx).Icon = icon;
        QueueRedraw();
    }

    /// <summary>
    /// Gets the icon displayed by an item.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    ///
    /// <returns>
    /// The item icon, or <c>null</c> when the item has no icon.
    /// </returns>
    ///
    /// <remarks>
    /// Use <see cref="SetItemIcon(int, Texture2D?)"/> to replace or clear the
    /// icon.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetItemIcon(int, Texture2D?)"/>
    public Texture2D? GetItemIcon(int idx)
    {
        ThrowIfFreed();
        return GetItem(idx).Icon;
    }

    /// <summary>
    /// Sets whether an item ignores user selection and activation.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    /// <param name="disabled"><c>true</c> to disable the item; otherwise, <c>false</c>.</param>
    ///
    /// <remarks>
    /// Disabling a selected item clears its selected state.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="IsItemDisabled(int)"/>
    public void SetItemDisabled(int idx, bool disabled)
    {
        ThrowIfFreed();
        var item = GetItem(idx);
        item.Disabled = disabled;
        if (disabled)
        {
            item.Selected = false;
        }

        QueueRedraw();
    }

    /// <summary>
    /// Reports whether an item is disabled.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item is disabled; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Disabled items cannot be selected by user input.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetItemDisabled(int, bool)"/>
    public bool IsItemDisabled(int idx)
    {
        ThrowIfFreed();
        return GetItem(idx).Disabled;
    }

    /// <summary>
    /// Sets whether an item can be selected.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    /// <param name="selectable"><c>true</c> to allow selection; otherwise, <c>false</c>.</param>
    ///
    /// <remarks>
    /// Making an item non-selectable clears its selected state.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="IsItemSelectable(int)"/>
    public void SetItemSelectable(int idx, bool selectable)
    {
        ThrowIfFreed();
        var item = GetItem(idx);
        item.Selectable = selectable;
        if (!selectable)
        {
            item.Selected = false;
        }

        QueueRedraw();
    }

    /// <summary>
    /// Reports whether an item can be selected.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item is selectable; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Disabled items are not selected even when this flag is <c>true</c>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="SetItemSelectable(int, bool)"/>
    public bool IsItemSelectable(int idx)
    {
        ThrowIfFreed();
        return GetItem(idx).Selectable;
    }

    /// <summary>
    /// Selects an item.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    /// <param name="single">Whether other selected items should be cleared.</param>
    ///
    /// <remarks>
    /// Disabled or non-selectable items are ignored. The method emits
    /// <c>item_selected</c> and, in multi selection mode,
    /// <c>multi_selected</c> when selection changes or
    /// <see cref="AllowReselect"/> is enabled.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Deselect(int)"/>
    public void Select(int idx, bool single = true)
    {
        ThrowIfFreed();
        var item = GetItem(idx);
        if (!CanSelect(item))
        {
            return;
        }

        if (single || SelectMode == SelectModeEnum.Single)
        {
            DeselectAllExcept(idx);
        }

        var changed = !item.Selected;
        item.Selected = true;
        if (changed || AllowReselect)
        {
            EmitSignal("item_selected", idx);
            if (SelectMode == SelectModeEnum.Multi)
            {
                EmitSignal("multi_selected", idx, true);
            }
        }

        QueueRedraw();
    }

    /// <summary>
    /// Clears selection from one item.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    ///
    /// <remarks>
    /// Calling this method for an unselected item is a no-op.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Select(int, bool)"/>
    public void Deselect(int idx)
    {
        ThrowIfFreed();
        var item = GetItem(idx);
        if (!item.Selected)
        {
            return;
        }

        item.Selected = false;
        if (SelectMode == SelectModeEnum.Multi)
        {
            EmitSignal("multi_selected", idx, false);
        }

        QueueRedraw();
    }

    /// <summary>
    /// Clears selection from every item.
    /// </summary>
    ///
    /// <remarks>
    /// Multi-selection deselect signals are emitted for items that were
    /// selected.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetSelectedItems"/>
    public void DeselectAll()
    {
        ThrowIfFreed();
        for (var index = 0; index < items.Count; index++)
        {
            if (!items[index].Selected)
            {
                continue;
            }

            items[index].Selected = false;
            if (SelectMode == SelectModeEnum.Multi)
            {
                EmitSignal("multi_selected", index, false);
            }
        }

        QueueRedraw();
    }

    /// <summary>
    /// Reports whether an item is selected.
    /// </summary>
    ///
    /// <param name="idx">The item index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item is selected; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Selection state can be changed by user input or by
    /// <see cref="Select(int, bool)"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="idx"/> is outside the item list.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="GetSelectedItems"/>
    public bool IsSelected(int idx)
    {
        ThrowIfFreed();
        return GetItem(idx).Selected;
    }

    /// <summary>
    /// Gets the indices of selected items.
    /// </summary>
    ///
    /// <returns>
    /// A new array containing selected item indices in ascending order.
    /// </returns>
    ///
    /// <remarks>
    /// The returned array is detached from the list and can be mutated by the
    /// caller.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="IsSelected(int)"/>
    public int[] GetSelectedItems()
    {
        ThrowIfFreed();
        return items.Select((item, index) => (item, index))
            .Where(pair => pair.item.Selected)
            .Select(pair => pair.index)
            .ToArray();
    }

    /// <summary>
    /// Gets the item index at a local position.
    /// </summary>
    ///
    /// <param name="position">The local list position.</param>
    /// <param name="exact">Whether positions outside the horizontal bounds should return <c>-1</c>.</param>
    ///
    /// <returns>
    /// The item index at the position, or <c>-1</c> when no item is present.
    /// </returns>
    ///
    /// <remarks>
    /// The preview list uses one row per item. Column wrapping is stored but not
    /// used for hit-testing yet.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="_GuiInput(InputEvent)"/>
    public int GetItemAtPosition(Vector2 position, bool exact = false)
    {
        ThrowIfFreed();
        if (position.Y < 0f || (exact && (position.X < 0f || position.X >= Size.X)))
        {
            return -1;
        }

        var rowHeight = GetRowHeight();
        var index = (int)MathF.Floor(position.Y / rowHeight);
        return index >= 0 && index < items.Count ? index : -1;
    }

    /// <summary>
    /// Handles GUI input routed to this list.
    /// </summary>
    ///
    /// <param name="inputEvent">The input event delivered by the viewport.</param>
    ///
    /// <remarks>
    /// Mouse and touch press select rows. Double-click and activation keys emit
    /// <c>item_activated</c> for the selected item.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Control._GuiInput(InputEvent)"/>
    public override void _GuiInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton mouseButton when IsSelectableMouseButton(mouseButton):
                var mouseIndex = GetItemAtPosition(mouseButton.GlobalPosition - GlobalPosition);
                if (mouseIndex >= 0)
                {
                    Select(mouseIndex, single: SelectMode != SelectModeEnum.Multi);
                    if (mouseButton.DoubleClick && IsSelected(mouseIndex))
                    {
                        EmitSignal("item_activated", mouseIndex);
                    }

                    AcceptEvent();
                }

                break;
            case InputEventScreenTouch { Pressed: true, Canceled: false } touch:
                var touchIndex = GetItemAtPosition(touch.Position - GlobalPosition);
                if (touchIndex >= 0)
                {
                    Select(touchIndex, single: SelectMode != SelectModeEnum.Multi);
                    AcceptEvent();
                }

                break;
            case InputEventKey key when key.Pressed && IsActivationKey(key.Keycode):
                var selected = GetSelectedItems().FirstOrDefault(-1);
                if (selected >= 0)
                {
                    EmitSignal("item_activated", selected);
                    AcceptEvent();
                }

                break;
        }
    }

    /// <summary>
    /// Draws the list rows, selection backgrounds and text.
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
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="CanvasItem.DrawRect(Rect2, Color, bool, float, bool)"/>
    public override void _Draw()
    {
        var rowHeight = GetRowHeight();
        var font = GetThemeFont("font");
        var fontSize = GetThemeFontSize("font_size");
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var rect = new Rect2(0f, index * rowHeight, Size.X, rowHeight);
            if (item.Selected)
            {
                DrawRect(rect, HasThemeColor("selected_color") ? GetThemeColor("selected_color") : new Color(0.24f, 0.38f, 0.62f, 1f));
            }
            else if (index % 2 == 1)
            {
                DrawRect(rect, new Color(0.12f, 0.13f, 0.15f, 0.35f));
            }

            if (font is not null && item.Text.Length > 0)
            {
                var color = item.Disabled ? new Color(0.55f, 0.56f, 0.60f, 1f) : Color.White;
                DrawString(font, new Vector2(6f, (index * rowHeight) + MathF.Max(font.GetAscent(fontSize), ((rowHeight - font.GetHeight(fontSize)) * 0.5f) + font.GetAscent(fontSize))), item.Text, HorizontalAlignment.Left, Size.X - 12f, fontSize, color);
            }
        }
    }

    /// <summary>
    /// Gets the minimum size requested by this list.
    /// </summary>
    ///
    /// <returns>
    /// A size large enough to show the stored rows at the current row height.
    /// </returns>
    ///
    /// <remarks>
    /// The minimum width uses <see cref="FixedColumnWidth"/> when it is
    /// positive; otherwise it uses a conservative fallback.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Control.GetMinimumSize"/>
    public override Vector2 _GetMinimumSize()
    {
        var width = fixedColumnWidth > 0 ? fixedColumnWidth * maxColumns : 96;
        return new Vector2(width, items.Count * GetRowHeight());
    }

    private ItemListItem GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Item index is outside the list.");
        }

        return items[index];
    }

    private void DeselectAllExcept(int keepIndex)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (index != keepIndex)
            {
                items[index].Selected = false;
            }
        }
    }

    private void KeepOnlyFirstSelected()
    {
        var keepFound = false;
        foreach (var item in items)
        {
            if (!item.Selected)
            {
                continue;
            }

            if (!keepFound)
            {
                keepFound = true;
                continue;
            }

            item.Selected = false;
        }
    }

    private int GetRowHeight()
    {
        return Math.Max(1, GetThemeConstantOrDefault("row_height", DefaultRowHeight));
    }

    private bool IsSelectableMouseButton(InputEventMouseButton mouseButton)
    {
        return mouseButton.Pressed &&
            (mouseButton.ButtonIndex == MouseButton.Left ||
            (AllowRmbSelect && mouseButton.ButtonIndex == MouseButton.Right));
    }

    private static bool CanSelect(ItemListItem item)
    {
        return item.Selectable && !item.Disabled;
    }

    private static bool IsActivationKey(Key key)
    {
        return key is Key.Enter or Key.KpEnter or Key.Space;
    }

    private static void ValidateNonNegative(Vector2I value, string parameterName)
    {
        if (value.X < 0 || value.Y < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Vector components must be non-negative.");
        }
    }

    private sealed class ItemListItem
    {
        public ItemListItem(string text, Texture2D? icon, bool selectable)
        {
            Text = text;
            Icon = icon;
            Selectable = selectable;
        }

        public string Text { get; set; }

        public Texture2D? Icon { get; set; }

        public bool Selectable { get; set; }

        public bool Disabled { get; set; }

        public bool Selected { get; set; }
    }
}
