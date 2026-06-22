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
/// Provides a single-line text editing control.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>LineEdit</c> accepts focused keyboard text input, basic caret movement,
/// deletion and submit input. It emits <c>text_changed</c>,
/// <c>text_submitted</c> and <c>text_change_rejected</c>.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate line edits on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Control"/>
/// <seealso cref="InputEventKey"/>
public class LineEdit : Control
{
    private string text = string.Empty;
    private string placeholderText = string.Empty;
    private string secretCharacter = "*";
    private int caretColumn;
    private int maxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="LineEdit"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The constructor registers text signals and enables focus by default.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="LineEdit"/>
    public LineEdit()
    {
        FocusMode = FocusMode.All;
        AddUserSignal("text_changed");
        AddUserSignal("text_submitted");
        AddUserSignal("text_change_rejected");
    }

    /// <summary>
    /// Gets or sets the editable text.
    /// </summary>
    ///
    /// <value>
    /// The current text value. The value is never <c>null</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Assigning text applies <see cref="MaxLength"/> and clamps
    /// <see cref="CaretColumn"/> to the new text length.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when the assigned value is <c>null</c>.
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
    /// <seealso cref="LineEdit"/>
    public string Text
    {
        get
        {
            ThrowIfFreed();
            return text;
        }
        set
        {
            ThrowIfFreed();
            ArgumentNullException.ThrowIfNull(value);
            SetText(value, emitSignal: true, rejectedText: null);
        }
    }

    /// <summary>
    /// Gets or sets the placeholder text drawn when <see cref="Text"/> is empty.
    /// </summary>
    /// <value>The placeholder text. The value is never <c>null</c>.</value>
    /// <remarks>Assigning this property queues a redraw.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when the assigned value is <c>null</c>.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="Text"/>
    public string PlaceholderText
    {
        get
        {
            ThrowIfFreed();
            return placeholderText;
        }
        set
        {
            ThrowIfFreed();
            ArgumentNullException.ThrowIfNull(value);
            placeholderText = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether user input can edit the text.
    /// </summary>
    /// <value><c>true</c> when user input can change text; otherwise, <c>false</c>.</value>
    /// <remarks>Programmatic methods can still change text when this property is <c>false</c>.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="_GuiInput(InputEvent)"/>
    public bool Editable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether text is drawn using <see cref="SecretCharacter"/>.
    /// </summary>
    /// <value><c>true</c> to hide the actual text while drawing; otherwise, <c>false</c>.</value>
    /// <remarks>This property affects drawing only. The <see cref="Text"/> value remains unchanged.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="SecretCharacter"/>
    public bool Secret { get; set; }

    /// <summary>
    /// Gets or sets the character string used when <see cref="Secret"/> is enabled.
    /// </summary>
    /// <value>A non-empty string used for each hidden text element.</value>
    /// <remarks>The value is repeated once per UTF-16 code unit in the preview implementation.</remarks>
    /// <exception cref="ArgumentException">Thrown when the assigned value is <c>null</c>, empty or whitespace.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="Secret"/>
    public string SecretCharacter
    {
        get
        {
            ThrowIfFreed();
            return secretCharacter;
        }
        set
        {
            ThrowIfFreed();
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            secretCharacter = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of UTF-16 code units accepted by this control.
    /// </summary>
    /// <value><c>0</c> for unlimited text, or a positive maximum length.</value>
    /// <remarks>Reducing the maximum length truncates existing text and clamps the caret.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is negative.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="Text"/>
    public int MaxLength
    {
        get
        {
            ThrowIfFreed();
            return maxLength;
        }
        set
        {
            ThrowIfFreed();
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            maxLength = value;
            if (maxLength > 0 && text.Length > maxLength)
            {
                SetText(text[..maxLength], emitSignal: true, rejectedText: text[maxLength..]);
            }
        }
    }

    /// <summary>
    /// Gets or sets the caret column.
    /// </summary>
    /// <value>The caret position clamped to the range <c>0</c> through <see cref="Text"/> length.</value>
    /// <remarks>The preview control stores one caret and no selection range.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="Text"/>
    public int CaretColumn
    {
        get
        {
            ThrowIfFreed();
            return caretColumn;
        }
        set
        {
            ThrowIfFreed();
            caretColumn = Math.Clamp(value, 0, text.Length);
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the horizontal alignment used for drawing text.
    /// </summary>
    /// <value>The alignment used inside <see cref="Control.Size"/>.</value>
    /// <remarks>Input editing still uses logical text order and a single caret.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="HorizontalAlignment"/>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>
    /// Clears all text from this control.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The caret moves to column zero and <c>text_changed</c> is emitted when
    /// the text actually changes.
    /// </para>
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
    /// <seealso cref="Text"/>
    public void Clear()
    {
        ThrowIfFreed();
        SetText(string.Empty, emitSignal: true, rejectedText: null);
    }

    /// <summary>
    /// Inserts text at the current caret column.
    /// </summary>
    ///
    /// <param name="value">The text to insert.</param>
    ///
    /// <remarks>
    /// <para>
    /// The method applies <see cref="MaxLength"/>. Rejected overflow text is
    /// emitted through <c>text_change_rejected</c>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <c>null</c>.
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
    /// <seealso cref="CaretColumn"/>
    public void InsertTextAtCaret(string value)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
        {
            return;
        }

        var available = maxLength <= 0 ? value.Length : Math.Max(0, maxLength - text.Length);
        var accepted = value.Length <= available ? value : value[..available];
        var rejected = value.Length <= available ? null : value[available..];
        if (accepted.Length > 0)
        {
            text = text.Insert(caretColumn, accepted);
            caretColumn += accepted.Length;
            EmitSignal("text_changed", text);
            QueueRedraw();
        }

        if (!string.IsNullOrEmpty(rejected))
        {
            EmitSignal("text_change_rejected", rejected);
        }
    }

    /// <summary>
    /// Deletes the character before the caret.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Calling this method at column zero is a no-op.
    /// </para>
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
    /// <seealso cref="CaretColumn"/>
    public void DeleteCharAtCaret()
    {
        ThrowIfFreed();
        if (caretColumn <= 0)
        {
            return;
        }

        DeleteText(caretColumn - 1, caretColumn);
    }

    /// <summary>
    /// Deletes text in the specified column range.
    /// </summary>
    ///
    /// <param name="fromColumn">The first column to delete.</param>
    /// <param name="toColumn">The column after the last deleted character.</param>
    ///
    /// <remarks>
    /// <para>
    /// Columns are clamped to the current text range. Reversed ranges are
    /// normalized before deletion.
    /// </para>
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
    /// <seealso cref="Text"/>
    public void DeleteText(int fromColumn, int toColumn)
    {
        ThrowIfFreed();
        var from = Math.Clamp(Math.Min(fromColumn, toColumn), 0, text.Length);
        var to = Math.Clamp(Math.Max(fromColumn, toColumn), 0, text.Length);
        if (from == to)
        {
            return;
        }

        text = text.Remove(from, to - from);
        caretColumn = Math.Clamp(from, 0, text.Length);
        EmitSignal("text_changed", text);
        QueueRedraw();
    }

    /// <summary>
    /// Handles focused text input and editing keys.
    /// </summary>
    /// <param name="inputEvent">The input event delivered by the viewport.</param>
    /// <remarks>Unsupported input events are ignored.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="Control._GuiInput(InputEvent)"/>
    public override void _GuiInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }:
            case InputEventScreenTouch { Pressed: true }:
                CaretColumn = text.Length;
                AcceptEvent();
                break;
            case InputEventKey key when key.Pressed:
                HandleKey(key);
                break;
        }
    }

    /// <summary>
    /// Draws the text field background and current text.
    /// </summary>
    /// <remarks>The preview implementation uses rectangle primitives and theme font overrides.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="CanvasItem.DrawString(Font, Vector2, string, HorizontalAlignment, float, int, Color?)"/>
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.10f, 0.11f, 0.13f, 1f));
        DrawRect(new Rect2(Vector2.Zero, Size), HasFocus() ? new Color(0.45f, 0.58f, 0.85f, 1f) : new Color(0.28f, 0.30f, 0.34f, 1f), filled: false, width: 1f);

        var font = GetThemeFont("font");
        if (font is null)
        {
            return;
        }

        var fontSize = GetThemeFontSize("font_size");
        var drawText = GetDrawText();
        if (drawText.Length == 0)
        {
            return;
        }

        var baseline = new Vector2(4f, MathF.Max(font.GetAscent(fontSize), ((Size.Y - font.GetHeight(fontSize)) * 0.5f) + font.GetAscent(fontSize)));
        DrawString(font, baseline, drawText, HorizontalAlignment, MathF.Max(0f, Size.X - 8f), fontSize, text.Length == 0 ? new Color(0.65f, 0.68f, 0.72f, 1f) : Color.White);
    }

    /// <summary>
    /// Gets the minimum size requested by this line edit.
    /// </summary>
    /// <returns>A conservative text-field size based on theme font state.</returns>
    /// <remarks>If no font is available, the method returns <c>96x24</c>.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1.0 Preview.</since>
    /// <seealso cref="Control.GetMinimumSize"/>
    public override Vector2 _GetMinimumSize()
    {
        var font = GetThemeFont("font");
        if (font is null)
        {
            return new Vector2(96f, 24f);
        }

        var fontSize = GetThemeFontSize("font_size");
        return new Vector2(96f, MathF.Max(24f, font.GetHeight(fontSize) + 8f));
    }

    private void HandleKey(InputEventKey key)
    {
        if (key.Keycode is Key.Enter or Key.KpEnter)
        {
            EmitSignal("text_submitted", text);
            AcceptEvent();
            return;
        }

        if (!Editable)
        {
            return;
        }

        switch (key.Keycode)
        {
            case Key.Backspace:
                DeleteCharAtCaret();
                AcceptEvent();
                return;
            case Key.Delete:
                if (caretColumn < text.Length)
                {
                    DeleteText(caretColumn, caretColumn + 1);
                }

                AcceptEvent();
                return;
            case Key.Left:
                CaretColumn--;
                AcceptEvent();
                return;
            case Key.Right:
                CaretColumn++;
                AcceptEvent();
                return;
            case Key.Home:
                CaretColumn = 0;
                AcceptEvent();
                return;
            case Key.End:
                CaretColumn = text.Length;
                AcceptEvent();
                return;
        }

        if (key.Unicode > 0)
        {
            InsertTextAtCaret(char.ConvertFromUtf32(key.Unicode));
            AcceptEvent();
        }
    }

    private void SetText(string value, bool emitSignal, string? rejectedText)
    {
        var accepted = value;
        var rejected = rejectedText;
        if (maxLength > 0 && accepted.Length > maxLength)
        {
            rejected = accepted[maxLength..];
            accepted = accepted[..maxLength];
        }

        if (text != accepted)
        {
            text = accepted;
            caretColumn = Math.Clamp(caretColumn, 0, text.Length);
            if (emitSignal)
            {
                EmitSignal("text_changed", text);
            }

            QueueRedraw();
        }

        if (!string.IsNullOrEmpty(rejected))
        {
            EmitSignal("text_change_rejected", rejected);
        }
    }

    private string GetDrawText()
    {
        if (text.Length == 0)
        {
            return placeholderText;
        }

        if (!Secret)
        {
            return text;
        }

        return string.Concat(Enumerable.Repeat(secretCharacter, text.Length));
    }
}
