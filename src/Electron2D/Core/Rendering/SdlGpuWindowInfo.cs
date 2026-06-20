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

internal readonly struct SdlGpuWindowInfo
{
    public SdlGpuWindowInfo(int width, int height, float dpiScale, bool fullscreen, nint nativeWindowHandle = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dpiScale);

        Width = width;
        Height = height;
        DpiScale = dpiScale;
        Fullscreen = fullscreen;
        NativeWindowHandle = nativeWindowHandle;
    }

    public int Width { get; }

    public int Height { get; }

    public float DpiScale { get; }

    public bool Fullscreen { get; }

    public nint NativeWindowHandle { get; }

    public SdlGpuWindowInfo WithSize(int width, int height, float dpiScale)
    {
        return new SdlGpuWindowInfo(width, height, dpiScale, Fullscreen, NativeWindowHandle);
    }

    public SdlGpuWindowInfo WithFullscreen(bool fullscreen)
    {
        return new SdlGpuWindowInfo(Width, Height, DpiScale, fullscreen, NativeWindowHandle);
    }
}
