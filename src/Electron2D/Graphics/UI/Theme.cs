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
/// Provides a reusable resource for styling UI controls.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>Theme</c> stores typed UI items by item name and theme type. Controls
/// resolve local overrides first, then the nearest theme assigned to a control
/// branch.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate themes on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Control.Theme"/>
/// <seealso cref="StyleBox"/>
public class Theme : Resource
{
    private readonly Dictionary<ThemeItemKey, Color> colors = new();
    private readonly Dictionary<ThemeItemKey, int> constants = new();
    private readonly Dictionary<ThemeItemKey, Font> fonts = new();
    private readonly Dictionary<ThemeItemKey, int> fontSizes = new();
    private readonly Dictionary<ThemeItemKey, Texture2D> icons = new();
    private readonly Dictionary<ThemeItemKey, StyleBox> styleBoxes = new();
    private float defaultBaseScale;
    private Font? defaultFont;
    private int defaultFontSize = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="Theme"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The new theme contains no items and uses runtime fallback values for
    /// missing theme lookups.
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
    /// <seealso cref="Theme"/>
    public Theme()
    {
    }

    /// <summary>
    /// Gets or sets the default base scale for this theme.
    /// </summary>
    ///
    /// <value>
    /// A finite non-negative scale. A value of <c>0</c> means the theme has no
    /// explicit base scale.
    /// </value>
    ///
    /// <remarks>
    /// Controls use this value to scale resolved constants and font sizes.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative or not finite.
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
    /// <seealso cref="HasDefaultBaseScale"/>
    public float DefaultBaseScale
    {
        get
        {
            ThrowIfFreed();
            return defaultBaseScale;
        }
        set
        {
            ThrowIfFreed();
            if (!Mathf.IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Default base scale must be finite and non-negative.");
            }

            defaultBaseScale = value;
        }
    }

    /// <summary>
    /// Gets or sets the default font for this theme.
    /// </summary>
    ///
    /// <value>
    /// The fallback font resource, or <c>null</c> when no default font is set.
    /// </value>
    ///
    /// <remarks>
    /// Controls use this value when a requested font item is not present.
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
    /// <seealso cref="HasDefaultFont"/>
    public Font? DefaultFont
    {
        get
        {
            ThrowIfFreed();
            return defaultFont;
        }
        set
        {
            ThrowIfFreed();
            defaultFont = value;
        }
    }

    /// <summary>
    /// Gets or sets the default font size for this theme.
    /// </summary>
    ///
    /// <value>
    /// The fallback font size in UI units. Values below <c>1</c> mean no
    /// default font size is set.
    /// </value>
    ///
    /// <remarks>
    /// Controls scale this value by the resolved theme base scale before using
    /// it for drawing.
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
    /// <seealso cref="HasDefaultFontSize"/>
    public int DefaultFontSize
    {
        get
        {
            ThrowIfFreed();
            return defaultFontSize;
        }
        set
        {
            ThrowIfFreed();
            defaultFontSize = value;
        }
    }

    /// <summary>
    /// Reports whether this theme has a valid default base scale.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when <see cref="DefaultBaseScale"/> is greater than
    /// <c>0</c>; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// A theme without a base scale falls back to the runtime scale
    /// <c>1</c>.
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
    /// <seealso cref="DefaultBaseScale"/>
    public bool HasDefaultBaseScale()
    {
        ThrowIfFreed();
        return defaultBaseScale > 0f;
    }

    /// <summary>
    /// Reports whether this theme has a default font.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when <see cref="DefaultFont"/> is not <c>null</c>;
    /// otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// A default font is used only when a specific font item is missing.
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
    /// <seealso cref="DefaultFont"/>
    public bool HasDefaultFont()
    {
        ThrowIfFreed();
        return DefaultFont is not null;
    }

    /// <summary>
    /// Reports whether this theme has a valid default font size.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when <see cref="DefaultFontSize"/> is greater than
    /// <c>0</c>; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// A default font size is used only when a specific font size item is
    /// missing.
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
    /// <seealso cref="DefaultFontSize"/>
    public bool HasDefaultFontSize()
    {
        ThrowIfFreed();
        return defaultFontSize > 0;
    }

    /// <summary>
    /// Removes all items and default values from this theme.
    /// </summary>
    ///
    /// <remarks>
    /// The theme remains usable after clearing. Later lookups fall back to
    /// runtime defaults until new items are added.
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
    /// <seealso cref="SetColor(string, string, Color)"/>
    public void Clear()
    {
        ThrowIfFreed();
        defaultBaseScale = 0f;
        DefaultFont = null;
        defaultFontSize = -1;
        colors.Clear();
        constants.Clear();
        fonts.Clear();
        fontSizes.Clear();
        icons.Clear();
        styleBoxes.Clear();
    }

    /// <summary>
    /// Sets a color item.
    /// </summary>
    ///
    /// <param name="name">The color item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    /// <param name="color">The color value to store.</param>
    ///
    /// <remarks>
    /// Existing items with the same name and theme type are replaced.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="GetColor(string, string)"/>
    public void SetColor(string name, string themeType, Color color)
    {
        ThrowIfFreed();
        colors[CreateKey(name, themeType)] = color;
    }

    /// <summary>
    /// Gets a color item.
    /// </summary>
    ///
    /// <param name="name">The color item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// The stored color, or opaque white when the item is missing.
    /// </returns>
    ///
    /// <remarks>
    /// Use <see cref="HasColor(string, string)"/> to distinguish an explicit
    /// white color from a missing item.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetColor(string, string, Color)"/>
    public Color GetColor(string name, string themeType)
    {
        ThrowIfFreed();
        return TryGetColor(name, themeType, out var color) ? color : new Color(1f, 1f, 1f, 1f);
    }

    /// <summary>
    /// Reports whether a color item exists.
    /// </summary>
    ///
    /// <param name="name">The color item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item exists; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// This method checks only color items and does not inspect defaults.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetColor(string, string, Color)"/>
    public bool HasColor(string name, string themeType)
    {
        ThrowIfFreed();
        return colors.ContainsKey(CreateKey(name, themeType));
    }

    /// <summary>
    /// Removes a color item.
    /// </summary>
    ///
    /// <param name="name">The color item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <remarks>
    /// Removing a missing item is a no-op.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="HasColor(string, string)"/>
    public void ClearColor(string name, string themeType)
    {
        ThrowIfFreed();
        colors.Remove(CreateKey(name, themeType));
    }

    /// <summary>
    /// Sets a constant item.
    /// </summary>
    ///
    /// <param name="name">The constant item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    /// <param name="constant">The non-negative integer value to store.</param>
    ///
    /// <remarks>
    /// Constants are scaled by controls when resolved through
    /// <see cref="Control.GetThemeConstant(string, string)"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="constant"/> is negative.
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
    /// <seealso cref="GetConstant(string, string)"/>
    public void SetConstant(string name, string themeType, int constant)
    {
        ThrowIfFreed();
        if (constant < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(constant), constant, "Theme constant must be non-negative.");
        }

        constants[CreateKey(name, themeType)] = constant;
    }

    /// <summary>
    /// Gets a constant item.
    /// </summary>
    ///
    /// <param name="name">The constant item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// The stored constant, or <c>0</c> when the item is missing.
    /// </returns>
    ///
    /// <remarks>
    /// Use <see cref="HasConstant(string, string)"/> to distinguish an explicit
    /// zero from a missing item.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetConstant(string, string, int)"/>
    public int GetConstant(string name, string themeType)
    {
        ThrowIfFreed();
        return TryGetConstant(name, themeType, out var constant) ? constant : 0;
    }

    /// <summary>
    /// Reports whether a constant item exists.
    /// </summary>
    ///
    /// <param name="name">The constant item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item exists; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// This method checks only constant items and does not inspect defaults.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetConstant(string, string, int)"/>
    public bool HasConstant(string name, string themeType)
    {
        ThrowIfFreed();
        return constants.ContainsKey(CreateKey(name, themeType));
    }

    /// <summary>
    /// Removes a constant item.
    /// </summary>
    ///
    /// <param name="name">The constant item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <remarks>
    /// Removing a missing item is a no-op.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="HasConstant(string, string)"/>
    public void ClearConstant(string name, string themeType)
    {
        ThrowIfFreed();
        constants.Remove(CreateKey(name, themeType));
    }

    /// <summary>
    /// Sets a font item.
    /// </summary>
    ///
    /// <param name="name">The font item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    /// <param name="font">The font resource to store.</param>
    ///
    /// <remarks>
    /// Existing items with the same name and theme type are replaced.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="font"/> is <c>null</c>.
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
    /// <seealso cref="GetFont(string, string)"/>
    public void SetFont(string name, string themeType, Font font)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(font);
        fonts[CreateKey(name, themeType)] = font;
    }

    /// <summary>
    /// Gets a font item.
    /// </summary>
    ///
    /// <param name="name">The font item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// The stored font, the default font, or <c>null</c> when neither exists.
    /// </returns>
    ///
    /// <remarks>
    /// Use <see cref="HasFont(string, string)"/> to distinguish a named item
    /// from the default font fallback.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetFont(string, string, Font)"/>
    public Font? GetFont(string name, string themeType)
    {
        ThrowIfFreed();
        return TryGetFont(name, themeType, out var font) ? font : DefaultFont;
    }

    /// <summary>
    /// Reports whether a font item exists.
    /// </summary>
    ///
    /// <param name="name">The font item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item exists; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// This method checks only named font items and does not inspect
    /// <see cref="DefaultFont"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetFont(string, string, Font)"/>
    public bool HasFont(string name, string themeType)
    {
        ThrowIfFreed();
        return fonts.ContainsKey(CreateKey(name, themeType));
    }

    /// <summary>
    /// Removes a font item.
    /// </summary>
    ///
    /// <param name="name">The font item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <remarks>
    /// Removing a missing item is a no-op.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="HasFont(string, string)"/>
    public void ClearFont(string name, string themeType)
    {
        ThrowIfFreed();
        fonts.Remove(CreateKey(name, themeType));
    }

    /// <summary>
    /// Sets a font size item.
    /// </summary>
    ///
    /// <param name="name">The font size item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    /// <param name="fontSize">The font size value. It must be greater than zero.</param>
    ///
    /// <remarks>
    /// Font sizes are scaled by controls when resolved through
    /// <see cref="Control.GetThemeFontSize(string, string)"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fontSize"/> is less than or equal to zero.
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
    /// <seealso cref="GetFontSize(string, string)"/>
    public void SetFontSize(string name, string themeType, int fontSize)
    {
        ThrowIfFreed();
        if (fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), fontSize, "Theme font size must be greater than zero.");
        }

        fontSizes[CreateKey(name, themeType)] = fontSize;
    }

    /// <summary>
    /// Gets a font size item.
    /// </summary>
    ///
    /// <param name="name">The font size item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// The stored font size, the default font size, or <c>16</c> when neither
    /// exists.
    /// </returns>
    ///
    /// <remarks>
    /// This method returns the raw theme value. Controls apply base scale when
    /// resolving font size items.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetFontSize(string, string, int)"/>
    public int GetFontSize(string name, string themeType)
    {
        ThrowIfFreed();
        if (TryGetFontSize(name, themeType, out var fontSize))
        {
            return fontSize;
        }

        return HasDefaultFontSize() ? DefaultFontSize : 16;
    }

    /// <summary>
    /// Reports whether a font size item exists.
    /// </summary>
    ///
    /// <param name="name">The font size item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item exists; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// This method checks only named font size items and does not inspect
    /// <see cref="DefaultFontSize"/>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetFontSize(string, string, int)"/>
    public bool HasFontSize(string name, string themeType)
    {
        ThrowIfFreed();
        return fontSizes.ContainsKey(CreateKey(name, themeType));
    }

    /// <summary>
    /// Removes a font size item.
    /// </summary>
    ///
    /// <param name="name">The font size item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <remarks>
    /// Removing a missing item is a no-op.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="HasFontSize(string, string)"/>
    public void ClearFontSize(string name, string themeType)
    {
        ThrowIfFreed();
        fontSizes.Remove(CreateKey(name, themeType));
    }

    /// <summary>
    /// Sets an icon item.
    /// </summary>
    ///
    /// <param name="name">The icon item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    /// <param name="icon">The texture resource to store.</param>
    ///
    /// <remarks>
    /// Existing items with the same name and theme type are replaced.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
    /// </exception>
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
    /// <seealso cref="GetIcon(string, string)"/>
    public void SetIcon(string name, string themeType, Texture2D icon)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(icon);
        icons[CreateKey(name, themeType)] = icon;
    }

    /// <summary>
    /// Gets an icon item.
    /// </summary>
    ///
    /// <param name="name">The icon item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// The stored icon, or <c>null</c> when the item is missing.
    /// </returns>
    ///
    /// <remarks>
    /// Icon items are runtime texture resources resolved by controls and
    /// custom widgets.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetIcon(string, string, Texture2D)"/>
    public Texture2D? GetIcon(string name, string themeType)
    {
        ThrowIfFreed();
        return TryGetIcon(name, themeType, out var icon) ? icon : null;
    }

    /// <summary>
    /// Reports whether an icon item exists.
    /// </summary>
    ///
    /// <param name="name">The icon item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item exists; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// This method checks only icon items.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetIcon(string, string, Texture2D)"/>
    public bool HasIcon(string name, string themeType)
    {
        ThrowIfFreed();
        return icons.ContainsKey(CreateKey(name, themeType));
    }

    /// <summary>
    /// Removes an icon item.
    /// </summary>
    ///
    /// <param name="name">The icon item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <remarks>
    /// Removing a missing item is a no-op.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="HasIcon(string, string)"/>
    public void ClearIcon(string name, string themeType)
    {
        ThrowIfFreed();
        icons.Remove(CreateKey(name, themeType));
    }

    /// <summary>
    /// Sets a style box item.
    /// </summary>
    ///
    /// <param name="name">The style box item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    /// <param name="styleBox">The style box resource to store.</param>
    ///
    /// <remarks>
    /// Existing items with the same name and theme type are replaced.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="styleBox"/> is <c>null</c>.
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
    /// <seealso cref="GetStyleBox(string, string)"/>
    public void SetStyleBox(string name, string themeType, StyleBox styleBox)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(styleBox);
        styleBoxes[CreateKey(name, themeType)] = styleBox;
    }

    /// <summary>
    /// Gets a style box item.
    /// </summary>
    ///
    /// <param name="name">The style box item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// The stored style box, or <c>null</c> when the item is missing.
    /// </returns>
    ///
    /// <remarks>
    /// Style boxes are used by controls to draw themed backgrounds and
    /// borders.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetStyleBox(string, string, StyleBox)"/>
    public StyleBox? GetStyleBox(string name, string themeType)
    {
        ThrowIfFreed();
        return TryGetStyleBox(name, themeType, out var styleBox) ? styleBox : null;
    }

    /// <summary>
    /// Reports whether a style box item exists.
    /// </summary>
    ///
    /// <param name="name">The style box item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <returns>
    /// <c>true</c> when the item exists; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// This method checks only style box items.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="SetStyleBox(string, string, StyleBox)"/>
    public bool HasStyleBox(string name, string themeType)
    {
        ThrowIfFreed();
        return styleBoxes.ContainsKey(CreateKey(name, themeType));
    }

    /// <summary>
    /// Removes a style box item.
    /// </summary>
    ///
    /// <param name="name">The style box item name.</param>
    /// <param name="themeType">The theme type that owns the item.</param>
    ///
    /// <remarks>
    /// Removing a missing item is a no-op.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="themeType"/> is
    /// empty.
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
    /// <seealso cref="HasStyleBox(string, string)"/>
    public void ClearStyleBox(string name, string themeType)
    {
        ThrowIfFreed();
        styleBoxes.Remove(CreateKey(name, themeType));
    }

    internal bool TryGetColor(string name, string themeType, out Color color)
    {
        return colors.TryGetValue(CreateKey(name, themeType), out color);
    }

    internal bool TryGetConstant(string name, string themeType, out int constant)
    {
        return constants.TryGetValue(CreateKey(name, themeType), out constant);
    }

    internal bool TryGetFont(string name, string themeType, out Font? font)
    {
        if (fonts.TryGetValue(CreateKey(name, themeType), out var value))
        {
            font = value;
            return true;
        }

        font = null;
        return false;
    }

    internal bool TryGetFontSize(string name, string themeType, out int fontSize)
    {
        return fontSizes.TryGetValue(CreateKey(name, themeType), out fontSize);
    }

    internal bool TryGetIcon(string name, string themeType, out Texture2D? icon)
    {
        if (icons.TryGetValue(CreateKey(name, themeType), out var value))
        {
            icon = value;
            return true;
        }

        icon = null;
        return false;
    }

    internal bool TryGetStyleBox(string name, string themeType, out StyleBox? styleBox)
    {
        if (styleBoxes.TryGetValue(CreateKey(name, themeType), out var value))
        {
            styleBox = value;
            return true;
        }

        styleBox = null;
        return false;
    }

    internal IReadOnlyList<ThemeColorItem> GetColorItemsForSerialization()
    {
        return colors.Select(pair => new ThemeColorItem(pair.Key.Name, pair.Key.ThemeType, pair.Value))
            .OrderBy(item => item.ThemeType, StringComparer.Ordinal)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<ThemeConstantItem> GetConstantItemsForSerialization()
    {
        return constants.Select(pair => new ThemeConstantItem(pair.Key.Name, pair.Key.ThemeType, pair.Value))
            .OrderBy(item => item.ThemeType, StringComparer.Ordinal)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<ThemeFontSizeItem> GetFontSizeItemsForSerialization()
    {
        return fontSizes.Select(pair => new ThemeFontSizeItem(pair.Key.Name, pair.Key.ThemeType, pair.Value))
            .OrderBy(item => item.ThemeType, StringComparer.Ordinal)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();
    }

    internal IReadOnlyList<ThemeStyleBoxItem> GetStyleBoxItemsForSerialization()
    {
        return styleBoxes.Select(pair => new ThemeStyleBoxItem(pair.Key.Name, pair.Key.ThemeType, pair.Value))
            .OrderBy(item => item.ThemeType, StringComparer.Ordinal)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static ThemeItemKey CreateKey(string name, string themeType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(themeType);
        return new ThemeItemKey(name, themeType);
    }

    private readonly record struct ThemeItemKey(string Name, string ThemeType);

    internal readonly record struct ThemeColorItem(string Name, string ThemeType, Color Value);

    internal readonly record struct ThemeConstantItem(string Name, string ThemeType, int Value);

    internal readonly record struct ThemeFontSizeItem(string Name, string ThemeType, int Value);

    internal readonly record struct ThemeStyleBoxItem(string Name, string ThemeType, StyleBox Value);
}
