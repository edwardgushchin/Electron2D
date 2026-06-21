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
/// Provides the Godot-like base node for 2D user interface controls.
/// </summary>
///
/// <remarks>
/// `Control` inherits from <see cref="CanvasItem" /> and adds a rectangular UI
/// area plus minimal theme font overrides used by <see cref="Label" /> in
/// Electron2D 0.1.0 Preview. Anchors, containers, focus and input filtering are
/// planned UI tasks and are not implemented by this baseline.
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate controls on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="CanvasItem" />
/// <seealso cref="Label" />
public class Control : CanvasItem
{
    private readonly Dictionary<string, Font> fontOverrides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> fontSizeOverrides = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the local position of this control.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the local size of this control.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Vector2 Size { get; set; } = Vector2.Zero;

    /// <summary>
    /// Adds or replaces a font theme override for this control.
    /// </summary>
    ///
    /// <param name="name">The theme font name, for example <c>font</c>.</param>
    /// <param name="font">The font resource to use.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="font" /> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void AddThemeFontOverride(string name, Font font)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(font);

        fontOverrides[name] = font;
        QueueRedraw();
    }

    /// <summary>
    /// Gets a font theme override by name.
    /// </summary>
    ///
    /// <param name="name">The theme font name.</param>
    /// <returns>The overridden font, or <c>null</c> when no override exists.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public Font? GetThemeFont(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return fontOverrides.TryGetValue(name, out var font) ? font : null;
    }

    /// <summary>
    /// Adds or replaces a font size theme override for this control.
    /// </summary>
    ///
    /// <param name="name">The theme font size name, for example <c>font_size</c>.</param>
    /// <param name="fontSize">The font size in pixels. It must be greater than zero.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fontSize" /> is less than or equal to zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public void AddThemeFontSizeOverride(string name, int fontSize)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), fontSize, "Font size must be greater than zero.");
        }

        fontSizeOverrides[name] = fontSize;
        QueueRedraw();
    }

    /// <summary>
    /// Gets a font size theme override by name.
    /// </summary>
    ///
    /// <param name="name">The theme font size name.</param>
    /// <returns>The overridden font size in pixels, or <c>16</c> when no override exists.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public int GetThemeFontSize(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return fontSizeOverrides.TryGetValue(name, out var fontSize) ? fontSize : 16;
    }

    internal Vector2 GlobalPosition
    {
        get
        {
            ThrowIfFreed();
            return GetParent() switch
            {
                Control parentControl => parentControl.GlobalPosition + Position,
                Node2D parentNode2D => parentNode2D.GlobalTransform.Xform(Position),
                _ => Position
            };
        }
    }

    internal Transform2D GlobalTransform => new(Vector2.Right, Vector2.Down, GlobalPosition);
}
