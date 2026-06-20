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

public sealed class TextureResourceRegistryTests
{
    [Fact]
    public void RegistryUploadsReloadsReleasesTexturesAndTracksLeaks()
    {
        var api = new FakeTextureGpuApi();
        var registry = new Electron2D.TextureResourceRegistry(api);
        var sampling = new Electron2D.TextureSamplingOptions(
            Electron2D.TextureFilterMode.Nearest,
            Electron2D.TextureRepeatMode.Mirror);
        var texture = new Electron2D.RuntimeTexture2D(32, 16, hasAlpha: true, hasMipmaps: true, mipmapCount: 2);

        var handle = registry.Upload(texture, sampling);
        registry.Reload(handle, new Electron2D.RuntimeTexture2D(32, 16, hasAlpha: false));

        Assert.True(handle.IsValid);
        Assert.Equal(1, registry.ActiveTextureCount);
        Assert.Equal(1, registry.LeakCount);
        Assert.Equal(1, api.UploadCalls);
        Assert.Equal(1, api.ReloadCalls);
        Assert.Equal(32, api.LastUploadDescriptor.Width);
        Assert.Equal(16, api.LastUploadDescriptor.Height);
        Assert.True(api.LastUploadDescriptor.HasAlpha);
        Assert.True(api.LastUploadDescriptor.HasMipmaps);
        Assert.Equal(2, api.LastUploadDescriptor.MipmapCount);
        Assert.Equal(sampling, api.LastUploadDescriptor.Sampling);
        Assert.False(api.LastReloadDescriptor.HasAlpha);

        Assert.True(registry.Release(handle));

        Assert.Equal(0, registry.ActiveTextureCount);
        Assert.Equal(0, registry.LeakCount);
        Assert.Equal(1, api.ReleaseCalls);
        Assert.Equal(
            new[]
            {
                Electron2D.TextureResourceEventKind.Uploaded,
                Electron2D.TextureResourceEventKind.Reloaded,
                Electron2D.TextureResourceEventKind.Released
            },
            registry.Events.Select(item => item.Kind).ToArray());
    }

    [Fact]
    public void RegistryBuildsAtlasUploadDescriptor()
    {
        var api = new FakeTextureGpuApi();
        var registry = new Electron2D.TextureResourceRegistry(api);
        var atlas = new Electron2D.RuntimeTexture2D(64, 32, hasAlpha: true);
        var texture = new Electron2D.AtlasTexture
        {
            Atlas = atlas,
            Region = new Electron2D.Rect2(4f, 6f, 8f, 10f),
            Margin = new Electron2D.Rect2(1f, 2f, 3f, 4f),
            FilterClip = true
        };

        _ = registry.Upload(texture, Electron2D.TextureSamplingOptions.Default);

        Assert.Equal(8, api.LastUploadDescriptor.Width);
        Assert.Equal(10, api.LastUploadDescriptor.Height);
        Assert.True(api.LastUploadDescriptor.HasAlpha);
        Assert.Equal(new Electron2D.Rect2(4f, 6f, 8f, 10f), api.LastUploadDescriptor.SourceRegion);
        Assert.Equal(new Electron2D.Rect2(1f, 2f, 3f, 4f), api.LastUploadDescriptor.Margin);
        Assert.True(api.LastUploadDescriptor.FilterClip);
    }

    [Fact]
    public void RegistryDoesNotKeepActiveHandleAfterFailedUpload()
    {
        var api = new FakeTextureGpuApi
        {
            UploadError = "GPU texture allocation failed."
        };
        var registry = new Electron2D.TextureResourceRegistry(api);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            registry.Upload(new Electron2D.RuntimeTexture2D(8, 8, hasAlpha: false), Electron2D.TextureSamplingOptions.Default));

        Assert.Contains("GPU texture allocation", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, registry.ActiveTextureCount);
        Assert.Equal(0, registry.LeakCount);
        var error = Assert.Single(registry.Events, item => item.Kind == Electron2D.TextureResourceEventKind.Error);
        Assert.Equal("GPU texture allocation failed.", error.Error);
    }

    [Fact]
    public void RegistryRejectsUnknownReloadAndRelease()
    {
        var registry = new Electron2D.TextureResourceRegistry(new FakeTextureGpuApi());
        var handle = new Electron2D.TextureResourceHandle(new Electron2D.Rid(404));

        Assert.Throws<InvalidOperationException>(() =>
            registry.Reload(handle, new Electron2D.RuntimeTexture2D(8, 8, hasAlpha: false)));
        Assert.False(registry.Release(handle));
    }

    private sealed class FakeTextureGpuApi : Electron2D.ITextureGpuApi
    {
        public int UploadCalls { get; private set; }

        public int ReloadCalls { get; private set; }

        public int ReleaseCalls { get; private set; }

        public string? UploadError { get; init; }

        public Electron2D.TextureUploadDescriptor LastUploadDescriptor { get; private set; }

        public Electron2D.TextureUploadDescriptor LastReloadDescriptor { get; private set; }

        public bool Upload(Electron2D.Rid texture, Electron2D.TextureUploadDescriptor descriptor, out string? error)
        {
            UploadCalls++;
            LastUploadDescriptor = descriptor;
            error = UploadError;
            return error is null;
        }

        public bool Reload(Electron2D.Rid texture, Electron2D.TextureUploadDescriptor descriptor, out string? error)
        {
            ReloadCalls++;
            LastReloadDescriptor = descriptor;
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
}
