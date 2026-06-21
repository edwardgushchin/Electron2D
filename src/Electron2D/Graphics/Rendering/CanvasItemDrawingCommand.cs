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

internal readonly struct CanvasItemDrawingCommand
{
    private CanvasItemDrawingCommand(
        CanvasItemRenderCommandKind kind,
        Vector2 position,
        Rect2 rect,
        Vector2[] points,
        Color[] colors,
        Vector2[] uvs,
        Texture2D? texture,
        Font? font,
        string text,
        TextLayout? textLayout,
        HorizontalAlignment alignment,
        float textWidth,
        int fontSize,
        Color modulate,
        float radius,
        float width,
        bool filled,
        bool antialiased)
    {
        Kind = kind;
        Position = position;
        Rect = rect;
        Points = points;
        Colors = colors;
        Uvs = uvs;
        Texture = texture;
        Font = font;
        Text = text;
        TextLayout = textLayout;
        Alignment = alignment;
        TextWidth = textWidth;
        FontSize = fontSize;
        Modulate = modulate;
        Radius = radius;
        Width = width;
        Filled = filled;
        Antialiased = antialiased;
    }

    public CanvasItemRenderCommandKind Kind { get; }

    public Vector2 Position { get; }

    public Rect2 Rect { get; }

    public IReadOnlyList<Vector2> Points { get; }

    public IReadOnlyList<Color> Colors { get; }

    public IReadOnlyList<Vector2> Uvs { get; }

    public Texture2D? Texture { get; }

    public Font? Font { get; }

    public string Text { get; }

    public TextLayout? TextLayout { get; }

    public HorizontalAlignment Alignment { get; }

    public float TextWidth { get; }

    public int FontSize { get; }

    public Color Modulate { get; }

    public float Radius { get; }

    public float Width { get; }

    public bool Filled { get; }

    public bool Antialiased { get; }

    public static CanvasItemDrawingCommand CreateLine(
        Vector2 from,
        Vector2 to,
        Color color,
        float width,
        bool antialiased)
    {
        return new CanvasItemDrawingCommand(
            CanvasItemRenderCommandKind.Line,
            position: default,
            rect: default,
            new[] { from, to },
            Array.Empty<Color>(),
            Array.Empty<Vector2>(),
            texture: null,
            font: null,
            text: string.Empty,
            textLayout: null,
            HorizontalAlignment.Left,
            textWidth: -1f,
            fontSize: 16,
            color,
            radius: 0f,
            width,
            filled: true,
            antialiased);
    }

    public static CanvasItemDrawingCommand CreateRect(
        Rect2 rect,
        Color color,
        bool filled,
        float width,
        bool antialiased)
    {
        return new CanvasItemDrawingCommand(
            CanvasItemRenderCommandKind.Rect,
            position: default,
            rect,
            Array.Empty<Vector2>(),
            Array.Empty<Color>(),
            Array.Empty<Vector2>(),
            texture: null,
            font: null,
            text: string.Empty,
            textLayout: null,
            HorizontalAlignment.Left,
            textWidth: -1f,
            fontSize: 16,
            color,
            radius: 0f,
            width,
            filled,
            antialiased);
    }

    public static CanvasItemDrawingCommand CreateCircle(
        Vector2 position,
        float radius,
        Color color,
        bool filled,
        float width,
        bool antialiased)
    {
        return new CanvasItemDrawingCommand(
            CanvasItemRenderCommandKind.Circle,
            position,
            rect: default,
            Array.Empty<Vector2>(),
            Array.Empty<Color>(),
            Array.Empty<Vector2>(),
            texture: null,
            font: null,
            text: string.Empty,
            textLayout: null,
            HorizontalAlignment.Left,
            textWidth: -1f,
            fontSize: 16,
            color,
            radius,
            width,
            filled,
            antialiased);
    }

    public static CanvasItemDrawingCommand CreatePolygon(
        Vector2[] points,
        Color[] colors,
        Vector2[] uvs,
        Texture2D? texture)
    {
        return new CanvasItemDrawingCommand(
            CanvasItemRenderCommandKind.Polygon,
            position: default,
            rect: default,
            points,
            colors,
            uvs,
            texture,
            font: null,
            text: string.Empty,
            textLayout: null,
            HorizontalAlignment.Left,
            textWidth: -1f,
            fontSize: 16,
            Color.White,
            radius: 0f,
            width: -1f,
            filled: true,
            antialiased: false);
    }

    public static CanvasItemDrawingCommand CreateTexture(
        Texture2D texture,
        Vector2 position,
        Color modulate)
    {
        var size = texture.GetSize();
        return new CanvasItemDrawingCommand(
            CanvasItemRenderCommandKind.Texture,
            position,
            new Rect2(position, new Vector2(size.X, size.Y)),
            Array.Empty<Vector2>(),
            Array.Empty<Color>(),
            Array.Empty<Vector2>(),
            texture,
            font: null,
            text: string.Empty,
            textLayout: null,
            HorizontalAlignment.Left,
            textWidth: -1f,
            fontSize: 16,
            modulate,
            radius: 0f,
            width: -1f,
            filled: true,
            antialiased: false);
    }

    public static CanvasItemDrawingCommand CreateString(
        Font font,
        Vector2 position,
        string text,
        HorizontalAlignment alignment,
        float width,
        int fontSize,
        Color modulate)
    {
        var textLayout = font.GetTextLayout(text, alignment, width, fontSize);
        return new CanvasItemDrawingCommand(
            CanvasItemRenderCommandKind.String,
            position,
            textLayout.GetDestinationRect(position),
            Array.Empty<Vector2>(),
            Array.Empty<Color>(),
            Array.Empty<Vector2>(),
            texture: null,
            font,
            text,
            textLayout,
            alignment,
            width,
            fontSize,
            modulate,
            radius: 0f,
            width: -1f,
            filled: true,
            antialiased: false);
    }
}
