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
/// Displays a temporary menu of command items.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>PopupMenu</c> stores labeled items, separators and checkable commands.
/// It emits both item index and command id when an enabled item is activated.
/// </para>
/// <para>
/// The 0.1.0 Preview implementation is a <see cref="Control"/>-based runtime
/// menu. Native window integration and submenus are outside this task.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate popup menus on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Control"/>
/// <seealso cref="ItemList"/>
public class PopupMenu : Control
{
    private const int DefaultRowHeight = 24;
    private readonly List<PopupMenuItem> items = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PopupMenu"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The constructor registers <c>index_pressed</c> and <c>id_pressed</c>
    /// signals and hides the menu until <see cref="Popup(Rect2)"/> is called.
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
    /// <seealso cref="PopupMenu"/>
    public PopupMenu()
    {
        FocusMode = FocusMode.All;
        ClipContents = true;
        Visible = false;
        AddUserSignal("index_pressed");
        AddUserSignal("id_pressed");
    }

    /// <summary>
    /// Adds a plain text command item.
    /// </summary>
    ///
    /// <param name="label">The item label.</param>
    /// <param name="id">The command id emitted by <c>id_pressed</c>.</param>
    ///
    /// <remarks>
    /// Passing <c>-1</c> for <paramref name="id"/> stores the current item
    /// index as the command id.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="label"/> is <c>null</c>.
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
    /// <seealso cref="AddIconItem(Texture2D, string, int)"/>
    public void AddItem(string label, int id = -1)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(label);
        AddMenuItem(new PopupMenuItem(label, icon: null, id, checkable: false, separator: false));
    }

    /// <summary>
    /// Adds a command item with an icon.
    /// </summary>
    ///
    /// <param name="texture">The icon texture.</param>
    /// <param name="label">The item label.</param>
    /// <param name="id">The command id emitted by <c>id_pressed</c>.</param>
    ///
    /// <remarks>
    /// Icon drawing uses the existing texture draw path when a renderer consumes
    /// the generated canvas commands.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="texture"/> or <paramref name="label"/> is
    /// <c>null</c>.
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
    /// <seealso cref="AddItem(string, int)"/>
    public void AddIconItem(Texture2D texture, string label, int id = -1)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(label);
        AddMenuItem(new PopupMenuItem(label, texture, id, checkable: false, separator: false));
    }

    /// <summary>
    /// Adds a checkable command item.
    /// </summary>
    ///
    /// <param name="label">The item label.</param>
    /// <param name="id">The command id emitted by <c>id_pressed</c>.</param>
    ///
    /// <remarks>
    /// The item starts unchecked. Use <see cref="SetItemChecked"/> to change
    /// the checked state.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="label"/> is <c>null</c>.
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
    /// <seealso cref="AddIconCheckItem(Texture2D, string, int)"/>
    public void AddCheckItem(string label, int id = -1)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(label);
        AddMenuItem(new PopupMenuItem(label, icon: null, id, checkable: true, separator: false));
    }

    /// <summary>
    /// Adds a checkable command item with an icon.
    /// </summary>
    ///
    /// <param name="texture">The icon texture.</param>
    /// <param name="label">The item label.</param>
    /// <param name="id">The command id emitted by <c>id_pressed</c>.</param>
    ///
    /// <remarks>
    /// The item starts unchecked and can be toggled by
    /// <see cref="SetItemChecked"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="texture"/> or <paramref name="label"/> is
    /// <c>null</c>.
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
    /// <seealso cref="AddCheckItem(string, int)"/>
    public void AddIconCheckItem(Texture2D texture, string label, int id = -1)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(label);
        AddMenuItem(new PopupMenuItem(label, texture, id, checkable: true, separator: false));
    }

    /// <summary>
    /// Adds a separator item.
    /// </summary>
    ///
    /// <param name="label">The optional separator label.</param>
    /// <param name="id">The separator id stored for metadata queries.</param>
    ///
    /// <remarks>
    /// Separators cannot be activated and do not emit press signals.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="label"/> is <c>null</c>.
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
    /// <seealso cref="IsItemSeparator(int)"/>
    public void AddSeparator(string label = "", int id = -1)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(label);
        AddMenuItem(new PopupMenuItem(label, icon: null, id, checkable: false, separator: true));
    }

    /// <summary>
    /// Removes every menu item.
    /// </summary>
    ///
    /// <remarks>
    /// Visibility and registered signals are not changed.
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
    /// <seealso cref="AddItem(string, int)"/>
    public void Clear()
    {
        ThrowIfFreed();
        items.Clear();
        QueueRedraw();
    }

    /// <summary>
    /// Gets the number of menu items.
    /// </summary>
    ///
    /// <returns>
    /// The current menu item count.
    /// </returns>
    ///
    /// <remarks>
    /// Separators are included in the count.
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
    /// <seealso cref="Clear"/>
    public int GetItemCount()
    {
        ThrowIfFreed();
        return items.Count;
    }

    /// <summary>
    /// Shows this menu inside a rectangle.
    /// </summary>
    ///
    /// <param name="bounds">The global rectangle used for the menu position and size.</param>
    ///
    /// <remarks>
    /// The menu becomes visible and grabs focus when it is already inside a
    /// scene tree.
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
    /// <seealso cref="CanvasItem.Hide"/>
    public void Popup(Rect2 bounds)
    {
        ThrowIfFreed();
        Position = bounds.Position;
        Size = bounds.Size;
        Visible = true;
        QueueRedraw();
        if (IsInsideTree())
        {
            GrabFocus();
        }
    }

    /// <summary>
    /// Sets the text of an item.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    /// <param name="text">The new item text.</param>
    ///
    /// <remarks>
    /// Assigning text queues a redraw.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="GetItemText(int)"/>
    public void SetItemText(int index, string text)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(text);
        GetItem(index).Text = text;
        QueueRedraw();
    }

    /// <summary>
    /// Gets the text of an item.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
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
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="SetItemText(int, string)"/>
    public string GetItemText(int index)
    {
        ThrowIfFreed();
        return GetItem(index).Text;
    }

    /// <summary>
    /// Sets the icon of an item.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    /// <param name="icon">The icon texture, or <c>null</c> to clear it.</param>
    ///
    /// <remarks>
    /// Separators may also store icons, although the preview drawing path does
    /// not render separator icons.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="GetItemIcon(int)"/>
    public void SetItemIcon(int index, Texture2D? icon)
    {
        ThrowIfFreed();
        GetItem(index).Icon = icon;
        QueueRedraw();
    }

    /// <summary>
    /// Gets the icon of an item.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    ///
    /// <returns>
    /// The item icon, or <c>null</c> when none is set.
    /// </returns>
    ///
    /// <remarks>
    /// Use <see cref="SetItemIcon(int, Texture2D?)"/> to replace the icon.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="SetItemIcon(int, Texture2D?)"/>
    public Texture2D? GetItemIcon(int index)
    {
        ThrowIfFreed();
        return GetItem(index).Icon;
    }

    /// <summary>
    /// Sets whether a checkable item is checked.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    /// <param name="checked"><c>true</c> to mark the item checked; otherwise, <c>false</c>.</param>
    ///
    /// <remarks>
    /// The value can be stored on plain items too, but only checkable items draw
    /// a check indicator.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="IsItemChecked(int)"/>
    public void SetItemChecked(int index, bool @checked)
    {
        ThrowIfFreed();
        GetItem(index).Checked = @checked;
        QueueRedraw();
    }

    /// <summary>
    /// Reports whether an item is checked.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item is checked; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Checked state is independent from activation.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="SetItemChecked(int, bool)"/>
    public bool IsItemChecked(int index)
    {
        ThrowIfFreed();
        return GetItem(index).Checked;
    }

    /// <summary>
    /// Sets whether an item ignores activation input.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    /// <param name="disabled"><c>true</c> to disable the item; otherwise, <c>false</c>.</param>
    ///
    /// <remarks>
    /// Disabled items still occupy rows and can be inspected through metadata
    /// methods.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="IsItemDisabled(int)"/>
    public void SetItemDisabled(int index, bool disabled)
    {
        ThrowIfFreed();
        GetItem(index).Disabled = disabled;
        QueueRedraw();
    }

    /// <summary>
    /// Reports whether an item is disabled.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item is disabled; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Disabled items cannot emit press signals from user input.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="SetItemDisabled(int, bool)"/>
    public bool IsItemDisabled(int index)
    {
        ThrowIfFreed();
        return GetItem(index).Disabled;
    }

    /// <summary>
    /// Sets the id stored on an item.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    /// <param name="id">The id emitted by <c>id_pressed</c>.</param>
    ///
    /// <remarks>
    /// Item ids are independent from item order.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="GetItemId(int)"/>
    public void SetItemId(int index, int id)
    {
        ThrowIfFreed();
        GetItem(index).Id = id;
    }

    /// <summary>
    /// Gets the id stored on an item.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    ///
    /// <returns>
    /// The item id.
    /// </returns>
    ///
    /// <remarks>
    /// The id is emitted through <c>id_pressed</c> when the item activates.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="SetItemId(int, int)"/>
    public int GetItemId(int index)
    {
        ThrowIfFreed();
        return GetItem(index).Id;
    }

    /// <summary>
    /// Reports whether an item is a separator.
    /// </summary>
    ///
    /// <param name="index">The item index.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item is a separator; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// Separators cannot be activated.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is outside the item list.
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
    /// <seealso cref="AddSeparator(string, int)"/>
    public bool IsItemSeparator(int index)
    {
        ThrowIfFreed();
        return GetItem(index).Separator;
    }

    /// <summary>
    /// Handles GUI input routed to this menu.
    /// </summary>
    ///
    /// <param name="inputEvent">The input event delivered by the viewport.</param>
    ///
    /// <remarks>
    /// Mouse and touch press activate the row under the pointer. Keyboard
    /// activation currently uses the first enabled item.
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
        if (!Visible)
        {
            return;
        }

        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButton:
                ActivateAtPosition(mouseButton.GlobalPosition - GlobalPosition);
                AcceptEvent();
                break;
            case InputEventScreenTouch { Pressed: true, Canceled: false } touch:
                ActivateAtPosition(touch.Position - GlobalPosition);
                AcceptEvent();
                break;
            case InputEventKey key when key.Pressed && IsActivationKey(key.Keycode):
                var index = items.FindIndex(CanActivate);
                if (index >= 0)
                {
                    ActivateItem(index);
                    AcceptEvent();
                }

                break;
            case InputEventKey { Pressed: true, Keycode: Key.Escape }:
                Hide();
                AcceptEvent();
                break;
        }
    }

    /// <summary>
    /// Draws the menu rows and labels.
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
        if (!Visible)
        {
            return;
        }

        DrawRect(new Rect2(Vector2.Zero, Size), HasThemeColor("panel") ? GetThemeColor("panel") : new Color(0.10f, 0.11f, 0.13f, 1f));
        var rowHeight = GetRowHeight();
        var font = GetThemeFont("font");
        var fontSize = GetThemeFontSize("font_size");
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var y = index * rowHeight;
            if (item.Separator)
            {
                DrawRect(new Rect2(6f, y + (rowHeight * 0.5f), MathF.Max(0f, Size.X - 12f), 1f), new Color(0.35f, 0.36f, 0.40f, 1f));
                continue;
            }

            if (font is not null && item.Text.Length > 0)
            {
                var prefix = item.Checkable && item.Checked ? "[x] " : item.Checkable ? "[ ] " : string.Empty;
                var color = item.Disabled ? new Color(0.55f, 0.56f, 0.60f, 1f) : Color.White;
                DrawString(font, new Vector2(8f, y + MathF.Max(font.GetAscent(fontSize), ((rowHeight - font.GetHeight(fontSize)) * 0.5f) + font.GetAscent(fontSize))), prefix + item.Text, HorizontalAlignment.Left, Size.X - 16f, fontSize, color);
            }
        }
    }

    /// <summary>
    /// Gets the minimum size requested by this popup menu.
    /// </summary>
    ///
    /// <returns>
    /// A conservative menu size based on item count.
    /// </returns>
    ///
    /// <remarks>
    /// The minimum width is fixed in the preview implementation.
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
        return new Vector2(96f, items.Count * GetRowHeight());
    }

    private void AddMenuItem(PopupMenuItem item)
    {
        if (item.Id < 0)
        {
            item.Id = items.Count;
        }

        items.Add(item);
        QueueRedraw();
    }

    private PopupMenuItem GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Item index is outside the menu.");
        }

        return items[index];
    }

    private void ActivateAtPosition(Vector2 localPosition)
    {
        if (localPosition.Y < 0f)
        {
            return;
        }

        var index = (int)MathF.Floor(localPosition.Y / GetRowHeight());
        if (index >= 0 && index < items.Count)
        {
            ActivateItem(index);
        }
    }

    private void ActivateItem(int index)
    {
        var item = items[index];
        if (!CanActivate(item))
        {
            return;
        }

        EmitSignal("index_pressed", index);
        EmitSignal("id_pressed", item.Id);
        Hide();
    }

    private int GetRowHeight()
    {
        return Math.Max(1, GetThemeConstantOrDefault("row_height", DefaultRowHeight));
    }

    private static bool CanActivate(PopupMenuItem item)
    {
        return !item.Disabled && !item.Separator;
    }

    private static bool IsActivationKey(Key key)
    {
        return key is Key.Enter or Key.KpEnter or Key.Space;
    }

    private sealed class PopupMenuItem
    {
        public PopupMenuItem(string text, Texture2D? icon, int id, bool checkable, bool separator)
        {
            Text = text;
            Icon = icon;
            Id = id;
            Checkable = checkable;
            Separator = separator;
        }

        public string Text { get; set; }

        public Texture2D? Icon { get; set; }

        public int Id { get; set; }

        public bool Checkable { get; }

        public bool Checked { get; set; }

        public bool Disabled { get; set; }

        public bool Separator { get; }
    }
}
