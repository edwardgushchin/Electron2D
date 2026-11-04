using System.Buffers.Binary;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private uint _depthTransferChecked, _depthTransferSupported;
    private bool DepthTransfersSupported(DataFormat format)
    {
        var native = NativeFormat(format); var index = (int)native - (int)SDL.GPUTextureFormat.D16Unorm;
        if ((uint)index >= 5) return false; var bit = 1u << index;
        if ((_depthTransferChecked & bit) == 0)
        {
            try { if (NativeAspectSupport(Device, 1, native) == 1) _depthTransferSupported |= bit; }
            catch (EntryPointNotFoundException) { }
            _depthTransferChecked |= bit;
        }
        return (_depthTransferSupported & bit) != 0;
    }
    private void RequireDepthTransfers(DataFormat format)
    { if (!DepthTransfersSupported(format)) throw new NotSupportedException("Depth/stencil transfers require the native aspect adapter and format support."); }
    private static uint DepthBytes(DataFormat format) => format == DataFormat.D16UNORM ? 2u : 4u;
    private static uint DepthPackedBytes(DataFormat format) => format == DataFormat.D32SFLOATS8UINT ? 8u : DepthBytes(format);
    private static uint AlignAspect(uint value) => checked((value + 3) & ~3u);
    private static uint StencilOffset(TextureInfo info, int mip)
    { return AlignAspect(checked(info.Offsets[mip] + Math.Max(1, info.Width >> mip) * Math.Max(1, info.Height >> mip) * DepthBytes(info.Format))); }
    private static TextureInfo CaptureDepthInfo(DataFormat format, TextureType type, uint width, uint height, uint layers, uint mips, TextureUsageBits usage, DataFormat[] shareable, uint swizzle, DataFormat stored)
    {
        var offsets = new uint[mips]; var packed = new uint[mips]; var sizes = new uint[mips]; uint total = 0, transfer = 0;
        for (var mip = 0; mip < mips; mip++)
        {
            var pixels = checked((ulong)Math.Max(1, width >> mip) * Math.Max(1, height >> mip));
            var size = pixels * DepthPackedBytes(format); var next = (ulong)AlignAspect(transfer) + pixels * DepthBytes(format);
            if (StencilFormat(format)) next = ((next + 3) & ~3ul) + pixels;
            if (size + total > int.MaxValue || next > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(width), "The depth mip chain exceeds managed transfer capacity.");
            offsets[mip] = AlignAspect(transfer); packed[mip] = total; sizes[mip] = (uint)size; total += (uint)size; transfer = (uint)next;
        }
        return new(format, type, width, height, layers, mips, usage, offsets, packed, sizes, total, transfer, shareable, Swizzle: swizzle, StoredFormat: stored);
    }
    private static void ValidateDepthData(DataFormat format, ReadOnlySpan<byte> data)
    {
        if (format is not (DataFormat.D32SFLOAT or DataFormat.D32SFLOATS8UINT)) return;
        var stride = (int)DepthPackedBytes(format);
        for (var at = 0; at < data.Length; at += stride)
        { var value = BinaryPrimitives.ReadSingleLittleEndian(data[at..]); if (!float.IsFinite(value) || value is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(data), "Depth floats must be finite and within zero through one."); }
    }
    private static void WriteTextureTransfer(TextureInfo info, nint pointer, ReadOnlySpan<byte> data)
    {
        for (var mip = 0; mip < info.Mips; mip++)
        {
            var source = data.Slice((int)info.PackedOffsets[mip], (int)info.Sizes[mip]);
            if (!DepthFormat(info.Format)) { source.CopyTo(new Span<byte>((void*)(pointer + (nint)info.Offsets[mip]), source.Length)); continue; }
            var depthBytes = (int)DepthBytes(info.Format); var stride = (int)DepthPackedBytes(info.Format); var pixels = source.Length / stride;
            var target = new Span<byte>((void*)(pointer + (nint)info.Offsets[mip]), pixels * depthBytes);
            var stencil = StencilFormat(info.Format) ? new Span<byte>((void*)(pointer + (nint)StencilOffset(info, mip)), pixels) : default;
            for (var i = 0; i < pixels; i++)
            {
                source.Slice(i * stride, depthBytes).CopyTo(target.Slice(i * depthBytes, depthBytes));
                if (info.Format is DataFormat.D24UNORMS8UINT or DataFormat.X8D24UNORMPACK32) target[i * 4 + 3] = 0;
                if (!stencil.IsEmpty) stencil[i] = source[i * stride + (info.Format == DataFormat.D24UNORMS8UINT ? 3 : 4)];
            }
        }
    }
    private static void ReadTextureTransfer(TextureInfo info, nint pointer, Span<byte> destination)
    {
        for (var mip = 0; mip < info.Mips; mip++)
        {
            var target = destination.Slice((int)info.PackedOffsets[mip], (int)info.Sizes[mip]);
            if (!DepthFormat(info.Format)) { new ReadOnlySpan<byte>((void*)(pointer + (nint)info.Offsets[mip]), target.Length).CopyTo(target); continue; }
            var stride = (int)DepthPackedBytes(info.Format); var bytes = (int)DepthBytes(info.Format); var pixels = target.Length / stride;
            var source = new ReadOnlySpan<byte>((void*)(pointer + (nint)info.Offsets[mip]), pixels * bytes);
            var stencil = StencilFormat(info.Format) ? new ReadOnlySpan<byte>((void*)(pointer + (nint)StencilOffset(info, mip)), pixels) : default;
            target.Clear();
            for (var i = 0; i < pixels; i++)
            {
                source.Slice(i * bytes, bytes).CopyTo(target.Slice(i * stride, bytes));
                if (info.Format == DataFormat.X8D24UNORMPACK32) target[i * 4 + 3] = 0;
                if (!stencil.IsEmpty) target[i * stride + (info.Format == DataFormat.D24UNORMS8UINT ? 3 : 4)] = stencil[i];
            }
        }
    }
    private void TransferTextureMip(Entry entry, uint layer, int mip, nint transfer, bool upload, nint pass)
    {
        var info = entry.Texture!; var region = Region(entry, layer, mip);
        if (!DepthFormat(info.Format))
        {
            var buffer = new SDL.GPUTextureTransferInfo { TransferBuffer = transfer, Offset = info.Offsets[mip] };
            if (upload) SDL.UploadToGPUTexture(pass, buffer, region, false); else SDL.DownloadFromGPUTexture(pass, region, buffer);
            return;
        }
        RequireDepthTransfers(info.Format);
        if (NativeAspectTransfer(Device, 1, _command, in region, transfer, info.Offsets[mip], StencilFormat(info.Format) ? StencilOffset(info, mip) : 0, upload ? 1u : 0u) != 1) throw GPUPhysicsDevice.Failure("record depth/stencil aspect transfer");
    }
    private void CopyTextureRegion(nint pass, Entry entry, SDL.GPUTextureLocation source, SDL.GPUTextureLocation destination, uint width, uint height)
    {
        if (!DepthFormat(entry.Texture!.Format)) { GPUTextureViews.Copy(Device, pass, source, destination, width, height, 1, false); return; }
        source = GPUTextureViews.Location(Device, source); destination = GPUTextureViews.Location(Device, destination);
        if (NativeAspectCopy(Device, 1, _command, in source, in destination, width, height) != 1) throw GPUPhysicsDevice.Failure("record depth/stencil image copy");
    }
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_aspect_support")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeAspectSupport(nint device, uint abi, SDL.GPUTextureFormat format);
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_aspect_transfer")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeAspectTransfer(nint device, uint abi, nint command, in SDL.GPUTextureRegion region, nint transfer, uint offset, uint stencilOffset, uint upload);
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_aspect_copy")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeAspectCopy(nint device, uint abi, nint command, in SDL.GPUTextureLocation source, in SDL.GPUTextureLocation destination, uint width, uint height);
}
