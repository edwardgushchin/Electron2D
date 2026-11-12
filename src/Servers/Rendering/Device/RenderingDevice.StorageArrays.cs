using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private const uint VulkanComputeImageABI = 2;
    private int? _storageArrayViewSupport;
    private uint StorageArrayABI => TextureDriver == "direct3d12" ? GPUTextureViews.Direct3DArrayABI : VulkanComputeImageABI;
    private void RequireStorageArrayViews()
    {
        if ((_storageArrayViewSupport ??= GPUTextureViews.Support(Device, StorageArrayABI)) != 1)
            throw new NotSupportedException("Array storage images require the pinned native array-view adapter.");
    }
    private nint StorageImageHandle(Entry image, bool array)
    {
        if (!array) return image.NativeHandle;
        return (image.StorageArrayView ??= CreateStorageArrayView(image)).DangerousGetHandle();
    }
    private RenderHandle CreateStorageArrayView(Entry image)
    {
        RequireStorageArrayViews();
        var info = image.Texture!;
        var abi = StorageArrayABI;
        var destroyABI = abi == GPUTextureViews.Direct3DArrayABI ? abi : 1;
        // Storage descriptors expose one mip while their image coordinate selects any retained layer.
        return new(GPUTextureViews.View(Device, abi, image.NativeHandle, NativeFormat(info.Format),
            0, info.Layers, 0, 1, 2), h => GPUTextureViews.Destroy(Device, destroyABI, h), _context.Handle);
    }
    private uint ComputeImageABI(nint* images, ReadOnlySpan<uint> layers, bool arrayImages,
        SDL.GPUTextureSamplerBinding* samplers, uint samplerCount)
    {
        var imageCount = layers.Length;
        if (imageCount == 0) return 0;
        if (TextureDriver == "direct3d12")
        {
            // SDL transitions each writable slot. An aliased mip would be transitioned twice.
            for (var i = 0; i < imageCount; i++)
                for (var j = 0; j < i; j++)
                    if (GPUTextureViews.Overlap(Device, images[i], layers[i], 1, images[j], layers[j], 1))
                        throw new NotSupportedException("Direct3D 12 cannot bind one subresource to multiple writable compute slots.");
            for (var i = 0; i < samplerCount; i++)
                for (var j = 0; j < imageCount; j++)
                    if (GPUTextureViews.Overlap(Device, images[j], layers[j], 1, samplers[i].Texture, uint.MaxValue, uint.MaxValue))
                        throw new NotSupportedException("Direct3D 12 cannot sample a subresource during a compute write to it.");
            return arrayImages ? GPUTextureViews.Direct3DArrayABI : 0;
        }
        if (TextureDriver != "vulkan") return 0;
        if ((_storageArrayViewSupport ??= GPUTextureViews.Support(Device, VulkanComputeImageABI)) != 1)
        {
            for (var i = 0; i < samplerCount; i++) for (var j = 0; j < imageCount; j++)
                    if (GPUTextureViews.Location(Device, new() { Texture = samplers[i].Texture }).Texture == GPUTextureViews.Location(Device, new() { Texture = images[j] }).Texture) throw new NotSupportedException("Simultaneous sampled/storage aliases require the pinned native descriptor adapter.");
            return 0;
        }
        return VulkanComputeImageABI;
    }
    private void PrepareComputeImages(uint abi, nint* images, uint imageCount, bool prepare)
    {
        if (NativePrepareComputeImages(Device, abi, _command, images, imageCount, prepare ? 1u : 0u) != 1)
            throw new InvalidOperationException("Compute image preparation failed: " + SDL.GetError());
    }
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_prepare_compute_images")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativePrepareComputeImages(nint device, uint abi, nint commands, nint* images, uint count, uint prepare);
}
