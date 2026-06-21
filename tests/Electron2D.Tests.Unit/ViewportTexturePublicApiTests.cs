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

public sealed class ViewportTexturePublicApiTests
{
    [Fact]
    public void ViewportGetTextureReturnsSceneLocalViewportTexture()
    {
        var viewport = new Electron2D.Viewport { Size = new Electron2D.Vector2I(320, 180) };

        var texture = viewport.GetTexture();

        Assert.IsType<Electron2D.ViewportTexture>(texture);
        Assert.Same(texture, viewport.GetTexture());
        Assert.True(typeof(Electron2D.Texture2D).IsAssignableFrom(typeof(Electron2D.ViewportTexture)));
        Assert.True(texture.ResourceLocalToScene);
        Assert.Equal(320, texture.GetWidth());
        Assert.Equal(180, texture.GetHeight());
        Assert.Equal(new Electron2D.Vector2(320f, 180f), texture.GetSize());
        Assert.True(texture.HasAlpha());
        Assert.False(texture.HasMipmaps());
        Assert.Equal(0, texture.GetMipmapCount());
        Assert.False(texture.IsPixelOpaque(0, 0));

        viewport.Size = new Electron2D.Vector2I(640, 360);

        Assert.Equal(640, texture.GetWidth());
        Assert.Equal(360, texture.GetHeight());
    }
}
