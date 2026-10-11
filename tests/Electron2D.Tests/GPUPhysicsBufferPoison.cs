using SDL3;

internal static unsafe class GPUPhysicsBufferPoison
{
    internal static void Fill(nint device, nint buffer, uint size) => Fill(device, buffer, size, 0x7fc00001);
    internal static void FillFinite(nint device, nint buffer, uint size) => Fill(device, buffer, size, 0x3f800000);
    private static void Fill(nint device, nint buffer, uint size, uint pattern)
    {
        var transfer = SDL.CreateGPUTransferBuffer(device, new SDL.GPUTransferBufferCreateInfo
        { Size = size, Usage = SDL.GPUTransferBufferUsage.Upload });
        if (transfer == 0) throw new InvalidOperationException(SDL.GetError());
        nint command = 0, fence = 0;
        try
        {
            var mapped = SDL.MapGPUTransferBuffer(device, transfer, false);
            if (mapped == 0) throw new InvalidOperationException(SDL.GetError());
            // Finite stale values can evade checks that reject only NaN or infinity.
            try { new Span<uint>((void*)mapped, checked((int)size / sizeof(uint))).Fill(pattern); }
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
