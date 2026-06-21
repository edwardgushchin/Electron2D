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

internal sealed class TextureResourceRegistry
{
    private ITextureGpuApi api;
    private readonly RidAllocator allocator = new();
    private readonly Dictionary<Rid, TextureUploadDescriptor> activeTextures = new();
    private readonly List<TextureResourceEvent> events = new();

    public TextureResourceRegistry(ITextureGpuApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        this.api = api;
    }

    public int ActiveTextureCount => activeTextures.Count;

    public int LeakCount => activeTextures.Count;

    public IReadOnlyList<TextureResourceEvent> Events => events;

    public TextureResourceHandle Upload(Texture2D texture, TextureSamplingOptions sampling)
    {
        ArgumentNullException.ThrowIfNull(texture);

        var rid = allocator.Allocate();
        var descriptor = TextureUploadDescriptor.FromTexture(texture, sampling);
        if (!api.Upload(rid, descriptor, out var error))
        {
            allocator.Free(rid);
            Fail(TextureResourceEventKind.Error, rid, error ?? "Texture upload failed.");
        }

        activeTextures.Add(rid, descriptor);
        events.Add(new TextureResourceEvent(TextureResourceEventKind.Uploaded, rid, "Texture uploaded."));
        return new TextureResourceHandle(rid);
    }

    public TextureResourceHandle CreateRenderTarget(Vector2I size, bool hasAlpha, TextureSamplingOptions sampling)
    {
        if (size.X <= 0 || size.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Render target size must be positive.");
        }

        var rid = allocator.Allocate();
        var descriptor = TextureUploadDescriptor.ForRenderTarget(size, hasAlpha, sampling);
        if (!api.Upload(rid, descriptor, out var error))
        {
            allocator.Free(rid);
            Fail(TextureResourceEventKind.Error, rid, error ?? "Render target allocation failed.");
        }

        activeTextures.Add(rid, descriptor);
        events.Add(new TextureResourceEvent(TextureResourceEventKind.RenderTargetCreated, rid, "Render target created."));
        return new TextureResourceHandle(rid);
    }

    public void Reload(TextureResourceHandle handle, Texture2D texture)
    {
        ArgumentNullException.ThrowIfNull(texture);

        if (!handle.IsValid || !activeTextures.ContainsKey(handle.Rid))
        {
            throw new InvalidOperationException("Cannot reload an unknown texture resource.");
        }

        var previous = activeTextures[handle.Rid];
        var descriptor = TextureUploadDescriptor.FromTexture(texture, previous.Sampling);
        if (!api.Reload(handle.Rid, descriptor, out var error))
        {
            Fail(TextureResourceEventKind.Error, handle.Rid, error ?? "Texture reload failed.");
        }

        activeTextures[handle.Rid] = descriptor;
        events.Add(new TextureResourceEvent(TextureResourceEventKind.Reloaded, handle.Rid, "Texture reloaded."));
    }

    public void RestoreAfterDeviceLoss(ITextureGpuApi newApi)
    {
        ArgumentNullException.ThrowIfNull(newApi);

        var restored = new List<Rid>(activeTextures.Count);
        foreach (var (rid, descriptor) in activeTextures)
        {
            if (!newApi.Upload(rid, descriptor, out var error))
            {
                Fail(TextureResourceEventKind.Error, rid, error ?? "Texture restore failed.");
            }

            restored.Add(rid);
        }

        api = newApi;
        foreach (var rid in restored)
        {
            events.Add(new TextureResourceEvent(TextureResourceEventKind.Restored, rid, "Texture resource restored."));
        }
    }

    public bool Release(TextureResourceHandle handle)
    {
        if (!handle.IsValid || !activeTextures.ContainsKey(handle.Rid))
        {
            return false;
        }

        if (!api.Release(handle.Rid, out var error))
        {
            Fail(TextureResourceEventKind.Error, handle.Rid, error ?? "Texture release failed.");
        }

        activeTextures.Remove(handle.Rid);
        allocator.Free(handle.Rid);
        events.Add(new TextureResourceEvent(TextureResourceEventKind.Released, handle.Rid, "Texture released."));
        return true;
    }

    private void Fail(TextureResourceEventKind kind, Rid texture, string error)
    {
        events.Add(new TextureResourceEvent(kind, texture, "Texture resource error.", error));
        throw new InvalidOperationException(error);
    }
}
