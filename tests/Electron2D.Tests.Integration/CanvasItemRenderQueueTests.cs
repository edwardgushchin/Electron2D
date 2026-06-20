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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class CanvasItemRenderQueueTests
{
    [Fact]
    public void RenderQueueBuildsEmptyPlanWhenNoCommandsWereSubmitted()
    {
        var plan = new Electron2D.CanvasItemRenderQueue().BuildPlan();

        Assert.Empty(plan.Commands);
        Assert.Empty(plan.Batches);
        Assert.Equal(0, plan.DrawCallCount);
    }

    [Fact]
    public void RenderQueueClearDropsCommandsAndResetsCount()
    {
        var queue = new Electron2D.CanvasItemRenderQueue();
        queue.Add(Command(itemId: 1, BatchKey(textureId: 1)));

        queue.Clear();

        Assert.Equal(0, queue.Count);
        Assert.Empty(queue.BuildPlan().Commands);
    }

    [Fact]
    public void RenderCommandRejectsInvalidCanvasItemRid()
    {
        Assert.Throws<ArgumentException>(() => new Electron2D.CanvasItemRenderCommand(
            default,
            BatchKey(textureId: 1),
            layer: 0,
            zIndex: 0,
            ySortEnabled: false,
            ySortPosition: 0f,
            treeOrder: 0,
            visible: true,
            Electron2D.Color.White,
            Electron2D.Color.White));
    }

    [Fact]
    public void RenderQueueSortsByLayerZTreeOrderAndKeepsStableTieOrder()
    {
        var queue = new Electron2D.CanvasItemRenderQueue();
        var key = BatchKey(textureId: 1);

        queue.Add(Command(itemId: 1, key, layer: 0, zIndex: 0, treeOrder: 10));
        queue.Add(Command(itemId: 2, key, layer: 0, zIndex: 0, treeOrder: 10));
        queue.Add(Command(itemId: 3, key, layer: 0, zIndex: -1, treeOrder: 99));
        queue.Add(Command(itemId: 4, key, layer: 1, zIndex: -100, treeOrder: 0));

        var plan = queue.BuildPlan();

        Assert.Equal(new long[] { 3, 1, 2, 4 }, plan.Commands.Select(command => command.CanvasItem.GetId()).ToArray());
    }

    [Fact]
    public void RenderQueueUsesYSortInsideSameZIndexOnly()
    {
        var queue = new Electron2D.CanvasItemRenderQueue();
        var key = BatchKey(textureId: 1);

        queue.Add(Command(itemId: 1, key, zIndex: 0, ySortEnabled: true, ySortPosition: 20f, treeOrder: 0));
        queue.Add(Command(itemId: 2, key, zIndex: 0, ySortEnabled: true, ySortPosition: 10f, treeOrder: 1));
        queue.Add(Command(itemId: 3, key, zIndex: -1, ySortEnabled: true, ySortPosition: 100f, treeOrder: 2));

        var plan = queue.BuildPlan();

        Assert.Equal(new long[] { 3, 2, 1 }, plan.Commands.Select(command => command.CanvasItem.GetId()).ToArray());
    }

    [Fact]
    public void RenderQueueFiltersInvisibleCommandsAndKeepsEffectiveModulate()
    {
        var queue = new Electron2D.CanvasItemRenderQueue();
        var key = BatchKey(textureId: 1);

        queue.Add(Command(itemId: 1, key, visible: false));
        queue.Add(Command(
            itemId: 2,
            key,
            modulate: new Electron2D.Color(0.5f, 1f, 0.25f, 0.75f),
            selfModulate: new Electron2D.Color(1f, 0.5f, 0.5f, 0.5f)));

        var command = Assert.Single(queue.BuildPlan().Commands);

        Assert.Equal(2, command.CanvasItem.GetId());
        Assert.Equal(new Electron2D.Color(0.5f, 0.5f, 0.125f, 0.375f), command.EffectiveModulate);
    }

    [Fact]
    public void RenderQueueBatchesAdjacentCompatibleCommands()
    {
        var queue = new Electron2D.CanvasItemRenderQueue();
        var compatible = BatchKey(textureId: 1);
        var separate = BatchKey(textureId: 2);

        queue.Add(Command(itemId: 1, compatible, treeOrder: 0));
        queue.Add(Command(itemId: 2, compatible, treeOrder: 1));
        queue.Add(Command(itemId: 3, separate, treeOrder: 2));

        var plan = queue.BuildPlan();

        Assert.Equal(3, plan.Commands.Count);
        Assert.Equal(2, plan.DrawCallCount);
        Assert.Equal(new[] { 2, 1 }, plan.Batches.Select(batch => batch.Count).ToArray());
    }

    [Fact]
    public void RenderQueueDoesNotBatchAcrossOrderingBarriers()
    {
        var queue = new Electron2D.CanvasItemRenderQueue();
        var repeated = BatchKey(textureId: 1);
        var barrier = BatchKey(textureId: 2);

        queue.Add(Command(itemId: 1, repeated, zIndex: 0, treeOrder: 0));
        queue.Add(Command(itemId: 2, barrier, zIndex: 1, treeOrder: 1));
        queue.Add(Command(itemId: 3, repeated, zIndex: 2, treeOrder: 2));

        var plan = queue.BuildPlan();

        Assert.Equal(3, plan.DrawCallCount);
        Assert.All(plan.Batches, batch => Assert.Equal(1, batch.Count));
    }

    private static Electron2D.CanvasItemRenderCommand Command(
        long itemId,
        Electron2D.CanvasItemBatchKey batchKey,
        int layer = 0,
        int zIndex = 0,
        bool ySortEnabled = false,
        float ySortPosition = 0f,
        long treeOrder = 0,
        bool visible = true,
        Electron2D.Color? modulate = null,
        Electron2D.Color? selfModulate = null)
    {
        return new Electron2D.CanvasItemRenderCommand(
            new Electron2D.Rid(itemId),
            batchKey,
            layer,
            zIndex,
            ySortEnabled,
            ySortPosition,
            treeOrder,
            visible,
            modulate ?? Electron2D.Color.White,
            selfModulate ?? Electron2D.Color.White);
    }

    private static Electron2D.CanvasItemBatchKey BatchKey(long textureId)
    {
        return new Electron2D.CanvasItemBatchKey(
            new Electron2D.Rid(textureId),
            material: default,
            clip: default,
            Electron2D.CanvasItemBlendMode.Mix);
    }
}
