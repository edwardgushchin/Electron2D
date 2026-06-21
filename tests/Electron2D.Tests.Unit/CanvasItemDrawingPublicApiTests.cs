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

namespace Electron2D.Tests.Unit;

public sealed class CanvasItemDrawingPublicApiTests
{
    [Fact]
    public void DrawMethodsRejectCallsOutsideDrawCallback()
    {
        var node = new Electron2D.Node2D();

        Assert.Throws<InvalidOperationException>(() =>
            node.DrawLine(Electron2D.Vector2.Zero, Electron2D.Vector2.One, Electron2D.Color.White));
    }

    [Fact]
    public void FontAndHorizontalAlignmentAreGodotLikePublicTypes()
    {
        Assert.True(typeof(Electron2D.Resource).IsAssignableFrom(typeof(Electron2D.Font)));
        Assert.Equal(0, (int)Electron2D.HorizontalAlignment.Left);
        Assert.Equal(1, (int)Electron2D.HorizontalAlignment.Center);
        Assert.Equal(2, (int)Electron2D.HorizontalAlignment.Right);
        Assert.Equal(3, (int)Electron2D.HorizontalAlignment.Fill);
    }
}
