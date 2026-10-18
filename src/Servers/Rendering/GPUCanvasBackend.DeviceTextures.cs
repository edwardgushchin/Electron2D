namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    internal RID ExportTexture(Resource resource, RenderingDevice device, bool SRGB)
    {
        if (resource is DrawableTexture drawable)
        {
            if (SRGB && drawable.NativeFormat != RenderingDevice.DataFormat.R8G8B8A8SRGB) throw new NotSupportedException("Distinct sRGB views require native format-view integration.");
            return device.BorrowDrawableTexture(drawable, drawable.EnsureImage());
        }
        if (resource is ViewportTexture viewportTexture)
        {
            var viewport = viewportTexture.Bound; var target = viewport is null ? null : FindTarget(viewport);
            if (target is null || !target.HasFrame || viewportTexture.GetSize().X == 0 || viewportTexture.GetSize().Y == 0) return default;
            if (SRGB) throw new NotSupportedException("Distinct sRGB renderer views require native format-view integration.");
            return device.BorrowRendererTexture(viewportTexture, null, target);
        }
        if (resource is Texture texture)
        {
            var pixels = texture.CapturePixels(); if (pixels is null) return default;
            if (SRGB) throw new NotSupportedException("Distinct sRGB renderer views require native format-view integration.");
            if (!_textures.TryGetValue(texture, out var gpu) || !ReferenceEquals(gpu.Pixels.Allocation, pixels.Allocation))
            {
                var replacement = new GPUTexture(_device, pixels); gpu?.Dispose(); _textures[texture] = gpu = replacement;
            }
            else gpu.Pixels = pixels;
            return device.BorrowRendererTexture(texture, gpu, null);
        }
        if (resource is TextureLayered layered)
        {
            var pixels = layered.CaptureLayers(); if (pixels is null) return default;
            if (SRGB) throw new NotSupportedException("Distinct sRGB renderer views require native format-view integration.");
            if (!_layeredTextures.TryGetValue(layered, out var gpu) || !ReferenceEquals(gpu.ArrayPixels!.Allocation, pixels.Allocation))
            {
                var replacement = new GPUTexture(_device, pixels); gpu?.Dispose(); _layeredTextures[layered] = gpu = replacement;
            }
            else { gpu.ArrayPixels = pixels; gpu.Pixels = pixels.First; }
            return device.BorrowRendererTexture(layered, gpu, null);
        }
        return default;
    }
}
