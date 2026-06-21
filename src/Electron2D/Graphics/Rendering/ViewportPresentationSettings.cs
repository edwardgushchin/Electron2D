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

internal sealed class ViewportPresentationSettings
{
    public Vector2I BaseSize { get; set; } = new(640, 360);

    public Vector2I WindowSize { get; set; } = new(640, 360);

    public float DpiScale { get; set; } = 1f;

    public float StretchScale { get; set; } = 1f;

    public ViewportStretchMode StretchMode { get; set; } = ViewportStretchMode.Disabled;

    public ViewportStretchAspect StretchAspect { get; set; } = ViewportStretchAspect.Keep;

    public ViewportStretchScaleMode StretchScaleMode { get; set; } = ViewportStretchScaleMode.Fractional;

    public ViewportPresentationPlan BuildPlan()
    {
        Validate();

        var framebufferSize = new Vector2I(
            Math.Max(1, (int)MathF.Round(WindowSize.X * DpiScale, MidpointRounding.AwayFromZero)),
            Math.Max(1, (int)MathF.Round(WindowSize.Y * DpiScale, MidpointRounding.AwayFromZero)));
        var scale = ResolveScale(framebufferSize);
        var viewportSize = new Vector2I(
            Math.Max(1, (int)MathF.Round(BaseSize.X * scale, MidpointRounding.AwayFromZero)),
            Math.Max(1, (int)MathF.Round(BaseSize.Y * scale, MidpointRounding.AwayFromZero)));
        var viewportPosition = new Vector2I(
            (framebufferSize.X - viewportSize.X) / 2,
            (framebufferSize.Y - viewportSize.Y) / 2);
        var renderTargetSize = StretchMode == ViewportStretchMode.Viewport
            ? new Vector2I(
                Math.Max(1, (int)MathF.Round(viewportSize.X / scale, MidpointRounding.AwayFromZero)),
                Math.Max(1, (int)MathF.Round(viewportSize.Y / scale, MidpointRounding.AwayFromZero)))
            : viewportSize;
        var canvasScale = new Vector2(scale, scale);
        var canvasOffset = new Vector2(viewportPosition.X, viewportPosition.Y);
        var canvasTransform = new Transform2D(
            new Vector2(canvasScale.X, 0f),
            new Vector2(0f, canvasScale.Y),
            canvasOffset);

        return new ViewportPresentationPlan(
            WindowSize,
            framebufferSize,
            renderTargetSize,
            new Rect2I(viewportPosition, viewportSize),
            canvasScale,
            canvasOffset,
            canvasTransform);
    }

    private float ResolveScale(Vector2I framebufferSize)
    {
        var scaleX = framebufferSize.X / (float)BaseSize.X;
        var scaleY = framebufferSize.Y / (float)BaseSize.Y;
        var scale = StretchAspect switch
        {
            ViewportStretchAspect.Ignore => MathF.Min(scaleX, scaleY),
            ViewportStretchAspect.Keep => MathF.Min(scaleX, scaleY),
            ViewportStretchAspect.KeepWidth => scaleX,
            ViewportStretchAspect.KeepHeight => scaleY,
            ViewportStretchAspect.Expand => MathF.Max(scaleX, scaleY),
            _ => MathF.Min(scaleX, scaleY)
        };

        scale *= StretchScale;
        if (StretchScaleMode == ViewportStretchScaleMode.Integer)
        {
            scale = MathF.Floor(scale);
        }

        return MathF.Max(1f, scale);
    }

    private void Validate()
    {
        if (BaseSize.X <= 0 || BaseSize.Y <= 0)
        {
            throw new InvalidOperationException("Base viewport size must be positive.");
        }

        if (WindowSize.X <= 0 || WindowSize.Y <= 0)
        {
            throw new InvalidOperationException("Window size must be positive.");
        }

        if (!Mathf.IsFinite(DpiScale) || DpiScale <= 0f)
        {
            throw new InvalidOperationException("DPI scale must be positive and finite.");
        }

        if (!Mathf.IsFinite(StretchScale) || StretchScale <= 0f)
        {
            throw new InvalidOperationException("Stretch scale must be positive and finite.");
        }
    }
}
