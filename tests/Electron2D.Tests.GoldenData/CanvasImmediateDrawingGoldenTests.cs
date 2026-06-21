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
using System.Globalization;
using Xunit;

namespace Electron2D.Tests.GoldenData;

public sealed class CanvasImmediateDrawingGoldenTests
{
    [Fact]
    public void ImmediatePrimitiveCommandStreamMatchesGoldenText()
    {
        var tree = new Electron2D.SceneTree();
        var node = new PrimitiveDrawNode { Position = new Electron2D.Vector2(2f, 3f) };
        tree.Root.AddChild(node);
        tree.ProcessFrame(1.0 / 60.0);

        var plan = new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root);
        var actual = string.Join(
            "\n",
            plan.Commands.Select(command =>
                $"{command.Kind}|origin={Format(command.Transform.Origin)}|rect={Format(command.DestinationRect)}|points={string.Join(';', command.Points.Select(Format))}|color={Format(command.CommandModulate)}|width={Format(command.Width)}|filled={command.Filled}"));

        const string expected =
            "Line|origin=2,3|rect=0,0,0,0|points=0,0;16,0|color=1,0,0,1|width=1|filled=True\n" +
            "Rect|origin=2,3|rect=1,1,8,4|points=|color=0,1,0,1|width=-1|filled=True\n" +
            "Circle|origin=2,3|rect=0,0,0,0|points=|color=0,0,1,1|width=-1|filled=True";

        Assert.Equal(expected, actual);
    }

    private static string Format(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Format(Electron2D.Vector2 value)
    {
        return $"{Format(value.X)},{Format(value.Y)}";
    }

    private static string Format(Electron2D.Rect2 value)
    {
        return $"{Format(value.Position.X)},{Format(value.Position.Y)},{Format(value.Size.X)},{Format(value.Size.Y)}";
    }

    private static string Format(Electron2D.Color value)
    {
        return $"{Format(value.R)},{Format(value.G)},{Format(value.B)},{Format(value.A)}";
    }

    private sealed class PrimitiveDrawNode : Electron2D.Node2D
    {
        public override void _Draw()
        {
            DrawLine(Electron2D.Vector2.Zero, new Electron2D.Vector2(16f, 0f), new Electron2D.Color(1f, 0f, 0f), width: 1f);
            DrawRect(new Electron2D.Rect2(1f, 1f, 8f, 4f), new Electron2D.Color(0f, 1f, 0f));
            DrawCircle(new Electron2D.Vector2(4f, 4f), 3f, new Electron2D.Color(0f, 0f, 1f));
        }
    }
}
