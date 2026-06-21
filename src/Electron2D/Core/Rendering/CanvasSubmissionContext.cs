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
            InheritedModulate: Color.White);
        var treeOrder = 0L;
        Traverse(root, state, ref treeOrder);
        return queue.BuildPlan();
    }

    private void Traverse(Node node, SubmissionState state, ref long treeOrder)
    {
        var currentState = state;
        if (node is CanvasLayer layer)
        {
            currentState = new SubmissionState(
                layer.Layer,
                state.LayerVisible && layer.Visible,
                state.LayerTransform * layer.GetFinalTransform(),
                ParentCanvasItemActive: false,
                InheritedVisible: state.LayerVisible && layer.Visible,
                InheritedModulate: Color.White);
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
            state.LayerTransform * sprite.GlobalTransform,
            sprite.GetSourceRect(),
            sprite.GetRect(),
            sprite.FlipH,
            sprite.FlipV,
            sprite.Name));
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

    private readonly record struct SubmissionState(
        int Layer,
        bool LayerVisible,
        Transform2D LayerTransform,
        bool ParentCanvasItemActive,
        bool InheritedVisible,
        Color InheritedModulate);
}
