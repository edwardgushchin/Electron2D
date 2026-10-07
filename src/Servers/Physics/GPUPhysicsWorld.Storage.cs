using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    private sealed class Storage<T>(GPUPhysicsWorld world) : IDisposable where T : unmanaged
    {
        private RenderHandle? _buffer, _upload, _download;
        internal T[] Data = [];
        internal nint Handle => _buffer!.DangerousGetHandle();

        internal void Reserve(int count)
        {
            if (count <= Data.Length) return;
            var capacity = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(64, count)));
            var size = checked((uint)(capacity * sizeof(T)));
            RenderHandle? buffer = null, upload = null, download = null;
            try
            {
                var info = new SDL.GPUBufferCreateInfo { Size = size, Usage = SDL.GPUBufferUsageFlags.ComputeStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite };
                buffer = new(SDL.CreateGPUBuffer(world.Device, in info), handle => SDL.ReleaseGPUBuffer(world.Device, handle), world._device);
                var transfer = new SDL.GPUTransferBufferCreateInfo { Size = size, Usage = SDL.GPUTransferBufferUsage.Upload };
                upload = new(SDL.CreateGPUTransferBuffer(world.Device, in transfer), handle => SDL.ReleaseGPUTransferBuffer(world.Device, handle), world._device);
                transfer.Usage = SDL.GPUTransferBufferUsage.Download;
                download = new(SDL.CreateGPUTransferBuffer(world.Device, in transfer), handle => SDL.ReleaseGPUTransferBuffer(world.Device, handle), world._device);
                var data = new T[capacity];
                Dispose(); (_buffer, _upload, _download, Data) = (buffer, upload, download, data);
                buffer = upload = download = null;
            }
            finally { buffer?.Dispose(); upload?.Dispose(); download?.Dispose(); }
        }

        internal void Upload(nint copy, int count)
        {
            if (count == 0) return;
            var size = checked((uint)(count * sizeof(T)));
            var mapped = SDL.MapGPUTransferBuffer(world.Device, _upload!.DangerousGetHandle(), true);
            if (mapped == 0) throw Failure("map a physics upload");
            try { fixed (T* source = Data) Buffer.MemoryCopy(source, (void*)mapped, size, size); }
            finally { SDL.UnmapGPUTransferBuffer(world.Device, _upload.DangerousGetHandle()); }
            SDL.UploadToGPUBuffer(copy, new SDL.GPUTransferBufferLocation { TransferBuffer = _upload.DangerousGetHandle() },
                new SDL.GPUBufferRegion { Buffer = Handle, Size = size }, true);
        }

        internal void Download(nint copy, int count)
        {
            if (count == 0) return;
            SDL.DownloadFromGPUBuffer(copy, new SDL.GPUBufferRegion { Buffer = Handle, Size = checked((uint)(count * sizeof(T))) },
                new SDL.GPUTransferBufferLocation { TransferBuffer = _download!.DangerousGetHandle() });
        }

        internal void Read(int count)
        {
            if (count == 0) return;
            var size = checked((uint)(count * sizeof(T)));
            var mapped = SDL.MapGPUTransferBuffer(world.Device, _download!.DangerousGetHandle(), false);
            if (mapped == 0) throw Failure("map a physics readback");
            try { fixed (T* destination = Data) Buffer.MemoryCopy((void*)mapped, destination, size, size); }
            finally { SDL.UnmapGPUTransferBuffer(world.Device, _download.DangerousGetHandle()); }
        }

        public void Dispose() { _buffer?.Dispose(); _upload?.Dispose(); _download?.Dispose(); }
    }
}
