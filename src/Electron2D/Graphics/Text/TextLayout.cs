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

internal sealed class TextLayout
{
    public TextLayout(
        string text,
        HorizontalAlignment alignment,
        float width,
        int fontSize,
        TextLayoutDirection direction,
        Vector2 size,
        float alignmentOffset,
        float ascent,
        float descent,
        IReadOnlyList<TextGlyph> glyphs)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(glyphs);

        Text = text;
        Alignment = alignment;
        Width = width;
        FontSize = fontSize;
        Direction = direction;
        Size = size;
        AlignmentOffset = alignmentOffset;
        Ascent = ascent;
        Descent = descent;
        Glyphs = glyphs.ToArray();
    }

    public string Text { get; }

    public HorizontalAlignment Alignment { get; }

    public float Width { get; }

    public int FontSize { get; }

    public TextLayoutDirection Direction { get; }

    public Vector2 Size { get; }

    public float AlignmentOffset { get; }

    public float Ascent { get; }

    public float Descent { get; }

    public IReadOnlyList<TextGlyph> Glyphs { get; }

    public Rect2 GetDestinationRect(Vector2 baselinePosition)
    {
        return new Rect2(
            baselinePosition.X + AlignmentOffset,
            baselinePosition.Y - Ascent,
            Size.X,
            Size.Y);
    }
}
