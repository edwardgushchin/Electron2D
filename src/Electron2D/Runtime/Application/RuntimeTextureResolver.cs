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

internal static class RuntimeTextureResolver
{
    public static bool TryCreateTexturePixels(Texture2D texture, out int width, out int height, out byte[] pixels)
    {
        if (!TryResolveImageTexture(texture, default, out var image, out var sourceRect))
        {
            width = 0;
            height = 0;
            pixels = [];
            return false;
        }

        width = Math.Max(1, (int)MathF.Round(sourceRect.Size.X, MidpointRounding.AwayFromZero));
        height = Math.Max(1, (int)MathF.Round(sourceRect.Size.Y, MidpointRounding.AwayFromZero));
        pixels = new byte[checked(width * height * 4)];
        var offset = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sourceX = (int)MathF.Round(sourceRect.Position.X + x, MidpointRounding.AwayFromZero);
                var sourceY = (int)MathF.Round(sourceRect.Position.Y + y, MidpointRounding.AwayFromZero);
                if (!image.TryGetPixel(sourceX, sourceY, out var color))
                {
                    color = default;
                }

                pixels[offset++] = color.R;
                pixels[offset++] = color.G;
                pixels[offset++] = color.B;
                pixels[offset++] = color.A;
            }
        }

        return true;
    }

    public static bool TryResolveImageTexture(Texture2D texture, Rect2 requestedSource, out ImageTexture image, out Rect2 sourceRect)
    {
        switch (texture)
        {
            case ImageTexture imageTexture:
                image = imageTexture;
                sourceRect = NormalizeSourceRect(requestedSource, imageTexture.GetWidth(), imageTexture.GetHeight());
                return true;
            case AtlasTexture { Atlas: not null } atlas:
                var atlasRegion = atlas.GetSourceRegion();
                var requested = NormalizeSourceRect(requestedSource, atlas.GetWidth(), atlas.GetHeight());
                var atlasRequested = new Rect2(
                    atlasRegion.Position + requested.Position,
                    requested.Size);
                return TryResolveImageTexture(atlas.Atlas, atlasRequested, out image, out sourceRect);
            default:
                image = null!;
                sourceRect = default;
                return false;
        }
    }

    public static Rect2 NormalizeSourceRect(Rect2 sourceRect, int textureWidth, int textureHeight)
    {
        var width = sourceRect.Size.X > 0f ? sourceRect.Size.X : textureWidth;
        var height = sourceRect.Size.Y > 0f ? sourceRect.Size.Y : textureHeight;
        return new Rect2(sourceRect.Position, new Vector2(width, height));
    }
}
