using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

/// <summary>A local, owner-thread compute device with owned buffer, shader and uniform-set identities.</summary>
/// <remarks>Create through RenderingServer.CreateLocalRenderingDevice. Compute lists record in order; Submit
/// starts execution and Sync waits. Buffer reads synchronize pending work. Resource identities never cross devices.
/// This compute slice supports storage and uniform buffers; graphics pipelines and texture descriptors remain unavailable.</remarks>
public sealed unsafe partial class RenderingDevice : ElectronObject
{
    /// <summary>Identifies the executable compute stage.</summary>
    public enum ShaderStage
    {
        /// <summary>A general-purpose compute shader.</summary>
        Compute = 4
    }
    /// <summary>Identifies a shader descriptor's resource category.</summary>
    public enum UniformType
    {
        /// <summary>A sampler descriptor.</summary>
        Sampler = 0,
        /// <summary>A combined sampled texture descriptor.</summary>
        SamplerWithTexture = 1,
        /// <summary>A sampled texture descriptor.</summary>
        Texture = 2,
        /// <summary>A writable image descriptor.</summary>
        Image = 3,
        /// <summary>A texture buffer descriptor.</summary>
        TextureBuffer = 4,
        /// <summary>A sampled texture buffer descriptor.</summary>
        SamplerWithTextureBuffer = 5,
        /// <summary>A writable image buffer descriptor.</summary>
        ImageBuffer = 6,
        /// <summary>A constant uniform buffer.</summary>
        UniformBuffer = 7,
        /// <summary>A read/write storage buffer.</summary>
        StorageBuffer = 8,
        /// <summary>A framebuffer input attachment.</summary>
        InputAttachment = 9,
        /// <summary>A uniform buffer with dynamic offset.</summary>
        UniformBufferDynamic = 10,
        /// <summary>A storage buffer with dynamic offset.</summary>
        StorageBufferDynamic = 11
    }
    private enum Kind { Storage, Uniform, Shader, Pipeline, Set }
    private sealed class Entry
    {
        internal Kind Kind;
        internal RenderHandle? Handle, Upload, Download;
        internal byte[] Data = [];
        internal uint Size;
        internal RID Shader;
        internal string Name = "";
        internal uint Set;
        internal Binding[] Layout = [];
        internal Dictionary<int, RID> Bindings = [];
    }
    private readonly GPUPhysicsDevice _context = new();
    private readonly Dictionary<RID, Entry> _entries = [];
    private readonly Dictionary<uint, Entry> _sets = [];
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private Entry? _pipeline;
    private nint _command, _fence;
    private long _activeList, _nextList;
    private nint Device => _context.Device;
    internal RenderingDevice() { }
    private void Access() { ThrowIfDisposed(); if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("A local device belongs to its creating thread."); }
    private Entry Get(RID rid) { Access(); return _entries.TryGetValue(rid, out var entry) ? entry : throw new ArgumentException("The RID is not a live resource of this device.", nameof(rid)); }
    private Entry Get(RID rid, Kind kind) { var entry = Get(rid); return entry.Kind == kind ? entry : throw new ArgumentException("The RID has the wrong resource kind.", nameof(rid)); }
    private RID Add(Entry entry) { var rid = RID.Allocate(); _entries.Add(rid, entry); return rid; }
    private void IdleList() { Access(); if (_activeList != 0) throw new InvalidOperationException("End the active compute list first."); }
    private void List(long id) { Access(); if (_activeList == 0 || id != _activeList) throw new ArgumentException("The compute list is not active.", nameof(id)); }
    private nint Commands()
    {
        if (_fence != 0) Sync();
        if (_command == 0) { _command = SDL.AcquireGPUCommandBuffer(Device); if (_command == 0) throw GPUPhysicsDevice.Failure("acquire compute commands"); }
        return _command;
    }
    private RenderHandle Transfer(uint size, SDL.GPUTransferBufferUsage usage) => new(SDL.CreateGPUTransferBuffer(Device, new SDL.GPUTransferBufferCreateInfo { Size = size, Usage = usage }), h => SDL.ReleaseGPUTransferBuffer(Device, h), _context.Handle);

    /// <summary>Creates a zero-initialized read/write storage buffer.</summary>
    /// <param name="sizeBytes">Positive byte capacity, divisible by four.</param>
    /// <param name="data">Optional initial prefix; remaining bytes are zero.</param>
    /// <returns>A device-owned buffer identity.</returns>
    public RID StorageBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data = default)
    {
        IdleList(); if (sizeBytes == 0 || sizeBytes % 4 != 0 || sizeBytes > int.MaxValue || data.Length > sizeBytes) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        var entry = new Entry { Kind = Kind.Storage, Size = sizeBytes };
        try
        {
            entry.Handle = new(SDL.CreateGPUBuffer(Device, new SDL.GPUBufferCreateInfo { Size = sizeBytes, Usage = SDL.GPUBufferUsageFlags.ComputeStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite }), h => SDL.ReleaseGPUBuffer(Device, h), _context.Handle);
            entry.Upload = Transfer(sizeBytes, SDL.GPUTransferBufferUsage.Upload);
            var initial = new byte[sizeBytes]; data.CopyTo(initial); Upload(entry, 0, initial);
            return Add(entry);
        }
        catch { entry.Handle?.Dispose(); entry.Upload?.Dispose(); throw; }
    }

    /// <summary>Creates a constant uniform buffer.</summary>
    /// <param name="sizeBytes">Positive capacity divisible by four, at most 16 KiB.</param>
    /// <param name="data">Optional initial prefix; remaining bytes are zero.</param>
    /// <returns>A device-owned buffer identity.</returns>
    public RID UniformBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data = default)
    {
        Access(); if (sizeBytes == 0 || sizeBytes % 4 != 0 || sizeBytes > 16384 || data.Length > sizeBytes) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        var entry = new Entry { Kind = Kind.Uniform, Size = sizeBytes, Data = new byte[sizeBytes] }; data.CopyTo(entry.Data); return Add(entry);
    }

    /// <summary>Updates a validated byte range of a buffer.</summary>
    /// <param name="buffer">A live storage or uniform buffer.</param>
    /// <param name="offset">First byte to replace.</param>
    /// <param name="sizeBytes">Number of bytes copied from data.</param>
    /// <param name="data">At least sizeBytes source bytes; copied before returning.</param>
    public void BufferUpdate(RID buffer, uint offset, uint sizeBytes, ReadOnlySpan<byte> data)
    {
        var entry = Get(buffer); Range(entry, offset, sizeBytes);
        if (sizeBytes > data.Length) throw new ArgumentException("Source data is too short.", nameof(data));
        var source = data[..(int)sizeBytes];
        if (entry.Kind == Kind.Uniform) source.CopyTo(entry.Data.AsSpan((int)offset));
        else { IdleList(); Upload(entry, offset, source); }
    }
    private static void Range(Entry entry, uint offset, uint size)
    {
        if (entry.Kind is not (Kind.Storage or Kind.Uniform)) throw new ArgumentException("The RID is not a buffer.");
        if (offset > entry.Size || size > entry.Size - offset) throw new ArgumentOutOfRangeException(nameof(offset));
    }
    private void Upload(Entry entry, uint offset, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return;
        var command = Commands();
        var pointer = SDL.MapGPUTransferBuffer(Device, entry.Upload!.DangerousGetHandle(), true);
        if (pointer == 0) throw GPUPhysicsDevice.Failure("map upload storage");
        try { data.CopyTo(new Span<byte>((void*)pointer, data.Length)); }
        finally { SDL.UnmapGPUTransferBuffer(Device, entry.Upload.DangerousGetHandle()); }
        var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw GPUPhysicsDevice.Failure("begin buffer upload");
        SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = entry.Upload.DangerousGetHandle() }, new() { Buffer = entry.Handle!.DangerousGetHandle(), Offset = offset, Size = (uint)data.Length }, false);
        SDL.EndGPUCopyPass(copy);
    }

    /// <summary>Synchronizes and copies a buffer range into an independent array.</summary>
    /// <param name="buffer">A live buffer.</param>
    /// <param name="offsetBytes">First byte to read.</param>
    /// <param name="sizeBytes">Bytes to read, or zero for the remaining buffer.</param>
    /// <returns>An independent array containing the completed device data.</returns>
    public byte[] BufferGetData(RID buffer, uint offsetBytes = 0, uint sizeBytes = 0)
    {
        var entry = Get(buffer); Range(entry, offsetBytes, sizeBytes);
        var output = new byte[sizeBytes == 0 ? entry.Size - offsetBytes : sizeBytes]; BufferGetData(buffer, output.AsSpan(), offsetBytes); return output;
    }
    /// <summary>Synchronizes and copies a buffer range into caller-provided storage.</summary>
    /// <param name="buffer">A live buffer.</param>
    /// <param name="destination">Determines the number of bytes to read.</param>
    /// <param name="offsetBytes">First byte to read.</param>
    public void BufferGetData(RID buffer, Span<byte> destination, uint offsetBytes = 0)
    {
        IdleList(); var entry = Get(buffer); Range(entry, offsetBytes, (uint)destination.Length);
        if (entry.Kind == Kind.Uniform) { entry.Data.AsSpan((int)offsetBytes, destination.Length).CopyTo(destination); return; }
        if (destination.IsEmpty) return;
        entry.Download ??= Transfer(entry.Size, SDL.GPUTransferBufferUsage.Download);
        var copy = SDL.BeginGPUCopyPass(Commands()); if (copy == 0) throw GPUPhysicsDevice.Failure("begin buffer read");
        SDL.DownloadFromGPUBuffer(copy, new() { Buffer = entry.Handle!.DangerousGetHandle(), Offset = offsetBytes, Size = (uint)destination.Length }, new() { TransferBuffer = entry.Download.DangerousGetHandle() });
        SDL.EndGPUCopyPass(copy); Submit(); Sync();
        var pointer = SDL.MapGPUTransferBuffer(Device, entry.Download.DangerousGetHandle(), false);
        if (pointer == 0) throw GPUPhysicsDevice.Failure("map buffer read");
        try { new ReadOnlySpan<byte>((void*)pointer, destination.Length).CopyTo(destination); }
        finally { SDL.UnmapGPUTransferBuffer(Device, entry.Download.DangerousGetHandle()); }
    }

    /// <summary>Validates and creates a compute shader from compiled bytecode.</summary>
    /// <param name="spirvData">The copied compute stage, with no compiler error.</param>
    /// <param name="name">Optional diagnostic label.</param>
    /// <returns>A shader identity owned by this device.</returns>
    public RID ShaderCreateFromSPIRV(RDShaderSPIRV spirvData, string name = "")
    {
        IdleList(); ArgumentNullException.ThrowIfNull(spirvData); ArgumentNullException.ThrowIfNull(name);
        if (spirvData.CompileErrorCompute.Length != 0) throw new ArgumentException(spirvData.CompileErrorCompute, nameof(spirvData));
        var (code, layout) = Remap(spirvData.BytecodeCompute);
        var handle = new RenderHandle(ShaderCompiler.CreateComputePipeline(Device, code), h => SDL.ReleaseGPUComputePipeline(Device, h), _context.Handle);
        return Add(new Entry { Kind = Kind.Shader, Handle = handle, Layout = layout, Name = name });
    }
    /// <summary>Creates an executable pipeline borrowing a live compute shader.</summary>
    /// <param name="shader">A shader on this device.</param>
    /// <returns>A pipeline identity invalidated when its shader is freed.</returns>
    public RID ComputePipelineCreate(RID shader) { Get(shader, Kind.Shader); return Add(new Entry { Kind = Kind.Pipeline, Shader = shader }); }
    /// <summary>Reports whether a pipeline and its shader remain alive.</summary>
    /// <param name="pipeline">The identity to inspect.</param>
    /// <returns>True for a live executable pipeline of this device.</returns>
    public bool ComputePipelineIsValid(RID pipeline) { Access(); return _entries.TryGetValue(pipeline, out var e) && e.Kind == Kind.Pipeline && _entries.ContainsKey(e.Shader); }

    /// <summary>Creates a copied set of validated buffer bindings.</summary>
    /// <param name="uniforms">Descriptors with one buffer identity per binding.</param>
    /// <param name="shader">The shader whose layout owns this set.</param>
    /// <param name="shaderSet">The shader descriptor-set number.</param>
    /// <returns>A set identity borrowing its shader and buffers.</returns>
    public RID UniformSetCreate(ReadOnlySpan<RDUniform> uniforms, RID shader, uint shaderSet)
    {
        var program = Get(shader, Kind.Shader); var entry = new Entry { Kind = Kind.Set, Shader = shader, Set = shaderSet };
        foreach (var uniform in uniforms)
        {
            ArgumentNullException.ThrowIfNull(uniform); var ids = uniform.GetIDs();
            if (uniform.Binding < 0 || ids.Length != 1) throw new ArgumentException("Each supported binding needs one buffer.", nameof(uniforms));
            var descriptor = program.Layout.SingleOrDefault(b => b.Set == shaderSet && b.Slot == uniform.Binding);
            if (descriptor == default || descriptor.Type != uniform.UniformType) throw new ArgumentException("Descriptor does not match the shader.", nameof(uniforms));
            Get(ids[0], uniform.UniformType == UniformType.UniformBuffer ? Kind.Uniform : Kind.Storage);
            if (!entry.Bindings.TryAdd(uniform.Binding, ids[0])) throw new ArgumentException("Duplicate descriptor binding.", nameof(uniforms));
        }
        if (program.Layout.Count(b => b.Set == shaderSet) != entry.Bindings.Count) throw new ArgumentException("The uniform set is incomplete.", nameof(uniforms));
        return Add(entry);
    }
    /// <summary>Reports whether a set and every borrowed resource remain alive.</summary>
    /// <param name="uniformSet">The identity to inspect.</param>
    /// <returns>True for an intact set of this device.</returns>
    public bool UniformSetIsValid(RID uniformSet)
    {
        Access();
        if (!_entries.TryGetValue(uniformSet, out var e) || e.Kind != Kind.Set || !_entries.ContainsKey(e.Shader)) return false;
        foreach (var rid in e.Bindings.Values) if (!_entries.ContainsKey(rid)) return false;
        return true;
    }
    /// <summary>Begins an owner-thread compute list.</summary>
    /// <returns>The active list identity; only one list may be active.</returns>
    public long ComputeListBegin() { IdleList(); Commands(); _pipeline = null; _sets.Clear(); return _activeList = ++_nextList; }
    /// <summary>Selects a compute pipeline for subsequent dispatches.</summary>
    /// <param name="computeList">The active list.</param>
    /// <param name="computePipeline">A live executable pipeline.</param>
    public void ComputeListBindComputePipeline(long computeList, RID computePipeline) { List(computeList); _pipeline = Get(Get(computePipeline, Kind.Pipeline).Shader, Kind.Shader); }
    /// <summary>Binds a resource set for subsequent dispatches.</summary>
    /// <param name="computeList">The active list.</param>
    /// <param name="uniformSet">A complete live uniform set.</param>
    /// <param name="setIndex">Its descriptor-set index.</param>
    public void ComputeListBindUniformSet(long computeList, RID uniformSet, uint setIndex)
    {
        List(computeList); var set = Get(uniformSet, Kind.Set);
        if (set.Set != setIndex || !UniformSetIsValid(uniformSet)) throw new ArgumentException("Invalid set or set index.", nameof(uniformSet));
        _sets[setIndex] = set;
    }
    /// <summary>Records a compute dispatch with barriers against previous dispatches.</summary>
    /// <param name="computeList">The active list.</param>
    /// <param name="xGroups">Positive X group count, at most 65535.</param>
    /// <param name="yGroups">Positive Y group count, at most 65535.</param>
    /// <param name="zGroups">Positive Z group count, at most 65535.</param>
    public void ComputeListDispatch(long computeList, uint xGroups, uint yGroups, uint zGroups)
    {
        List(computeList);
        if (xGroups is 0 or > 65535 || yGroups is 0 or > 65535 || zGroups is 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(xGroups));
        if (_pipeline is null) throw new InvalidOperationException("Bind a compute pipeline first.");
        var buffers = stackalloc SDL.GPUStorageBufferReadWriteBinding[8]; var count = 0;
        foreach (var descriptor in _pipeline.Layout)
        {
            if (!_sets.TryGetValue(descriptor.Set, out var set) || !ReferenceEquals(Get(set.Shader), _pipeline)) throw new InvalidOperationException("Bind each pipeline resource set first.");
            var buffer = Get(set.Bindings[descriptor.Slot]);
            if (descriptor.Type == UniformType.StorageBuffer) { buffers[descriptor.Native] = new() { Buffer = buffer.Handle!.DangerousGetHandle() }; count++; }
            else fixed (byte* data = buffer.Data) SDL.PushGPUComputeUniformData(_command, (uint)descriptor.Native, (nint)data, buffer.Size);
        }
        var pass = SDL.BeginGPUComputePass(_command, 0, 0, (nint)buffers, (uint)count);
        if (pass == 0) throw GPUPhysicsDevice.Failure("begin compute dispatch for " + _pipeline.Name);
        SDL.BindGPUComputePipeline(pass, _pipeline.Handle!.DangerousGetHandle());
        SDL.DispatchGPUCompute(pass, xGroups, yGroups, zGroups); SDL.EndGPUComputePass(pass);
    }
    /// <summary>Establishes a full dependency after prior dispatches in this list.</summary>
    /// <param name="computeList">The active list.</param>
    /// <remarks>Every dispatch already closes its native compute pass, providing this dependency.</remarks>
    public void ComputeListAddBarrier(long computeList) => List(computeList);
    /// <summary>Ends the active list without submitting it.</summary>
    public void ComputeListEnd() { List(_activeList); _activeList = 0; _pipeline = null; _sets.Clear(); }
    /// <summary>Submits all recorded uploads and compute lists.</summary>
    public void Submit()
    {
        IdleList(); if (_fence != 0) throw new InvalidOperationException("Synchronize the previous submission first.");
        if (_command == 0) return;
        var command = _command; _command = 0; _fence = SDL.SubmitGPUCommandBufferAndAcquireFence(command);
        if (_fence == 0) throw GPUPhysicsDevice.Failure("submit compute commands");
    }
    /// <summary>Waits for the submitted work and releases its fence.</summary>
    public void Sync()
    {
        IdleList(); if (_fence == 0) return;
        var fence = _fence;
        if (!SDL.WaitForGPUFences(Device, true, (nint)(&fence), 1)) throw GPUPhysicsDevice.Failure("wait for compute commands");
        SDL.ReleaseGPUFence(Device, _fence); _fence = 0;
    }
    /// <summary>Returns the selected compute-device name.</summary>
    /// <returns>The native device description.</returns>
    public string GetDeviceName() { Access(); return _context.DeviceName; }
    /// <summary>Frees a resource after completing pending work.</summary>
    /// <param name="rid">A live identity owned by this device.</param>
    /// <remarks>Borrowers become invalid; they may still be freed explicitly.</remarks>
    public void FreeRID(RID rid)
    {
        IdleList(); var entry = Get(rid); if (_fence != 0) Sync(); if (_command != 0) { Submit(); Sync(); }
        _entries.Remove(rid); entry.Handle?.Dispose(); entry.Upload?.Dispose(); entry.Download?.Dispose();
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { Access(); if (_activeList != 0) throw new InvalidOperationException("End the compute list before disposing the device."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_command != 0) { SDL.CancelGPUCommandBuffer(_command); _command = 0; }
            if (_fence != 0) { var fence = _fence; SDL.WaitForGPUFences(Device, true, (nint)(&fence), 1); SDL.ReleaseGPUFence(Device, _fence); _fence = 0; }
            foreach (var e in _entries.Values) { e.Handle?.Dispose(); e.Upload?.Dispose(); e.Download?.Dispose(); }
            _entries.Clear(); _context.Dispose();
        }
        base.Dispose(disposing);
    }
}
