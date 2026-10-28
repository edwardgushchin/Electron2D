using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private sealed record GraphicsPipeline(int FramebufferFormat, int VertexFormat, RenderPrimitive Primitive, Color Constant, PipelineDynamicStateFlags Dynamic, byte StencilReference, uint ForRenderPass);
    private static SDL.GPUBlendFactor Blend(BlendFactor value) => value switch
    {
        >= BlendFactor.Zero and <= BlendFactor.OneMinusConstantColor => (SDL.GPUBlendFactor)((int)value + 1),
        BlendFactor.SrcAlphaSaturate => SDL.GPUBlendFactor.SrcAlphaSaturate,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
    private static SDL.GPUBlendOp Blend(BlendOperation value) => Enum.IsDefined(value) && value != BlendOperation.Max ? (SDL.GPUBlendOp)((int)value + 1) : throw new ArgumentOutOfRangeException(nameof(value));
    /// <summary>Creates an executable color and depth/stencil graphics pipeline with copied multisample state.</summary>
    /// <param name="shader">A live vertex shader with an optional fragment stage; vertex-only programs require primitive discard.</param>
    /// <param name="framebufferFormat">A color and optional depth/stencil layout on this device.</param>
    /// <param name="vertexFormat">A copied vertex layout, or -1 for procedural vertices.</param>
    /// <param name="primitive">Point, line or triangle topology.</param>
    /// <param name="rasterizationState">Copied rasterization configuration, including primitive discard and line width.</param>
    /// <param name="colorBlendState">Copied blending configuration covering every logical color slot; unused slots and trailing entries are ignored.</param>
    /// <param name="dynamicStateFlags">Allows dynamic blend constants; other dynamic states require their native adapters.</param>
    /// <param name="depthStencilState">Copied depth/stencil configuration; null disables both tests and depth writes.</param>
    /// <param name="multisampleState">Copied sample count and alpha-to-coverage state; null selects one sample.</param>
    /// <param name="specializationConstants">Copied scalar overrides shared by the paired stages; omitted values retain defaults.</param>
    /// <param name="forRenderPass">Zero-based framebuffer pass whose outputs and inputs this pipeline consumes.</param>
    /// <returns>A pipeline borrowing the shader identity and owning specialized native stages when needed.</returns>
    /// <exception cref="ArgumentException">Shader interfaces, layouts or configurations disagree.</exception>
    /// <remarks>Depth-bounds tests and sample shading require their native state adapters. Ordered passes select forRenderPass. Typed specialization freezes constants before native creation. Without primitive discard, fragment outputs must exactly match active logical color slots; sparse slots use private native output remapping.</remarks>
    public RID RenderPipelineCreate(RID shader, int framebufferFormat, int vertexFormat, RenderPrimitive primitive,
        RDPipelineRasterizationState rasterizationState, RDPipelineColorBlendState colorBlendState, PipelineDynamicStateFlags dynamicStateFlags = 0, RDPipelineDepthStencilState? depthStencilState = null, RDPipelineMultisampleState? multisampleState = null, ReadOnlySpan<RDPipelineSpecializationConstant> specializationConstants = default, uint forRenderPass = 0)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(rasterizationState); ArgumentNullException.ThrowIfNull(colorBlendState);
        var program = Get(shader, Kind.Shader); if (program.Stage != ShaderStage.Vertex) throw new ArgumentException("A paired graphics shader is required.", nameof(shader));
        if ((dynamicStateFlags & ~PipelineDynamicStateFlags.BlendConstants) != 0) throw new ArgumentOutOfRangeException(nameof(dynamicStateFlags));
        var discard = rasterizationState.DiscardPrimitives; var lineWidth = rasterizationState.LineWidth;
        if (program.VertexOnly && !discard) throw new ArgumentException("A vertex-only shader requires primitive discard.", nameof(rasterizationState));
        var target = FramebufferFormat(framebufferFormat, forRenderPass);
        var samples = multisampleState?.SampleCount ?? TextureSamples.Samples1;
        if (samples != target.Samples) throw new ArgumentException("Pipeline sample count must match the framebuffer raster targets.", nameof(multisampleState));
        using var defaultDepth = depthStencilState is null ? new RDPipelineDepthStencilState() : null;
        var depthState = DepthState(depthStencilState ?? defaultDepth!, rasterizationState, target, out var stencilReference);
        var variant = PipelineVariant(program, specializationConstants, discard ? null : target.ColorSlots);
        try
        {
            program = variant ?? program;
            var vertex = vertexFormat == -1 ? new VertexLayout([], [], []) : VertexFormat(vertexFormat);
            ValidateVertexInterface(program, vertex);
            if (!discard) ValidateInputLayout(program, target);
            if (!discard && program.Outputs.Length != target.ColorSlots.Length) throw new ArgumentException("Fragment outputs must exactly match the active framebuffer color slots.", nameof(framebufferFormat));
            if (!discard) foreach (var output in program.Outputs)
                    if (output.Location >= target.Colors.Length || target.Colors[output.Location] < 0 || output.Type != PixelType(target.Formats[target.Colors[output.Location]])) throw new ArgumentException("Fragment outputs must match the framebuffer scalar formats.", nameof(framebufferFormat));
            var topology = primitive switch
            {
                RenderPrimitive.Points => SDL.GPUPrimitiveType.PointList,
                RenderPrimitive.Lines => SDL.GPUPrimitiveType.LineList,
                RenderPrimitive.LineStrips => SDL.GPUPrimitiveType.LineStrip,
                RenderPrimitive.Triangles => SDL.GPUPrimitiveType.TriangleList,
                RenderPrimitive.TriangleStrips or RenderPrimitive.TriangleStripsWithRestartIndex => SDL.GPUPrimitiveType.TriangleStrip,
                _ => throw new ArgumentOutOfRangeException(nameof(primitive))
            };
            if (!Enum.IsDefined(rasterizationState.CullMode) || !Enum.IsDefined(rasterizationState.FrontFace)) throw new ArgumentException("Unknown rasterization value.", nameof(rasterizationState));
            var attachments = colorBlendState.Attachments;
            if (attachments.Length < target.Colors.Length) throw new ArgumentException("Provide blending for every logical color slot.", nameof(colorBlendState));
            var colors = stackalloc SDL.GPUColorTargetDescription[8];
            for (var native = 0; native < target.ColorSlots.Length; native++)
            {
                var i = target.ColorSlots[native]; var a = attachments[i];
                if (a.EnableBlend && PixelType(target.Formats[target.Colors[i]]) != 0) throw new ArgumentException("Integer color targets cannot blend.", nameof(colorBlendState));
                colors[native] = new()
                {
                    Format = NativeFormat(target.Formats[target.Colors[i]]),
                    BlendState = new()
                    {
                        EnableBlend = a.EnableBlend,
                        SrcColorBlendFactor = Blend(a.SrcColorBlendFactor),
                        DstColorBlendFactor = Blend(a.DstColorBlendFactor),
                        SrcAlphaBlendFactor = Blend(a.SrcAlphaBlendFactor),
                        DstAlphaBlendFactor = Blend(a.DstAlphaBlendFactor),
                        ColorBlendOp = Blend(a.ColorBlendOp),
                        AlphaBlendOp = Blend(a.AlphaBlendOp),
                        EnableColorWriteMask = true,
                        ColorWriteMask = (a.WriteR ? SDL.GPUColorComponentFlags.R : 0) | (a.WriteG ? SDL.GPUColorComponentFlags.G : 0) | (a.WriteB ? SDL.GPUColorComponentFlags.B : 0) | (a.WriteA ? SDL.GPUColorComponentFlags.A : 0)
                    }
                };
            }
            fixed (SDL.GPUVertexAttribute* attributes = vertex.Attributes)
            fixed (SDL.GPUVertexBufferDescription* buffers = vertex.Buffers)
            {
                var info = new SDL.GPUGraphicsPipelineCreateInfo
                {
                    VertexShader = program.Handle!.DangerousGetHandle(),
                    FragmentShader = program.FragmentHandle!.DangerousGetHandle(),
                    PrimitiveType = topology,
                    RasterizerState = new()
                    {
                        FillMode = rasterizationState.Wireframe ? SDL.GPUFillMode.Line : SDL.GPUFillMode.Fill,
                        CullMode = rasterizationState.CullMode switch { PolygonCullMode.Front => SDL.GPUCullMode.Front, PolygonCullMode.Back => SDL.GPUCullMode.Back, _ => SDL.GPUCullMode.None },
                        FrontFace = rasterizationState.FrontFace == PolygonFrontFace.Clockwise ? SDL.GPUFrontFace.Clockwise : SDL.GPUFrontFace.CounterClockwise,
                        EnableDepthClip = !rasterizationState.EnableDepthClamp,
                        EnableDepthBias = rasterizationState.DepthBiasEnabled,
                        DepthBiasConstantFactor = rasterizationState.DepthBiasConstantFactor,
                        DepthBiasClamp = rasterizationState.DepthBiasClamp,
                        DepthBiasSlopeFactor = rasterizationState.DepthBiasSlopeFactor
                    },
                    VertexInputState = new() { VertexAttributes = (nint)attributes, NumVertexAttributes = (uint)vertex.Attributes.Length, VertexBufferDescriptions = (nint)buffers, NumVertexBuffers = (uint)vertex.Buffers.Length },
                    DepthStencilState = depthState,
                    MultisampleState = new() { SampleCount = (SDL.GPUSampleCount)samples, EnableAlphaToCoverage = multisampleState?.EnableAlphaToCoverage ?? false },
                    TargetInfo = new() { ColorTargetDescriptions = (nint)colors, NumColorTargets = (uint)target.ColorSlots.Length, HasDepthStencilTarget = target.DepthIndex >= 0, DepthStencilFormat = target.DepthIndex >= 0 ? NativeFormat(target.Formats[target.DepthIndex]) : SDL.GPUTextureFormat.Invalid }
                };
                var entry = new Entry { Kind = Kind.GraphicsPipeline, Shader = shader, Variant = variant, Graphics = new(framebufferFormat, vertexFormat, primitive, colorBlendState.BlendConstant, dynamicStateFlags, stencilReference, forRenderPass) };
                entry.Recipe = new(info, vertex.Attributes, vertex.Buffers, new ReadOnlySpan<SDL.GPUColorTargetDescription>(colors, target.ColorSlots.Length).ToArray(), discard, lineWidth);
                var views = FramebufferLayouts(framebufferFormat).ViewCount;
                entry.ViewPipelines = new RenderHandle[views - 1]; entry.ViewPrograms = new Entry[views - 1];
                try
                {
                    entry.Handle = new(CreateGraphicsPipeline(in info, discard, lineWidth), h => SDL.ReleaseGPUGraphicsPipeline(Device, h), _context.Handle);
                    for (var view = 1; view < views; view++)
                    {
                        var source = Get(shader, Kind.Shader);
                        var viewProgram = CreateDeviceProgram(source.Sources, source.Name, CaptureSpecialization(specializationConstants), outputSlots: discard ? null : target.ColorSlots, viewIndex: (uint)view);
                        entry.ViewPrograms[view - 1] = viewProgram;
                        info.VertexShader = viewProgram.Handle!.DangerousGetHandle(); info.FragmentShader = viewProgram.FragmentHandle!.DangerousGetHandle();
                        entry.ViewPipelines[view - 1] = new(CreateGraphicsPipeline(in info, discard, lineWidth), h => SDL.ReleaseGPUGraphicsPipeline(Device, h), _context.Handle);
                    }
                    return Add(entry);
                }
                catch { foreach (var handle in entry.ViewPipelines) handle?.Dispose(); foreach (var viewProgram in entry.ViewPrograms) ReleaseProgram(viewProgram); entry.Handle?.Dispose(); throw; }
            }
        }
        catch { ReleaseProgram(variant); throw; }
    }
    /// <summary>Reports whether a graphics pipeline and its borrowed shader are alive.</summary>
    /// <param name="renderPipeline">The identity to inspect.</param>
    /// <returns>True for an intact executable pipeline of this device.</returns>
    public bool RenderPipelineIsValid(RID renderPipeline) { Access(); return _entries.TryGetValue(renderPipeline, out var entry) && entry.Kind == Kind.GraphicsPipeline && _entries.ContainsKey(entry.Shader); }
    /// <summary>Returns the mask of vertex input locations declared by a graphics shader.</summary>
    /// <param name="shader">A live graphics shader.</param>
    /// <returns>A bit per declared location; procedural shaders return zero.</returns>
    public ulong ShaderGetVertexInputAttributeMask(RID shader)
    { var entry = Get(shader, Kind.Shader); if (entry.Stage != ShaderStage.Vertex) throw new ArgumentException("A graphics shader is required.", nameof(shader)); ulong mask = 0; foreach (var input in entry.Inputs) mask |= 1UL << (int)input.Location; return mask; }
    private static void ValidateVertexInterface(Entry program, VertexLayout vertex)
    {
        foreach (var input in program.Inputs)
        {
            var attribute = vertex.Attributes.SingleOrDefault(a => a.Location == input.Location);
            if (!vertex.Attributes.Any(a => a.Location == input.Location)) throw new ArgumentException("A shader vertex input is missing from the layout.");
            var type = attribute.Format switch
            {
                SDL.GPUVertexElementFormat.Int or SDL.GPUVertexElementFormat.Int2 or SDL.GPUVertexElementFormat.Int3 or SDL.GPUVertexElementFormat.Int4 or SDL.GPUVertexElementFormat.Byte2 or SDL.GPUVertexElementFormat.Byte4 or SDL.GPUVertexElementFormat.Short2 or SDL.GPUVertexElementFormat.Short4 => 1,
                SDL.GPUVertexElementFormat.Uint or SDL.GPUVertexElementFormat.Uint2 or SDL.GPUVertexElementFormat.Uint3 or SDL.GPUVertexElementFormat.Uint4 or SDL.GPUVertexElementFormat.Ubyte2 or SDL.GPUVertexElementFormat.Ubyte4 or SDL.GPUVertexElementFormat.Ushort2 or SDL.GPUVertexElementFormat.Ushort4 => 2,
                _ => 0
            };
            if (type != input.Type) throw new ArgumentException("Vertex encodings must match shader scalar types.");
        }
    }
}
