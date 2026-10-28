using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    internal void ValidateCanvasMutation() => IdleList();
    internal void ValidateCanvasDisposal() => Access();
    internal TextureInfo? CanvasInfo(RID rid)
    {
        Access(true);
        return _entries.TryGetValue(rid, out var entry) && entry.Kind == Kind.Texture && entry.Alive ? entry.Texture : null;
    }
    internal TextureInfo ValidateCanvasImage(RID rid, bool array)
    {
        IdleList(); var info = Get(rid, Kind.Texture).Texture!;
        if ((info.Type == TextureType.TextureArray) != array) throw new ArgumentException("The canvas image sampling role does not match.", nameof(rid));
        if (info.Samples != TextureSamples.Samples1) throw new NotSupportedException("Resolve multisample images before canvas sampling.");
        if ((info.Usage & TextureUsageBits.Sampling) == 0) throw new ArgumentException("The image has no sampling role.", nameof(rid));
        return info;
    }
    internal nint CanvasHandle(RID rid, nint device)
    {
        Access(true); if (Device != device) throw new InvalidOperationException("The image belongs to a different native renderer.");
        return _entries.TryGetValue(rid, out var entry) && entry.Kind == Kind.Texture && entry.Alive ? entry.NativeHandle : throw new ArgumentException("The canvas image is not live.", nameof(rid));
    }
    internal void FlushRendererCommands()
    {
        IdleList(true); if (_command != 0 || _recordedReadbacks.Count != 0) { CompleteSubmission(); SubmitRecorded(true); }
    }
    internal void CompleteRendererReadbacks()
    {
        IdleList(); if (_fence != 0 || _submittedReadbacks.Count != 0 || _timestampPending) CompleteSubmission(); RetireRendererTextures(); GPUTextureViews.CollectIfActive(Device); CollectAddressBuffers(); DeliverReadbacks();
    }
    internal Image? CanvasImage(RID rid, uint layer)
    {
        IdleList(); var entry = Get(rid, Kind.Texture); Layer(entry, layer); var info = entry.Texture!;
        if ((info.Usage & (TextureUsageBits.CPURead | TextureUsageBits.CanCopyFrom)) == 0) return null;
        if (PixelType(info.Format) != 0) return IntegerCanvasImage(rid, layer, info);
        var format = CanvasImageFormat(info.Format);
        if (format is null) return ConvertedCanvasImage(entry, layer);
        return CanvasSnapshot(info, format.Value, TextureGetData(rid, layer));
    }
    private Image IntegerCanvasImage(RID rid, uint layer, TextureInfo info)
    {
        var source = TextureGetData(rid, layer); var native = NativeFormat(info.Format);
        var pixelBytes = SDL.GPUTextureFormatTexelBlockSize(native);
        var componentBytes = info.Format switch
        {
            DataFormat.R8UINT or DataFormat.R8SINT or DataFormat.R8G8UINT or DataFormat.R8G8SINT or DataFormat.R8G8B8A8UINT or DataFormat.R8G8B8A8SINT => 1,
            DataFormat.R16UINT or DataFormat.R16SINT or DataFormat.R16G16UINT or DataFormat.R16G16SINT or DataFormat.R16G16B16A16UINT or DataFormat.R16G16B16A16SINT => 2,
            _ => 4
        };
        var components = (int)pixelBytes / componentBytes;
        var pixels = source.Length / (int)pixelBytes;
        var output = new float[checked(pixels * 4)]; var signed = PixelType(info.Format) == 1;
        for (var i = 0; i < pixels; i++)
        {
            output[i * 4 + 3] = 1;
            for (var c = 0; c < components; c++)
            {
                var offset = (i * components + c) * componentBytes;
                output[i * 4 + c] = componentBytes switch
                {
                    1 => signed ? unchecked((sbyte)source[offset]) : source[offset],
                    2 => signed ? System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(source.AsSpan(offset)) : System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(source.AsSpan(offset)),
                    _ => signed ? System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(source.AsSpan(offset)) : System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(offset))
                };
            }
        }
        return CanvasSnapshot(info, Image.Format.Rgbaf, System.Runtime.InteropServices.MemoryMarshal.AsBytes(output.AsSpan()).ToArray());
    }
    private Image ConvertedCanvasImage(Entry source, uint layer)
    {
        var info = source.Texture!; var offsets = new uint[info.Mips]; uint total = 0;
        for (var mip = 0; mip < info.Mips; mip++)
        {
            offsets[mip] = total;
            var bytes = (ulong)Math.Max(1, info.Width >> mip) * Math.Max(1, info.Height >> mip) * 16;
            if (bytes + total > int.MaxValue) throw new NotSupportedException("The converted image exceeds managed readback capacity.");
            total += (uint)bytes;
        }
        var create = new SDL.GPUTextureCreateInfo
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = SDL.GPUTextureFormat.R32G32B32A32Float,
            Width = info.Width,
            Height = info.Height,
            LayerCountOrDepth = 1,
            NumLevels = info.Mips,
            SampleCount = SDL.GPUSampleCount.SampleCount1,
            Usage = SDL.GPUTextureUsageFlags.ColorTarget
        };
        using var target = new RenderHandle(SDL.CreateGPUTexture(Device, in create), h => SDL.ReleaseGPUTexture(Device, h), _context.Handle);
        using var transfer = Transfer(total, SDL.GPUTransferBufferUsage.Download);
        var command = Commands();
        for (var mip = 0; mip < info.Mips; mip++)
        {
            var width = Math.Max(1, info.Width >> mip); var height = Math.Max(1, info.Height >> mip);
            var blit = new SDL.GPUBlitInfo
            {
                Source = new() { Texture = source.NativeHandle, LayerOrDepthPlane = layer, MipLevel = (uint)mip, W = width, H = height },
                Destination = new() { Texture = target.DangerousGetHandle(), MipLevel = (uint)mip, W = width, H = height },
                LoadOp = SDL.GPULoadOp.DontCare,
                Filter = SDL.GPUFilter.Nearest
            };
            SDL.BlitGPUTexture(command, in blit);
        }
        var pass = SDL.BeginGPUCopyPass(command); if (pass == 0) throw GPUPhysicsDevice.Failure("begin converted image read");
        try
        {
            for (var mip = 0; mip < info.Mips; mip++) SDL.DownloadFromGPUTexture(pass,
            new() { Texture = target.DangerousGetHandle(), MipLevel = (uint)mip, W = Math.Max(1, info.Width >> mip), H = Math.Max(1, info.Height >> mip), D = 1 },
            new() { TransferBuffer = transfer.DangerousGetHandle(), Offset = offsets[mip] });
        }
        finally { SDL.EndGPUCopyPass(pass); }
        SubmitRecorded(); CompleteSubmission(); var pointer = SDL.MapGPUTransferBuffer(Device, transfer.DangerousGetHandle(), false);
        if (pointer == 0) throw GPUPhysicsDevice.Failure("map converted image read");
        Image image;
        try { image = CanvasSnapshot(info, Image.Format.Rgbaf, new ReadOnlySpan<byte>((void*)pointer, (int)total).ToArray()); }
        finally { SDL.UnmapGPUTransferBuffer(Device, transfer.DangerousGetHandle()); }
        try { DeliverReadbacks(); return image; } catch { image.Dispose(); throw; }
    }
    private static Image CanvasSnapshot(TextureInfo info, Image.Format format, byte[] bytes)
    {
        // Image stores complete mip chains; a bounded native view can contain fewer levels.
        var complete = info.Mips == System.Numerics.BitOperations.Log2(Math.Max(info.Width, info.Height)) + 1;
        if (!complete && info.Mips > 1) bytes = bytes.AsSpan(0, Image.GetRequiredDataSize((int)info.Width, (int)info.Height, format, false)).ToArray();
        return Image.CreateFromData((int)info.Width, (int)info.Height, complete && info.Mips > 1, format, bytes);
    }
    internal static Image.Format? CanvasImageFormat(DataFormat format) => format switch
    {
        DataFormat.R8UNORM => Image.Format.R8,
        DataFormat.R8G8UNORM => Image.Format.Rg8,
        DataFormat.R8G8B8A8UNORM or DataFormat.R8G8B8A8SRGB => Image.Format.Rgba8,
        DataFormat.R16UNORM => Image.Format.R16,
        DataFormat.R16G16UNORM => Image.Format.Rg16,
        DataFormat.R16G16B16A16UNORM => Image.Format.Rgba16,
        DataFormat.R16SFLOAT => Image.Format.Rh,
        DataFormat.R16G16SFLOAT => Image.Format.Rgh,
        DataFormat.R16G16B16A16SFLOAT => Image.Format.Rgbah,
        DataFormat.R32SFLOAT => Image.Format.Rf,
        DataFormat.R32G32SFLOAT => Image.Format.Rgf,
        DataFormat.R32G32B32A32SFLOAT => Image.Format.Rgbaf,
        DataFormat.BC1RGBAUNORMBLOCK or DataFormat.BC1RGBASRGBBLOCK => Image.Format.Dxt1,
        DataFormat.BC2UNORMBLOCK or DataFormat.BC2SRGBBLOCK => Image.Format.Dxt3,
        DataFormat.BC3UNORMBLOCK or DataFormat.BC3SRGBBLOCK => Image.Format.Dxt5,
        DataFormat.BC4UNORMBLOCK => Image.Format.RgtcR,
        DataFormat.BC5UNORMBLOCK => Image.Format.RgtcRg,
        DataFormat.BC6HSFLOATBLOCK => Image.Format.BptcRgbf,
        DataFormat.BC6HUFLOATBLOCK => Image.Format.BptcRgbfu,
        DataFormat.BC7UNORMBLOCK or DataFormat.BC7SRGBBLOCK => Image.Format.BptcRgba,
        DataFormat.ASTC4X4UNORMBLOCK or DataFormat.ASTC4X4SRGBBLOCK => Image.Format.Astc4X4,
        DataFormat.ASTC4X4SFLOATBLOCK => Image.Format.Astc4X4Hdr,
        DataFormat.ASTC8X8UNORMBLOCK or DataFormat.ASTC8X8SRGBBLOCK => Image.Format.Astc8X8,
        DataFormat.ASTC8X8SFLOATBLOCK => Image.Format.Astc8X8Hdr,
        _ => null
    };
}
