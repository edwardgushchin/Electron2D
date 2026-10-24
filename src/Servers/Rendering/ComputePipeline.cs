using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe class ComputePipeline : IDisposable
{
    internal readonly record struct BufferLayout(bool Enabled, uint Uniforms, uint ReadOnly, uint ReadWrite);
    internal RenderHandle Handle { get; }
    private readonly RenderHandle _device;
    private readonly BufferLayout _layout;
    private RenderHandle? _sizes, _upload;
    private const uint TableBytes = 32 * sizeof(uint);

    internal ComputePipeline(RenderHandle device, byte[] code)
    {
        _device = device;
        var compiled = ShaderCompiler.CreateComputePipelineWithBufferSizes(device.DangerousGetHandle(), code);
        _layout = compiled.Layout;
        var nativeDevice = device.DangerousGetHandle();
        Handle = new(compiled.Handle, h => SDL.ReleaseGPUComputePipeline(nativeDevice, h), device);
        try
        {
            if (!_layout.Enabled) return;
            _sizes = new(SDL.CreateGPUBuffer(nativeDevice, new() { Size = TableBytes, Usage = SDL.GPUBufferUsageFlags.ComputeStorageRead }), h => SDL.ReleaseGPUBuffer(nativeDevice, h), device);
            _upload = new(SDL.CreateGPUTransferBuffer(nativeDevice, new() { Size = TableBytes, Usage = SDL.GPUTransferBufferUsage.Upload }), h => SDL.ReleaseGPUTransferBuffer(nativeDevice, h), device);
        }
        catch { Dispose(); throw; }
    }

    internal nint DangerousGetHandle() => Handle.DangerousGetHandle();

    internal void PrepareBufferSizes(nint command, ReadOnlySpan<uint> readOnly, ReadOnlySpan<uint> readWrite)
    {
        if (!_layout.Enabled) return;
        if (readOnly.Length != _layout.ReadOnly || readWrite.Length != _layout.ReadWrite)
            throw new InvalidOperationException("Compute buffer sizes must cover every native storage binding.");
        Span<uint> table = stackalloc uint[32]; table.Clear();
        readOnly.CopyTo(table[(int)_layout.Uniforms..]);
        readWrite.CopyTo(table[(int)(_layout.Uniforms + _layout.ReadOnly)..]);
        var device = _device.DangerousGetHandle();
        var mapped = SDL.MapGPUTransferBuffer(device, _upload!.DangerousGetHandle(), true);
        if (mapped == 0) throw CanvasBackend.Failure("map compute buffer sizes");
        try { MemoryMarshal.AsBytes(table).CopyTo(new Span<byte>((void*)mapped, (int)TableBytes)); }
        finally { SDL.UnmapGPUTransferBuffer(device, _upload.DangerousGetHandle()); }
        var copy = SDL.BeginGPUCopyPass(command);
        if (copy == 0) throw CanvasBackend.Failure("begin compute buffer-size upload");
        SDL.UploadToGPUBuffer(copy, new SDL.GPUTransferBufferLocation { TransferBuffer = _upload.DangerousGetHandle() },
            new SDL.GPUBufferRegion { Buffer = _sizes!.DangerousGetHandle(), Size = TableBytes }, true);
        SDL.EndGPUCopyPass(copy);
    }

    internal void BindBufferSizes(nint pass)
    {
        if (!_layout.Enabled) return;
        var buffer = _sizes!.DangerousGetHandle();
        SDL.BindGPUComputeStorageBuffers(pass, _layout.ReadOnly, new ReadOnlySpan<nint>(&buffer, 1), 1);
    }

    public void Dispose() { _upload?.Dispose(); _sizes?.Dispose(); Handle.Dispose(); }
}
