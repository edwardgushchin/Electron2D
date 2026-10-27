using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

/// <summary>An owner-thread graphics and compute device with owned resource identities.</summary>
/// <remarks>Create a local device through RenderingServer.CreateLocalRenderingDevice or borrow the retained renderer device
/// through RenderingServer.GetRenderingDevice. Local Submit starts execution and Sync waits; the renderer device
/// flushes before canvas submission and completes queued reads before FramePostDraw. Buffer reads synchronize pending work. Resource identities never cross devices.
/// TextureGetRDTexture exports renderer uploads and completed viewport images with borrowed allocation lifetimes;
/// native writes leave authored CPU snapshots unchanged. FreeRID releases these wrappers without freeing their sources.
/// The device executes compute and color/depth graphics pipelines with exact multisample state and color resolve, with sampled images, storage resources, borrowed vertex/index arrays and framebuffer attachments.
/// Framebuffer view counts select ordered layer draws with private per-view shader indices and preserved draw state.
/// Independent Texture and Sampler descriptors may occupy different logical sets; recording resolves their validated native pairs.</remarks>
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
        /// <summary>One formatted buffer for indexed shader reads.</summary>
        TextureBuffer = 4,
        /// <summary>A sampler followed by one formatted indexed buffer; texel fetches do not apply filtering.</summary>
        SamplerWithTextureBuffer = 5,
        /// <summary>One formatted image buffer for indexed reads, compute stores and supported 32-bit atomics.</summary>
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
    private enum Kind { Storage, Texel, Uniform, Shader, Pipeline, Set, Texture, Sampler, Vertex, Index, VertexArray, IndexArray, Framebuffer, GraphicsPipeline, ShaderPlaceholder }
    private sealed class Entry
    {
        internal Kind Kind;
        internal RendererTextureBorrow? Borrowed;
        internal MultiMeshGPUState? MeshOwner;
        internal bool MeshCommand, MeshExported;
        internal bool Expired;
        internal bool Alive => !Expired && (Borrowed?.Alive ?? true) && (MeshOwner?.Alive ?? true) && (!MeshCommand || MeshOwner?.CommandsAlive != false);
        internal nint NativeHandle => Borrowed?.Native ?? Handle!.DangerousGetHandle();
        internal RenderHandle? Handle, Upload, Download, TextureScratch, InputShadow;
        internal bool ScratchColorTarget;
        internal byte[] Data = [];
        internal uint Size, BufferOffset;
        internal TexelLayout? Texel;
        internal StorageBufferUsage Usage;
        internal TextureInfo? Texture;
        internal bool? IntegerBorder;
        internal bool UnnormalizedSampling;
        internal bool CompareSampling, LinearSampling, LinearFormatSampling, GraphicsStorage, RestartIndices;
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
        internal ComputePipeline? Compute;
        internal uint Set;
        internal Binding[] Layout = [];
        internal SamplerLink[] Samplers = [];
        internal PushStage[] Push = [];
        internal byte[][] Sources = [];
        internal Dictionary<uint, Specialization> Constants = null!;
        internal int[]? OutputSlots;
        internal uint ViewIndex;
        internal GraphicsRecipe? Recipe;
        internal Dictionary<TextureSwizzleKey, SwizzleVariant>? SwizzleVariants;
        internal Entry? Variant;
        internal RenderHandle[] ViewPipelines = [];
        internal Entry[] ViewPrograms = [];
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
    private Entry Get(RID rid) { Access(); if (!_entries.TryGetValue(rid, out var entry) || !entry.Alive) throw new ArgumentException("The RID is not a live resource of this device.", nameof(rid)); PrepareRendererTexture(entry); entry.MeshOwner?.Prepare(entry.MeshCommand); if (!entry.Alive) throw new ArgumentException("The retained storage expired during preparation.", nameof(rid)); return entry; }
    private Entry Get(RID rid, Kind kind) { var entry = Get(rid); return entry.Kind == kind ? entry : throw new ArgumentException("The RID has the wrong resource kind.", nameof(rid)); }
    private RID Add(Entry entry)
    {
        var rid = RID.Allocate();
        try { if (NeedsGraphicsSnapshot(entry) && entry.InputShadow is null) PrepareInputShadow(entry); _entries.Add(rid, entry); }
        catch { entry.InputShadow?.Dispose(); entry.Borrowed?.Dispose(); throw; }
        if (NeedsGraphicsSnapshot(entry)) _graphicsSnapshotCount++;
        return rid;
    }
    private void IdleList(bool canvasMetadata = false) { Access(canvasMetadata); if (_activeList != 0) throw new InvalidOperationException("End the active command list first."); }
    private void List(long id) { Access(); if (_drawPass != 0 || _activeList == 0 || id != _activeList) throw new ArgumentException("The compute list is not active.", nameof(id)); }
    private nint Commands()
    {
        RenderingServer.NotifyRenderMutation();
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
        Access(); if (sizeBytes == 0 || sizeBytes % 4 != 0 || sizeBytes > 16384 || ValidationLimits is { } limits && sizeBytes > limits[21] || data.Length > sizeBytes) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        var entry = new Entry { Kind = Kind.Uniform, Size = sizeBytes, Data = new byte[sizeBytes] }; data.CopyTo(entry.Data); return Add(entry);
    }

    /// <summary>Updates a validated byte range of a buffer.</summary>
    /// <param name="buffer">A live storage, uniform, vertex, index or formatted texel buffer.</param>
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
        if (entry.Kind is not (Kind.Storage or Kind.Texel or Kind.Uniform or Kind.Vertex or Kind.Index)) throw new ArgumentException("The RID is not a buffer.");
        if (offset > entry.Size || size > entry.Size - offset) throw new ArgumentOutOfRangeException(nameof(offset));
    }
    private void Upload(Entry entry, uint offset, ReadOnlySpan<byte> data) => UploadRaw(entry, entry.BufferOffset + offset, data);
    private void UploadRaw(Entry entry, uint offset, ReadOnlySpan<byte> data)
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
        SDL.DownloadFromGPUBuffer(copy, new() { Buffer = entry.Handle!.DangerousGetHandle(), Offset = entry.BufferOffset + offsetBytes, Size = (uint)destination.Length }, new() { TransferBuffer = entry.Download.DangerousGetHandle() });
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
    /// <remarks>FP64 and 64-bit integer modules require their enabled native Vulkan shaderFloat64/shaderInt64 features, including symbolic templates. Compute 64-bit buffer/workgroup atomics additionally require their independently enabled native Vulkan 1.2 storage features. Half and Int16/UInt16 modules require their independently enabled native arithmetic/storage features; exact compact push reconstruction also requires the matching Float16 or Int16 arithmetic. Specialization modules retain copied symbolic templates. Pipeline creation applies typed values before native/layout validation; ordinary shaders retain their validated native program.</remarks>
    public RID ShaderCreateFromSPIRV(RDShaderSPIRV spirvData, string name = "")
    {
        IdleList(); var program = CaptureDeviceProgram(spirvData, name);
        try { return Add(program); }
        catch { ReleaseProgram(program); throw; }
    }
    private Entry CaptureDeviceProgram(RDShaderSPIRV spirvData, string name)
    {
        ArgumentNullException.ThrowIfNull(spirvData); ArgumentNullException.ThrowIfNull(name);
        var compute = spirvData.BytecodeCompute; var vertex = spirvData.BytecodeVertex; var fragment = spirvData.BytecodeFragment;
        if (compute.Length != 0 && (vertex.Length != 0 || fragment.Length != 0)) throw new ArgumentException("Compute and graphics modules cannot share a shader identity.", nameof(spirvData));
        if (compute.Length != 0 ? spirvData.CompileErrorCompute.Length != 0 : vertex.Length == 0 || fragment.Length == 0 || spirvData.CompileErrorVertex.Length != 0 || spirvData.CompileErrorFragment.Length != 0) throw new ArgumentException("Supply successful compute or paired graphics modules.", nameof(spirvData));
        byte[][] sources = compute.Length != 0 ? [compute] : [vertex, fragment];
        foreach (var source in sources) ShaderPrecision.ValidateNative(Device, source);
        var symbolic = sources.Any(code => new SpecializationModule(code, [], []).HasSpecialization);
        var program = CreateDeviceProgram(sources, name, [], symbolic); program.Sources = sources; return program;
    }
    private Entry CreateDeviceProgram(byte[][] sources, string name, Dictionary<uint, Specialization> constants, bool template = false, int[]? outputSlots = null, uint viewIndex = 0, IReadOnlyDictionary<(uint Set, int Binding, int Element), uint>? swizzles = null)
    {
        var used = new HashSet<uint>(); var modules = sources.Select(code => template ? code.ToArray() : new SpecializationModule(code, constants, used).Freeze()).ToArray();
        if (!template && constants.Keys.Any(id => !used.Contains(id))) throw new ArgumentException("A specialization identifier is not declared by this shader.", nameof(constants));
        var layouts = new Binding[modules.Length][]; var pushes = new PushStage[modules.Length][]; var samplers = new SamplerLink[modules.Length][];
        for (var i = 0; i < modules.Length; i++)
        { var stage = modules.Length == 1 ? ShaderStage.Compute : i == 0 ? ShaderStage.Vertex : ShaderStage.Fragment; (modules[i], layouts[i], pushes[i], samplers[i]) = RemapCore(LowerViewIndex(modules[i], viewIndex, stage), stage, template, swizzles); }
        var layout = layouts.SelectMany(l => l).ToArray();
        var samplerLinks = samplers.SelectMany(s => s).ToArray();
        NormalizeSamplerSources(layout, samplerLinks);
        foreach (var group in layout.Select((binding, index) => (binding, index)).GroupBy(p => (p.binding.Set, p.binding.Slot)))
            if (group.Any(p => p.binding.Type == UniformType.SamplerWithTextureBuffer) && group.All(p => p.binding.Type is UniformType.TextureBuffer or UniformType.SamplerWithTextureBuffer))
                foreach (var item in group) layout[item.index] = item.binding with { Type = UniformType.SamplerWithTextureBuffer };
        foreach (var group in layout.GroupBy(b => (b.Set, b.Slot)))
        { var first = group.First(); if (group.Any(b => b.Type != first.Type || b.Array != first.Array || !b.DynamicCount && !first.DynamicCount && b.Count != first.Count || b.ImageFormat != first.ImageFormat || b.PixelType != first.PixelType)) throw new ArgumentException("A shared descriptor has inconsistent stage types."); }
        var interfaces = modules.Length == 2 ? ShaderCompiler.ValidateDeviceGraphics(modules[0], modules[1]) : default;
        var entry = new Entry { Kind = Kind.Shader, Stage = modules.Length == 1 ? ShaderStage.Compute : ShaderStage.Vertex, Layout = layout, Samplers = samplerLinks, Push = pushes.SelectMany(p => p).ToArray(), Name = name, Inputs = interfaces.Inputs ?? [], Outputs = interfaces.Outputs ?? [] };
        entry.Sources = sources; entry.Constants = constants; entry.OutputSlots = outputSlots; entry.ViewIndex = viewIndex;
        if (template) return entry;
        ValidateShaderLimits(modules, entry);
        if (entry.Samplers.Any(s => s.Sampler is null)) PrepareFetchSampler();
        if (outputSlots is not null) modules[1] = RemapFragmentOutputs(modules[1], outputSlots, entry.Outputs);
        try
        {
            if (modules.Length == 1) { entry.Compute = new(_context.Handle, modules[0]); entry.Handle = entry.Compute.Handle; }
            else { entry.Handle = new(ShaderCompiler.CreateShader(Device, modules[0], false, _context.SupportsSizedPoints), h => SDL.ReleaseGPUShader(Device, h), _context.Handle); entry.FragmentHandle = new(ShaderCompiler.CreateShader(Device, modules[1], true, _context.SupportsSizedPoints), h => SDL.ReleaseGPUShader(Device, h), _context.Handle); }
            return entry;
        }
        catch { ReleaseProgram(entry); throw; }
    }
    private static void ReleaseProgram(Entry? program)
    {
        program?.FragmentHandle?.Dispose();
        if (program?.Compute is { } compute) compute.Dispose(); else program?.Handle?.Dispose();
    }
    private Entry PipelineProgram(Entry pipeline) { var source = Get(pipeline.Shader, Kind.Shader); return pipeline.Variant ?? source; }
    private Entry? PipelineVariant(Entry source, ReadOnlySpan<RDPipelineSpecializationConstant> constants, int[]? outputSlots = null)
    {
        if (outputSlots is not null && !outputSlots.Where((slot, index) => slot != index).Any()) outputSlots = null;
        var values = CaptureSpecialization(constants); return values.Count == 0 && source.Handle is not null && outputSlots is null ? null : CreateDeviceProgram(source.Sources, source.Name, values, outputSlots: outputSlots);
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

    /// <summary>Creates a copied set of validated buffer, image, sampler and framebuffer-input bindings.</summary>
    /// <param name="uniforms">Shader-matching descriptions. Fixed image, sampler, texel-buffer and framebuffer-input arrays use element order; combined categories repeat sampler then image or buffer per element. Ordinary uniform/storage buffers borrow one identity.</param>
    /// <param name="shader">The shader whose layout owns this set.</param>
    /// <param name="shaderSet">The shader descriptor-set number.</param>
    /// <returns>A set identity borrowing its shader and every described resource.</returns>
    public RID UniformSetCreate(ReadOnlySpan<RDUniform> uniforms, RID shader, uint shaderSet)
    {
        var program = Get(shader, Kind.Shader); var entry = new Entry { Kind = Kind.Set, Shader = shader, Set = shaderSet };
        foreach (var uniform in uniforms)
        {
            ArgumentNullException.ThrowIfNull(uniform); var ids = uniform.GetIDs();
            if (uniform.Binding < 0 || ids.Length == 0) throw new ArgumentException("Each supported binding needs resource identities.", nameof(uniforms));
            var descriptor = program.Layout.FirstOrDefault(b => b.Set == shaderSet && b.Slot == uniform.Binding);
            if (descriptor == default || descriptor.Type != uniform.UniformType) throw new ArgumentException("Descriptor does not match the shader.", nameof(uniforms));
            foreach (var stageBinding in program.Layout)
                if (stageBinding.Set == shaderSet && stageBinding.Slot == uniform.Binding)
                {
                    if (!stageBinding.DynamicCount) ValidateBinding(stageBinding, ids);
                    else if (stageBinding.Element == 0)
                    {
                        var width = ResourceWidth(stageBinding.Type);
                        if (ids.Length % width != 0 || ids.Length / width is 0 or > 16) throw new ArgumentException("The symbolic descriptor array has an invalid element count.", nameof(uniforms));
                        for (var element = 0; element < ids.Length / width; element++) ValidateBinding(stageBinding with { Count = ids.Length / width, Element = element, DynamicCount = false }, ids);
                    }
                }
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
        Access(); return CachedUniformIsValid(uniformSet);
    }
    internal bool CachedUniformIsValid(RID uniformSet)
    {
        if (!_entries.TryGetValue(uniformSet, out var e) || e.Kind != Kind.Set || !_entries.ContainsKey(e.Shader)) return false;
        foreach (var ids in e.Bindings.Values) foreach (var rid in ids) if (!_entries.TryGetValue(rid, out var resource) || !resource.Alive) return false;
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
    /// <remarks>Runtime storage-buffer lengths reflect each resource bound for this dispatch, including rebinding within one submission.</remarks>
    public void ComputeListDispatch(long computeList, uint xGroups, uint yGroups, uint zGroups)
    {
        List(computeList);
        if (xGroups is 0 or > 65535 || yGroups is 0 or > 65535 || zGroups is 0 or > 65535 || ValidationLimits is { } limits && (xGroups > limits[28] || yGroups > limits[29] || zGroups > limits[30])) throw new ArgumentOutOfRangeException(nameof(xGroups));
        Dispatch(xGroups, yGroups, zGroups);
    }
    private void Dispatch(uint xGroups, uint yGroups, uint zGroups, Entry? indirect = null, uint offset = 0)
    {
        if (_pipeline is null) throw new InvalidOperationException("Bind a compute pipeline first.");
        RequirePush(_pipeline);
        var sampledVariant = SampledVariant(Get(_pushPipeline, Kind.Pipeline));
        _pipeline = sampledVariant?.Programs[0] ?? PipelineProgram(Get(_pushPipeline, Kind.Pipeline));
        var buffers = stackalloc SDL.GPUStorageBufferReadWriteBinding[8]; var count = 0;
        Span<uint> bufferSizes = stackalloc uint[8];
        var images = stackalloc SDL.GPUStorageTextureReadWriteBinding[8]; var imageCount = 0;
        var samplers = stackalloc SDL.GPUTextureSamplerBinding[16]; var samplerCount = 0;
        foreach (var descriptor in _pipeline.Layout)
        {
            if (!_sets.TryGetValue(descriptor.Set, out var set) || set.Shader != Get(_pushPipeline).Shader) throw new InvalidOperationException("Bind each pipeline resource set first.");
            var ids = set.Bindings[descriptor.Slot];
            ValidateBinding(descriptor, ids); if (descriptor.Native < 0) continue; var buffer = Get(ids[ResourceID(descriptor, BufferID(descriptor.Type))]);
            if (descriptor.Type == UniformType.Image) { images[descriptor.Native] = new() { Texture = buffer.NativeHandle }; imageCount++; }
            else if (descriptor.Type == UniformType.SamplerWithTexture) { samplers[descriptor.Native] = new() { Sampler = buffer.NativeHandle, Texture = Get(ids[ResourceID(descriptor, 1)]).NativeHandle }; samplerCount++; }
            else if (StorageBinding(descriptor.Type)) { buffers[descriptor.Native] = new() { Buffer = buffer.Handle!.DangerousGetHandle() }; bufferSizes[descriptor.Native] = buffer.Size; count++; }
            else fixed (byte* data = buffer.Data) SDL.PushGPUComputeUniformData(_command, (uint)descriptor.Native, (nint)data, buffer.Size);
        }
        foreach (var link in _pipeline.Samplers) { samplers[link.Native.Native] = LinkedSampler(link, false); samplerCount++; }
        PushUniforms(_pipeline);
        _pipeline.Compute?.PrepareBufferSizes(_command, ReadOnlySpan<uint>.Empty, bufferSizes[..count]);
        var pass = SDL.BeginGPUComputePass(_command, (nint)images, (uint)imageCount, (nint)buffers, (uint)count);
        if (pass == 0) throw GPUPhysicsDevice.Failure("begin compute dispatch for " + _pipeline.Name);
        SDL.BindGPUComputePipeline(pass, _pipeline.Handle!.DangerousGetHandle());
        _pipeline.Compute?.BindBufferSizes(pass);
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
    private void SubmitRecorded(bool canvasMetadata = false)
    {
        IdleList(canvasMetadata); if (_fence != 0 || _submittedReadbacks.Count != 0) throw new InvalidOperationException("Synchronize the previous submission first.");
        if (_command != 0)
        {
            var command = _command; _command = 0; _fence = SDL.SubmitGPUCommandBufferAndAcquireFence(command);
            if (_fence == 0)
            {
                CancelRendererUploads();
                while (_recordedReadbacks.TryDequeue(out var cancelled)) RecycleReadback(cancelled);
                throw GPUPhysicsDevice.Failure("submit compute commands");
            }
        }
        if (_serverOwner is null) _drawableShaderSamplers?.AdvanceEpoch();
        CommitRendererUploads();
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
    /// <remarks>Borrowed renderer image identities release their wrapper and leases, preserving source ownership.
    /// Live renderer-owned instance buffers retain their resource lifetime and cannot be freed directly.
    /// Expired borrowed identities may still be freed. Borrowers become invalid; they may still be freed explicitly. Pending readbacks complete
    /// before removal; their callbacks run after the resource is removed.</remarks>
    /// <exception cref="AggregateException">One or more completed readback callbacks failed.</exception>
    /// <exception cref="InvalidOperationException">A live instance buffer has resource-owned lifetime, or owner/list/native state is invalid.</exception>
    public void FreeRID(RID rid)
    {
        IdleList(); if (!_entries.TryGetValue(rid, out var entry)) throw new ArgumentException("The resource identity is not owned by this device.", nameof(rid)); if (entry.MeshOwner is not null && entry.Alive) throw new InvalidOperationException("The instance resource owns this retained buffer lifetime."); CompleteSubmission();
        if (_command != 0 || _recordedReadbacks.Count != 0) { SubmitRecorded(); CompleteSubmission(); }
        RenderingServer.NotifyRenderMutation();
        _entries.Remove(rid); if (NeedsGraphicsSnapshot(entry)) _graphicsSnapshotCount--; if (entry.Borrowed is { } borrow && _rendererTextures.GetValueOrDefault(borrow.Source) == rid) _rendererTextures.Remove(borrow.Source); entry.Borrowed?.Dispose(); ReleaseViews(entry); ReleaseSwizzles(entry); ReleaseProgram(entry.Variant); ReleaseProgram(entry); entry.Upload?.Dispose(); entry.Download?.Dispose(); entry.TextureScratch?.Dispose(); entry.InputShadow?.Dispose();
        EvictResourceCaches(rid); DeliverReadbacks();
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_serverOwner is not null && !_serverOwner.DeviceClosing) throw new InvalidOperationException("The renderer owns this device lifetime."); Access(); if (_activeList != 0 && _serverOwner is null) throw new InvalidOperationException("End the command list before disposing the device."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RenderingServer.NotifyRenderMutation();
            if (_drawPass != 0) { SDL.EndGPURenderPass(_drawPass); _drawPass = 0; }
            if (_command != 0) { SDL.CancelGPUCommandBuffer(_command); _command = 0; }
            if (_fence != 0) { var fence = _fence; SDL.WaitForGPUFences(Device, true, (nint)(&fence), 1); SDL.ReleaseGPUFence(Device, _fence); _fence = 0; }
            CancelRendererUploads(); ReleaseDrawablePipelines(); RetireResourceCaches();
            foreach (var e in _entries.Values) { e.Borrowed?.Dispose(); ReleaseViews(e); ReleaseSwizzles(e); ReleaseProgram(e.Variant); ReleaseProgram(e); e.Upload?.Dispose(); e.Download?.Dispose(); e.TextureScratch?.Dispose(); e.InputShadow?.Dispose(); }
            _entries.Clear(); _graphicsSnapshots.Clear(); _graphicsSnapshotCount = 0; _rendererTextures.Clear(); _sourceCache.Clear(); _sourceCacheBytes = 0; _sets.Clear(); _pipeline = null; _drawPass = 0; _drawTarget = null; _graphicsPipeline = null; _drawVertices = null; _drawIndices = null; _vertexFormats.Clear(); _framebufferFormats.Clear(); _screens.Clear(); _screenFormats.Clear(); _screenDraw = null; _activeList = 0; CancelReadbacks(); _fetchSampler?.Dispose(); _fetchSampler = null; _context.Dispose();
        }
        base.Dispose(disposing);
    }
}
