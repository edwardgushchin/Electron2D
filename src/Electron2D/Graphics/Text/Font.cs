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
using System.Runtime.CompilerServices;
using System.Text;

namespace Electron2D;

/// <summary>
/// Provides the Electron2D base resource for text layout and drawing.
/// </summary>
///
/// <remarks>
/// <para>
/// `Font` supplies the text metrics used by <see cref="CanvasItem.DrawString" />
/// and <see cref="Label" />. Electron2D 0.1.0 Preview implements a minimal
/// layout cache, Unicode scalar enumeration through <see cref="Rune" /> and
/// fallback font resolution. Concrete font loading remains a later resource
/// import task.
/// </para>
///
/// <para>
/// The public surface follows Electron2D's text resource contract. Native text
/// objects stay internal to the renderer backend.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create, mutate and query font resources on
/// the main thread that owns the scene tree.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="CanvasItem.DrawString" />
public abstract class Font : Resource
{
    private readonly TextLayoutCache layoutCache = new();

    /// <summary>
    /// Returns the size required to draw a single-line string.
    /// </summary>
    ///
    /// <param name="text">The Unicode text to measure.</param>
    /// <param name="alignment">The horizontal alignment used when <paramref name="width" /> is non-negative.</param>
    /// <param name="width">The optional alignment width. Negative values measure without a width constraint.</param>
    /// <param name="fontSize">The requested font size in pixels.</param>
    /// <returns>The measured string size in pixels.</returns>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <c>null</c>.
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
    public Vector2 GetStringSize(
        string text,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        float width = -1f,
        int fontSize = 16)
    {
        return GetTextLayout(text, alignment, width, fontSize).Size;
    }

    /// <summary>
    /// Returns the line height for a font size.
    /// </summary>
    ///
    /// <param name="fontSize">The requested font size in pixels.</param>
    /// <returns>The line height in pixels.</returns>
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
    public float GetHeight(int fontSize = 16)
    {
        ValidateFontSize(fontSize);
        return GetAscent(fontSize) + GetDescent(fontSize);
    }

    /// <summary>
    /// Returns the ascent above the text baseline for a font size.
    /// </summary>
    ///
    /// <param name="fontSize">The requested font size in pixels.</param>
    /// <returns>The ascent in pixels.</returns>
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
    public float GetAscent(int fontSize = 16)
    {
        ValidateFontSize(fontSize);
        return GetFontAscent(fontSize);
    }

    /// <summary>
    /// Returns the descent below the text baseline for a font size.
    /// </summary>
    ///
    /// <param name="fontSize">The requested font size in pixels.</param>
    /// <returns>The descent in pixels.</returns>
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
    public float GetDescent(int fontSize = 16)
    {
        ValidateFontSize(fontSize);
        return GetFontDescent(fontSize);
    }

    /// <summary>
    /// Checks whether this font contains a glyph for a Unicode codepoint.
    /// </summary>
    ///
    /// <param name="charCode">The Unicode scalar value to query.</param>
    /// <returns><c>true</c> when the font contains the glyph; otherwise, <c>false</c>.</returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool HasChar(int charCode)
    {
        return Rune.IsValid(charCode) && HasGlyph(new Rune(charCode));
    }

    internal virtual int TextLayoutGeneration => 0;

    internal TextLayout GetTextLayout(
        string text,
        HorizontalAlignment alignment,
        float width,
        int fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        ValidateFontSize(fontSize);

        var key = new TextLayoutKey(
            text,
            alignment,
            width,
            fontSize,
            GetCacheGeneration());
        return layoutCache.GetOrCreate(key, () => CreateTextLayout(text, alignment, width, fontSize));
    }

    internal TextLayoutCacheStats GetTextLayoutCacheStats()
    {
        return layoutCache.GetStats();
    }

    internal virtual bool HasGlyph(Rune rune)
    {
        return true;
    }

    internal virtual float GetGlyphAdvance(Rune rune, int fontSize)
    {
        return fontSize / 2f;
    }

    internal virtual IReadOnlyList<Font> GetFallbackFonts()
    {
        return Array.Empty<Font>();
    }

    internal virtual float GetFontAscent(int fontSize)
    {
        return fontSize * 0.8f;
    }

    internal virtual float GetFontDescent(int fontSize)
    {
        return fontSize * 0.2f;
    }

    private TextLayout CreateTextLayout(
        string text,
        HorizontalAlignment alignment,
        float width,
        int fontSize)
    {
        var runes = text.EnumerateRunes().ToArray();
        var direction = runes.Any(IsRightToLeftRune)
            ? TextLayoutDirection.RightToLeft
            : TextLayoutDirection.LeftToRight;
        var advances = runes.Select(rune => ResolveFont(rune).Font.GetGlyphAdvance(rune, fontSize)).ToArray();
        var textWidth = advances.Sum();
        var height = GetHeight(fontSize);
        var alignmentOffset = GetAlignmentOffset(alignment, width, textWidth);
        var glyphs = new List<TextGlyph>(runes.Length);

        if (direction == TextLayoutDirection.LeftToRight)
        {
            var cursor = alignmentOffset;
            for (var index = 0; index < runes.Length; index++)
            {
                var rune = runes[index];
                var resolved = ResolveFont(rune);
                var advance = advances[index];
                glyphs.Add(new TextGlyph(rune.Value, resolved.Font, new Vector2(cursor, 0f), advance, resolved.Available));
                cursor += advance;
            }
        }
        else
        {
            var cursor = alignmentOffset + textWidth;
            for (var index = runes.Length - 1; index >= 0; index--)
            {
                var rune = runes[index];
                var resolved = ResolveFont(rune);
                var advance = advances[index];
                cursor -= advance;
                glyphs.Add(new TextGlyph(rune.Value, resolved.Font, new Vector2(cursor, 0f), advance, resolved.Available));
            }
        }

        return new TextLayout(
            text,
            alignment,
            width,
            fontSize,
            direction,
            new Vector2(textWidth, height),
            alignmentOffset,
            GetAscent(fontSize),
            GetDescent(fontSize),
            glyphs);
    }

    private (Font Font, bool Available) ResolveFont(Rune rune)
    {
        if (HasGlyph(rune))
        {
            return (this, true);
        }

        foreach (var fallback in GetFallbackFonts())
        {
            if (fallback.HasGlyph(rune))
            {
                return (fallback, true);
            }
        }

        return (this, false);
    }

    private int GetCacheGeneration()
    {
        var hash = new HashCode();
        hash.Add(TextLayoutGeneration);
        foreach (var fallback in GetFallbackFonts())
        {
            hash.Add(RuntimeHelpers.GetHashCode(fallback));
            hash.Add(fallback.TextLayoutGeneration);
        }

        return hash.ToHashCode();
    }

    private static float GetAlignmentOffset(HorizontalAlignment alignment, float width, float textWidth)
    {
        if (width < 0f)
        {
            return 0f;
        }

        return alignment switch
        {
            HorizontalAlignment.Center => MathF.Max(0f, (width - textWidth) / 2f),
            HorizontalAlignment.Right => MathF.Max(0f, width - textWidth),
            _ => 0f
        };
    }

    private static bool IsRightToLeftRune(Rune rune)
    {
        return rune.Value is >= 0x0590 and <= 0x08FF;
    }

    private static void ValidateFontSize(int fontSize)
    {
        if (fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), fontSize, "Font size must be greater than zero.");
        }
    }
}
