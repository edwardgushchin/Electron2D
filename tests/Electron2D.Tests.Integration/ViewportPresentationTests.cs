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

public sealed class ViewportPresentationTests
{
    [Fact]
    public void CanvasSubmissionAppliesCurrentCameraTransform()
    {
        var viewport = new Electron2D.Viewport { Size = new Electron2D.Vector2I(100, 100) };
        var camera = new Electron2D.Camera2D { Position = new Electron2D.Vector2(10f, 0f) };
        var sprite = new Electron2D.Sprite2D
        {
            Name = "sprite",
            Texture = new Electron2D.RuntimeTexture2D(4, 4, hasAlpha: false),
            Centered = false,
            Position = new Electron2D.Vector2(20f, 0f)
        };

        viewport.AddChild(camera);
        viewport.AddChild(sprite);
        camera.MakeCurrent();

        var command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(viewport).Commands);

        Assert.Equal("sprite", command.DebugName);
        Assert.True(command.Transform.Origin.IsEqualApprox(new Electron2D.Vector2(60f, 50f)));
    }

    [Fact]
    public void CanvasSubmissionAppliesViewportPixelSnappingWithoutMutatingNodes()
    {
        var viewport = new Electron2D.Viewport
        {
            Snap2DTransformsToPixel = true,
            Snap2DVerticesToPixel = true
        };
        var sprite = new Electron2D.Sprite2D
        {
            Name = "sprite",
            Texture = new Electron2D.RuntimeTexture2D(4, 4, hasAlpha: false),
            Centered = false,
            Offset = new Electron2D.Vector2(0.2f, 0.6f),
            Position = new Electron2D.Vector2(1.2f, 2.6f)
        };
        viewport.AddChild(sprite);

        var command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(viewport).Commands);

        Assert.True(command.Transform.Origin.IsEqualApprox(new Electron2D.Vector2(1f, 3f)));
        Assert.Equal(new Electron2D.Rect2(0f, 1f, 4f, 4f), command.DestinationRect);
        Assert.Equal(new Electron2D.Vector2(1.2f, 2.6f), sprite.Position);
        Assert.Equal(new Electron2D.Vector2(0.2f, 0.6f), sprite.Offset);
    }

    [Fact]
    public void PresentationCanvasItemsModeScalesCanvasToWindow()
    {
        var plan = new Electron2D.ViewportPresentationSettings
        {
            BaseSize = new Electron2D.Vector2I(320, 180),
            WindowSize = new Electron2D.Vector2I(1280, 720),
            DpiScale = 1f,
            StretchMode = Electron2D.ViewportStretchMode.CanvasItems,
            StretchAspect = Electron2D.ViewportStretchAspect.Keep,
            StretchScaleMode = Electron2D.ViewportStretchScaleMode.Fractional
        }.BuildPlan();

        Assert.Equal(new Electron2D.Vector2I(1280, 720), plan.FramebufferSize);
        Assert.Equal(new Electron2D.Vector2I(1280, 720), plan.RenderTargetSize);
        Assert.Equal(new Electron2D.Rect2I(0, 0, 1280, 720), plan.ViewportRect);
        Assert.Equal(new Electron2D.Vector2(4f, 4f), plan.CanvasScale);
    }

    [Fact]
    public void PresentationViewportModeSupportsIntegerScaleAndHighDpiFramebuffer()
    {
        var plan = new Electron2D.ViewportPresentationSettings
        {
            BaseSize = new Electron2D.Vector2I(320, 180),
            WindowSize = new Electron2D.Vector2I(1000, 720),
            DpiScale = 2f,
            StretchMode = Electron2D.ViewportStretchMode.Viewport,
            StretchAspect = Electron2D.ViewportStretchAspect.Keep,
            StretchScaleMode = Electron2D.ViewportStretchScaleMode.Integer
        }.BuildPlan();

        Assert.Equal(new Electron2D.Vector2I(2000, 1440), plan.FramebufferSize);
        Assert.Equal(new Electron2D.Vector2I(320, 180), plan.RenderTargetSize);
        Assert.Equal(new Electron2D.Rect2I(40, 180, 1920, 1080), plan.ViewportRect);
        Assert.Equal(new Electron2D.Vector2(6f, 6f), plan.CanvasScale);
    }
}
