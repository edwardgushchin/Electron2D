using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private int? _storageArrayViewSupport;
    private void RequireStorageArrayViews()
    {
        if ((_storageArrayViewSupport ??= GPUTextureViews.Support(Device, 2)) != 1) throw new NotSupportedException("Array storage images require the pinned native array-view adapter.");
    }
    private nint StorageImageHandle(Entry image, bool array)
    {
        if (!array) return image.NativeHandle;
        RequireStorageArrayViews(); var info = image.Texture!;
        // Storage descriptors expose one mip while their image coordinate selects any retained layer.
        image.StorageArrayView ??= new(GPUTextureViews.View(Device, 2, image.NativeHandle, NativeFormat(info.Format), 0, info.Layers, 0, 1, 2), h => GPUTextureViews.Destroy(Device, 1, h), _context.Handle);
        return image.StorageArrayView.DangerousGetHandle();
    }
    private bool UseNativeComputeImages(nint* images, uint imageCount, SDL.GPUTextureSamplerBinding* samplers, uint samplerCount)
    {
        if (TextureDriver != "vulkan" || imageCount == 0) return false;
        if ((_storageArrayViewSupport ??= GPUTextureViews.Support(Device, 2)) != 1)
        {
            for (var i = 0; i < samplerCount; i++) for (var j = 0; j < imageCount; j++)
                    if (GPUTextureViews.Location(Device, new() { Texture = samplers[i].Texture }).Texture == GPUTextureViews.Location(Device, new() { Texture = images[j] }).Texture) throw new NotSupportedException("Simultaneous sampled/storage aliases require the pinned native descriptor adapter.");
            return false;
        }
        return true;
    }
    private void PrepareComputeImages(nint* images, uint imageCount, bool prepare)
    { if (NativePrepareComputeImages(Device, 2, _command, images, imageCount, prepare ? 1u : 0u) != 1) throw new InvalidOperationException("Compute image descriptor preparation failed: " + SDL.GetError()); }
    [LibraryImport(NativeLibraries.RenderLibrary, EntryPoint = "e2d_render_prepare_compute_images")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativePrepareComputeImages(nint device, uint abi, nint commands, nint* images, uint count, uint prepare);
}
