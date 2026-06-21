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

internal sealed class CanvasSubmissionContext
{
    private readonly CanvasItemRenderQueue queue = new();
    private readonly RidAllocator textureAllocator = new();
    private readonly Dictionary<Texture2D, Rid> textureRids = new(ReferenceEqualityComparer.Instance);

    public CanvasItemRenderPlan BuildPlan(Node root)
    {
        ArgumentNullException.ThrowIfNull(root);

        queue.Clear();
        var state = new SubmissionState(
            Layer: 0,
            LayerVisible: true,
            LayerTransform: Transform2D.Identity,
            ParentCanvasItemActive: false,
            InheritedVisible: true,
            InheritedModulate: Color.White,
            SnapTransformsToPixel: false,
            SnapVerticesToPixel: false);
        var treeOrder = 0L;
        Traverse(root, state, ref treeOrder);
        return queue.BuildPlan();
    }

    private void Traverse(Node node, SubmissionState state, ref long treeOrder)
    {
        var currentState = state;
        if (node is Viewport viewport)
        {
            currentState = state with
            {
                LayerTransform = state.LayerTransform * viewport.GetFinalCanvasTransform(),
                ParentCanvasItemActive = false,
                InheritedVisible = state.LayerVisible,
                InheritedModulate = Color.White,
                SnapTransformsToPixel = viewport.Snap2DTransformsToPixel,
                SnapVerticesToPixel = viewport.Snap2DVerticesToPixel
            };
        }
        else if (node is CanvasLayer layer)
        {
            currentState = new SubmissionState(
                layer.Layer,
                state.LayerVisible && layer.Visible,
                state.LayerTransform * layer.GetFinalTransform(),
                ParentCanvasItemActive: false,
                InheritedVisible: state.LayerVisible && layer.Visible,
                InheritedModulate: Color.White,
                state.SnapTransformsToPixel,
                state.SnapVerticesToPixel);
        }
        else if (node is CanvasItem canvasItem)
        {
            var inheritedVisible = state.ParentCanvasItemActive ? state.InheritedVisible : state.LayerVisible;
            var inheritedModulate = state.ParentCanvasItemActive ? state.InheritedModulate : Color.White;
            var visible = inheritedVisible && canvasItem.Visible;
            var modulate = inheritedModulate * canvasItem.Modulate;
            currentState = state with
            {
                ParentCanvasItemActive = true,
                InheritedVisible = visible,
                InheritedModulate = modulate
            };

            if (canvasItem is Sprite2D sprite)
            {
                SubmitSprite(sprite, currentState, visible, modulate, treeOrder++);
            }

            foreach (var drawingCommand in canvasItem.DrawingCommands)
            {
                SubmitDrawingCommand(canvasItem, drawingCommand, currentState, visible, modulate, ref treeOrder);
            }
        }
        else
        {
            currentState = state with
            {
                ParentCanvasItemActive = false,
                InheritedVisible = state.LayerVisible,
                InheritedModulate = Color.White
            };
        }

        foreach (var child in node.GetChildrenSnapshot())
        {
            Traverse(child, currentState, ref treeOrder);
        }
    }

    private void SubmitSprite(Sprite2D sprite, SubmissionState state, bool visible, Color modulate, long treeOrder)
    {
        if (sprite.Texture is null)
        {
            return;
        }

        var textureRid = GetTextureRid(sprite.Texture);
        var key = new CanvasItemBatchKey(textureRid, material: default, clip: default, CanvasItemBlendMode.Mix);
        var transform = state.LayerTransform * sprite.GlobalTransform;
        if (state.SnapTransformsToPixel)
        {
            transform.Origin = transform.Origin.Round();
        }

        var destinationRect = sprite.GetRect();
        if (state.SnapVerticesToPixel)
        {
            destinationRect = SnapRect(destinationRect);
        }

        queue.Add(new CanvasItemRenderCommand(
            sprite.CanvasItemRid,
            key,
            state.Layer,
            sprite.ZIndex,
            sprite.YSortEnabled,
            sprite.GlobalPosition.Y,
            treeOrder,
            state.LayerVisible && visible,
            modulate,
            sprite.SelfModulate,
            transform: transform,
            sourceRect: sprite.GetSourceRect(),
            destinationRect: destinationRect,
            texture: sprite.Texture,
            flipH: sprite.FlipH,
            flipV: sprite.FlipV,
            debugName: sprite.Name));
    }

    private void SubmitDrawingCommand(
        CanvasItem canvasItem,
        CanvasItemDrawingCommand drawingCommand,
        SubmissionState state,
        bool visible,
        Color modulate,
        ref long treeOrder)
    {
        var textureRid = drawingCommand.Texture is null ? default : GetTextureRid(drawingCommand.Texture);
        var key = new CanvasItemBatchKey(
            textureRid,
            material: default,
            clip: default,
            CanvasItemBlendMode.Mix,
            drawingCommand.Kind);
        var transform = state.LayerTransform * GetCanvasItemTransform(canvasItem);
        if (state.SnapTransformsToPixel)
        {
            transform.Origin = transform.Origin.Round();
        }

        var destinationRect = drawingCommand.Rect;
        if (state.SnapVerticesToPixel && destinationRect.Size != Vector2.Zero)
        {
            destinationRect = SnapRect(destinationRect);
        }

        queue.Add(new CanvasItemRenderCommand(
            canvasItem.CanvasItemRid,
            key,
            state.Layer,
            canvasItem.ZIndex,
            canvasItem.YSortEnabled,
            GetYSortPosition(canvasItem),
            treeOrder++,
            state.LayerVisible && visible,
            modulate,
            canvasItem.SelfModulate,
            commandModulate: drawingCommand.Modulate,
            kind: drawingCommand.Kind,
            transform: transform,
            sourceRect: GetDrawingSourceRect(drawingCommand),
            destinationRect: destinationRect,
            position: drawingCommand.Position,
            points: drawingCommand.Points,
            colors: drawingCommand.Colors,
            uvs: drawingCommand.Uvs,
            texture: drawingCommand.Texture,
            font: drawingCommand.Font,
            text: drawingCommand.Text,
            alignment: drawingCommand.Alignment,
            textWidth: drawingCommand.TextWidth,
            fontSize: drawingCommand.FontSize,
            radius: drawingCommand.Radius,
            width: drawingCommand.Width,
            filled: drawingCommand.Filled,
            antialiased: drawingCommand.Antialiased,
            debugName: canvasItem.Name));
    }

    private Rid GetTextureRid(Texture2D texture)
    {
        if (textureRids.TryGetValue(texture, out var existing))
        {
            return existing;
        }

        var rid = textureAllocator.Allocate();
        textureRids.Add(texture, rid);
        return rid;
    }

    private static Rect2 SnapRect(Rect2 rect)
    {
        var position = rect.Position.Round();
        var end = rect.End.Round();
        return new Rect2(position, end - position);
    }

    private static Transform2D GetCanvasItemTransform(CanvasItem canvasItem)
    {
        return canvasItem is Node2D node2D ? node2D.GlobalTransform : Transform2D.Identity;
    }

    private static float GetYSortPosition(CanvasItem canvasItem)
    {
        return canvasItem is Node2D node2D ? node2D.GlobalPosition.Y : 0f;
    }

    private static Rect2 GetDrawingSourceRect(CanvasItemDrawingCommand drawingCommand)
    {
        if (drawingCommand.Kind != CanvasItemRenderCommandKind.Texture || drawingCommand.Texture is null)
        {
            return default;
        }

        var size = drawingCommand.Texture.GetSize();
        return new Rect2(0f, 0f, size.X, size.Y);
    }

    private readonly record struct SubmissionState(
        int Layer,
        bool LayerVisible,
        Transform2D LayerTransform,
        bool ParentCanvasItemActive,
        bool InheritedVisible,
        Color InheritedModulate,
        bool SnapTransformsToPixel,
        bool SnapVerticesToPixel);
}
