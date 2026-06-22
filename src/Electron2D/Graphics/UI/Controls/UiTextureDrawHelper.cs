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

internal static class UiTextureDrawHelper
{
    public static Rect2 GetDestinationRect(Vector2 textureSize, Vector2 controlSize, int stretchMode)
    {
        if (textureSize.X <= 0f || textureSize.Y <= 0f || controlSize.X <= 0f || controlSize.Y <= 0f)
        {
            return new Rect2(Vector2.Zero, Vector2.Zero);
        }

        return stretchMode switch
        {
            2 => new Rect2(Vector2.Zero, textureSize),
            3 => Center(textureSize, controlSize),
            4 => new Rect2(Vector2.Zero, FitInside(textureSize, controlSize)),
            5 => Center(FitInside(textureSize, controlSize), controlSize),
            6 => Center(Cover(textureSize, controlSize), controlSize),
            _ => new Rect2(Vector2.Zero, controlSize)
        };
    }

    private static Rect2 Center(Vector2 size, Vector2 controlSize)
    {
        return new Rect2((controlSize - size) * 0.5f, size);
    }

    private static Vector2 FitInside(Vector2 textureSize, Vector2 controlSize)
    {
        var scale = MathF.Min(controlSize.X / textureSize.X, controlSize.Y / textureSize.Y);
        return textureSize * scale;
    }

    private static Vector2 Cover(Vector2 textureSize, Vector2 controlSize)
    {
        var scale = MathF.Max(controlSize.X / textureSize.X, controlSize.Y / textureSize.Y);
        return textureSize * scale;
    }
}
