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

public sealed class RenderTargetRecoveryTests
{
    [Fact]
    public void RegistryCreatesOffscreenRenderTargetDescriptorAndTracksLifecycle()
    {
        var api = new FakeTextureGpuApi();
        var registry = new Electron2D.TextureResourceRegistry(api);
        var sampling = new Electron2D.TextureSamplingOptions(
            Electron2D.TextureFilterMode.Linear,
            Electron2D.TextureRepeatMode.Disabled);

        var handle = registry.CreateRenderTarget(new Electron2D.Vector2I(128, 64), hasAlpha: true, sampling);

        Assert.True(handle.IsValid);
        Assert.Equal(1, registry.ActiveTextureCount);
        var upload = Assert.Single(api.Uploads);
        Assert.Equal(handle.Rid, upload.Rid);
        Assert.Equal(Electron2D.TextureResourceUsage.RenderTarget, upload.Descriptor.Usage);
        Assert.Equal(128, upload.Descriptor.Width);
        Assert.Equal(64, upload.Descriptor.Height);
        Assert.True(upload.Descriptor.HasAlpha);
        Assert.Equal(sampling, upload.Descriptor.Sampling);
        Assert.Equal(Electron2D.TextureResourceEventKind.RenderTargetCreated, registry.Events[0].Kind);

        Assert.True(registry.Release(handle));

        Assert.Equal(0, registry.ActiveTextureCount);
        Assert.Equal(1, api.ReleaseCalls);
    }

    [Fact]
    public void RegistryRestoresUploadedTexturesAndRenderTargetsAfterDeviceRecreation()
    {
        var firstApi = new FakeTextureGpuApi();
        var secondApi = new FakeTextureGpuApi();
        var registry = new Electron2D.TextureResourceRegistry(firstApi);
        var texture = registry.Upload(
            new Electron2D.RuntimeTexture2D(32, 16, hasAlpha: true),
            Electron2D.TextureSamplingOptions.Default);
        var renderTarget = registry.CreateRenderTarget(
            new Electron2D.Vector2I(64, 64),
            hasAlpha: true,
            Electron2D.TextureSamplingOptions.Default);

        registry.RestoreAfterDeviceLoss(secondApi);

        Assert.Equal(2, secondApi.Uploads.Count);
        Assert.Equal(new[] { texture.Rid, renderTarget.Rid }, secondApi.Uploads.Select(item => item.Rid).ToArray());
        Assert.Equal(
            new[] { Electron2D.TextureResourceUsage.Sampled, Electron2D.TextureResourceUsage.RenderTarget },
            secondApi.Uploads.Select(item => item.Descriptor.Usage).ToArray());
        Assert.Equal(2, registry.Events.Count(item => item.Kind == Electron2D.TextureResourceEventKind.Restored));

        Assert.True(registry.Release(texture));
        Assert.True(registry.Release(renderTarget));
        Assert.Equal(2, secondApi.ReleaseCalls);
        Assert.Equal(0, registry.LeakCount);
    }

    private sealed class FakeTextureGpuApi : Electron2D.ITextureGpuApi
    {
        public List<UploadCall> Uploads { get; } = new();

        public int ReleaseCalls { get; private set; }

        public bool Upload(Electron2D.Rid texture, Electron2D.TextureUploadDescriptor descriptor, out string? error)
        {
            Uploads.Add(new UploadCall(texture, descriptor));
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
            ReleaseCalls++;
            error = null;
            return true;
        }
    }

    private readonly record struct UploadCall(Electron2D.Rid Rid, Electron2D.TextureUploadDescriptor Descriptor);
}
