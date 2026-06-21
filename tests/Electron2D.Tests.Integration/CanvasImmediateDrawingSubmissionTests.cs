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

public sealed class CanvasImmediateDrawingSubmissionTests
{
    [Fact]
    public void QueueRedrawCoalescesDrawCallbackAndKeepsCachedCommands()
    {
        var tree = new Electron2D.SceneTree();
        var node = new LineDrawNode();
        tree.Root.AddChild(node);

        node.QueueRedraw();
        node.QueueRedraw();
        tree.ProcessFrame(1.0 / 60.0);

        Assert.Equal(1, node.DrawCount);
        var command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands);
        Assert.Equal(Electron2D.CanvasItemRenderCommandKind.Line, command.Kind);
        Assert.Equal(new[] { Electron2D.Vector2.Zero, new Electron2D.Vector2(4f, 0f) }, command.Points);

        node.LineEnd = new Electron2D.Vector2(8f, 0f);
        tree.ProcessFrame(1.0 / 60.0);

        Assert.Equal(1, node.DrawCount);
        command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands);
        Assert.Equal(new[] { Electron2D.Vector2.Zero, new Electron2D.Vector2(4f, 0f) }, command.Points);

        node.QueueRedraw();
        tree.ProcessFrame(1.0 / 60.0);

        Assert.Equal(2, node.DrawCount);
        command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands);
        Assert.Equal(new[] { Electron2D.Vector2.Zero, new Electron2D.Vector2(8f, 0f) }, command.Points);
    }

    [Fact]
    public void DrawValidationFailuresAreReportedAsDrawDiagnostics()
    {
        var tree = new Electron2D.SceneTree();
        var node = new InvalidDrawNode();
        tree.Root.AddChild(node);

        tree.ProcessFrame(1.0 / 60.0);

        var diagnostic = Assert.Single(tree.Diagnostics);
        Assert.Same(node, diagnostic.Node);
        Assert.Equal(nameof(Electron2D.CanvasItem._Draw), diagnostic.Callback);
        Assert.IsType<ArgumentException>(diagnostic.Exception);
        Assert.Empty(new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands);
    }

    [Fact]
    public void SubmissionIncludesImmediateDrawingCommandsInDrawOrder()
    {
        var texture = new Electron2D.RuntimeTexture2D(8, 4, hasAlpha: true);
        var font = new TestFont();
        var tree = new Electron2D.SceneTree();
        var node = new DrawAllNode(texture, font)
        {
            Name = "draw-all",
            Position = new Electron2D.Vector2(10f, 20f),
            Modulate = new Electron2D.Color(0.5f, 1f, 1f, 1f),
            SelfModulate = new Electron2D.Color(1f, 0.5f, 1f, 1f)
        };
        tree.Root.AddChild(node);

        tree.ProcessFrame(1.0 / 60.0);

        var commands = new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands;

        Assert.Equal(
            new[]
            {
                Electron2D.CanvasItemRenderCommandKind.Line,
                Electron2D.CanvasItemRenderCommandKind.Rect,
                Electron2D.CanvasItemRenderCommandKind.Circle,
                Electron2D.CanvasItemRenderCommandKind.Polygon,
                Electron2D.CanvasItemRenderCommandKind.Texture,
                Electron2D.CanvasItemRenderCommandKind.String
            },
            commands.Select(command => command.Kind).ToArray());
        Assert.All(commands, command => Assert.True(command.Transform.Origin.IsEqualApprox(new Electron2D.Vector2(10f, 20f))));
        Assert.All(commands, command => Assert.Equal("draw-all", command.DebugName));

        var line = commands[0];
        Assert.Equal(new[] { Electron2D.Vector2.Zero, new Electron2D.Vector2(4f, 0f) }, line.Points);
        Assert.Equal(new Electron2D.Color(1f, 0f, 0f, 1f), line.CommandModulate);
        Assert.Equal(2f, line.Width);
        Assert.True(line.Antialiased);

        var rect = commands[1];
        Assert.Equal(new Electron2D.Rect2(1f, 2f, 3f, 4f), rect.DestinationRect);
        Assert.False(rect.Filled);
        Assert.Equal(3f, rect.Width);

        var circle = commands[2];
        Assert.Equal(new Electron2D.Vector2(5f, 6f), circle.Position);
        Assert.Equal(7f, circle.Radius);
        Assert.True(circle.Filled);

        var polygon = commands[3];
        Assert.Equal(3, polygon.Points.Count);
        Assert.Equal(3, polygon.Colors.Count);
        Assert.Equal(3, polygon.Uvs.Count);
        Assert.Equal(texture, polygon.Texture);

        var drawnTexture = commands[4];
        Assert.Equal(texture, drawnTexture.Texture);
        Assert.Equal(new Electron2D.Rect2(0f, 0f, 8f, 4f), drawnTexture.SourceRect);
        Assert.Equal(new Electron2D.Rect2(10f, 11f, 8f, 4f), drawnTexture.DestinationRect);
        Assert.Equal(new Electron2D.Color(0.8f, 0.8f, 1f, 1f), drawnTexture.CommandModulate);

        var text = commands[5];
        Assert.Equal(font, text.Font);
        Assert.Equal("Hello", text.Text);
        Assert.Equal(Electron2D.HorizontalAlignment.Center, text.Alignment);
        Assert.Equal(120f, text.TextWidth);
        Assert.Equal(20, text.FontSize);
    }

    private sealed class LineDrawNode : Electron2D.Node2D
    {
        public int DrawCount { get; private set; }

        public Electron2D.Vector2 LineEnd { get; set; } = new(4f, 0f);

        public override void _Draw()
        {
            DrawCount++;
            DrawLine(Electron2D.Vector2.Zero, LineEnd, Electron2D.Color.White, width: 2f, antialiased: true);
        }
    }

    private sealed class InvalidDrawNode : Electron2D.Node2D
    {
        public override void _Draw()
        {
            DrawPolygon(
                new[] { Electron2D.Vector2.Zero, Electron2D.Vector2.Right, Electron2D.Vector2.Down },
                new[] { Electron2D.Color.White });
        }
    }

    private sealed class DrawAllNode : Electron2D.Node2D
    {
        private readonly Electron2D.Texture2D texture;
        private readonly Electron2D.Font font;

        public DrawAllNode(Electron2D.Texture2D texture, Electron2D.Font font)
        {
            this.texture = texture;
            this.font = font;
        }

        public override void _Draw()
        {
            DrawLine(Electron2D.Vector2.Zero, new Electron2D.Vector2(4f, 0f), new Electron2D.Color(1f, 0f, 0f), width: 2f, antialiased: true);
            DrawRect(new Electron2D.Rect2(1f, 2f, 3f, 4f), new Electron2D.Color(0f, 1f, 0f), filled: false, width: 3f, antialiased: true);
            DrawCircle(new Electron2D.Vector2(5f, 6f), 7f, new Electron2D.Color(0f, 0f, 1f));
            DrawPolygon(
                new[] { Electron2D.Vector2.Zero, new Electron2D.Vector2(4f, 0f), new Electron2D.Vector2(0f, 4f) },
                new[] { Electron2D.Color.White, new Electron2D.Color(1f, 0f, 0f), new Electron2D.Color(0f, 1f, 0f) },
                new[] { Electron2D.Vector2.Zero, Electron2D.Vector2.Right, Electron2D.Vector2.Down },
                texture);
            DrawTexture(texture, new Electron2D.Vector2(10f, 11f), new Electron2D.Color(0.8f, 0.8f, 1f));
            DrawString(font, new Electron2D.Vector2(12f, 13f), "Hello", Electron2D.HorizontalAlignment.Center, width: 120f, fontSize: 20);
        }
    }

    private sealed class TestFont : Electron2D.Font
    {
    }
}
