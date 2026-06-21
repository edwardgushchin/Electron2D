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
        Transform2D? transform = null,
        Rect2? sourceRect = null,
        Rect2? destinationRect = null,
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
        Transform = transform ?? Transform2D.Identity;
        SourceRect = sourceRect ?? new Rect2();
        DestinationRect = destinationRect ?? new Rect2();
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

    public Color EffectiveModulate => Modulate * SelfModulate;

    public Transform2D Transform { get; }

    public Rect2 SourceRect { get; }

    public Rect2 DestinationRect { get; }

    public bool FlipH { get; }

    public bool FlipV { get; }

    public string DebugName { get; }
}
