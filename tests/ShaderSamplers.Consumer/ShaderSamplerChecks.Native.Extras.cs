using Electron2D;
using Path = System.IO.Path;

internal static partial class ShaderSamplerChecks
{
    private sealed partial class SamplerScene
    {
        private readonly List<Quad> _quads = [];
        private readonly List<(DrawableTexture Target, ShaderMaterial Material, Texture Input)> _warmBlits = [];
        private readonly List<RID> _deviceImages = [];
        private readonly List<GPUParticles> _particles = [];
        private long _ownerBytes, _allBytes;
        private bool _global;
        private void Extended(Texture texture, Texture drawing)
        {
            using var first = Image.CreateEmpty(2, 2, false, Image.Format.Rgba8); first.Fill(Colors.Blue);
            using var second = texture.GetImage()!; var array = Own(new TextureArray()); array.CreateFromImages([first, second]);
            RenderingServer.GlobalShaderParameterAdd("sampler_image", texture); _global = true;
            foreach (var language in new[] { "GLSL", "HLSL" })
            {
                var shader = Own(Load("Array" + language));
                Add(shader, array, drawing, new(.5f, .25f, 0, 0), new(), new(0, 1, 0, 1), language + "/layered");
                var global = Own(Load("Global" + language));
                Add(global, texture, drawing, new(1.25f, .25f, 0, 0), new() { RepeatU = RenderingDevice.SamplerRepeatMode.Repeat }, Colors.Red, language + "/global", binding: "GLOBAL_sampler_image");
                var compute = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", "Particle" + language + ".comp." + (language == "GLSL" ? "glsl" : "hlsl") + ".spv"));
                var process = Own(Shader.CreateFromSPIRV(compute, Shader.Mode.Particles));
                process.SetDefaultTextureParameter("pixels", texture); using var nearest = new RDSamplerState(); process.SetTextureSamplerState("pixels", nearest);
                var material = Own(new ShaderMaterial { Shader = process });
                using var particleImage = Image.CreateEmpty(8, 8, false, Image.Format.Rgba8); particleImage.Fill(Colors.White);
                var particleTexture = Own(ImageTexture.CreateFromImage(particleImage));
                var point = new Vector2i(language == "GLSL" ? 224 : 240, 112);
                var node = new GPUParticles { Amount = 1, Emitting = false, Lifetime = 1000, FixedFPS = 0, Interpolate = false, LocalCoords = true, Texture = particleTexture, Position = (Vector2)point, ProcessMaterial = material };
                Parent!.AddChild(node); _particles.Add(node); node.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, GPUParticles.EmitFlags.Position);
                _pixels.Add((point, new(0, 1, 0, 1), language + "/particles"));
            }
            var vertex = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", "SampleGLSL.vert.glsl.spv"));
            using var fragment = Load("PassGLSL");
            var pair = Own(Shader.CreateFromSPIRV(vertex, fragment.GetSPIRV()));
            Add(pair, texture, drawing, Vector4.Zero, new(), new(0, 1, 0, 1), "vertex-only");
            using var regular = Load("CanvasGLSL"); var shared = Own(Shader.CreateFromSPIRV(vertex, regular.GetSPIRV()));
            Add(shared, texture, drawing, new(.75f, .25f, 0, 0), new(), new(0, 1, 0, 1), "shared-stages");
            var device = RenderingServer.GetRenderingDevice()!;
            using var format = new RDTextureFormat { Width = 2, Height = 2, Format = RenderingDevice.DataFormat.R32UINT, UsageBits = RenderingDevice.TextureUsageBits.Sampling };
            using var view = new RDTextureView(); var data = new byte[16];
            for (var i = 0; i < 4; i++) System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(i * 4), (uint)(4 << i));
            var rid = device.TextureCreate(format, view, [data]); _deviceImages.Add(rid); var integerTexture = Own(new TextureRD { TextureRDRID = rid });
            var integer = Own(Load("IntegerGLSL"));
            using (var linear = new RDSamplerState { MagFilter = RenderingDevice.SamplerFilter.Linear }) Reject<ArgumentException>(() => integer.SetTextureSamplerState("pixels", linear));
            Add(integer, integerTexture, drawing, new(.75f, .25f, 0, 0), new() { UseAnisotropy = true, AnisotropyMax = 1 }, new(.25f, 0, 0, 1), "integer-anisotropy-disabled-at-one");
            var basis = Own(Load("CanvasGLSL")); var blitBasis = Own(Load("BlitGLSL", true));
            for (var i = 0; i < 40; i++)
            {
                Add(basis, texture, drawing, new(.25f, .25f, 0, 0), new() { MaxLOD = 100 + i }, Colors.Red, "cache/" + i);
                var shader = Own((Shader)blitBasis.Duplicate()); using var state = new RDSamplerState { MaxLOD = 200 + i }; shader.SetTextureSamplerState("SOURCE_TEXTURE0", state);
                var material = Own(new ShaderMaterial { Shader = shader }); material.SetShaderParameter("Coordinates", new Vector4(.25f, .25f, 0, 0));
                var target = Own(new DrawableTexture()); target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Black); _warmBlits.Add((target, material, texture));
            }
        }
        private void WarmFrame()
        {
            if (_frames > 4 && _frames < 85)
            {
                var offset = (_frames & 1) == 0 ? 1 : -1;
                foreach (var quad in _quads) quad.Position += new Vector2(0, offset);
                foreach (var draw in _warmBlits) draw.Target.BlitRect(new(0, 0, 4, 4), draw.Input, material: draw.Material);
            }
            if (_frames == 20) { _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true); }
            if (_frames == 21 || _frames == 106) { _ownerBytes = GC.GetAllocatedBytesForCurrentThread(); _allBytes = GC.GetTotalAllocatedBytes(true); }
            if (_frames == 85 || _frames == 170)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _ownerBytes; var all = GC.GetTotalAllocatedBytes(true) - _allBytes;
                Check(owner == 0 && all == 0, $"Full sampler frames phase {_frames}: owner={owner}, all={all}.");
                Console.WriteLine($"Sampler phase {_frames}: 64 full frames, owner/all managed bytes={owner}/{all}.");
                if (_frames == 85)
                {
                    using var image = _warmBlits[39].Target.GetImage()!; Match(image.GetPixel(2, 2), Colors.Red, "40th prepared drawable configuration");
                    foreach (var quad in _quads) quad.Visible = false;
                    foreach (var particle in _particles) { particle.Visible = false; particle.SpeedScale = 0; }
                }
                else Tree!.Quit();
            }
        }
        private void CleanupExtended()
        {
            if (!RenderingServer.IsAvailable) return;
            if (_global) RenderingServer.GlobalShaderParameterRemove("sampler_image");
            var device = RenderingServer.GetRenderingDevice(); if (device is not null) foreach (var rid in _deviceImages) if (device.TextureIsValid(rid)) device.FreeRID(rid);
        }
    }
}
