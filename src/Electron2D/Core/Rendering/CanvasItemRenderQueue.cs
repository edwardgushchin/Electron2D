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

internal sealed class CanvasItemRenderQueue
{
    private readonly List<PendingCommand> commands = new();
    private long nextSequence;

    public int Count => commands.Count;

    public void Add(CanvasItemRenderCommand command)
    {
        commands.Add(new PendingCommand(command, nextSequence++));
    }

    public void Clear()
    {
        commands.Clear();
        nextSequence = 0;
    }

    public CanvasItemRenderPlan BuildPlan()
    {
        var ordered = commands
            .Where(command => command.Command.Visible)
            .ToList();

        ordered.Sort(ComparePendingCommands);

        var renderCommands = ordered.Select(command => command.Command).ToArray();
        var batches = BuildBatches(renderCommands);
        return new CanvasItemRenderPlan(renderCommands, batches);
    }

    private static CanvasItemRenderBatch[] BuildBatches(IReadOnlyList<CanvasItemRenderCommand> renderCommands)
    {
        if (renderCommands.Count == 0)
        {
            return Array.Empty<CanvasItemRenderBatch>();
        }

        var batches = new List<CanvasItemRenderBatch>
        {
            new(renderCommands[0].BatchKey, startIndex: 0, count: 1)
        };

        for (var index = 1; index < renderCommands.Count; index++)
        {
            var command = renderCommands[index];
            var current = batches[^1];
            if (current.Key == command.BatchKey)
            {
                batches[^1] = current.Extend();
                continue;
            }

            batches.Add(new CanvasItemRenderBatch(command.BatchKey, index, count: 1));
        }

        return batches.ToArray();
    }

    private static int ComparePendingCommands(PendingCommand left, PendingCommand right)
    {
        var layer = left.Command.Layer.CompareTo(right.Command.Layer);
        if (layer != 0)
        {
            return layer;
        }

        var z = left.Command.ZIndex.CompareTo(right.Command.ZIndex);
        if (z != 0)
        {
            return z;
        }

        var y = GetYSortKey(left.Command).CompareTo(GetYSortKey(right.Command));
        if (y != 0)
        {
            return y;
        }

        var tree = left.Command.TreeOrder.CompareTo(right.Command.TreeOrder);
        if (tree != 0)
        {
            return tree;
        }

        return left.Sequence.CompareTo(right.Sequence);
    }

    private static float GetYSortKey(CanvasItemRenderCommand command)
    {
        return command.YSortEnabled ? command.YSortPosition : 0f;
    }

    private readonly record struct PendingCommand(CanvasItemRenderCommand Command, long Sequence);
}
