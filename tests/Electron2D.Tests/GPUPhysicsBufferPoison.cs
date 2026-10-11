using SDL3;

internal static unsafe class GPUPhysicsBufferPoison
{
    internal static void Fill(nint device, nint buffer, uint size)
    {
        var transfer = SDL.CreateGPUTransferBuffer(device, new SDL.GPUTransferBufferCreateInfo
        { Size = size, Usage = SDL.GPUTransferBufferUsage.Upload });
        if (transfer == 0) throw new InvalidOperationException(SDL.GetError());
        nint command = 0, fence = 0;
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(device, transfer, false);
            if (mapped == 0) throw new InvalidOperationException(SDL.GetError());
            // Every float word is NaN; untouched counters/indices are invalid as well.
            try { new Span<uint>((void*)mapped, checked((int)size / sizeof(uint))).Fill(0x7fc00001); }
            finally { SDL.UnmapGPUTransferBuffer(device, transfer); }
            command = SDL.AcquireGPUCommandBuffer(device);
            if (command == 0) throw new InvalidOperationException(SDL.GetError());
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw new InvalidOperationException(SDL.GetError());
            SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = transfer }, new() { Buffer = buffer, Size = size }, false);
            SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0;
            fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0 || !SDL.WaitForGPUFences(device, true, new ReadOnlySpan<nint>(&fence, 1), 1))
                throw new InvalidOperationException(SDL.GetError());
        }
        finally
        {
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
            if (fence != 0) SDL.ReleaseGPUFence(device, fence);
            SDL.ReleaseGPUTransferBuffer(device, transfer);
        }
    }
}
