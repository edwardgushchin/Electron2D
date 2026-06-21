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

internal sealed class Electron2DDisplaySettings
{
    public Vector2I WindowSize { get; set; } = new(640, 360);

    public bool Fullscreen { get; set; }

    public float DpiScale { get; set; } = 1f;

    public ViewportStretchMode StretchMode { get; set; } = ViewportStretchMode.Disabled;

    public ViewportStretchAspect StretchAspect { get; set; } = ViewportStretchAspect.Keep;

    public ViewportStretchScaleMode StretchScaleMode { get; set; } = ViewportStretchScaleMode.Fractional;

    public float StretchScale { get; set; } = 1f;

    public DisplayServer.ScreenOrientation Orientation { get; set; } = DisplayServer.ScreenOrientation.Landscape;

    public Rect2I SafeArea { get; set; }

    public ViewportPresentationSettings ToViewportPresentationSettings()
    {
        Validate();
        return new ViewportPresentationSettings
        {
            BaseSize = WindowSize,
            WindowSize = WindowSize,
            DpiScale = DpiScale,
            StretchScale = StretchScale,
            StretchMode = StretchMode,
            StretchAspect = StretchAspect,
            StretchScaleMode = StretchScaleMode
        };
    }

    public void ApplyToRuntime()
    {
        Validate();
        DisplayServer.ScreenSetOrientation(Orientation);
        DisplayServer.SetDisplaySafeArea(SafeArea);
    }

    public void Validate()
    {
        if (WindowSize.X <= 0 || WindowSize.Y <= 0)
        {
            throw new FormatException("Display window size must be positive.");
        }

        if (!Mathf.IsFinite(DpiScale) || DpiScale <= 0f)
        {
            throw new FormatException("Display DPI scale must be positive and finite.");
        }

        if (!Mathf.IsFinite(StretchScale) || StretchScale <= 0f)
        {
            throw new FormatException("Display stretch scale must be positive and finite.");
        }

        if (!Enum.IsDefined(StretchMode))
        {
            throw new FormatException($"Display stretch mode '{StretchMode}' is not supported.");
        }

        if (!Enum.IsDefined(StretchAspect))
        {
            throw new FormatException($"Display stretch aspect '{StretchAspect}' is not supported.");
        }

        if (!Enum.IsDefined(StretchScaleMode))
        {
            throw new FormatException($"Display stretch scale mode '{StretchScaleMode}' is not supported.");
        }

        if (!Enum.IsDefined(Orientation))
        {
            throw new FormatException($"Display orientation '{Orientation}' is not supported.");
        }
    }
}
