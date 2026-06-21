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

namespace Electron2D.Tests.RuntimeSmoke;

public sealed class RenderTargetRecoverySmokeTests
{
    [Fact]
    public void TextureRegistryRestoresResourcesAndReportsNoLeaksAfterRelease()
    {
        var registry = new Electron2D.TextureResourceRegistry(new SmokeTextureGpuApi());
        var texture = registry.Upload(
            new Electron2D.RuntimeTexture2D(16, 16, hasAlpha: true),
            Electron2D.TextureSamplingOptions.Default);
        var renderTarget = registry.CreateRenderTarget(
            new Electron2D.Vector2I(32, 32),
            hasAlpha: true,
            Electron2D.TextureSamplingOptions.Default);

        registry.RestoreAfterDeviceLoss(new SmokeTextureGpuApi());

        Assert.Equal(2, registry.LeakCount);
        Assert.True(registry.Release(texture));
        Assert.True(registry.Release(renderTarget));
        Assert.Equal(0, registry.ActiveTextureCount);
        Assert.Equal(0, registry.LeakCount);
    }

    private sealed class SmokeTextureGpuApi : Electron2D.ITextureGpuApi
    {
        public bool Upload(Electron2D.Rid texture, Electron2D.TextureUploadDescriptor descriptor, out string? error)
        {
            error = null;
            return true;
        }

        public bool Reload(Electron2D.Rid texture, Electron2D.TextureUploadDescriptor descriptor, out string? error)
        {
            error = null;
            return true;
        }

        public bool Release(Electron2D.Rid texture, out string? error)
        {
            error = null;
            return true;
        }
    }
}
