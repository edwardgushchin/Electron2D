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

public sealed class CameraViewportPublicApiTests
{
    [Fact]
    public void Camera2DDefaultsAndScreenQueriesFollowGodotLikeBaseline()
    {
        var camera = new Electron2D.Camera2D
        {
            Position = new Electron2D.Vector2(10f, 20f),
            Offset = new Electron2D.Vector2(3f, -2f),
            Rotation = Electron2D.Mathf.Pi
        };

        Assert.True(camera.Enabled);
        Assert.True(camera.IgnoreRotation);
        Assert.Equal(Electron2D.Vector2.One, camera.Zoom);
        Assert.Equal(new Electron2D.Vector2(13f, 18f), camera.GetTargetPosition());
        Assert.Equal(new Electron2D.Vector2(13f, 18f), camera.GetScreenCenterPosition());
        Assert.Equal(0f, camera.GetScreenRotation());

        camera.IgnoreRotation = false;

        Assert.True(Electron2D.Mathf.IsEqualApprox(Electron2D.Mathf.Pi, camera.GetScreenRotation()));
    }

    [Fact]
    public void ViewportTracksCurrentCamera()
    {
        var viewport = new Electron2D.Viewport { Size = new Electron2D.Vector2I(320, 180) };
        var first = new Electron2D.Camera2D { Name = "First" };
        var second = new Electron2D.Camera2D { Name = "Second" };
        viewport.AddChild(first);
        viewport.AddChild(second);

        first.MakeCurrent();

        Assert.True(first.IsCurrent());
        Assert.False(second.IsCurrent());
        Assert.Same(first, viewport.GetCamera2D());

        second.MakeCurrent();

        Assert.False(first.IsCurrent());
        Assert.True(second.IsCurrent());
        Assert.Same(second, viewport.GetCamera2D());

        second.ClearCurrent(enableNext: false);

        Assert.Null(viewport.GetCamera2D());
        Assert.False(second.IsCurrent());
    }

    [Fact]
    public void SceneTreeRootIsViewportNamedRoot()
    {
        var tree = new Electron2D.SceneTree();

        var viewport = Assert.IsType<Electron2D.Viewport>(tree.Root);

        Assert.Equal("root", viewport.Name);
        Assert.Same(viewport, viewport.GetNode("/root"));
    }

    [Fact]
    public void ViewportVisibleRectAndPixelSnapDefaultsAreGodotLike()
    {
        var viewport = new Electron2D.Viewport { Size = new Electron2D.Vector2I(640, 360) };

        Assert.Equal(Electron2D.Transform2D.Identity, viewport.CanvasTransform);
        Assert.False(viewport.Snap2DTransformsToPixel);
        Assert.False(viewport.Snap2DVerticesToPixel);
        Assert.Equal(new Electron2D.Rect2(0f, 0f, 640f, 360f), viewport.GetVisibleRect());
    }

    [Fact]
    public void Camera2DRejectsZeroZoom()
    {
        var camera = new Electron2D.Camera2D();

        Assert.Throws<ArgumentOutOfRangeException>(() => { camera.Zoom = new Electron2D.Vector2(0f, 1f); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { camera.Zoom = new Electron2D.Vector2(1f, 0f); });
    }
}
