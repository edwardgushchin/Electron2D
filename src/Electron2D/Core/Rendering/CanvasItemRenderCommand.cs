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

internal readonly struct CanvasItemRenderCommand
{
    public CanvasItemRenderCommand(
        Rid canvasItem,
        CanvasItemBatchKey batchKey,
        int layer,
        int zIndex,
        bool ySortEnabled,
        float ySortPosition,
        long treeOrder,
        bool visible,
        Color modulate,
        Color selfModulate,
        Color? commandModulate = null,
        CanvasItemRenderCommandKind kind = CanvasItemRenderCommandKind.Texture,
        Transform2D? transform = null,
        Rect2? sourceRect = null,
        Rect2? destinationRect = null,
        Vector2? position = null,
        IReadOnlyList<Vector2>? points = null,
        IReadOnlyList<Color>? colors = null,
        IReadOnlyList<Vector2>? uvs = null,
        Texture2D? texture = null,
        Font? font = null,
        string? text = null,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        float textWidth = -1f,
        int fontSize = 16,
        float radius = 0f,
        float width = -1f,
        bool filled = true,
        bool antialiased = false,
        bool flipH = false,
        bool flipV = false,
        string? debugName = null)
    {
        if (!canvasItem.IsValid())
        {
            throw new ArgumentException("Canvas item RID must be valid.", nameof(canvasItem));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(treeOrder);

        CanvasItem = canvasItem;
        BatchKey = batchKey;
        Layer = layer;
        ZIndex = zIndex;
        YSortEnabled = ySortEnabled;
        YSortPosition = ySortPosition;
        TreeOrder = treeOrder;
        Visible = visible;
        Modulate = modulate;
        SelfModulate = selfModulate;
        CommandModulate = commandModulate ?? Color.White;
        Kind = kind;
        Transform = transform ?? Transform2D.Identity;
        SourceRect = sourceRect ?? new Rect2();
        DestinationRect = destinationRect ?? new Rect2();
        Position = position ?? Vector2.Zero;
        Points = (points ?? Array.Empty<Vector2>()).ToArray();
        Colors = (colors ?? Array.Empty<Color>()).ToArray();
        Uvs = (uvs ?? Array.Empty<Vector2>()).ToArray();
        Texture = texture;
        Font = font;
        Text = text ?? string.Empty;
        Alignment = alignment;
        TextWidth = textWidth;
        FontSize = fontSize;
        Radius = radius;
        Width = width;
        Filled = filled;
        Antialiased = antialiased;
        FlipH = flipH;
        FlipV = flipV;
        DebugName = debugName ?? string.Empty;
    }

    public Rid CanvasItem { get; }

    public CanvasItemBatchKey BatchKey { get; }

    public int Layer { get; }

    public int ZIndex { get; }

    public bool YSortEnabled { get; }

    public float YSortPosition { get; }

    public long TreeOrder { get; }

    public bool Visible { get; }

    public Color Modulate { get; }

    public Color SelfModulate { get; }

    public Color CommandModulate { get; }

    public Color EffectiveModulate => Modulate * SelfModulate * CommandModulate;

    public CanvasItemRenderCommandKind Kind { get; }

    public Transform2D Transform { get; }

    public Rect2 SourceRect { get; }

    public Rect2 DestinationRect { get; }

    public Vector2 Position { get; }

    public IReadOnlyList<Vector2> Points { get; }

    public IReadOnlyList<Color> Colors { get; }

    public IReadOnlyList<Vector2> Uvs { get; }

    public Texture2D? Texture { get; }

    public Font? Font { get; }

    public string Text { get; }

    public HorizontalAlignment Alignment { get; }

    public float TextWidth { get; }

    public int FontSize { get; }

    public float Radius { get; }

    public float Width { get; }

    public bool Filled { get; }

    public bool Antialiased { get; }

    public bool FlipH { get; }

    public bool FlipV { get; }

    public string DebugName { get; }
}
