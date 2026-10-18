using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend : CanvasBackend
{
    private readonly RenderHandle _device;
    private readonly nint _window;
    private readonly RenderHandle _vertexShader, _instancedVertexShader, _lightingVertexShader, _lightingInstancedVertexShader;
    private readonly byte[] _defaultFragment;
    private readonly Dictionary<(byte[] Code, byte[]? Vertex, BlendMode Blend, bool Instanced, SDL.GPUSampleCount Samples, bool LCD, SDL.GPUTextureFormat Format, Mesh.PrimitiveType Primitive), RenderHandle> _pipelines = [];
    // Keep one previous frame to avoid recreating pipelines for alternating visibility.
    private HashSet<(byte[] Code, byte[]? Vertex, BlendMode Blend, bool Instanced, SDL.GPUSampleCount Samples, bool LCD, SDL.GPUTextureFormat Format, Mesh.PrimitiveType Primitive)> _usedPrograms = [], _previousPrograms = [];
    private readonly Dictionary<Texture, GPUTexture> _textures = [];
    private readonly Dictionary<TextureLayered, GPUTexture> _layeredTextures = [];
    private readonly HashSet<TextureLayered> _usedLayeredTextures = [];
    private readonly HashSet<Texture> _usedTextures = [];
    private readonly Dictionary<MaterialState, SDL.GPUTextureSamplerBinding[]> _textureBindings = [];
    private readonly HashSet<MaterialState> _usedMaterials = [];
    private readonly Resource?[] _textureScratch = new Resource?[32];
    private readonly Dictionary<(TextureFilter, TextureRepeat, int, bool), RenderHandle> _samplers = [];
    private readonly bool _nearestMipmaps = ProjectSettings.GetWithOverride(ProjectSettings.UseNearestMIPMapFilter);
    private ImageTexture? _whiteTexture;
    private readonly RenderHandle?[,] _clearPipelines = new RenderHandle?[2, 4];
    private readonly RenderHandle?[,] _seedPipelines = new RenderHandle?[2, 4];
    private RenderHandle? _vertexBuffer;
    private RenderHandle? _transfer;
    private int _bufferSize;
    private bool _disposed, _windowClaimed, _hasPresented, _waylandRemapBlocked;
    private readonly bool _relaxedAndroidDevice;
    private DisplayServer.VSyncMode _vSync = DisplayServer.VSyncMode.Enabled;
    internal override DisplayServer.VSyncMode VSync => _vSync;
    internal override void SetVSync(DisplayServer.VSyncMode mode)
    {
        var native = mode switch { DisplayServer.VSyncMode.Disabled => SDL.GPUPresentMode.Immediate, DisplayServer.VSyncMode.Mailbox => SDL.GPUPresentMode.Mailbox, _ => SDL.GPUPresentMode.VSync };
        if (!SDL.WindowSupportsGPUPresentMode(Device, _window, native)) native = SDL.GPUPresentMode.VSync;
        Check(SDL.SetGPUSwapchainParameters(Device, _window, SDL.GPUSwapchainComposition.SDR, native), "set GPU presentation policy");
        _vSync = native switch { SDL.GPUPresentMode.Immediate => DisplayServer.VSyncMode.Disabled, SDL.GPUPresentMode.Mailbox => DisplayServer.VSyncMode.Mailbox, _ => DisplayServer.VSyncMode.Enabled };
    }
    internal override string Method => "gpu";
    internal override string Driver { get; }
    private nint Device => _device.DangerousGetHandle();

    internal RenderHandle RetainComputeDevice() => new(Device, static _ => { }, _device);

    internal GPUCanvasBackend(SafeHandle window)
    {
        _window = window.DangerousGetHandle();
        var (device, relaxed) = CreateDevice();
        _relaxedAndroidDevice = relaxed;
        _device = new RenderHandle(device, SDL.DestroyGPUDevice, window);
        var claimed = false;
        RenderHandle? vertex = null, instancedVertex = null, lightingVertex = null, lightingInstanced = null;
        try
        {
            Check(SDL.ClaimWindowForGPUDevice(Device, _window), "claim window for GPU rendering");
            claimed = _windowClaimed = true;
            Driver = SDL.GetGPUDeviceDriver(Device) ?? "unknown";
            vertex = CreateShader(BuiltInShaders.Vertex, fragment: false);
            _vertexShader = vertex;
            _instancedVertexShader = instancedVertex = CreateShader(BuiltInShaders.InstancedVertex, fragment: false);
            _lightingVertexShader = lightingVertex = CreateShader(BuiltInShaders.LightingVertex, fragment: false);
            _lightingInstancedVertexShader = lightingInstanced = CreateShader(BuiltInShaders.LightingInstancedVertex, fragment: false);
            _defaultFragment = BuiltInShaders.Fragment;
            _pipelines.Add((_defaultFragment, null, BlendMode.Mix, false, SDL.GPUSampleCount.SampleCount1, false, SDL.GPUTextureFormat.R8G8B8A8Unorm, Mesh.PrimitiveType.Triangles), CreatePipeline(_defaultFragment, BlendMode.Mix));
        }
        catch
        {
            vertex?.Dispose(); instancedVertex?.Dispose(); lightingVertex?.Dispose(); lightingInstanced?.Dispose();
            if (claimed) SDL.ReleaseWindowFromGPUDevice(Device, _window);
            _device.Dispose();
            throw;
        }
    }

    private static (nint Device, bool Relaxed) CreateDevice()
    {
        var formats = ShaderCompiler.GetFormats();
        var device = SDL.CreateGPUDevice(formats, false, null);
        if (device != 0 || !OperatingSystem.IsAndroid() || (formats & SDL.GPUShaderFormat.SPIRV) == 0)
            return (device, false);

        // The canvas does not use these optional Vulkan features; older Android GPUs may lack them.
        var props = SDL.CreateProperties();
        if (props == 0) throw Failure("configure an Android GPU device");
        try
        {
            if (!SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersSPIRVBoolean, true) ||
                !SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateFeatureClipDistanceBoolean, false) ||
                !SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateFeatureDepthClampingBoolean, false) ||
                !SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateFeatureIndirectDrawFirstInstanceBoolean, false) ||
                !SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateFeatureAnisotropyBoolean, false))
                throw Failure("configure optional Android GPU features");
            return (SDL.CreateGPUDeviceWithProperties(props), true);
        }
        finally { SDL.DestroyProperties(props); }
    }

    internal override Vector2i GetPixelSize()
    {
        Check(SDL.GetWindowSizeInPixels(_window, out var width, out var height), "query GPU window size");
        return new(width, height);
    }

    private RenderHandle CreateShader(byte[] code, bool fragment)
    {
        return new RenderHandle(ShaderCompiler.CreateShader(Device, code, fragment), h => SDL.ReleaseGPUShader(Device, h), _device);
    }

    private RenderHandle CreatePipeline(byte[] code, BlendMode blend, bool overwrite = false, bool instanced = false, bool lighting = false, SDL.GPUTextureFormat format = SDL.GPUTextureFormat.R8G8B8A8Unorm, SDL.GPUSampleCount samples = SDL.GPUSampleCount.SampleCount1, bool lcd = false, byte[]? vertexCode = null, Mesh.PrimitiveType primitive = Mesh.PrimitiveType.Triangles)
    {
        using var vertex = vertexCode is null ? null : CreateShader(vertexCode, fragment: false);
        using var fragment = CreateShader(code, fragment: true);
        var buffers = stackalloc SDL.GPUVertexBufferDescription[2];
        buffers[0] = new() { Slot = 0, Pitch = (uint)sizeof(CanvasVertex), InputRate = SDL.GPUVertexInputRate.Vertex };
        buffers[1] = new() { Slot = 1, Pitch = (uint)sizeof(CanvasInstance), InputRate = SDL.GPUVertexInputRate.Instance };
        var attributes = stackalloc SDL.GPUVertexAttribute[16];
        attributes[0] = new() { Location = 0, Format = SDL.GPUVertexElementFormat.Float2, Offset = 0 };
        attributes[1] = new() { Location = 1, Format = SDL.GPUVertexElementFormat.Float4, Offset = 8 };
        attributes[2] = new() { Location = 2, Format = SDL.GPUVertexElementFormat.Float2, Offset = 24 };
        attributes[3] = new() { Location = 3, Format = SDL.GPUVertexElementFormat.Float4, Offset = 32 };
        if (instanced) for (uint i = 3; i < 7; i++) attributes[i] = new() { Location = i, BufferSlot = 1, Format = SDL.GPUVertexElementFormat.Float4, Offset = (i - 3) * 16 };
        attributes[instanced ? 7 : 4] = new() { Location = 7, Format = SDL.GPUVertexElementFormat.Float2, Offset = 48 };
        attributes[instanced ? 8 : 5] = new() { Location = 8, Format = SDL.GPUVertexElementFormat.Float3, Offset = 56 };
        attributes[instanced ? 9 : 6] = new() { Location = 9, Format = SDL.GPUVertexElementFormat.Float4, Offset = 68 };
        for (uint channel = 0; channel < 4; channel++) attributes[(instanced ? 10 : 7) + channel] = new() { Location = 10 + channel, Format = SDL.GPUVertexElementFormat.Float4, Offset = 84 + channel * 16 };
        attributes[instanced ? 14 : 11] = new() { Location = 14, Format = SDL.GPUVertexElementFormat.Float4, Offset = 148 };
        attributes[instanced ? 15 : 12] = new() { Location = 15, Format = SDL.GPUVertexElementFormat.Uint, Offset = 164 };
        var blendState = blend switch
        {
            BlendMode.Mix => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendOp.Add),
            BlendMode.Add => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendOp.Add),
            BlendMode.Sub => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendOp.ReverseSubtract),
            BlendMode.Mul => (SDL.GPUBlendFactor.DstColor, SDL.GPUBlendFactor.Zero, SDL.GPUBlendFactor.DstAlpha, SDL.GPUBlendFactor.Zero, SDL.GPUBlendOp.Add),
            BlendMode.PremultAlpha => (SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendOp.Add),
            _ => throw new ArgumentOutOfRangeException(nameof(blend)),
        };
        if (lcd) blendState = (SDL.GPUBlendFactor.ConstantColor, SDL.GPUBlendFactor.OneMinusSrcColor, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendOp.Add);
        var color = new SDL.GPUColorTargetDescription
        {
            Format = format,
            BlendState = new SDL.GPUColorTargetBlendState
            {
                EnableBlend = !overwrite,
                SrcColorBlendFactor = blendState.Item1,
                DstColorBlendFactor = blendState.Item2,
                ColorBlendOp = blendState.Item5,
                SrcAlphaBlendFactor = blendState.Item3,
                DstAlphaBlendFactor = blendState.Item4,
                AlphaBlendOp = blendState.Item5
            }
        };
        var info = new SDL.GPUGraphicsPipelineCreateInfo
        {
            VertexShader = vertex?.DangerousGetHandle() ?? (lighting ? instanced ? _lightingInstancedVertexShader : _lightingVertexShader : instanced ? _instancedVertexShader : _vertexShader).DangerousGetHandle(),
            FragmentShader = fragment.DangerousGetHandle(),
            PrimitiveType = primitive switch { Mesh.PrimitiveType.Points => SDL.GPUPrimitiveType.PointList, Mesh.PrimitiveType.Lines => SDL.GPUPrimitiveType.LineList, Mesh.PrimitiveType.LineStrip => SDL.GPUPrimitiveType.LineStrip, Mesh.PrimitiveType.TriangleStrip => SDL.GPUPrimitiveType.TriangleStrip, _ => SDL.GPUPrimitiveType.TriangleList },
            // Required when the Android device does not support depth clamping.
            RasterizerState = new() { EnableDepthClip = true },
            MultisampleState = new() { SampleCount = samples },
            VertexInputState = new() { VertexBufferDescriptions = (nint)buffers, NumVertexBuffers = instanced ? 2u : 1u, VertexAttributes = (nint)attributes, NumVertexAttributes = instanced ? 16u : 13u },
            TargetInfo = new() { ColorTargetDescriptions = (nint)(&color), NumColorTargets = 1 }
        };
        return new RenderHandle(SDL.CreateGPUGraphicsPipeline(Device, in info), h => SDL.ReleaseGPUGraphicsPipeline(Device, h), _device);
    }

    internal override void Draw(CanvasRenderTarget output, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, double time, ReadOnlySpan<CanvasInstance> instances, CanvasLightingFrame lighting)
    {
        _currentHDR = output.HDR; _linearSources.Clear();
        if (output.HDR) clear = LinearColor(clear);
        var samples = (SDL.GPUSampleCount)output.MSAA; var format = CanvasFormat(output.HDR);
        _materialMasks = lighting.Masks.Count != 0;
        var size = output.Size; var lightingUsed = false; var sdfUsed = false; var screen = false; var screenMipmaps = false;
        foreach (var batch in batches)
        {
            lightingUsed |= batch.Count != 0 && batch.Lighting is not null && !batch.MaskShader && !batch.GroupShader;
            sdfUsed |= batch.Material?.Program.UsesSDFTexture == true;
            screen |= batch.Operation != CanvasOperation.Draw || batch.Material?.Program.UsesScreenTexture == true;
            screenMipmaps |= batch.MIPMaps || batch.Operation is not (CanvasOperation.GroupEnd or CanvasOperation.MaskEnd) && batch.Material?.Program.UsesScreenTexture == true;
            if (batch.Material?.Program.TimeUniform is not null && !float.IsFinite((float)time))
                throw new InvalidOperationException("The render clock exceeds the finite float32 range required by shader TIME.");
            var code = GlyphCode(batch); var vertexCode = VertexCode(batch); var effectiveBlend = batch.GroupShader ? BlendMode.PremultAlpha : batch.Blend; var key = (code, vertexCode, effectiveBlend, batch.InstanceCount > 0, samples, batch.Glyph.Mode == CanvasGlyphMode.LCD && !batch.MaskShader && !batch.GroupShader, format, batch.Primitive);
            _usedPrograms.Add(key);
            if (!_pipelines.ContainsKey(key)) _pipelines.Add(key, CreatePipeline(code, effectiveBlend, instanced: batch.InstanceCount > 0, lighting: batch.Lighting is not null && batch.Material is not null && !batch.MaskShader && !batch.GroupShader, samples: samples, lcd: batch.Glyph.Mode == CanvasGlyphMode.LCD && !batch.MaskShader && !batch.GroupShader, format: format, vertexCode: vertexCode, primitive: batch.Primitive));
            if (batch.MaskShader) _ = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled);
            if (batch.GroupShader)
            {
                _usedPrograms.Add((_defaultFragment, null, BlendMode.PremultAlpha, false, samples, false, format, Mesh.PrimitiveType.Triangles));
                if (!_pipelines.ContainsKey((_defaultFragment, null, BlendMode.PremultAlpha, false, samples, false, format, Mesh.PrimitiveType.Triangles)))
                    _pipelines.Add((_defaultFragment, null, BlendMode.PremultAlpha, false, samples, false, format, Mesh.PrimitiveType.Triangles), CreatePipeline(_defaultFragment, BlendMode.PremultAlpha, format: format, samples: samples));
                _ = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled);
            }
            if (batch.Material is { } material && material.Textures.Length != 0) { _usedMaterials.Add(material); PrepareTextures(material); }
            if (!batch.GroupShader && UsesCanvasTexture(batch)) _ = CanvasBinding(batch);
            if (batch.Glyph.Mode != CanvasGlyphMode.None) _ = CanvasBinding(batch with { Material = null });
            if (batch.Lighting is not null) PrepareLightingChannels(batch);
            if (batch.Material?.Program.UsesScreenTexture == true) _ = Sampler(TextureFilter.LinearWithMipmaps, TextureRepeat.Disabled);
            if (batch.Operation == CanvasOperation.GroupBegin) _clearPipelines[output.HDR ? 1 : 0, (int)samples] ??= CreatePipeline(_defaultFragment, BlendMode.Mix, overwrite: true, format: format, samples: samples);
        }
        if (screen)
        {
            _ = BackBuffer(output, screenMipmaps);
            if (samples != SDL.GPUSampleCount.SampleCount1) output.MultisampleBackBuffer ??= CreateMultisampleTarget(size, default, output.MSAA, output.HDR);
        }
        if (samples != SDL.GPUSampleCount.SampleCount1)
        { _seedPipelines[output.HDR ? 1 : 0, (int)samples] ??= CreatePipeline(_defaultFragment, BlendMode.Mix, overwrite: true, format: format, samples: samples); _ = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled); }
        if (lightingUsed) PrepareLightingAtlas(lighting);
        if (sdfUsed) PrepareSDF(lighting);
        PrepareMaterialLighting(batches, lighting);
        var lightingBytes = lightingUsed || sdfUsed ? lighting.Bytes : 0;
        var maskFirst = checked(vertices.Length + (sdfUsed ? 6 : 0));
        var seedFirst = checked(maskFirst + (lightingUsed ? lighting.Masks.Count * 6 : 0));
        var colorFirst = checked(seedFirst + (samples != SDL.GPUSampleCount.SampleCount1 ? 6 : 0));
        var colorQuad = _linearSources.Count != 0;
        var vertexBytes = checked((colorFirst + (colorQuad ? 6 : 0)) * sizeof(CanvasVertex));
        var uploadBytes = checked(lightingBytes + vertexBytes + instances.Length * sizeof(CanvasInstance));
        EnsureBuffers(uploadBytes);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire a GPU command buffer");
        nint pass = 0;
        try
        {
            foreach (var texture in _textures.Values) texture.Upload(command);
            foreach (var texture in _layeredTextures.Values) texture.Upload(command);
            if (uploadBytes != 0)
            {
                var mapped = SDL.MapGPUTransferBuffer(Device, _transfer!.DangerousGetHandle(), true);
                if (mapped == 0) throw Failure("map vertex upload memory");
                try
                {
                    if (lightingBytes != 0) lighting.CopyTo(new Span<byte>((void*)mapped, lightingBytes));
                    var vertexData = new Span<CanvasVertex>((void*)(mapped + lightingBytes), vertices.Length); vertices.CopyTo(vertexData);
                    if (output.HDR) foreach (var batch in batches)
                            if (!batch.LinearColors) for (var i = batch.First; i < batch.First + batch.Count; ++i)
                                    // Color/mask passes may share a range; always convert the original source once.
                                    vertexData[i] = vertices[i] with { Color = LinearColor(vertices[i].Color) };
                    foreach (var batch in batches)
                        if (batch.CanvasModulation is { } modulation && modulation != Colors.White)
                        {
                            if (output.HDR) modulation = LinearColor(modulation);
                            for (var i = batch.First; i < batch.First + batch.Count; ++i)
                                if (!(vertexData[i].Color * modulation).IsFinite()) throw new InvalidOperationException("Canvas modulation overflowed finite colors.");
                            for (var i = batch.FirstInstance; batch.GPU.State is null && i < batch.FirstInstance + batch.InstanceCount; ++i)
                                if (!(instances[i].Color * modulation).IsFinite()) throw new InvalidOperationException("Canvas instance modulation overflowed finite colors.");
                        }
                    if (sdfUsed) WriteSDFVertices(new Span<CanvasVertex>((void*)(mapped + lightingBytes + vertices.Length * sizeof(CanvasVertex)), 6), lighting);
                    if (lightingUsed) WriteLightingMaskVertices(new Span<CanvasVertex>((void*)(mapped + lightingBytes + maskFirst * sizeof(CanvasVertex)), lighting.Masks.Count * 6), lighting);
                    if (samples != SDL.GPUSampleCount.SampleCount1) WriteSeedVertices(new Span<CanvasVertex>((void*)(mapped + lightingBytes + seedFirst * sizeof(CanvasVertex)), 6), size);
                    if (colorQuad) WriteSeedVertices(new Span<CanvasVertex>((void*)(mapped + lightingBytes + colorFirst * sizeof(CanvasVertex)), 6), Vector2i.One);
                    var instanceData = new Span<CanvasInstance>((void*)(mapped + lightingBytes + vertexBytes), instances.Length); instances.CopyTo(instanceData);

                }
                finally { SDL.UnmapGPUTransferBuffer(Device, _transfer.DangerousGetHandle()); }
                var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw Failure("begin vertex upload");
                SDL.UploadToGPUBuffer(copy, new SDL.GPUTransferBufferLocation { TransferBuffer = _transfer.DangerousGetHandle() },
                    new SDL.GPUBufferRegion { Buffer = _vertexBuffer!.DangerousGetHandle(), Size = (uint)uploadBytes }, true);
                SDL.EndGPUCopyPass(copy);
            }
            PrepareInstanceStreams(command, batches);
            if (_linearSources.Count != 0) ConvertColorSources(command, lightingBytes, colorFirst);
            if (lightingUsed) CopyLightingAtlas(command, lighting, lightingBytes, maskFirst);
            if (sdfUsed) DrawSDF(command, lighting, lightingBytes, vertices.Length);
            if (!clearEnabled && output.Multisample is null) Blit(command, output.Current.DangerousGetHandle(), output.Next.DangerousGetHandle(), new(0, 0, size.X, size.Y));
            pass = BeginCanvasPass(command, output, false, clearEnabled ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load, clear);
            var inGroup = false; var copied = false;
            var maskSamplers = stackalloc SDL.GPUTextureSamplerBinding[2];
            var vertexSamplers = stackalloc SDL.GPUTextureSamplerBinding[16];
            var bindings = stackalloc SDL.GPUBufferBinding[2];
            var batchIndex = 0;
            foreach (var batch in batches)
            {
                var streamIndex = _streamByBatch.Length > batchIndex ? _streamByBatch[batchIndex] : -1;
                var lightingOffset = _lightingOffsets[batchIndex++];
                if (batch.Operation == CanvasOperation.Copy)
                {
                    SDL.EndGPURenderPass(pass); pass = 0;
                    if (batch.Region.HasArea()) Blit(command, output.Next.DangerousGetHandle(), output.BackBuffer!.DangerousGetHandle(), batch.Region);
                    if (screenMipmaps) SDL.GenerateMipmapsForGPUTexture(command, output.BackBuffer!.DangerousGetHandle());
                    copied = true; pass = BeginCanvasPass(command, output, false, SDL.GPULoadOp.Load, default);
                    continue;
                }
                if (batch.Operation == CanvasOperation.MaskFinish) continue;
                if (batch.Operation == CanvasOperation.MaskBegin)
                {
                    SDL.EndGPURenderPass(pass); pass = 0; inGroup = true;
                    if (batch.Region.HasArea()) Blit(command, output.Next.DangerousGetHandle(), output.BackBuffer!.DangerousGetHandle(), batch.Region);
                    if (output.MultisampleBackBuffer is not null) SeedBackBuffer(command, output, seedFirst, lightingBytes);
                    pass = BeginCanvasPass(command, output, true, SDL.GPULoadOp.Load, default);
                    continue;
                }
                if (batch.Operation == CanvasOperation.GroupBegin)
                {
                    SDL.EndGPURenderPass(pass); pass = 0; inGroup = true;
                    if (output.MultisampleBackBuffer is not null) SeedBackBuffer(command, output, seedFirst, lightingBytes);
                    pass = BeginCanvasPass(command, output, true, SDL.GPULoadOp.Load, default);
                }
                if (batch.Operation is CanvasOperation.GroupEnd or CanvasOperation.MaskEnd && inGroup)
                {
                    SDL.EndGPURenderPass(pass); pass = 0; inGroup = false;
                    if (batch.MIPMaps) SDL.GenerateMipmapsForGPUTexture(command, output.BackBuffer!.DangerousGetHandle());
                    pass = BeginCanvasPass(command, output, false, SDL.GPULoadOp.Load, default);
                }
                if (batch.Material?.Program.UsesScreenTexture == true && batch.Operation is not (CanvasOperation.GroupEnd or CanvasOperation.MaskEnd) && !copied)
                {
                    SDL.EndGPURenderPass(pass); pass = 0;
                    Blit(command, output.Next.DangerousGetHandle(), output.BackBuffer!.DangerousGetHandle(), new(0, 0, size.X, size.Y));
                    if (screenMipmaps) SDL.GenerateMipmapsForGPUTexture(command, output.BackBuffer!.DangerousGetHandle());
                    copied = true; pass = BeginCanvasPass(command, output, false, SDL.GPULoadOp.Load, default);
                }
                if (batch.Count == 0) continue;
                var clip = batch.Clip ?? new Rect2i(0, 0, size.X, size.Y);
                if (!clip.HasArea()) continue;
                var scissor = new SDL.Rect { X = clip.Position.X, Y = clip.Position.Y, W = clip.Size.X, H = clip.Size.Y }; SDL.SetGPUScissor(pass, in scissor);
                var dimensions = new Vector4(size.X, size.Y, BitConverter.Int32BitsToSingle(lightingOffset), BitConverter.Int32BitsToSingle((batch.SnapGeneratedVertices ? 1 : 0) | ((batch.Procedural || batch.GPU.State is not null) && batch.GroupShader ? 2 : 0))); var canvasColor = batch.CanvasModulation is { } modulation ? output.HDR ? LinearColor(modulation) : modulation : Colors.White;
                PushFrame(command, dimensions, canvasColor);
                bindings[0] = new() { Buffer = _vertexBuffer!.DangerousGetHandle(), Offset = (uint)lightingBytes };
                bindings[1] = new() { Buffer = _vertexBuffer.DangerousGetHandle(), Offset = checked((uint)(lightingBytes + vertexBytes + batch.FirstInstance * sizeof(CanvasInstance))) };
                if (batch.GPU.State is not null && streamIndex >= 0) bindings[1] = new() { Buffer = _instanceStreams[streamIndex].Instances!.DangerousGetHandle() };
                var bindingCount = batch.InstanceCount > 0 ? 2 : 1;
                SDL.BindGPUVertexBuffers(pass, 0, new ReadOnlySpan<SDL.GPUBufferBinding>(bindings, bindingCount), (uint)bindingCount);
                var pipeline = batch.Operation == CanvasOperation.GroupBegin ? _clearPipelines[output.HDR ? 1 : 0, (int)samples]! : _pipelines[(GlyphCode(batch), VertexCode(batch), batch.GroupShader ? BlendMode.PremultAlpha : batch.Blend, batch.InstanceCount > 0, samples, batch.Glyph.Mode == CanvasGlyphMode.LCD && !batch.MaskShader && !batch.GroupShader, format, batch.Primitive)];
                SDL.BindGPUGraphicsPipeline(pass, pipeline.DangerousGetHandle());
                if (batch.Glyph.Mode == CanvasGlyphMode.LCD && !batch.MaskShader && !batch.GroupShader)
                { var tint = (output.HDR ? LinearColor(batch.GlyphModulation) : batch.GlyphModulation) * canvasColor; SDL.SetGPUBlendConstants(pass, new() { R = tint.R, G = tint.G, B = tint.B, A = tint.A }); }
                batch.Material?.PushUniforms(command, (float)time, new(1f / size.X, 1f / size.Y), SDFCoords(lighting), batch.InstanceOwner);
                if (batch.Material is { Textures.Length: > 0 } textured)
                {
                    var samplers = _textureBindings[textured];
                    for (var i = 0; i < samplers.Length; i++)
                        if (textured.Program.Textures[i].IsSDFTexture) samplers[i] = new() { Texture = _sdfTarget!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Linear, TextureRepeat.Disabled) };
                        else if (textured.Program.Textures[i].IsScreenTexture) samplers[i] = new() { Texture = output.BackBuffer!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.LinearWithMipmaps, TextureRepeat.Disabled) };
                        else if (textured.Program.Textures[i].IsCanvasTexture) samplers[i] = CanvasBinding(batch);
                    if (VertexCode(batch) is not null)
                    {
                        var indices = textured.Program.VertexTextureIndices;
                        for (var i = 0; i < indices.Length; i++) vertexSamplers[i] = samplers[indices[i]];
                        if (indices.Length != 0) SDL.BindGPUVertexSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(vertexSamplers, indices.Length), (uint)indices.Length);
                    }
                }
                if (batch.MaskShader)
                {
                    var inverse = new Vector4(1f / size.X, 1f / size.Y, 0, 0);
                    SDL.PushGPUFragmentUniformData(command, 0, (nint)(&inverse), (uint)sizeof(Vector4));
                    var samplers = maskSamplers;
                    samplers[0] = CanvasBinding(batch);
                    samplers[1] = new() { Texture = output.BackBuffer!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled) };
                    SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(samplers, 2), 2);
                }
                else if (batch.GroupShader)
                {
                    var groupSampler = new SDL.GPUTextureSamplerBinding { Texture = output.BackBuffer!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled) };
                    SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(&groupSampler, 1), 1);
                }
                else if (batch.Lighting is not null && batch.Material is null) BindLighting(command, pass, batch, lighting);
                else if (batch.Material is { Textures.Length: > 0 } fragmentTextures && fragmentTextures.Program.FragmentTextureCount != 0)
                    SDL.BindGPUFragmentSamplers(pass, 0, _textureBindings[fragmentTextures].AsSpan(0, fragmentTextures.Program.FragmentTextureCount), (uint)fragmentTextures.Program.FragmentTextureCount);
                else if (batch.Material is null)
                {
                    var sampler = batch.GroupShader ? new SDL.GPUTextureSamplerBinding { Texture = output.BackBuffer!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled) } : CanvasBinding(batch);
                    SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(&sampler, 1), 1);
                }
                if (batch.Lighting is not null && batch.Material is not null && !batch.MaskShader && !batch.GroupShader) BindMaterialLighting(pass, batch);
                BindGlyphTexture(pass, batch);
                if (batch.GPU.State is not null && streamIndex >= 0)
                {
                    if (batch.GPU.Indices is not null)
                    {
                        var indexBinding = new SDL.GPUBufferBinding { Buffer = _instanceIndices!.DangerousGetHandle() }; SDL.BindGPUIndexBuffer(pass, in indexBinding, SDL.GPUIndexElementSize.IndexElementSize32Bit);
                        var count = batch.GPU.LODs is { Length: > 0 } ? (uint)batch.GPU.Capacity : 1u;
                        if (_relaxedAndroidDevice && count > 1)
                            for (uint i = 0; i < count; i++) { var instanceBinding = new SDL.GPUBufferBinding { Buffer = _instanceStreams[streamIndex].Instances!.DangerousGetHandle(), Offset = i * 64 }; SDL.BindGPUVertexBuffers(pass, 1, new ReadOnlySpan<SDL.GPUBufferBinding>(in instanceBinding), 1); SDL.DrawGPUIndexedPrimitivesIndirect(pass, _instanceStreams[streamIndex].Commands!.DangerousGetHandle(), i * 20, 1); }
                        else SDL.DrawGPUIndexedPrimitivesIndirect(pass, _instanceStreams[streamIndex].Commands!.DangerousGetHandle(), 0, count);
                    }
                    else SDL.DrawGPUPrimitivesIndirect(pass, _instanceStreams[streamIndex].Commands!.DangerousGetHandle(), 0, 1);
                }
                else SDL.DrawGPUPrimitives(pass, (uint)batch.Count, (uint)Math.Max(1, batch.InstanceCount), (uint)batch.First, 0);
            }
            SDL.EndGPURenderPass(pass); pass = 0;
            var submitted = command; command = 0; Check(SDL.SubmitGPUCommandBuffer(submitted), "submit canvas frame");
            foreach (var texture in _textures.Values) texture.CommitUpload();
            foreach (var texture in _layeredTextures.Values) texture.CommitUpload(); output.Commit();
        }
        finally
        {
            if (pass != 0) SDL.EndGPURenderPass(pass);
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
        }
    }
    private nint BeginPass(nint command, nint texture, SDL.GPULoadOp load, Color clear)
    {
        var target = new SDL.GPUColorTargetInfo { Texture = texture, ClearColor = new() { R = clear.R, G = clear.G, B = clear.B, A = clear.A }, LoadOp = load, StoreOp = SDL.GPUStoreOp.Store, Cycle = false };
        var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&target, 1), 1, 0);
        if (pass == 0) throw Failure("begin canvas render pass"); return pass;
    }
    private static void Blit(nint command, nint source, nint destination, Rect2i rect)
    {
        var blit = new SDL.GPUBlitInfo { Source = new() { Texture = source, X = (uint)rect.Position.X, Y = (uint)rect.Position.Y, W = (uint)rect.Size.X, H = (uint)rect.Size.Y }, Destination = new() { Texture = destination, X = (uint)rect.Position.X, Y = (uint)rect.Position.Y, W = (uint)rect.Size.X, H = (uint)rect.Size.Y }, LoadOp = SDL.GPULoadOp.Load, Filter = SDL.GPUFilter.Nearest };
        SDL.BlitGPUTexture(command, in blit);
    }

    private nint Sampler(TextureFilter filter, TextureRepeat repeat, int anisotropy = 1, bool integer = false)
    {
        if (integer && (filter is TextureFilter.Linear or TextureFilter.LinearWithMipmaps or TextureFilter.LinearWithMipmapsAnisotropic || filter >= TextureFilter.NearestWithMipmapsAnisotropic && anisotropy > 1)) throw new NotSupportedException("Integer canvas images require nearest filtering without anisotropy.");
        var nearestMipmaps = integer || _nearestMipmaps;
        var anisotropic = filter >= TextureFilter.NearestWithMipmapsAnisotropic && anisotropy > 1;
        if (_relaxedAndroidDevice && anisotropic)
            throw new NotSupportedException("Anisotropic texture filtering is unavailable on this Android GPU device.");
        var key = (filter, repeat, anisotropic ? anisotropy : 1, nearestMipmaps);
        if (!_samplers.TryGetValue(key, out var sampler))
        {
            var linear = filter is TextureFilter.Linear or TextureFilter.LinearWithMipmaps or TextureFilter.LinearWithMipmapsAnisotropic;
            var address = repeat switch
            {
                TextureRepeat.Enabled => SDL.GPUSamplerAddressMode.Repeat,
                TextureRepeat.Mirror => SDL.GPUSamplerAddressMode.MirroredRepeat,
                _ => SDL.GPUSamplerAddressMode.ClampToEdge,
            };
            var info = new SDL.GPUSamplerCreateInfo
            {
                MinFilter = linear ? SDL.GPUFilter.Linear : SDL.GPUFilter.Nearest,
                MagFilter = linear ? SDL.GPUFilter.Linear : SDL.GPUFilter.Nearest,
                MipmapMode = nearestMipmaps ? SDL.GPUSamplerMipmapMode.Nearest : SDL.GPUSamplerMipmapMode.Linear,
                AddressModeU = address,
                AddressModeV = address,
                AddressModeW = SDL.GPUSamplerAddressMode.ClampToEdge,
                MaxLod = filter >= TextureFilter.NearestWithMipmaps ? 1000 : 0,
                EnableAnisotropy = anisotropic,
                MaxAnisotropy = anisotropic ? anisotropy : 1,
            };
            sampler = new RenderHandle(SDL.CreateGPUSampler(Device, in info), h => SDL.ReleaseGPUSampler(Device, h), _device);
            _samplers.Add(key, sampler);
        }
        return sampler.DangerousGetHandle();
    }

    private static bool UsesCanvasTexture(CanvasBatch batch) => batch.Material is null ||
        batch.Material.Program.Textures.Any(t => t.IsCanvasTexture);

    private SDL.GPUTextureSamplerBinding CanvasBinding(CanvasBatch batch, bool color = true)
    {
        var texture = batch.Texture;
        texture = (Texture?)ShaderTexture.Source(texture);
        if (texture is null)
        {
            if (_whiteTexture is null)
            {
                using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
                image.Fill(Colors.White);
                _whiteTexture = ImageTexture.CreateFromImage(image);
                _whiteTexture.RetainRendererCache = true;
            }
            texture = _whiteTexture;
        }
        var scalar = ShaderTexture.ResourcePixelType(texture);
        if (batch.Material is { } material && Array.FindIndex(material.Program.Textures, t => t.IsCanvasTexture) is var canvasIndex && canvasIndex >= 0 && material.Program.Textures[canvasIndex].PixelType != scalar) throw new ArgumentException("The canvas image scalar type does not match TEXTURE.");
        var handle = PrepareTexture(texture);
        if (_currentHDR && color && scalar == 0 && batch.Glyph.Mode == CanvasGlyphMode.None) handle = LinearTexture(texture, handle);
        return new() { Texture = handle, Sampler = Sampler(batch.Filter, batch.Repeat, batch.MaxAnisotropy, scalar != 0) };
    }

    private nint PrepareTexture(Texture texture)
    {
        if (texture is ServerTexture { IsProxy: true } proxy) texture = RenderingTextureRegistry.ResolveProxySource(proxy.ProxyTarget) ?? RenderingTextureRegistry.PlaceholderTexture;
        if (texture is CanvasTexture channels) texture = ShaderTexture.Source(channels) as Texture ?? RenderingTextureRegistry.PlaceholderTexture;
        if (texture is AtlasTexture atlas) texture = atlas.RenderingTexture
            ?? throw new InvalidOperationException("A sampled atlas has no source texture.");
        if (texture is ViewportTexture viewportTexture)
        {
            var viewport = viewportTexture.Bound ?? throw new InvalidOperationException("The sampled viewport texture is unresolved.");
            return (FindTarget(viewport) ?? throw new InvalidOperationException("The sampled viewport has no native target.")).Current.DangerousGetHandle();
        }
        if (texture is DrawableTexture drawable) return drawable.NativeTexture(Device);
        if (texture is TextureRD deviceTexture && deviceTexture.NativeTexture(Device) is var native && native != 0) return native;
        if (_usedTextures.Add(texture))
        {
            var pixels = texture.CapturePixels() ?? RenderingTextureRegistry.PlaceholderPixels;
            if (!_textures.TryGetValue(texture, out var gpu) || !ReferenceEquals(gpu.Pixels.Allocation, pixels.Allocation))
            {
                var replacement = new GPUTexture(_device, pixels);
                gpu?.Dispose(); _textures[texture] = replacement;
            }
            else gpu.Pixels = pixels;
        }
        return _textures[texture].Handle;
    }

    private nint PrepareLayeredTexture(TextureLayered texture)
    {
        if (texture is LayeredTextureProxy proxy) texture = RenderingTextureRegistry.ResolveLayeredProxySource(proxy.ProxyTarget) ?? throw new InvalidOperationException("The sampled layered proxy has no live source.");
        if (texture is TextureLayeredRD deviceTexture && deviceTexture.NativeTexture(Device) is var native && native != 0) return native;
        if (_usedLayeredTextures.Add(texture))
        {
            if (texture.GetLayeredType() != TextureLayered.LayeredType.Array) throw new NotSupportedException("Only independent image-array sampling is applicable.");
            var pixels = texture.CaptureLayers() ?? RenderingTextureRegistry.PlaceholderLayers;
            if (!_layeredTextures.TryGetValue(texture, out var gpu) || !ReferenceEquals(gpu.ArrayPixels!.Allocation, pixels.Allocation))
            {
                var replacement = new GPUTexture(_device, pixels);
                gpu?.Dispose(); _layeredTextures[texture] = replacement;
            }
            else gpu.ArrayPixels = pixels;
        }
        return _layeredTextures[texture].Handle;
    }

    private static byte[]? VertexCode(CanvasBatch batch) => (batch.GroupShader || batch.MaskShader) && !batch.Procedural ? null : batch.Material?.Program.VertexCode(batch.InstanceCount > 0, batch.Lighting is not null && !batch.MaskShader && !batch.GroupShader, batch.Procedural);

    private void PrepareTextures(MaterialState material)
    {
        if (!_textureBindings.TryGetValue(material, out var bindings))
            _textureBindings.Add(material, bindings = new SDL.GPUTextureSamplerBinding[material.Textures.Length]);
        try
        {
            material.CopyTextures(_textureScratch);
            for (var i = 0; i < bindings.Length; i++)
            {
                if (material.Program.Textures[i].IsEngineTexture) continue;
                bindings[i] = new SDL.GPUTextureSamplerBinding { Texture = _textureScratch[i] is TextureLayered array ? PrepareLayeredTexture(array) : PrepareTexture((Texture)_textureScratch[i]!), Sampler = Sampler(material.Program.Textures[i].PixelType == 0 ? TextureFilter.Linear : TextureFilter.Nearest, TextureRepeat.Disabled, integer: material.Program.Textures[i].PixelType != 0) };
            }
        }
        finally { Array.Clear(_textureScratch); }
    }

    internal override void SetWindowVisible(DisplayServer display, bool visible)
    {
        if (visible && _waylandRemapBlocked)
            throw new NotSupportedException("This Wayland Vulkan profile cannot safely remap a previously presented surface; native presentation-completion/unmap acknowledgement is required.");
        Check(SDL.WaitForGPUIdle(Device), "synchronize window visibility");
        if (!visible && _hasPresented && Driver == "vulkan" && SDL.GetCurrentVideoDriver() == "wayland") _waylandRemapBlocked = true;
        if (_windowClaimed) { SDL.ReleaseWindowFromGPUDevice(Device, _window); _windowClaimed = false; }
        display.SetWindowVisible(visible);
        if (visible) { Check(SDL.ClaimWindowForGPUDevice(Device, _window), "reclaim visible window"); _windowClaimed = true; SetVSync(_vSync); }
    }
    internal override void Sync(CanvasRenderTarget? target) => Check(SDL.WaitForGPUIdle(Device), "synchronize canvas GPU");

    internal override void BeginFrame() { (_usedPrograms, _previousPrograms) = (_previousPrograms, _usedPrograms); _usedLinearViews.Clear(); _usedGlyphPrograms.Clear(); _usedLitPrograms.Clear(); _usedPrograms.Clear(); _usedPrograms.Add((_defaultFragment, null, BlendMode.Mix, false, SDL.GPUSampleCount.SampleCount1, false, SDL.GPUTextureFormat.R8G8B8A8Unorm, Mesh.PrimitiveType.Triangles)); _usedTextures.Clear(); _usedLayeredTextures.Clear(); _usedMaterials.Clear(); }
    internal override void EndFrame()
    {
        RetireColorViews();
        foreach (var pair in _glyphPrograms) if (!_usedGlyphPrograms.Contains(pair.Key)) _unusedGlyphPrograms.Add(pair.Key);
        foreach (var key in _unusedGlyphPrograms) _glyphPrograms.Remove(key); _unusedGlyphPrograms.Clear();
        foreach (var pair in _pipelines) if (!_usedPrograms.Contains(pair.Key) && !_previousPrograms.Contains(pair.Key) && !ReferenceEquals(pair.Key.Code, _defaultFragment) && !ReferenceEquals(pair.Key.Code, BuiltInShaders.Clip(0)) && !ReferenceEquals(pair.Key.Code, BuiltInShaders.Clip(1)) && !ReferenceEquals(pair.Key.Code, BuiltInShaders.Clip(2))) { pair.Value.Dispose(); _pipelines.Remove(pair.Key); }
        foreach (var pair in _textures) if (!_usedTextures.Contains(pair.Key) && (!pair.Key.RetainRendererCache || pair.Key.IsDisposed)) { pair.Value.Dispose(); _textures.Remove(pair.Key); }
        foreach (var pair in _layeredTextures) if (!_usedLayeredTextures.Contains(pair.Key) && (!pair.Key.RetainRendererCache || pair.Key.IsDisposed)) { pair.Value.Dispose(); _layeredTextures.Remove(pair.Key); }
        foreach (var pair in _textureBindings) if (!_usedMaterials.Contains(pair.Key)) _textureBindings.Remove(pair.Key);
        foreach (var pair in _litPrograms) if (!_usedLitPrograms.Contains(pair.Key)) _litPrograms.Remove(pair.Key);
    }
    internal override void ValidateMSAA(Viewport.MSAA mode)
    { if (!SDL.GPUTextureSupportsSampleCount(Device, SDL.GPUTextureFormat.R8G8B8A8Unorm, (SDL.GPUSampleCount)mode)) throw new NotSupportedException("The GPU cannot use the requested RGBA8 canvas coverage sample count."); }
    private static SDL.GPUTextureFormat CanvasFormat(bool HDR) => HDR ? SDL.GPUTextureFormat.R16G16B16A16Float : SDL.GPUTextureFormat.R8G8B8A8Unorm;
    internal override void ValidateHDR(bool enabled)
    {
        if (enabled && !SDL.GPUTextureSupportsFormat(Device, CanvasFormat(true), SDL.GPUTextureType.TextureType2D, SDL.GPUTextureUsageFlags.Sampler | SDL.GPUTextureUsageFlags.ColorTarget))
            throw new NotSupportedException("The GPU cannot sample and render half-float HDR canvas targets.");
    }
    internal override RenderHandle CreateMultisampleTarget(Vector2i size, Color clear, Viewport.MSAA mode, bool HDR = false) => CreateGPUTarget(size, clear, false, (SDL.GPUSampleCount)mode, HDR);
    internal override RenderHandle CreateTarget(Vector2i size, Color clear, bool mipmaps = false, bool HDR = false) => CreateGPUTarget(size, clear, mipmaps, SDL.GPUSampleCount.SampleCount1, HDR);
    private RenderHandle CreateGPUTarget(Vector2i size, Color clear, bool mipmaps, SDL.GPUSampleCount samples, bool HDR, uint levels = 0)
    {
        var format = CanvasFormat(HDR); if (HDR) clear = LinearColor(clear);
        if (!SDL.GPUTextureSupportsSampleCount(Device, format, samples)) throw new NotSupportedException("The GPU cannot use the requested canvas format and coverage sample count.");
        var usage = SDL.GPUTextureUsageFlags.ColorTarget;
        if (samples == SDL.GPUSampleCount.SampleCount1) usage |= SDL.GPUTextureUsageFlags.Sampler;
        var storage = SDL.GPUTextureUsageFlags.GraphicsStorageRead | SDL.GPUTextureUsageFlags.ComputeStorageRead | SDL.GPUTextureUsageFlags.ComputeStorageWrite | SDL.GPUTextureUsageFlags.ComputeStorageSimultaneousReadWrite;
        if (samples == SDL.GPUSampleCount.SampleCount1 && SDL.GPUTextureSupportsFormat(Device, format, SDL.GPUTextureType.TextureType2D, usage | storage)) usage |= storage;
        var info = new SDL.GPUTextureCreateInfo { Type = SDL.GPUTextureType.TextureType2D, Format = format, Width = (uint)size.X, Height = (uint)size.Y, SampleCount = samples, LayerCountOrDepth = 1, NumLevels = levels != 0 ? levels : mipmaps ? (uint)(1 + System.Numerics.BitOperations.Log2((uint)Math.Max(size.X, size.Y))) : 1, Usage = usage };
        var target = new RenderHandle(SDL.CreateGPUTexture(Device, in info), h => SDL.ReleaseGPUTexture(Device, h), _device);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        try
        {
            if (command == 0) throw Failure("initialize canvas target");
            var color = new SDL.GPUColorTargetInfo { Texture = target.DangerousGetHandle(), ClearColor = new() { R = clear.R, G = clear.G, B = clear.B, A = clear.A }, LoadOp = SDL.GPULoadOp.Clear, StoreOp = SDL.GPUStoreOp.Store };
            var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&color, 1), 1, 0); if (pass == 0) throw Failure("clear initial canvas target"); SDL.EndGPURenderPass(pass);
            var submitted = command; command = 0; Check(SDL.SubmitGPUCommandBuffer(submitted), "initialize canvas target"); return target;
        }
        catch { target.Dispose(); throw; }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private void EnsureBuffers(int size)
    {
        if (size <= _bufferSize) return;
        size = Math.Max(size, checked(Math.Max(4096, _bufferSize) * 2));
        var bufferInfo = new SDL.GPUBufferCreateInfo { Usage = SDL.GPUBufferUsageFlags.Vertex | SDL.GPUBufferUsageFlags.GraphicsStorageRead, Size = (uint)size };
        var buffer = new RenderHandle(SDL.CreateGPUBuffer(Device, in bufferInfo), h => SDL.ReleaseGPUBuffer(Device, h), _device);
        RenderHandle transfer;
        try
        {
            var transferInfo = new SDL.GPUTransferBufferCreateInfo { Usage = SDL.GPUTransferBufferUsage.Upload, Size = (uint)size };
            transfer = new RenderHandle(SDL.CreateGPUTransferBuffer(Device, in transferInfo), h => SDL.ReleaseGPUTransferBuffer(Device, h), _device);
        }
        catch { buffer.Dispose(); throw; }
        _vertexBuffer?.Dispose(); _transfer?.Dispose();
        _vertexBuffer = buffer; _transfer = transfer; _bufferSize = size;
    }

    internal override Image Readback(CanvasRenderTarget output, bool backBuffer = false)
    {
        if (!output.HasFrame) throw new InvalidOperationException("No canvas frame has completed.");
        var bytesPerPixel = output.BytesPerPixel;
        if ((long)output.Size.X * output.Size.Y * bytesPerPixel > Array.MaxLength) throw new NotSupportedException("Canvas readback exceeds the managed image storage limit.");
        var pitch = checked((output.Size.X + 63) / 64 * 64);
        var info = new SDL.GPUTransferBufferCreateInfo { Usage = SDL.GPUTransferBufferUsage.Download, Size = checked((uint)((long)pitch * output.Size.Y * bytesPerPixel)) };
        using var transfer = new RenderHandle(SDL.CreateGPUTransferBuffer(Device, in info), h => SDL.ReleaseGPUTransferBuffer(Device, h), _device);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire readback commands");
        nint fence;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin framebuffer readback");
            SDL.DownloadFromGPUTexture(copy, new SDL.GPUTextureRegion { Texture = (backBuffer ? output.BackBuffer ?? throw new InvalidOperationException("No screen backbuffer is available.") : output.Current).DangerousGetHandle(), W = (uint)output.Size.X, H = (uint)output.Size.Y, D = 1 },
                new SDL.GPUTextureTransferInfo { TransferBuffer = transfer.DangerousGetHandle(), PixelsPerRow = (uint)pitch, RowsPerLayer = (uint)output.Size.Y });
            SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0;
            fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit framebuffer readback");
        }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
        using var fenceHandle = new RenderHandle(fence, h => SDL.ReleaseGPUFence(Device, h), _device);
        Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for framebuffer readback");
        var mapped = SDL.MapGPUTransferBuffer(Device, transfer.DangerousGetHandle(), false);
        if (mapped == 0) throw Failure("map framebuffer readback");
        try
        {
            var pixels = new byte[checked(output.Size.X * output.Size.Y * bytesPerPixel)];
            for (var y = 0; y < output.Size.Y; y++) Marshal.Copy(mapped + y * pitch * bytesPerPixel, pixels, y * output.Size.X * bytesPerPixel, output.Size.X * bytesPerPixel);
            return Image.CreateFromData(output.Size.X, output.Size.Y, false, output.ImageFormat, pixels);
        }
        finally { SDL.UnmapGPUTransferBuffer(Device, transfer.DangerousGetHandle()); }
    }

    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var idle = SDL.WaitForGPUIdle(Device);
        foreach (var pipeline in _pipelines.Values) pipeline.Dispose();
        foreach (var texture in _textures.Values) texture.Dispose();
        foreach (var texture in _layeredTextures.Values) texture.Dispose();
        _layeredTextures.Clear(); _usedLayeredTextures.Clear();
        _textures.Clear(); _usedTextures.Clear(); _textureBindings.Clear(); _usedMaterials.Clear();
        foreach (var sampler in _samplers.Values) sampler.Dispose();
        _samplers.Clear(); DisposeColorViews(); _whiteTexture?.Dispose(); _lightAtlas?.Dispose();
        foreach (var pipeline in _lightMaskPipelines) pipeline?.Dispose();
        _sdfTarget?.Dispose(); _sdfPipeline?.Dispose();
        _litPrograms.Clear(); _usedLitPrograms.Clear();
        ReleaseInstanceStreams(); _pipelines.Clear(); foreach (var pipeline in _clearPipelines) pipeline?.Dispose();
        foreach (var pipeline in _seedPipelines) pipeline?.Dispose(); ReleaseTargets(); _vertexBuffer?.Dispose(); _transfer?.Dispose(); _vertexShader.Dispose(); _instancedVertexShader.Dispose(); _lightingVertexShader.Dispose(); _lightingInstancedVertexShader.Dispose();
        if (_windowClaimed) SDL.ReleaseWindowFromGPUDevice(Device, _window);
        _device.Dispose();
        if (!idle) throw Failure("wait for GPU shutdown");
    }
}
