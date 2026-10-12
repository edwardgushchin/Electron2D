using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private sealed record GraphicsPipeline(int FramebufferFormat, int VertexFormat, RenderPrimitive Primitive, Color Constant, PipelineDynamicStateFlags Dynamic, byte StencilReference);
    private static SDL.GPUBlendFactor Blend(BlendFactor value) => value switch
    {
        >= BlendFactor.Zero and <= BlendFactor.OneMinusConstantColor => (SDL.GPUBlendFactor)((int)value + 1),
        BlendFactor.SrcAlphaSaturate => SDL.GPUBlendFactor.SrcAlphaSaturate,
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
    private static SDL.GPUBlendOp Blend(BlendOperation value) => Enum.IsDefined(value) && value != BlendOperation.Max ? (SDL.GPUBlendOp)((int)value + 1) : throw new ArgumentOutOfRangeException(nameof(value));
    /// <summary>Creates an executable color and depth/stencil graphics pipeline with copied multisample state.</summary>
    /// <param name="shader">A live paired vertex/fragment shader.</param>
    /// <param name="framebufferFormat">A color and optional depth/stencil layout on this device.</param>
    /// <param name="vertexFormat">A copied vertex layout, or -1 for procedural vertices.</param>
    /// <param name="primitive">Point, line or triangle topology.</param>
    /// <param name="rasterizationState">Copied rasterization configuration.</param>
    /// <param name="colorBlendState">Copied blending configuration, with one entry per target.</param>
    /// <param name="dynamicStateFlags">Allows dynamic blend constants; other dynamic states require their native adapters.</param>
    /// <param name="depthStencilState">Copied depth/stencil configuration; null disables both tests and depth writes.</param>
    /// <param name="multisampleState">Copied sample count and alpha-to-coverage state; null selects one sample.</param>
    /// <returns>A pipeline borrowing the shader identity.</returns>
    /// <exception cref="ArgumentException">Shader interfaces, layouts or configurations disagree.</exception>
    /// <remarks>Depth-bounds tests, sample shading, subpasses and pipeline specialization require additional attachment and shader interfaces.</remarks>
    public RID RenderPipelineCreate(RID shader, int framebufferFormat, int vertexFormat, RenderPrimitive primitive,
        RDPipelineRasterizationState rasterizationState, RDPipelineColorBlendState colorBlendState, PipelineDynamicStateFlags dynamicStateFlags = 0, RDPipelineDepthStencilState? depthStencilState = null, RDPipelineMultisampleState? multisampleState = null)
    {
        IdleList(); ArgumentNullException.ThrowIfNull(rasterizationState); ArgumentNullException.ThrowIfNull(colorBlendState);
        var program = Get(shader, Kind.Shader); if (program.Stage != ShaderStage.Vertex) throw new ArgumentException("A paired graphics shader is required.", nameof(shader));
        if ((dynamicStateFlags & ~PipelineDynamicStateFlags.BlendConstants) != 0) throw new ArgumentOutOfRangeException(nameof(dynamicStateFlags));
        var target = FramebufferFormat(framebufferFormat);
        var samples = multisampleState?.SampleCount ?? TextureSamples.Samples1;
        if (samples != target.Samples) throw new ArgumentException("Pipeline sample count must match the framebuffer raster targets.", nameof(multisampleState));
        using var defaultDepth = depthStencilState is null ? new RDPipelineDepthStencilState() : null;
        var depthState = DepthState(depthStencilState ?? defaultDepth!, rasterizationState, target, out var stencilReference);
        var vertex = vertexFormat == -1 ? new VertexLayout([], [], []) : VertexFormat(vertexFormat);
        ValidateVertexInterface(program, vertex);
        foreach (var output in program.Outputs)
            if (output.Location >= target.Colors.Length || output.Type != PixelType(target.Formats[target.Colors[output.Location]])) throw new ArgumentException("Fragment outputs must match the framebuffer scalar formats.", nameof(framebufferFormat));
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
        if (attachments.Length != target.Colors.Length) throw new ArgumentException("Provide blending for every color target.", nameof(colorBlendState));
        var colors = stackalloc SDL.GPUColorTargetDescription[8];
        for (var i = 0; i < attachments.Length; i++)
        {
            var a = attachments[i];
            if (a.EnableBlend && PixelType(target.Formats[target.Colors[i]]) != 0) throw new ArgumentException("Integer color targets cannot blend.", nameof(colorBlendState));
            colors[i] = new()
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
                TargetInfo = new() { ColorTargetDescriptions = (nint)colors, NumColorTargets = (uint)attachments.Length, HasDepthStencilTarget = target.DepthIndex >= 0, DepthStencilFormat = target.DepthIndex >= 0 ? NativeFormat(target.Formats[target.DepthIndex]) : SDL.GPUTextureFormat.Invalid }
            };
            var handle = new RenderHandle(SDL.CreateGPUGraphicsPipeline(Device, in info), h => SDL.ReleaseGPUGraphicsPipeline(Device, h), _context.Handle);
            return Add(new Entry { Kind = Kind.GraphicsPipeline, Shader = shader, Handle = handle, Graphics = new(framebufferFormat, vertexFormat, primitive, colorBlendState.BlendConstant, dynamicStateFlags, stencilReference) });
        }
    }
    /// <summary>Reports whether a graphics pipeline and its borrowed shader are alive.</summary>
    /// <param name="renderPipeline">The identity to inspect.</param>
    /// <returns>True for an intact executable pipeline of this device.</returns>
    public bool RenderPipelineIsValid(RID renderPipeline) { Access(); return _entries.TryGetValue(renderPipeline, out var entry) && entry.Kind == Kind.GraphicsPipeline && _entries.ContainsKey(entry.Shader); }
    /// <summary>Returns the mask of vertex input locations declared by a graphics shader.</summary>
    /// <param name="shader">A live paired graphics shader.</param>
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
