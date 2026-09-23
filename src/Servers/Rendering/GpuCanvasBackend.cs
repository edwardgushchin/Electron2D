using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe class GpuCanvasBackend : CanvasBackend
{
    private readonly RenderHandle _device;
    private readonly nint _window;
    private readonly RenderHandle _vertexShader;
    private readonly byte[] _defaultFragment;
    private readonly Dictionary<(byte[] Code, CanvasItemMaterial.BlendModeEnum Blend), RenderHandle> _pipelines = [];
    private readonly HashSet<(byte[] Code, CanvasItemMaterial.BlendModeEnum Blend)> _usedPrograms = [];
    private readonly Dictionary<Texture, GpuTexture> _textures = [];
    private readonly HashSet<Texture> _usedTextures = [];
    private readonly Dictionary<MaterialState, SDL.GPUTextureSamplerBinding[]> _textureBindings = [];
    private readonly HashSet<MaterialState> _usedMaterials = [];
    private readonly Texture?[] _textureScratch = new Texture?[16];
    private readonly Dictionary<(CanvasItem.TextureFilterEnum, CanvasItem.TextureRepeatEnum, int, bool), RenderHandle> _samplers = [];
    private readonly bool _nearestMipmaps = ProjectSettings.Instance.GetWithOverride(ProjectSettings.UseNearestMipmapFilter);
    private ImageTexture? _whiteTexture;
    private RenderHandle? _target;
    private RenderHandle? _vertexBuffer;
    private RenderHandle? _transfer;
    private int _bufferSize;
    private Vector2I _targetSize;
    private bool _hasFrame;
    private bool _disposed;
    internal override string Method => "gpu";
    internal override string Driver { get; }
    private nint Device => _device.DangerousGetHandle();

    internal GpuCanvasBackend(SafeHandle window)
    {
        _window = window.DangerousGetHandle();
        _device = new RenderHandle(SDL.CreateGPUDevice(ShaderCompiler.GetFormats(), false, null), SDL.DestroyGPUDevice, window);
        var claimed = false;
        RenderHandle? vertex = null;
        try
        {
            Check(SDL.ClaimWindowForGPUDevice(Device, _window), "claim window for GPU rendering");
            claimed = true;
            Driver = SDL.GetGPUDeviceDriver(Device) ?? "unknown";
            vertex = CreateShader(BuiltInShaders.Vertex, fragment: false);
            _vertexShader = vertex;
            _defaultFragment = BuiltInShaders.Fragment;
            _pipelines.Add((_defaultFragment, CanvasItemMaterial.BlendModeEnum.Mix), CreatePipeline(_defaultFragment, CanvasItemMaterial.BlendModeEnum.Mix));
        }
        catch
        {
            vertex?.Dispose();
            if (claimed) SDL.ReleaseWindowFromGPUDevice(Device, _window);
            _device.Dispose();
            throw;
        }
    }

    internal override Vector2I GetPixelSize()
    {
        Check(SDL.GetWindowSizeInPixels(_window, out var width, out var height), "query GPU window size");
        return new(width, height);
    }

    private RenderHandle CreateShader(byte[] code, bool fragment)
    {
        return new RenderHandle(ShaderCompiler.CreateShader(Device, code, fragment), h => SDL.ReleaseGPUShader(Device, h), _device);
    }

    private RenderHandle CreatePipeline(byte[] code, CanvasItemMaterial.BlendModeEnum blend)
    {
        using var fragment = CreateShader(code, fragment: true);
        var buffer = new SDL.GPUVertexBufferDescription { Slot = 0, Pitch = (uint)sizeof(CanvasVertex), InputRate = SDL.GPUVertexInputRate.Vertex };
        var attributes = stackalloc SDL.GPUVertexAttribute[3];
        attributes[0] = new() { Location = 0, Format = SDL.GPUVertexElementFormat.Float2, Offset = 0 };
        attributes[1] = new() { Location = 1, Format = SDL.GPUVertexElementFormat.Float4, Offset = 8 };
        attributes[2] = new() { Location = 2, Format = SDL.GPUVertexElementFormat.Float2, Offset = 24 };
        var blendState = blend switch
        {
            CanvasItemMaterial.BlendModeEnum.Mix => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendOp.Add),
            CanvasItemMaterial.BlendModeEnum.Add => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendOp.Add),
            CanvasItemMaterial.BlendModeEnum.Sub => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendOp.ReverseSubtract),
            CanvasItemMaterial.BlendModeEnum.Mul => (SDL.GPUBlendFactor.DstColor, SDL.GPUBlendFactor.Zero, SDL.GPUBlendFactor.DstAlpha, SDL.GPUBlendFactor.Zero, SDL.GPUBlendOp.Add),
            CanvasItemMaterial.BlendModeEnum.PremultAlpha => (SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendOp.Add),
            _ => throw new ArgumentOutOfRangeException(nameof(blend)),
        };
        var color = new SDL.GPUColorTargetDescription
        {
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
            BlendState = new SDL.GPUColorTargetBlendState
            {
                EnableBlend = true,
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
            VertexInputState = new() { VertexBufferDescriptions = (nint)(&buffer), NumVertexBuffers = 1, VertexAttributes = (nint)attributes, NumVertexAttributes = 3 },
            TargetInfo = new() { ColorTargetDescriptions = (nint)(&color), NumColorTargets = 1 }
        };
        return new RenderHandle(SDL.CreateGPUGraphicsPipeline(Device, in info), h => SDL.ReleaseGPUGraphicsPipeline(Device, h), _device);
    }

    internal override void Draw(ReadOnlySpan<CanvasVertex> vertices, ReadOnlySpan<CanvasBatch> batches, Color clear, bool present, double time)
    {
        var size = GetPixelSize();
        if (size.X <= 0 || size.Y <= 0) return;
        _usedPrograms.Clear();
        _usedPrograms.Add((_defaultFragment, CanvasItemMaterial.BlendModeEnum.Mix));
        _usedTextures.Clear(); _usedMaterials.Clear();
        foreach (var batch in batches)
        {
            if (batch.Material?.Program.TimeUniform is not null && !float.IsFinite((float)time))
                throw new InvalidOperationException("The render clock exceeds the finite float32 range required by shader TIME.");
            var code = batch.ShaderCode ?? _defaultFragment;
            var key = (code, batch.Blend);
            _usedPrograms.Add(key);
            if (!_pipelines.ContainsKey(key)) _pipelines.Add(key, CreatePipeline(code, batch.Blend));
            if (batch.Material is { } material && material.Textures.Length != 0 && _usedMaterials.Add(material)) PrepareTextures(material);
            if (UsesCanvasTexture(batch)) _ = CanvasBinding(batch);
        }
        foreach (var pair in _pipelines)
            if (!_usedPrograms.Contains(pair.Key)) { pair.Value.Dispose(); _pipelines.Remove(pair.Key); }
        foreach (var pair in _textures)
            if (!_usedTextures.Contains(pair.Key)) { pair.Value.Dispose(); _textures.Remove(pair.Key); }
        foreach (var pair in _textureBindings)
            if (!_usedMaterials.Contains(pair.Key)) _textureBindings.Remove(pair.Key);
        EnsureTarget(size);
        EnsureBuffers(checked(vertices.Length * sizeof(CanvasVertex)));
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire a GPU command buffer");
        var swapchainAcquired = false;
        try
        {
            foreach (var texture in _textures.Values) texture.Upload(command);
            if (!vertices.IsEmpty)
            {
                var mapped = SDL.MapGPUTransferBuffer(Device, _transfer!.DangerousGetHandle(), true);
                if (mapped == 0) throw Failure("map vertex upload memory");
                try { vertices.CopyTo(new Span<CanvasVertex>((void*)mapped, vertices.Length)); }
                finally { SDL.UnmapGPUTransferBuffer(Device, _transfer.DangerousGetHandle()); }
                var copy = SDL.BeginGPUCopyPass(command);
                if (copy == 0) throw Failure("begin vertex upload");
                SDL.UploadToGPUBuffer(copy, new SDL.GPUTransferBufferLocation { TransferBuffer = _transfer.DangerousGetHandle() },
                    new SDL.GPUBufferRegion { Buffer = _vertexBuffer!.DangerousGetHandle(), Size = (uint)(vertices.Length * sizeof(CanvasVertex)) }, true);
                SDL.EndGPUCopyPass(copy);
            }
            var target = new SDL.GPUColorTargetInfo
            {
                Texture = _target!.DangerousGetHandle(),
                ClearColor = new() { R = clear.R, G = clear.G, B = clear.B, A = clear.A },
                LoadOp = SDL.GPULoadOp.Clear,
                StoreOp = SDL.GPUStoreOp.Store,
                Cycle = true
            };
            var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&target, 1), 1, 0);
            if (pass == 0) throw Failure("begin canvas render pass");
            try
            {
                if (!vertices.IsEmpty)
                {
                    var dimensions = new Vector2(size.X, size.Y);
                    SDL.PushGPUVertexUniformData(command, 0, (nint)(&dimensions), (uint)sizeof(Vector2));
                    var binding = new SDL.GPUBufferBinding { Buffer = _vertexBuffer!.DangerousGetHandle() };
                    SDL.BindGPUVertexBuffers(pass, 0, new ReadOnlySpan<SDL.GPUBufferBinding>(&binding, 1), 1);
                    foreach (var batch in batches)
                    {
                        SDL.BindGPUGraphicsPipeline(pass, _pipelines[(batch.ShaderCode ?? _defaultFragment, batch.Blend)].DangerousGetHandle());
                        batch.Material?.PushUniforms(command, (float)time);
                        if (batch.Material is { Textures.Length: > 0 } textured)
                        {
                            var samplers = _textureBindings[textured];
                            if (UsesCanvasTexture(batch)) samplers[0] = CanvasBinding(batch);
                            SDL.BindGPUFragmentSamplers(pass, 0, samplers.AsSpan(), (uint)samplers.Length);
                        }
                        else if (batch.Material is null)
                        {
                            var sampler = CanvasBinding(batch);
                            SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(&sampler, 1), 1);
                        }
                        SDL.DrawGPUPrimitives(pass, (uint)batch.Count, 1, (uint)batch.First, 0);
                    }
                }
            }
            finally { SDL.EndGPURenderPass(pass); }
            if (present)
            {
                Check(SDL.WaitAndAcquireGPUSwapchainTexture(command, _window, out var swapchain, out var width, out var height), "acquire swapchain texture");
                if (swapchain != 0)
                {
                    swapchainAcquired = true;
                    var blit = new SDL.GPUBlitInfo
                    {
                        Source = new() { Texture = _target.DangerousGetHandle(), W = (uint)size.X, H = (uint)size.Y },
                        Destination = new() { Texture = swapchain, W = width, H = height },
                        LoadOp = SDL.GPULoadOp.DontCare,
                        Filter = SDL.GPUFilter.Nearest
                    };
                    SDL.BlitGPUTexture(command, in blit);
                }
            }
            var submitted = command;
            command = 0;
            Check(SDL.SubmitGPUCommandBuffer(submitted), "submit canvas frame");
            foreach (var texture in _textures.Values) texture.CommitUpload();
            _hasFrame = true;
        }
        finally
        {
            if (command != 0)
            {
                if (swapchainAcquired) SDL.SubmitGPUCommandBuffer(command);
                else SDL.CancelGPUCommandBuffer(command);
            }
        }
    }

    private nint Sampler(CanvasItem.TextureFilterEnum filter, CanvasItem.TextureRepeatEnum repeat, int anisotropy = 1)
    {
        var anisotropic = filter >= CanvasItem.TextureFilterEnum.NearestWithMipmapsAnisotropic && anisotropy > 1;
        var key = (filter, repeat, anisotropic ? anisotropy : 1, _nearestMipmaps);
        if (!_samplers.TryGetValue(key, out var sampler))
        {
            var linear = filter is CanvasItem.TextureFilterEnum.Linear or CanvasItem.TextureFilterEnum.LinearWithMipmaps or CanvasItem.TextureFilterEnum.LinearWithMipmapsAnisotropic;
            var address = repeat switch
            {
                CanvasItem.TextureRepeatEnum.Enabled => SDL.GPUSamplerAddressMode.Repeat,
                CanvasItem.TextureRepeatEnum.Mirror => SDL.GPUSamplerAddressMode.MirroredRepeat,
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
                MaxLod = filter >= CanvasItem.TextureFilterEnum.NearestWithMipmaps ? 1000 : 0,
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
        if (_usedTextures.Add(texture))
        {
            var pixels = texture.CapturePixels() ?? throw new InvalidOperationException("A sampled texture has no readable image.");
            if (!_textures.TryGetValue(texture, out var gpu) || !ReferenceEquals(gpu.Pixels.Allocation, pixels.Allocation))
            {
                var replacement = new GpuTexture(_device, pixels);
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
                if (material.Program.Textures[i].IsCanvasTexture) continue;
                bindings[i] = new SDL.GPUTextureSamplerBinding { Texture = PrepareTexture(_textureScratch[i]!), Sampler = Sampler(CanvasItem.TextureFilterEnum.Linear, CanvasItem.TextureRepeatEnum.Disabled) };
            }
        }
        finally { Array.Clear(_textureScratch); }
    }

    private void EnsureTarget(Vector2I size)
    {
        if (_targetSize == size) return;
        var info = new SDL.GPUTextureCreateInfo
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Format = SDL.GPUTextureFormat.R8G8B8A8Unorm,
            Width = (uint)size.X,
            Height = (uint)size.Y,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            Usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler
        };
        var target = new RenderHandle(SDL.CreateGPUTexture(Device, in info), h => SDL.ReleaseGPUTexture(Device, h), _device);
        _target?.Dispose(); _target = target; _targetSize = size; _hasFrame = false;
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

    internal override Image Readback()
    {
        if (!_hasFrame) throw new InvalidOperationException("No canvas frame has completed.");
        var pitch = checked((_targetSize.X + 63) / 64 * 64);
        var info = new SDL.GPUTransferBufferCreateInfo { Usage = SDL.GPUTransferBufferUsage.Download, Size = checked((uint)(pitch * _targetSize.Y * 4)) };
        using var transfer = new RenderHandle(SDL.CreateGPUTransferBuffer(Device, in info), h => SDL.ReleaseGPUTransferBuffer(Device, h), _device);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire readback commands");
        nint fence;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin framebuffer readback");
            SDL.DownloadFromGPUTexture(copy, new SDL.GPUTextureRegion { Texture = _target!.DangerousGetHandle(), W = (uint)_targetSize.X, H = (uint)_targetSize.Y, D = 1 },
                new SDL.GPUTextureTransferInfo { TransferBuffer = transfer.DangerousGetHandle(), PixelsPerRow = (uint)pitch, RowsPerLayer = (uint)_targetSize.Y });
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
            var pixels = new byte[checked(_targetSize.X * _targetSize.Y * 4)];
            for (var y = 0; y < _targetSize.Y; y++) Marshal.Copy(mapped + y * pitch * 4, pixels, y * _targetSize.X * 4, _targetSize.X * 4);
            return Image.CreateFromData(_targetSize.X, _targetSize.Y, false, Image.Format.Rgba8, pixels);
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
        _pipelines.Clear(); _target?.Dispose(); _vertexBuffer?.Dispose(); _transfer?.Dispose(); _vertexShader.Dispose();
        SDL.ReleaseWindowFromGPUDevice(Device, _window);
        _device.Dispose();
        if (!idle) throw Failure("wait for GPU shutdown");
    }
}
