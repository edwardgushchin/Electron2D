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

internal sealed class SdlRendererDrawCommand
{
    private SdlRendererDrawCommand(
        SdlRendererDrawCommandKind kind,
        string sdlOperation,
        string debugName,
        int layer,
        int zIndex,
        long treeOrder,
        Transform2D transform,
        Rect2 sourceRect,
        Rect2 destinationRect,
        Vector2 position,
        IReadOnlyList<Vector2> points,
        IReadOnlyList<Color> colors,
        IReadOnlyList<Vector2> uvs,
        Color modulate,
        float width,
        float radius,
        bool filled,
        bool antialiased,
        bool flipH,
        bool flipV,
        string text,
        HorizontalAlignment alignment,
        float textWidth,
        int fontSize,
        int glyphCount,
        bool usesTexture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sdlOperation);
        ArgumentNullException.ThrowIfNull(debugName);
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(colors);
        ArgumentNullException.ThrowIfNull(uvs);
        ArgumentNullException.ThrowIfNull(text);

        Kind = kind;
        SdlOperation = sdlOperation;
        DebugName = debugName;
        Layer = layer;
        ZIndex = zIndex;
        TreeOrder = treeOrder;
        Transform = transform;
        SourceRect = sourceRect;
        DestinationRect = destinationRect;
        Position = position;
        Points = points.ToArray();
        Colors = colors.ToArray();
        Uvs = uvs.ToArray();
        Modulate = modulate;
        Width = width;
        Radius = radius;
        Filled = filled;
        Antialiased = antialiased;
        FlipH = flipH;
        FlipV = flipV;
        Text = text;
        Alignment = alignment;
        TextWidth = textWidth;
        FontSize = fontSize;
        GlyphCount = glyphCount;
        UsesTexture = usesTexture;
    }

    public SdlRendererDrawCommandKind Kind { get; }

    public string SdlOperation { get; }

    public string DebugName { get; }

    public int Layer { get; }

    public int ZIndex { get; }

    public long TreeOrder { get; }

    public Transform2D Transform { get; }

    public Rect2 SourceRect { get; }

    public Rect2 DestinationRect { get; }

    public Vector2 Position { get; }

    public IReadOnlyList<Vector2> Points { get; }

    public IReadOnlyList<Color> Colors { get; }

    public IReadOnlyList<Vector2> Uvs { get; }

    public Color Modulate { get; }

    public float Width { get; }

    public float Radius { get; }

    public bool Filled { get; }

    public bool Antialiased { get; }

    public bool FlipH { get; }

    public bool FlipV { get; }

    public string Text { get; }

    public HorizontalAlignment Alignment { get; }

    public float TextWidth { get; }

    public int FontSize { get; }

    public int GlyphCount { get; }

    public bool UsesTexture { get; }

    public static SdlRendererDrawCommand FromCanvasCommand(CanvasItemRenderCommand command)
    {
        var kind = MapKind(command.Kind);
        return new SdlRendererDrawCommand(
            kind,
            ResolveSdlOperation(kind, command),
            command.DebugName,
            command.Layer,
            command.ZIndex,
            command.TreeOrder,
            command.Transform,
            command.SourceRect,
            command.DestinationRect,
            command.Position,
            command.Points,
            command.Colors,
            command.Uvs,
            command.EffectiveModulate,
            command.Width,
            command.Radius,
            command.Filled,
            command.Antialiased,
            command.FlipH,
            command.FlipV,
            command.Text,
            command.Alignment,
            command.TextWidth,
            command.FontSize,
            command.TextLayout?.Glyphs.Count ?? 0,
            command.Texture is not null);
    }

    private static SdlRendererDrawCommandKind MapKind(CanvasItemRenderCommandKind kind)
    {
        return kind switch
        {
            CanvasItemRenderCommandKind.Texture => SdlRendererDrawCommandKind.Texture,
            CanvasItemRenderCommandKind.Line => SdlRendererDrawCommandKind.Line,
            CanvasItemRenderCommandKind.Rect => SdlRendererDrawCommandKind.Rect,
            CanvasItemRenderCommandKind.Circle => SdlRendererDrawCommandKind.Circle,
            CanvasItemRenderCommandKind.Polygon => SdlRendererDrawCommandKind.Polygon,
            CanvasItemRenderCommandKind.String => SdlRendererDrawCommandKind.Text,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported canvas command kind.")
        };
    }

    private static string ResolveSdlOperation(SdlRendererDrawCommandKind kind, CanvasItemRenderCommand command)
    {
        return kind switch
        {
            SdlRendererDrawCommandKind.Texture => RequiresRotatedTexturePath(command) ? "SDL_RenderTextureRotated" : "SDL_RenderTexture",
            SdlRendererDrawCommandKind.Line => "SDL_RenderLine",
            SdlRendererDrawCommandKind.Rect => command.Filled ? "SDL_RenderFillRect" : "SDL_RenderRect",
            SdlRendererDrawCommandKind.Circle => "SDL_RenderGeometryCircle",
            SdlRendererDrawCommandKind.Polygon => "SDL_RenderGeometry",
            SdlRendererDrawCommandKind.Text => "SDL_ttf_RenderText",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported SDL_Renderer command kind.")
        };
    }

    private static bool RequiresRotatedTexturePath(CanvasItemRenderCommand command)
    {
        return command.FlipH ||
            command.FlipV ||
            !command.Transform.X.IsEqualApprox(Vector2.Right) ||
            !command.Transform.Y.IsEqualApprox(Vector2.Down);
    }
}
