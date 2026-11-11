namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    internal void RetireReplacedUpload(Resource resource)
    {
        if (resource is Texture texture)
        {
            ReleaseTemporaryTexture(texture);
            _linearSources.Remove(texture);
            _usedLinearViews.Remove(texture);
            if (_linearViews.Remove(texture, out var view)) view.View.Dispose();
        }
        else if (resource is TextureLayered layered)
        {
            _usedLayeredTextures.Remove(layered);
            if (_layeredTextures.Remove(layered, out var upload)) upload.Dispose();
            _linearArraySources.Remove(layered);
            _usedLinearArrayViews.Remove(layered);
            if (_linearArrayViews.Remove(layered, out var view)) view.View.Dispose();
        }
    }

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
            if (!_textures.TryGetValue(texture, out var gpu) || !ReferenceEquals(gpu.Pixels.Allocation, pixels.Allocation))
            {
                var replacement = new GPUTexture(_device, pixels, export: true);
                gpu?.Dispose(); _textures[texture] = gpu = replacement;
            }
            else
            {
                gpu.Pixels = pixels;
                gpu.PrepareForExport(_device);
            }
            if (SRGB && !gpu.HasSRGBView) throw new NotSupportedException("The renderer upload cannot supply an sRGB view.");
            return device.BorrowRendererTexture(texture, gpu, null);
        }
        if (resource is TextureLayered layered)
        {
            var pixels = layered.CaptureLayers(); if (pixels is null) return default;
            if (!_layeredTextures.TryGetValue(layered, out var gpu) || !ReferenceEquals(gpu.ArrayPixels!.Allocation, pixels.Allocation))
            {
                var replacement = new GPUTexture(_device, pixels, export: true);
                gpu?.Dispose(); _layeredTextures[layered] = gpu = replacement;
            }
            else
            {
                gpu.ArrayPixels = pixels; gpu.Pixels = pixels.First;
                gpu.PrepareForExport(_device);
            }
            if (SRGB && !gpu.HasSRGBView) throw new NotSupportedException("The renderer array cannot supply an sRGB view.");
            return device.BorrowRendererTexture(layered, gpu, null);
        }
        return default;
    }
}
