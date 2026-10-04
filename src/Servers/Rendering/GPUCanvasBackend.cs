using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe class GPUCanvasBackend : CanvasBackend
{
    private readonly RenderHandle _device;
    private readonly nint _window;
    private readonly RenderHandle _vertexShader;
    private readonly byte[] _defaultFragment;
    private readonly Dictionary<(byte[] Code, BlendMode Blend), RenderHandle> _pipelines = [];
    private readonly HashSet<(byte[] Code, BlendMode Blend)> _usedPrograms = [];
    private readonly Dictionary<Texture, GPUTexture> _textures = [];
    private readonly HashSet<Texture> _usedTextures = [];
    private readonly Dictionary<MaterialState, SDL.GPUTextureSamplerBinding[]> _textureBindings = [];
    private readonly HashSet<MaterialState> _usedMaterials = [];
    private readonly Texture?[] _textureScratch = new Texture?[16];
    private readonly Dictionary<(TextureFilter, TextureRepeat, int, bool), RenderHandle> _samplers = [];
    private readonly bool _nearestMipmaps = ProjectSettings.GetWithOverride(ProjectSettings.UseNearestMipmapFilter);
    private ImageTexture? _whiteTexture;
    private RenderHandle? _clearPipeline;
    private RenderHandle? _vertexBuffer;
    private RenderHandle? _transfer;
    private int _bufferSize;
    private bool _disposed, _windowClaimed, _hasPresented, _waylandRemapBlocked;
    private readonly bool _relaxedAndroidDevice;
    internal override string Method => "gpu";
    internal override string Driver { get; }
    private nint Device => _device.DangerousGetHandle();

    internal GPUCanvasBackend(SafeHandle window)
    {
        _window = window.DangerousGetHandle();
        var (device, relaxed) = CreateDevice();
        _relaxedAndroidDevice = relaxed;
        _device = new RenderHandle(device, SDL.DestroyGPUDevice, window);
        var claimed = false;
        RenderHandle? vertex = null;
        try
        {
            Check(SDL.ClaimWindowForGPUDevice(Device, _window), "claim window for GPU rendering");
            claimed = _windowClaimed = true;
            Driver = SDL.GetGPUDeviceDriver(Device) ?? "unknown";
            vertex = CreateShader(BuiltInShaders.Vertex, fragment: false);
            _vertexShader = vertex;
            _defaultFragment = BuiltInShaders.Fragment;
            _pipelines.Add((_defaultFragment, BlendMode.Mix), CreatePipeline(_defaultFragment, BlendMode.Mix));
        }
        catch
        {
            vertex?.Dispose();
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

    private RenderHandle CreatePipeline(byte[] code, BlendMode blend, bool overwrite = false)
    {
        using var fragment = CreateShader(code, fragment: true);
        var buffer = new SDL.GPUVertexBufferDescription { Slot = 0, Pitch = (uint)sizeof(CanvasVertex), InputRate = SDL.GPUVertexInputRate.Vertex };
        var attributes = stackalloc SDL.GPUVertexAttribute[4];
        attributes[0] = new() { Location = 0, Format = SDL.GPUVertexElementFormat.Float2, Offset = 0 };
        attributes[1] = new() { Location = 1, Format = SDL.GPUVertexElementFormat.Float4, Offset = 8 };
        attributes[2] = new() { Location = 2, Format = SDL.GPUVertexElementFormat.Float2, Offset = 24 };
        attributes[3] = new() { Location = 3, Format = SDL.GPUVertexElementFormat.Float4, Offset = 32 };
        var blendState = blend switch
        {
            BlendMode.Mix => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendOp.Add),
            BlendMode.Add => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendOp.Add),
            BlendMode.Sub => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendOp.ReverseSubtract),
            BlendMode.Mul => (SDL.GPUBlendFactor.DstColor, SDL.GPUBlendFactor.Zero, SDL.GPUBlendFactor.DstAlpha, SDL.GPUBlendFactor.Zero, SDL.GPUBlendOp.Add),
            BlendMode.PremultAlpha => (SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendOp.Add),
            _ => throw new ArgumentOutOfRangeException(nameof(blend)),
        };
        var color = new SDL.GPUColorTargetDescription
        {
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
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
            VertexShader = _vertexShader.DangerousGetHandle(),
            FragmentShader = fragment.DangerousGetHandle(),
            PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
            // Required when the Android device does not support depth clamping.
            RasterizerState = new() { EnableDepthClip = true },
            VertexInputState = new() { VertexBufferDescriptions = (nint)(&buffer), NumVertexBuffers = 1, VertexAttributes = (nint)attributes, NumVertexAttributes = 4 },
            TargetInfo = new() { ColorTargetDescriptions = (nint)(&color), NumColorTargets = 1 }
        };
        return new RenderHandle(SDL.CreateGPUGraphicsPipeline(Device, in info), h => SDL.ReleaseGPUGraphicsPipeline(Device, h), _device);
    }

    internal override void Draw(CanvasRenderTarget output, ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool clearEnabled, bool present, double time)
    {
        var size = output.Size; var screen = false; var screenMipmaps = false;
        foreach (var batch in batches)
        {
            screen |= batch.Operation != CanvasOperation.Draw || batch.Material?.Program.UsesScreenTexture == true;
            screenMipmaps |= batch.Mipmaps || batch.Operation is not (CanvasOperation.GroupEnd or CanvasOperation.MaskEnd) && batch.Material?.Program.UsesScreenTexture == true;
            if (batch.Material?.Program.TimeUniform is not null && !float.IsFinite((float)time))
                throw new InvalidOperationException("The render clock exceeds the finite float32 range required by shader TIME.");
            var code = batch.MaskShader ? BuiltInShaders.Clip : batch.ShaderCode ?? _defaultFragment; var key = (code, batch.Blend);
            _usedPrograms.Add(key);
            if (!_pipelines.ContainsKey(key)) _pipelines.Add(key, CreatePipeline(code, batch.Blend));
            if (batch.MaskShader) _ = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled);
            if (batch.GroupShader)
            {
                _usedPrograms.Add((_defaultFragment, BlendMode.PremultAlpha));
                if (!_pipelines.ContainsKey((_defaultFragment, BlendMode.PremultAlpha)))
                    _pipelines.Add((_defaultFragment, BlendMode.PremultAlpha), CreatePipeline(_defaultFragment, BlendMode.PremultAlpha));
                _ = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled);
            }
            if (batch.Material is { } material && material.Textures.Length != 0) { _usedMaterials.Add(material); PrepareTextures(material); }
            if (!batch.GroupShader && UsesCanvasTexture(batch)) _ = CanvasBinding(batch);
            if (batch.Material?.Program.UsesScreenTexture == true) _ = Sampler(TextureFilter.LinearWithMipmaps, TextureRepeat.Disabled);
            if (batch.Operation == CanvasOperation.GroupBegin) _clearPipeline ??= CreatePipeline(_defaultFragment, BlendMode.Mix, overwrite: true);
        }
        if (screen) _ = BackBuffer(output, screenMipmaps);
        EnsureBuffers(checked(vertices.Length * sizeof(CanvasVertex)));
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire a GPU command buffer");
        var swapchainAcquired = false; nint pass = 0;
        try
        {
            foreach (var texture in _textures.Values) texture.Upload(command);
            if (!vertices.IsEmpty)
            {
                var mapped = SDL.MapGPUTransferBuffer(Device, _transfer!.DangerousGetHandle(), true);
                if (mapped == 0) throw Failure("map vertex upload memory");
                try { vertices.CopyTo(new Span<CanvasVertex>((void*)mapped, vertices.Length)); }
                finally { SDL.UnmapGPUTransferBuffer(Device, _transfer.DangerousGetHandle()); }
                var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw Failure("begin vertex upload");
                SDL.UploadToGPUBuffer(copy, new SDL.GPUTransferBufferLocation { TransferBuffer = _transfer.DangerousGetHandle() },
                    new SDL.GPUBufferRegion { Buffer = _vertexBuffer!.DangerousGetHandle(), Size = (uint)(vertices.Length * sizeof(CanvasVertex)) }, true);
                SDL.EndGPUCopyPass(copy);
            }
            if (!clearEnabled) Blit(command, output.Current.DangerousGetHandle(), output.Next.DangerousGetHandle(), new(0, 0, size.X, size.Y));
            pass = BeginPass(command, output.Next.DangerousGetHandle(), clearEnabled ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load, clear);
            var inGroup = false; var copied = false;
            var maskSamplers = stackalloc SDL.GPUTextureSamplerBinding[2];
            foreach (var batch in batches)
            {
                if (batch.Operation == CanvasOperation.Copy)
                {
                    SDL.EndGPURenderPass(pass); pass = 0;
                    if (batch.Region.HasArea()) Blit(command, output.Next.DangerousGetHandle(), output.BackBuffer!.DangerousGetHandle(), batch.Region);
                    if (screenMipmaps) SDL.GenerateMipmapsForGPUTexture(command, output.BackBuffer!.DangerousGetHandle());
                    copied = true; pass = BeginPass(command, output.Next.DangerousGetHandle(), SDL.GPULoadOp.Load, default);
                    continue;
                }
                if (batch.Operation == CanvasOperation.MaskFinish) continue;
                if (batch.Operation == CanvasOperation.MaskBegin)
                {
                    SDL.EndGPURenderPass(pass); pass = 0; inGroup = true;
                    if (batch.Region.HasArea()) Blit(command, output.Next.DangerousGetHandle(), output.BackBuffer!.DangerousGetHandle(), batch.Region);
                    pass = BeginPass(command, output.BackBuffer!.DangerousGetHandle(), SDL.GPULoadOp.Load, default);
                    continue;
                }
                if (batch.Operation == CanvasOperation.GroupBegin)
                {
                    SDL.EndGPURenderPass(pass); pass = 0; inGroup = true;
                    pass = BeginPass(command, output.BackBuffer!.DangerousGetHandle(), SDL.GPULoadOp.Load, default);
                }
                if (batch.Operation is CanvasOperation.GroupEnd or CanvasOperation.MaskEnd && inGroup)
                {
                    SDL.EndGPURenderPass(pass); pass = 0; inGroup = false;
                    if (batch.Mipmaps) SDL.GenerateMipmapsForGPUTexture(command, output.BackBuffer!.DangerousGetHandle());
                    pass = BeginPass(command, output.Next.DangerousGetHandle(), SDL.GPULoadOp.Load, default);
                }
                if (batch.Material?.Program.UsesScreenTexture == true && batch.Operation is not (CanvasOperation.GroupEnd or CanvasOperation.MaskEnd) && !copied)
                {
                    SDL.EndGPURenderPass(pass); pass = 0;
                    Blit(command, output.Next.DangerousGetHandle(), output.BackBuffer!.DangerousGetHandle(), new(0, 0, size.X, size.Y));
                    if (screenMipmaps) SDL.GenerateMipmapsForGPUTexture(command, output.BackBuffer!.DangerousGetHandle());
                    copied = true; pass = BeginPass(command, output.Next.DangerousGetHandle(), SDL.GPULoadOp.Load, default);
                }
                if (batch.Count == 0) continue;
                var clip = batch.Clip ?? new Rect2i(0, 0, size.X, size.Y);
                if (!clip.HasArea()) continue;
                var scissor = new SDL.Rect { X = clip.Position.X, Y = clip.Position.Y, W = clip.Size.X, H = clip.Size.Y }; SDL.SetGPUScissor(pass, in scissor);
                var dimensions = new Vector2(size.X, size.Y); SDL.PushGPUVertexUniformData(command, 0, (nint)(&dimensions), (uint)sizeof(Vector2));
                var binding = new SDL.GPUBufferBinding { Buffer = _vertexBuffer!.DangerousGetHandle() }; SDL.BindGPUVertexBuffers(pass, 0, new ReadOnlySpan<SDL.GPUBufferBinding>(&binding, 1), 1);
                var pipeline = batch.Operation == CanvasOperation.GroupBegin ? _clearPipeline! : _pipelines[(batch.MaskShader ? BuiltInShaders.Clip : batch.ShaderCode ?? _defaultFragment, batch.GroupShader ? BlendMode.PremultAlpha : batch.Blend)];
                SDL.BindGPUGraphicsPipeline(pass, pipeline.DangerousGetHandle());
                batch.Material?.PushUniforms(command, (float)time, new(1f / size.X, 1f / size.Y));
                if (batch.MaskShader)
                {
                    var inverse = new Vector4(1f / size.X, 1f / size.Y, 0, 0);
                    SDL.PushGPUFragmentUniformData(command, 0, (nint)(&inverse), (uint)sizeof(Vector4));
                    var samplers = maskSamplers;
                    samplers[0] = CanvasBinding(batch);
                    samplers[1] = new() { Texture = output.BackBuffer!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled) };
                    SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(samplers, 2), 2);
                }
                else if (batch.Material is { Textures.Length: > 0 } textured)
                {
                    var samplers = _textureBindings[textured];
                    for (var i = 0; i < samplers.Length; i++)
                        if (textured.Program.Textures[i].IsScreenTexture) samplers[i] = new() { Texture = output.BackBuffer!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.LinearWithMipmaps, TextureRepeat.Disabled) };
                    if (UsesCanvasTexture(batch)) samplers[0] = CanvasBinding(batch);
                    SDL.BindGPUFragmentSamplers(pass, 0, samplers.AsSpan(), (uint)samplers.Length);
                }
                else if (batch.Material is null)
                {
                    var sampler = batch.GroupShader ? new SDL.GPUTextureSamplerBinding { Texture = output.BackBuffer!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled) } : CanvasBinding(batch);
                    SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(&sampler, 1), 1);
                }
                SDL.DrawGPUPrimitives(pass, (uint)batch.Count, 1, (uint)batch.First, 0);
            }
            SDL.EndGPURenderPass(pass); pass = 0;
            if (present)
            {
                Check(SDL.WaitAndAcquireGPUSwapchainTexture(command, _window, out var swapchain, out var width, out var height), "acquire swapchain texture");
                if (swapchain != 0)
                {
                    swapchainAcquired = _hasPresented = true;
                    var blit = new SDL.GPUBlitInfo { Source = new() { Texture = output.Next.DangerousGetHandle(), W = (uint)size.X, H = (uint)size.Y }, Destination = new() { Texture = swapchain, W = width, H = height }, LoadOp = SDL.GPULoadOp.DontCare, Filter = SDL.GPUFilter.Nearest };
                    SDL.BlitGPUTexture(command, in blit);
                }
            }
            var submitted = command; command = 0; Check(SDL.SubmitGPUCommandBuffer(submitted), "submit canvas frame");
            foreach (var texture in _textures.Values) texture.CommitUpload(); output.Commit();
        }
        finally
        {
            if (pass != 0) SDL.EndGPURenderPass(pass);
            if (command != 0) { if (swapchainAcquired) SDL.SubmitGPUCommandBuffer(command); else SDL.CancelGPUCommandBuffer(command); }
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

    private nint Sampler(TextureFilter filter, TextureRepeat repeat, int anisotropy = 1)
    {
        var anisotropic = filter >= TextureFilter.NearestWithMipmapsAnisotropic && anisotropy > 1;
        if (_relaxedAndroidDevice && anisotropic)
            throw new NotSupportedException("Anisotropic texture filtering is unavailable on this Android GPU device.");
        var key = (filter, repeat, anisotropic ? anisotropy : 1, _nearestMipmaps);
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
                MipmapMode = _nearestMipmaps ? SDL.GPUSamplerMipmapMode.Nearest : SDL.GPUSamplerMipmapMode.Linear,
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
        batch.Material.Program.Textures is [{ IsCanvasTexture: true }, ..];

    private SDL.GPUTextureSamplerBinding CanvasBinding(CanvasBatch batch)
    {
        var texture = batch.Texture;
        if (texture is null)
        {
            if (_whiteTexture is null)
            {
                using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
                image.Fill(Colors.White);
                _whiteTexture = ImageTexture.CreateFromImage(image);
            }
            texture = _whiteTexture;
        }
        return new() { Texture = PrepareTexture(texture), Sampler = Sampler(batch.Filter, batch.Repeat, batch.MaxAnisotropy) };
    }

    private nint PrepareTexture(Texture texture)
    {
        if (texture is AtlasTexture atlas) texture = atlas.RenderingTexture
            ?? throw new InvalidOperationException("A sampled atlas has no source texture.");
        if (texture is ViewportTexture viewportTexture)
        {
            var viewport = viewportTexture.Bound ?? throw new InvalidOperationException("The sampled viewport texture is unresolved.");
            return (FindTarget(viewport) ?? throw new InvalidOperationException("The sampled viewport has no native target.")).Current.DangerousGetHandle();
        }
        if (_usedTextures.Add(texture))
        {
            var pixels = texture.CapturePixels() ?? throw new InvalidOperationException("A sampled texture has no readable image.");
            if (!_textures.TryGetValue(texture, out var gpu) || !ReferenceEquals(gpu.Pixels.Allocation, pixels.Allocation))
            {
                var replacement = new GPUTexture(_device, pixels);
                gpu?.Dispose(); _textures[texture] = replacement;
            }
            else gpu.Pixels = pixels;
        }
        return _textures[texture].Handle;
    }

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
                bindings[i] = new SDL.GPUTextureSamplerBinding { Texture = PrepareTexture(_textureScratch[i]!), Sampler = Sampler(TextureFilter.Linear, TextureRepeat.Disabled) };
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
        if (visible) { Check(SDL.ClaimWindowForGPUDevice(Device, _window), "reclaim visible window"); _windowClaimed = true; }
    }
    internal override void BeginFrame() { _usedPrograms.Clear(); _usedPrograms.Add((_defaultFragment, BlendMode.Mix)); _usedTextures.Clear(); _usedMaterials.Clear(); }
    internal override void EndFrame()
    {
        foreach (var pair in _pipelines) if (!_usedPrograms.Contains(pair.Key) && !ReferenceEquals(pair.Key.Code, _defaultFragment)) { pair.Value.Dispose(); _pipelines.Remove(pair.Key); }
        foreach (var pair in _textures) if (!_usedTextures.Contains(pair.Key) && (!pair.Key.RetainRendererCache || pair.Key.IsDisposed)) { pair.Value.Dispose(); _textures.Remove(pair.Key); }
        foreach (var pair in _textureBindings) if (!_usedMaterials.Contains(pair.Key)) _textureBindings.Remove(pair.Key);
    }
    internal override RenderHandle CreateTarget(Vector2i size, Color clear, bool mipmaps = false)
    {
        var info = new SDL.GPUTextureCreateInfo { Type = SDL.GPUTextureType.TextureType2D, Format = SDL.GPUTextureFormat.R8G8B8A8Unorm, Width = (uint)size.X, Height = (uint)size.Y, LayerCountOrDepth = 1, NumLevels = mipmaps ? (uint)(1 + System.Numerics.BitOperations.Log2((uint)Math.Max(size.X, size.Y))) : 1, Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler };
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
        var bufferInfo = new SDL.GPUBufferCreateInfo { Usage = SDL.GPUBufferUsageFlags.Vertex, Size = (uint)size };
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
        var pitch = checked((output.Size.X + 63) / 64 * 64);
        var info = new SDL.GPUTransferBufferCreateInfo { Usage = SDL.GPUTransferBufferUsage.Download, Size = checked((uint)(pitch * output.Size.Y * 4)) };
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
            var pixels = new byte[checked(output.Size.X * output.Size.Y * 4)];
            for (var y = 0; y < output.Size.Y; y++) Marshal.Copy(mapped + y * pitch * 4, pixels, y * output.Size.X * 4, output.Size.X * 4);
            return Image.CreateFromData(output.Size.X, output.Size.Y, false, Image.Format.Rgba8, pixels);
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
        _textures.Clear(); _usedTextures.Clear(); _textureBindings.Clear(); _usedMaterials.Clear();
        foreach (var sampler in _samplers.Values) sampler.Dispose();
        _samplers.Clear(); _whiteTexture?.Dispose();
        _pipelines.Clear(); _clearPipeline?.Dispose(); ReleaseTargets(); _vertexBuffer?.Dispose(); _transfer?.Dispose(); _vertexShader.Dispose();
        if (_windowClaimed) SDL.ReleaseWindowFromGPUDevice(Device, _window);
        _device.Dispose();
        if (!idle) throw Failure("wait for GPU shutdown");
    }
}
