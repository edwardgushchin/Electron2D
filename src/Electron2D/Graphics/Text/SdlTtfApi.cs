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
using SDL3;

namespace Electron2D;

internal sealed class SdlTtfApi : ISdlTtfApi
{
    public nint OpenFont(string file, float pointSize)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        return TTF.OpenFont(file, pointSize);
    }

    public void CloseFont(nint font)
    {
        if (font != 0)
        {
            TTF.CloseFont(font);
        }
    }

    public bool AddFallbackFont(nint font, nint fallback)
    {
        return TTF.AddFallbackFont(font, fallback);
    }

    public bool FontHasGlyph(nint font, int codePoint)
    {
        return codePoint >= 0 && TTF.FontHasGlyph(font, (uint)codePoint);
    }

    public bool GetStringSize(nint font, string text, out int width, out int height)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TTF.GetStringSize(font, text, UIntPtr.Zero, out width, out height);
    }

    public nint CreateGpuTextEngine(SdlGpuDeviceHandle device)
    {
        return device.IsValid ? TTF.CreateGPUTextEngine(device.Value) : 0;
    }

    public nint CreateText(nint engine, nint font, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return TTF.CreateText(engine, font, text, UIntPtr.Zero);
    }
}
