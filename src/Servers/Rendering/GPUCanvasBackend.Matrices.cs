using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private RenderHandle? _modelPoseBuffer, _modelPoseUpload;
    private int _modelPoseBytes;
    private void PrepareModelPoses(nint command, ReadOnlySpan<CanvasBatch> batches, ReadOnlySpan<CanvasModelPose> poses)
    {
        var used = false;
        foreach (var batch in batches) used |= batch.InstanceCount > 0 && batch.GPU.State is null && batch.UsesModelPoseTable;
        if (!used) return;
        var bytes = checked(poses.Length * sizeof(CanvasModelPose));
        if (_modelPoseBytes < bytes)
        {
            var buffer = InstanceBuffer((uint)bytes, SDL.GPUBufferUsageFlags.GraphicsStorageRead);
            RenderHandle upload;
            try { upload = new(SDL.CreateGPUTransferBuffer(Device, new() { Size = (uint)bytes, Usage = SDL.GPUTransferBufferUsage.Upload }), h => SDL.ReleaseGPUTransferBuffer(Device, h), _device); }
            catch { buffer.Dispose(); throw; }
            _modelPoseBuffer?.Dispose(); _modelPoseUpload?.Dispose(); _modelPoseBuffer = buffer; _modelPoseUpload = upload; _modelPoseBytes = bytes;
        }
        var mapped = SDL.MapGPUTransferBuffer(Device, _modelPoseUpload!.DangerousGetHandle(), true); if (mapped == 0) throw Failure("map model poses");
        try { MemoryMarshal.AsBytes(poses).CopyTo(new Span<byte>((void*)mapped, bytes)); }
        finally { SDL.UnmapGPUTransferBuffer(Device, _modelPoseUpload.DangerousGetHandle()); }
        var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw Failure("upload model poses");
        SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _modelPoseUpload.DangerousGetHandle() }, new() { Buffer = _modelPoseBuffer!.DangerousGetHandle(), Size = (uint)bytes }, false); SDL.EndGPUCopyPass(copy);
    }
    private void ReleaseModelPoses() { _modelPoseBuffer?.Dispose(); _modelPoseUpload?.Dispose(); }
}
