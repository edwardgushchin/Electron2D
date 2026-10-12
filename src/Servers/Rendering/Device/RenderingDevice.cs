using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

/// <summary>An owner-thread graphics and compute device with owned resource identities.</summary>
/// <remarks>Create a local device through RenderingServer.CreateLocalRenderingDevice or borrow the retained renderer device
/// through RenderingServer.GetRenderingDevice. Local Submit starts execution and Sync waits; the renderer device
/// flushes before canvas submission and completes queued reads before FramePostDraw. Buffer reads synchronize pending work. Resource identities never cross devices.
/// The device executes compute and color/depth graphics pipelines with exact multisample state and color resolve, with sampled images, storage resources, borrowed vertex/index arrays and framebuffer attachments.</remarks>
public sealed unsafe partial class RenderingDevice : ElectronObject
{
    /// <summary>Identifies executable shader stages.</summary>
    public enum ShaderStage
    {
        /// <summary>A vertex processing stage.</summary>
        Vertex = 0,
        /// <summary>A fragment processing stage.</summary>
        Fragment = 1,
        /// <summary>A general-purpose compute shader.</summary>
        Compute = 4,
        /// <summary>Marks the exclusive stage numeric extent; it is not an executable stage.</summary>
        Max = 10
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
    private enum Kind { Storage, Uniform, Shader, Pipeline, Set, Texture, Sampler, Vertex, Index, VertexArray, IndexArray, Framebuffer, GraphicsPipeline }
    private sealed class Entry
    {
        internal Kind Kind;
        internal RenderHandle? Handle, Upload, Download, TextureScratch;
        internal bool ScratchColorTarget;
        internal byte[] Data = [];
        internal uint Size;
        internal StorageBufferUsage Usage;
        internal TextureInfo? Texture;
        internal bool CompareSampling, LinearSampling, GraphicsStorage, RestartIndices;
        internal IndexBufferFormat IndexFormat;
        internal VertexArray? Vertices;
        internal IndexArray? Indices;
        internal Framebuffer? Target;
        internal GraphicsPipeline? Graphics;
        internal ShaderCompiler.DeviceIO[] Inputs = [], Outputs = [];
        internal RID Shader;
        internal string Name = "";
        internal ShaderStage Stage = ShaderStage.Compute;
        internal RenderHandle? FragmentHandle;
        internal uint Set;
        internal Binding[] Layout = [];
        internal PushStage[] Push = [];
        internal byte[][] Sources = [];
        internal Entry? Variant;
        internal Dictionary<int, RID[]> Bindings = [];
    }
    private readonly GPUPhysicsDevice _context = new();
    private readonly Dictionary<RID, Entry> _entries = [];
    private readonly Dictionary<uint, Entry> _sets = [];
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private Entry? _pipeline;
    private nint _command, _fence;
    private nint _drawPass;
    private long _activeList, _nextList;
    private nint Device => _context.Device;
    private readonly RenderingServer? _serverOwner;
    internal RenderingDevice(RenderingServer? serverOwner = null) { _serverOwner = serverOwner; }
    private void Access(bool canvasMetadata = false) { ThrowIfDisposed(); if (!canvasMetadata) _serverOwner?.EnsureDeviceAccess(); if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("A rendering device belongs to its creating thread."); if (_accessingReadbackMemory) throw new InvalidOperationException("A readback memory accessor cannot reenter its device."); }
    private Entry Get(RID rid) { Access(); return _entries.TryGetValue(rid, out var entry) ? entry : throw new ArgumentException("The RID is not a live resource of this device.", nameof(rid)); }
    private Entry Get(RID rid, Kind kind) { var entry = Get(rid); return entry.Kind == kind ? entry : throw new ArgumentException("The RID has the wrong resource kind.", nameof(rid)); }
    private RID Add(Entry entry) { var rid = RID.Allocate(); _entries.Add(rid, entry); return rid; }
    private void IdleList() { Access(); if (_activeList != 0) throw new InvalidOperationException("End the active command list first."); }
    private void List(long id) { Access(); if (_drawPass != 0 || _activeList == 0 || id != _activeList) throw new ArgumentException("The compute list is not active.", nameof(id)); }
    private nint Commands()
    {
        if (_fence != 0 || _submittedReadbacks.Count != 0) CompleteSubmission();
        if (_command == 0) { _command = SDL.AcquireGPUCommandBuffer(Device); if (_command == 0) throw GPUPhysicsDevice.Failure("acquire compute commands"); }
        return _command;
    }
    private RenderHandle Transfer(uint size, SDL.GPUTransferBufferUsage usage) => new(SDL.CreateGPUTransferBuffer(Device, new SDL.GPUTransferBufferCreateInfo { Size = size, Usage = usage }), h => SDL.ReleaseGPUTransferBuffer(Device, h), _context.Handle);

    /// <summary>Creates a zero-initialized read/write storage buffer.</summary>
    /// <param name="sizeBytes">Positive byte capacity, divisible by four.</param>
    /// <param name="data">Optional initial prefix; remaining bytes are zero.</param>
    /// <param name="usage">Optional indirect-dispatch role.</param>
    /// <returns>A device-owned buffer identity.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The size, data length or usage flags are invalid.</exception>
    public RID StorageBufferCreate(uint sizeBytes, ReadOnlySpan<byte> data = default, StorageBufferUsage usage = 0)
    {
        IdleList(); if (sizeBytes == 0 || sizeBytes % 4 != 0 || sizeBytes > int.MaxValue || data.Length > sizeBytes) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        if ((usage & ~StorageBufferUsage.DispatchIndirect) != 0) throw new ArgumentOutOfRangeException(nameof(usage));
        var entry = new Entry { Kind = Kind.Storage, Size = sizeBytes, Usage = usage };
        try
        {
            entry.Handle = new(SDL.CreateGPUBuffer(Device, new SDL.GPUBufferCreateInfo { Size = sizeBytes, Usage = SDL.GPUBufferUsageFlags.ComputeStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite | SDL.GPUBufferUsageFlags.GraphicsStorageRead | (usage.HasFlag(StorageBufferUsage.DispatchIndirect) ? SDL.GPUBufferUsageFlags.Indirect : 0) }), h => SDL.ReleaseGPUBuffer(Device, h), _context.Handle);
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
        if (entry.Kind is not (Kind.Storage or Kind.Uniform or Kind.Vertex or Kind.Index)) throw new ArgumentException("The RID is not a buffer.");
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
    /// <remarks>A nonempty storage read delivers completed asynchronous callbacks after copying its result.</remarks>
    /// <exception cref="AggregateException">One or more completed readback callbacks failed.</exception>
    public byte[] BufferGetData(RID buffer, uint offsetBytes = 0, uint sizeBytes = 0)
    {
        var entry = Get(buffer); Range(entry, offsetBytes, sizeBytes);
        var output = new byte[sizeBytes == 0 ? entry.Size - offsetBytes : sizeBytes]; BufferGetData(buffer, output.AsSpan(), offsetBytes); return output;
    }
    /// <summary>Synchronizes and copies a buffer range into caller-provided storage.</summary>
    /// <param name="buffer">A live buffer.</param>
    /// <param name="destination">Determines the number of bytes to read.</param>
    /// <param name="offsetBytes">First byte to read.</param>
    /// <remarks>A nonempty storage read delivers completed asynchronous callbacks after copying its result.</remarks>
    /// <exception cref="AggregateException">One or more completed readback callbacks failed.</exception>
    public void BufferGetData(RID buffer, Span<byte> destination, uint offsetBytes = 0)
    {
        IdleList(); var entry = Get(buffer); Range(entry, offsetBytes, (uint)destination.Length);
        if (entry.Kind == Kind.Uniform) { entry.Data.AsSpan((int)offsetBytes, destination.Length).CopyTo(destination); return; }
        if (destination.IsEmpty) return;
        entry.Download ??= Transfer(entry.Size, SDL.GPUTransferBufferUsage.Download);
        var copy = SDL.BeginGPUCopyPass(Commands()); if (copy == 0) throw GPUPhysicsDevice.Failure("begin buffer read");
        SDL.DownloadFromGPUBuffer(copy, new() { Buffer = entry.Handle!.DangerousGetHandle(), Offset = offsetBytes, Size = (uint)destination.Length }, new() { TransferBuffer = entry.Download.DangerousGetHandle() });
        SDL.EndGPUCopyPass(copy); SubmitRecorded(); CompleteSubmission();
        var pointer = SDL.MapGPUTransferBuffer(Device, entry.Download.DangerousGetHandle(), false);
        if (pointer == 0) throw GPUPhysicsDevice.Failure("map buffer read");
        try { new ReadOnlySpan<byte>((void*)pointer, destination.Length).CopyTo(destination); }
        finally { SDL.UnmapGPUTransferBuffer(Device, entry.Download.DangerousGetHandle()); }
        DeliverReadbacks();
    }

    /// <summary>Validates and creates a compute shader or a paired graphics shader from compiled bytecode.</summary>
    /// <param name="spirvData">Copied stage modules with no compiler errors; either compute or both vertex and fragment.</param>
    /// <param name="name">Optional diagnostic label.</param>
    /// <returns>A shader identity owned by this device.</returns>
    /// <remarks>Specialization modules retain copied symbolic templates. Pipeline creation applies typed values before native/layout validation; ordinary shaders retain their validated native program.</remarks>
    public RID ShaderCreateFromSPIRV(RDShaderSPIRV spirvData, string name = "")
    {
        IdleList(); ArgumentNullException.ThrowIfNull(spirvData); ArgumentNullException.ThrowIfNull(name);
        var compute = spirvData.BytecodeCompute; var vertex = spirvData.BytecodeVertex; var fragment = spirvData.BytecodeFragment;
        if (compute.Length != 0 && (vertex.Length != 0 || fragment.Length != 0)) throw new ArgumentException("Compute and graphics modules cannot share a shader identity.", nameof(spirvData));
        if (compute.Length != 0 ? spirvData.CompileErrorCompute.Length != 0 : vertex.Length == 0 || fragment.Length == 0 || spirvData.CompileErrorVertex.Length != 0 || spirvData.CompileErrorFragment.Length != 0) throw new ArgumentException("Supply successful compute or paired graphics modules.", nameof(spirvData));
        byte[][] sources = compute.Length != 0 ? [compute] : [vertex, fragment];
        var symbolic = sources.Any(code => new SpecializationModule(code, [], []).HasSpecialization);
        var program = CreateDeviceProgram(sources, name, [], symbolic); program.Sources = sources; return Add(program);
    }
    private Entry CreateDeviceProgram(byte[][] sources, string name, Dictionary<uint, Specialization> constants, bool template = false)
    {
        var used = new HashSet<uint>(); var modules = sources.Select(code => template ? code.ToArray() : new SpecializationModule(code, constants, used).Freeze()).ToArray();
        if (!template && constants.Keys.Any(id => !used.Contains(id))) throw new ArgumentException("A specialization identifier is not declared by this shader.", nameof(constants));
        var layouts = new Binding[modules.Length][]; var pushes = new PushStage[modules.Length][];
        for (var i = 0; i < modules.Length; i++)
        { var stage = modules.Length == 1 ? ShaderStage.Compute : i == 0 ? ShaderStage.Vertex : ShaderStage.Fragment; (modules[i], layouts[i], pushes[i]) = RemapCore(modules[i], stage, template); }
        var layout = layouts.SelectMany(l => l).ToArray();
        foreach (var group in layout.GroupBy(b => (b.Set, b.Slot)))
        { var first = group.First(); if (group.Any(b => b.Type != first.Type || b.Array != first.Array || b.ImageFormat != first.ImageFormat || b.PixelType != first.PixelType || b.Depth != first.Depth)) throw new ArgumentException("A shared descriptor has inconsistent stage types."); }
        var interfaces = modules.Length == 2 ? ShaderCompiler.ValidateDeviceGraphics(modules[0], modules[1]) : default;
        var entry = new Entry { Kind = Kind.Shader, Stage = modules.Length == 1 ? ShaderStage.Compute : ShaderStage.Vertex, Layout = layout, Push = pushes.SelectMany(p => p).ToArray(), Name = name, Inputs = interfaces.Inputs ?? [], Outputs = interfaces.Outputs ?? [] };
        if (template) return entry;
        try
        {
            if (modules.Length == 1) entry.Handle = new(ShaderCompiler.CreateComputePipeline(Device, modules[0]), h => SDL.ReleaseGPUComputePipeline(Device, h), _context.Handle);
            else { entry.Handle = new(ShaderCompiler.CreateShader(Device, modules[0], false), h => SDL.ReleaseGPUShader(Device, h), _context.Handle); entry.FragmentHandle = new(ShaderCompiler.CreateShader(Device, modules[1], true), h => SDL.ReleaseGPUShader(Device, h), _context.Handle); }
            return entry;
        }
        catch { ReleaseProgram(entry); throw; }
    }
    private static void ReleaseProgram(Entry? program) { program?.FragmentHandle?.Dispose(); program?.Handle?.Dispose(); }
    private Entry PipelineProgram(Entry pipeline) { var source = Get(pipeline.Shader, Kind.Shader); return pipeline.Variant ?? source; }
    private Entry? PipelineVariant(Entry source, ReadOnlySpan<RDPipelineSpecializationConstant> constants)
    {
        var values = CaptureSpecialization(constants); return values.Count == 0 && source.Handle is not null ? null : CreateDeviceProgram(source.Sources, source.Name, values);
    }
    /// <summary>Creates an executable compute pipeline with copied typed specialization values.</summary>
    /// <param name="shader">A live compute shader on this device.</param>
    /// <param name="specializationConstants">Distinct declared scalar identifiers and typed values; omitted values keep module defaults.</param>
    /// <returns>A pipeline borrowing the shader identity and owning any specialized native program.</returns>
    /// <exception cref="ArgumentException">An identifier/type is invalid or the resulting module/layout is invalid.</exception>
    /// <exception cref="NotSupportedException">A specialized shader capability exceeds the native profile.</exception>
    public RID ComputePipelineCreate(RID shader, ReadOnlySpan<RDPipelineSpecializationConstant> specializationConstants = default)
    {
        IdleList(); var source = Get(shader, Kind.Shader); if (source.Stage != ShaderStage.Compute) throw new ArgumentException("A compute shader is required.", nameof(shader));
        var variant = PipelineVariant(source, specializationConstants);
        try { return Add(new Entry { Kind = Kind.Pipeline, Shader = shader, Variant = variant }); }
        catch { ReleaseProgram(variant); throw; }
    }
    /// <summary>Reports whether a pipeline and its shader remain alive.</summary>
    /// <param name="pipeline">The identity to inspect.</param>
    /// <returns>True for a live executable pipeline of this device.</returns>
    public bool ComputePipelineIsValid(RID pipeline) { Access(); return _entries.TryGetValue(pipeline, out var e) && e.Kind == Kind.Pipeline && _entries.ContainsKey(e.Shader); }

    /// <summary>Creates a copied set of validated buffer, sampled-image and writable-image bindings.</summary>
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
            if (uniform.Binding < 0 || ids.Length == 0) throw new ArgumentException("Each supported binding needs one buffer.", nameof(uniforms));
            var descriptor = program.Layout.FirstOrDefault(b => b.Set == shaderSet && b.Slot == uniform.Binding);
            if (descriptor == default || descriptor.Type != uniform.UniformType) throw new ArgumentException("Descriptor does not match the shader.", nameof(uniforms));
            foreach (var stageBinding in program.Layout) if (stageBinding.Set == shaderSet && stageBinding.Slot == uniform.Binding) ValidateBinding(stageBinding, ids);
            if (!entry.Bindings.TryAdd(uniform.Binding, ids)) throw new ArgumentException("Duplicate descriptor binding.", nameof(uniforms));
        }
        if (program.Layout.Where(b => b.Set == shaderSet).Select(b => b.Slot).Distinct().Count() != entry.Bindings.Count) throw new ArgumentException("The uniform set is incomplete.", nameof(uniforms));
        return Add(entry);
    }
    /// <summary>Reports whether a set and every borrowed resource remain alive.</summary>
    /// <param name="uniformSet">The identity to inspect.</param>
    /// <returns>True for an intact set of this device.</returns>
    public bool UniformSetIsValid(RID uniformSet)
    {
        Access();
        if (!_entries.TryGetValue(uniformSet, out var e) || e.Kind != Kind.Set || !_entries.ContainsKey(e.Shader)) return false;
        foreach (var ids in e.Bindings.Values) foreach (var rid in ids) if (!_entries.ContainsKey(rid)) return false;
        return true;
    }
    /// <summary>Begins an owner-thread compute list.</summary>
    /// <returns>The active list identity; only one list may be active.</returns>
    public long ComputeListBegin() { IdleList(); Commands(); _pipeline = null; _sets.Clear(); ResetPush(); return _activeList = ++_nextList; }
    /// <summary>Selects a compute pipeline for subsequent dispatches.</summary>
    /// <param name="computeList">The active list.</param>
    /// <param name="computePipeline">A live executable pipeline.</param>
    public void ComputeListBindComputePipeline(long computeList, RID computePipeline) { List(computeList); _pipeline = PipelineProgram(Get(computePipeline, Kind.Pipeline)); SelectPushPipeline(computePipeline); }
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
        Dispatch(xGroups, yGroups, zGroups);
    }
    private void Dispatch(uint xGroups, uint yGroups, uint zGroups, Entry? indirect = null, uint offset = 0)
    {
        if (_pipeline is null) throw new InvalidOperationException("Bind a compute pipeline first.");
        RequirePush(_pipeline);
        var buffers = stackalloc SDL.GPUStorageBufferReadWriteBinding[8]; var count = 0;
        var images = stackalloc SDL.GPUStorageTextureReadWriteBinding[8]; var imageCount = 0;
        var samplers = stackalloc SDL.GPUTextureSamplerBinding[16]; var samplerCount = 0;
        foreach (var descriptor in _pipeline.Layout)
        {
            if (!_sets.TryGetValue(descriptor.Set, out var set) || set.Shader != Get(_pushPipeline).Shader) throw new InvalidOperationException("Bind each pipeline resource set first.");
            var ids = set.Bindings[descriptor.Slot];
            ValidateBinding(descriptor, ids); var buffer = Get(ids[0]);
            if (descriptor.Type == UniformType.Image) { images[descriptor.Native] = new() { Texture = buffer.Handle!.DangerousGetHandle() }; imageCount++; }
            else if (descriptor.Type == UniformType.SamplerWithTexture) { samplers[descriptor.Native] = new() { Sampler = buffer.Handle!.DangerousGetHandle(), Texture = Get(ids[1]).Handle!.DangerousGetHandle() }; samplerCount++; }
            else if (descriptor.Type == UniformType.StorageBuffer) { buffers[descriptor.Native] = new() { Buffer = buffer.Handle!.DangerousGetHandle() }; count++; }
            else fixed (byte* data = buffer.Data) SDL.PushGPUComputeUniformData(_command, (uint)descriptor.Native, (nint)data, buffer.Size);
        }
        PushUniforms(_pipeline);
        var pass = SDL.BeginGPUComputePass(_command, (nint)images, (uint)imageCount, (nint)buffers, (uint)count);
        if (pass == 0) throw GPUPhysicsDevice.Failure("begin compute dispatch for " + _pipeline.Name);
        SDL.BindGPUComputePipeline(pass, _pipeline.Handle!.DangerousGetHandle());
        if (samplerCount != 0) SDL.BindGPUComputeSamplers(pass, 0, (nint)samplers, (uint)samplerCount);
        if (indirect is null) SDL.DispatchGPUCompute(pass, xGroups, yGroups, zGroups);
        else SDL.DispatchGPUComputeIndirect(pass, indirect.Handle!.DangerousGetHandle(), offset);
        SDL.EndGPUComputePass(pass);
    }
    /// <summary>Establishes a full dependency after prior dispatches in this list.</summary>
    /// <param name="computeList">The active list.</param>
    /// <remarks>Every dispatch already closes its native compute pass, providing this dependency.</remarks>
    public void ComputeListAddBarrier(long computeList) => List(computeList);
    /// <summary>Ends the active list without submitting it.</summary>
    public void ComputeListEnd() { List(_activeList); _activeList = 0; _pipeline = null; _sets.Clear(); ResetPush(); }
    /// <summary>Submits all recorded uploads, compute lists and readback requests.</summary>
    /// <remarks>Local devices only. Host-only and empty readback batches also require Sync before another explicit Submit.</remarks>
    /// <exception cref="InvalidOperationException">The device belongs to the renderer or a previous submission is pending.</exception>
    public void Submit()
    {
        if (_serverOwner is not null) throw new InvalidOperationException("The renderer submits its device; explicit submission belongs to local devices.");
        SubmitRecorded();
    }
    private void SubmitRecorded()
    {
        IdleList(); if (_fence != 0 || _submittedReadbacks.Count != 0) throw new InvalidOperationException("Synchronize the previous submission first.");
        if (_command != 0)
        {
            var command = _command; _command = 0; _fence = SDL.SubmitGPUCommandBufferAndAcquireFence(command);
            if (_fence == 0)
            {
                while (_recordedReadbacks.TryDequeue(out var cancelled)) RecycleReadback(cancelled);
                throw GPUPhysicsDevice.Failure("submit compute commands");
            }
        }
        while (_recordedReadbacks.TryDequeue(out var request)) _submittedReadbacks.Enqueue(request);
    }
    /// <summary>Waits for submitted work, releases its fence and delivers completed readback callbacks.</summary>
    /// <remarks>Local devices only. Callbacks run once in request order on the owner thread. Nested Sync defers callback
    /// delivery to the outer delivery loop. Disposal during a callback cancels remaining callbacks.</remarks>
    /// <exception cref="AggregateException">One or more callbacks failed; other completed callbacks were attempted.</exception>
    public void Sync()
    {
        if (_serverOwner is not null) throw new InvalidOperationException("Explicit synchronization belongs to local devices.");
        IdleList(); CompleteSubmission(); DeliverReadbacks();
    }
    /// <summary>Returns the selected compute-device name.</summary>
    /// <returns>The native device description.</returns>
    public string GetDeviceName() { Access(); return _context.DeviceName; }
    /// <summary>Frees a resource after completing pending work.</summary>
    /// <param name="rid">A live identity owned by this device.</param>
    /// <remarks>Borrowers become invalid; they may still be freed explicitly. Pending readbacks complete
    /// before removal; their callbacks run after the resource is removed.</remarks>
    /// <exception cref="AggregateException">One or more completed readback callbacks failed.</exception>
    public void FreeRID(RID rid)
    {
        IdleList(); var entry = Get(rid); CompleteSubmission();
        if (_command != 0 || _recordedReadbacks.Count != 0) { SubmitRecorded(); CompleteSubmission(); }
        _entries.Remove(rid); ReleaseProgram(entry.Variant); entry.Handle?.Dispose(); entry.FragmentHandle?.Dispose(); entry.Upload?.Dispose(); entry.Download?.Dispose(); entry.TextureScratch?.Dispose();
        DeliverReadbacks();
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_serverOwner is not null && !_serverOwner.DeviceClosing) throw new InvalidOperationException("The renderer owns this device lifetime."); Access(); if (_activeList != 0 && _serverOwner is null) throw new InvalidOperationException("End the command list before disposing the device."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_drawPass != 0) { SDL.EndGPURenderPass(_drawPass); _drawPass = 0; }
            if (_command != 0) { SDL.CancelGPUCommandBuffer(_command); _command = 0; }
            if (_fence != 0) { var fence = _fence; SDL.WaitForGPUFences(Device, true, (nint)(&fence), 1); SDL.ReleaseGPUFence(Device, _fence); _fence = 0; }
            foreach (var e in _entries.Values) { ReleaseProgram(e.Variant); e.Handle?.Dispose(); e.FragmentHandle?.Dispose(); e.Upload?.Dispose(); e.Download?.Dispose(); e.TextureScratch?.Dispose(); }
            _entries.Clear(); _sourceCache.Clear(); _sourceCacheBytes = 0; _sets.Clear(); _pipeline = null; _drawPass = 0; _drawTarget = null; _graphicsPipeline = null; _drawVertices = null; _drawIndices = null; _vertexFormats.Clear(); _framebufferFormats.Clear(); _activeList = 0; CancelReadbacks(); _context.Dispose();
        }
        base.Dispose(disposing);
    }
}
