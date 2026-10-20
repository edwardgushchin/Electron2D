using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    /// <summary>Additional roles for a storage buffer.</summary>
    [Flags]
    public enum StorageBufferUsage
    {
        /// <summary>The buffer can supply three unsigned group counts to an indirect compute dispatch.</summary>
        DispatchIndirect = 1
    }

    /// <summary>Copies a validated byte range between buffers on this device.</summary>
    /// <param name="sourceBuffer">Live source ordinary or formatted texel buffer.</param>
    /// <param name="destinationBuffer">Live destination ordinary or formatted texel buffer.</param>
    /// <param name="sourceOffset">First source byte.</param>
    /// <param name="destinationOffset">First destination byte.</param>
    /// <param name="sizeBytes">Number of bytes to copy; zero performs no copy.</param>
    /// <remarks>Storage-to-storage copies follow the GPU timeline without readback. Uniform destinations
    /// are updated before returning; reading a storage source for a uniform destination synchronizes GPU work.
    /// A same-buffer copy requires disjoint ranges. End the active compute list before copying.</remarks>
    /// <exception cref="ArgumentException">An identity is invalid, is not a buffer or the ranges overlap.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Either range exceeds its buffer.</exception>
    /// <exception cref="InvalidOperationException">The device is accessed from another thread or a compute list is active.</exception>
    public void BufferCopy(RID sourceBuffer, RID destinationBuffer, uint sourceOffset, uint destinationOffset, uint sizeBytes)
    {
        IdleList(); var source = Get(sourceBuffer); var destination = Get(destinationBuffer);
        Range(source, sourceOffset, sizeBytes); Range(destination, destinationOffset, sizeBytes);
        if (sizeBytes == 0) return;
        if (ReferenceEquals(source, destination) && sourceOffset < destinationOffset + sizeBytes && destinationOffset < sourceOffset + sizeBytes)
            throw new ArgumentException("A same-buffer copy requires disjoint ranges.");
        if (destination.Kind == Kind.Uniform)
        {
            BufferGetData(sourceBuffer, destination.Data.AsSpan((int)destinationOffset, (int)sizeBytes), sourceOffset);
            return;
        }
        if (source.Kind == Kind.Uniform) { Upload(destination, destinationOffset, source.Data.AsSpan((int)sourceOffset, (int)sizeBytes)); return; }
        var pass = SDL.BeginGPUCopyPass(Commands()); if (pass == 0) throw GPUPhysicsDevice.Failure("begin buffer copy");
        SDL.CopyGPUBufferToBuffer(pass, new() { Buffer = source.Handle!.DangerousGetHandle(), Offset = source.BufferOffset + sourceOffset },
            new() { Buffer = destination.Handle!.DangerousGetHandle(), Offset = destination.BufferOffset + destinationOffset }, sizeBytes, false);
        SDL.EndGPUCopyPass(pass);
    }

    /// <summary>Replaces a validated buffer range with zero bytes.</summary>
    /// <param name="buffer">Live ordinary or formatted texel buffer.</param>
    /// <param name="offset">First byte to clear.</param>
    /// <param name="sizeBytes">Number of bytes, divisible by four; zero performs no clear.</param>
    /// <remarks>Storage clears are ordered with uploads, copies and dispatches. Uniform clears update host
    /// data immediately. End the active compute list before clearing.</remarks>
    /// <exception cref="ArgumentException">The identity is invalid or is not a buffer.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The size is not divisible by four or the range exceeds its buffer.</exception>
    /// <exception cref="InvalidOperationException">The device is accessed from another thread or a compute list is active.</exception>
    public void BufferClear(RID buffer, uint offset, uint sizeBytes)
    {
        IdleList(); var entry = Get(buffer); Range(entry, offset, sizeBytes);
        if (sizeBytes % 4 != 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        if (sizeBytes == 0) return;
        if (entry.Kind == Kind.Uniform) { entry.Data.AsSpan((int)offset, (int)sizeBytes).Clear(); return; }
        var command = Commands();
        var pointer = SDL.MapGPUTransferBuffer(Device, entry.Upload!.DangerousGetHandle(), true);
        if (pointer == 0) throw GPUPhysicsDevice.Failure("map clear storage");
        try { new Span<byte>((void*)pointer, (int)sizeBytes).Clear(); }
        finally { SDL.UnmapGPUTransferBuffer(Device, entry.Upload.DangerousGetHandle()); }
        var pass = SDL.BeginGPUCopyPass(command); if (pass == 0) throw GPUPhysicsDevice.Failure("begin buffer clear");
        SDL.UploadToGPUBuffer(pass, new() { TransferBuffer = entry.Upload.DangerousGetHandle() }, new() { Buffer = entry.Handle!.DangerousGetHandle(), Offset = entry.BufferOffset + offset, Size = sizeBytes }, false);
        SDL.EndGPUCopyPass(pass);
    }

    /// <summary>Records a compute dispatch whose group counts are supplied by a storage buffer.</summary>
    /// <param name="computeList">The active compute list.</param>
    /// <param name="buffer">Live storage buffer created with <see cref="StorageBufferUsage.DispatchIndirect"/>.</param>
    /// <param name="offset">Four-byte-aligned offset of three consecutive unsigned 32-bit X, Y and Z counts.</param>
    /// <remarks>Prior GPU writes to the counts complete before this dispatch. A zero count skips work.
    /// The producer must keep counts within device limits (at most 65535 per axis); counts stay on the GPU
    /// and are not validated by a synchronous readback. Every dispatch closes its native pass.</remarks>
    /// <exception cref="ArgumentException">The list or buffer is invalid, or the indirect role is missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The offset is unaligned or twelve bytes do not fit.</exception>
    /// <exception cref="InvalidOperationException">The pipeline or one of its resource sets is not bound.</exception>
    public void ComputeListDispatchIndirect(long computeList, RID buffer, uint offset)
    {
        List(computeList); var entry = Get(buffer, Kind.Storage); Range(entry, offset, 12);
        if (offset % 4 != 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if ((entry.Usage & StorageBufferUsage.DispatchIndirect) == 0) throw new ArgumentException("The buffer has no indirect-dispatch role.", nameof(buffer));
        Dispatch(0, 0, 0, entry, offset);
    }
}
