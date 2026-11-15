using Electron2D;
using Path = System.IO.Path;
using static Electron2D.RenderingDevice;

internal static partial class ShaderSamplerChecks
{
    private static Shader Load(string name, bool blit = false)
    {
        var extension = name.EndsWith("HLSL", StringComparison.Ordinal) ? "hlsl" : "glsl";
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", name + ".frag." + extension + ".spv"));
        return blit ? Shader.CreateFromSPIRV(bytes, Shader.Mode.TextureBlit) : Shader.CreateFromSPIRV(bytes);
    }
    private static void Native(string output, string? filteringDriver = null)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(256, 128), Title = "Shader sampler state" };
        root.AddChild(new SamplerScene(output, filteringDriver)); Engine.Run(root);
        Check(root.IsDisposed && !RenderingServer.IsAvailable, "Native sampler host tears down.");
    }
    private sealed partial class SamplerScene(string output, string? filteringDriver) : Node
    {
        private readonly List<Resource> _resources = [];
        private readonly List<(Vector2i Position, Color Expected, string Name)> _pixels = [];
        private int _frames, _case;
        private T Own<T>(T resource) where T : Resource { _resources.Add(resource); return resource; }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            var bytes = new byte[] { 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255 };
            using var image = Image.CreateFromData(2, 2, false, Image.Format.Rgba8, bytes); var texture = Own(ImageTexture.CreateFromImage(image));
            using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); white.Fill(Colors.White); var drawing = Own(ImageTexture.CreateFromImage(white));
            var data = new byte[84];
            for (var i = 0; i < 21; i++) { data[i * 4 + (i < 16 ? 0 : i < 20 ? 1 : 2)] = 255; data[i * 4 + 3] = 255; }
            using var mipImage = Image.CreateFromData(4, 4, true, Image.Format.Rgba8, data); var mips = Own(ImageTexture.CreateFromImage(mipImage));
            if (filteringDriver is not null)
            {
                Check(RenderingServer.GetCurrentRenderingDriverName() == filteringDriver, "The requested GPU driver must execute the probes.");
                Filtering(texture, drawing, mips);
                return;
            }
            var ownedShader = RenderingServer.ShaderCreate();
            try
            {
                using var state = new RDSamplerState { RepeatV = RenderingDevice.SamplerRepeatMode.Repeat };
                RenderingServer.ShaderSetTextureSamplerState(ownedShader, "TEXTURE", state);
                using var result = RenderingServer.ShaderGetTextureSamplerState(ownedShader, "TEXTURE")!;
                Check(result.RepeatV == RenderingDevice.SamplerRepeatMode.Repeat, "Owned RID sampler API copies the same typed state.");
                using var borrowed = new Shader(); borrowed.SetTextureSamplerState("TEXTURE", state);
                using var read = RenderingServer.ShaderGetTextureSamplerState(borrowed.GetRID(), "TEXTURE")!;
                Check(read.RepeatV == RenderingDevice.SamplerRepeatMode.Repeat, "Borrowed RID reads the same resource metadata.");
                Reject<InvalidOperationException>(() => RenderingServer.ShaderSetTextureSamplerState(borrowed.GetRID(), "TEXTURE", state));
            }
            finally { RenderingServer.FreeRID(ownedShader); }
            foreach (var language in new[] { "GLSL", "HLSL" })
            {
                var basis = Own(Load("Canvas" + language));
                Add(basis, texture, drawing, new(.5f, .25f, 0, 0), null, new(.5f, .5f, 0, 1), language + "/default-linear");
                Add(basis, texture, drawing, new(.25f, .25f, 0, 0), new() { MinFilter = RenderingDevice.SamplerFilter.Linear, MagFilter = RenderingDevice.SamplerFilter.Linear, MIPFilter = RenderingDevice.SamplerFilter.Linear, UseAnisotropy = true, AnisotropyMax = 4 }, Colors.Red, language + "/native-anisotropy");
                Add(basis, texture, drawing, new(.5f, .25f, 0, 0), new(), new(0, 1, 0, 1), language + "/nearest");
                Add(basis, texture, drawing, new(1.25f, .25f, 0, 0), new() { RepeatU = RenderingDevice.SamplerRepeatMode.Repeat }, Colors.Red, language + "/repeat-u");
                Add(basis, texture, drawing, new(1.25f, .25f, 0, 0), new() { RepeatU = RenderingDevice.SamplerRepeatMode.MirroredRepeat }, new(0, 1, 0, 1), language + "/mirror-u");
                Add(basis, texture, drawing, new(.25f, 1.25f, 0, 0), new() { RepeatV = RenderingDevice.SamplerRepeatMode.Repeat }, Colors.Red, language + "/repeat-v");
                Add(basis, texture, drawing, new(.25f, -0.25f, 0, 0), new() { RepeatV = RenderingDevice.SamplerRepeatMode.MirroredRepeat }, Colors.Red, language + "/mirror-v");
                Add(basis, mips, drawing, new(0, 0, 1, 1), new() { MinLOD = 1, MaxLOD = 1 }, new(0, 1, 0, 1), language + "/lod-clamp", 4);
                Add(basis, mips, drawing, new(0, 0, 1, 1), new() { LODBias = 1 }, new(0, 1, 0, 1), language + "/bias", 4);
                Add(basis, mips, drawing, new(.5f, .5f, 0, 0), new() { MinLOD = .5f, MaxLOD = .5f, MIPFilter = RenderingDevice.SamplerFilter.Linear }, new(.5f, .5f, 0, 1), language + "/mip-linear", 4);
                Add(basis, mips, drawing, new(.5f, .5f, 0, 0), new() { MinLOD = .75f, MaxLOD = .75f }, new(0, 1, 0, 1), language + "/mip-nearest", 4);
                var engine = Own(Load("Engine" + language));
                Add(engine, texture, texture, new(.5f, .25f, 0, 0), new(), new(0, 1, 0, 1), language + "/engine", binding: "TEXTURE");
                var blit = Own(Load("Blit" + language, true));
                Blit(blit, texture, new() { RepeatU = RenderingDevice.SamplerRepeatMode.Repeat }, new(1.25f, .25f, 0, 0), Colors.Red, language + "/blit-repeat");
                Blit(blit, mips, new() { MinLOD = 1, MaxLOD = 1 }, new(.5f, .5f, 0, 0), new(0, 1, 0, 1), language + "/blit-lod");
                var userBlit = Own(Load("BlitUser" + language, true));
                Blit(userBlit, texture, new() { RepeatV = RenderingDevice.SamplerRepeatMode.Repeat }, new(.25f, 1.25f, 0, 0), Colors.Red, language + "/blit-user", "pixels");
                var bindingArray = Own(Load("BindingArray" + language));
                bindingArray.SetDefaultTextureParameter("pixels", texture, 0); bindingArray.SetDefaultTextureParameter("pixels", texture, 1);
                using var state = new RDSamplerState { MinFilter = RenderingDevice.SamplerFilter.Linear, MagFilter = RenderingDevice.SamplerFilter.Linear };
                bindingArray.SetTextureSamplerState("pixels", state, 1); using var nearest = new RDSamplerState(); bindingArray.SetTextureSamplerState("pixels", nearest, 0);
                Add(bindingArray, texture, drawing, new(.5f, .25f, 0, 0), state, new(.5f, .5f, 0, 1), language + "/descriptor-element", bindingIndex: 1);
            }
            Extended(texture, drawing);
        }

        private void Filtering(Texture texture, Texture drawing, Texture mips)
        {
            var pixels = new byte[(16 * 16 + 8 * 8 + 4 * 4 + 2 * 2 + 1) * 4];
            for (var i = 0; i < pixels.Length / 4; i++)
            {
                pixels[i * 4 + (i < 256 ? 0 : 1)] = 255;
                pixels[i * 4 + 3] = 255;
            }
            using var image = Image.CreateFromData(16, 16, true, Image.Format.Rgba8, pixels);
            var elongated = Own(ImageTexture.CreateFromImage(image));
            foreach (var language in new[] { "GLSL", "HLSL" })
            {
                var shader = Own(Load("Canvas" + language));
                Add(shader, texture, drawing, new(.375f, .25f, 0, 0), new() { MinFilter = SamplerFilter.Linear },
                    Colors.Red, language + "/nearest-mag");
                Add(shader, texture, drawing, new(.375f, .25f, 0, 0), new() { MagFilter = SamplerFilter.Linear },
                    new(.75f, .25f, 0, 1), language + "/linear-mag");
                Add(shader, texture, drawing, new(-4.125f, .25f, 8, 0), new() { MagFilter = SamplerFilter.Linear },
                    Colors.Red, language + "/nearest-min");
                Add(shader, texture, drawing, new(-4.125f, .25f, 8, 0), new() { MinFilter = SamplerFilter.Linear },
                    new(.75f, .25f, 0, 1), language + "/linear-min");
                var green = new Color(0, 1, 0, 1);
                Add(shader, mips, drawing, new(0, 0, 1, 1), new() { MinLOD = 1 }, green, language + "/min-lod", 4);
                Add(shader, mips, drawing, new(0, 0, 4, 4), new() { MaxLOD = 0 }, Colors.Red, language + "/max-lod", 4);
                Add(shader, mips, drawing, new(0, 0, 1, 1), new() { LODBias = 1 }, green, language + "/positive-bias", 4);
                Add(shader, mips, drawing, new(0, 0, 2, 2), new() { LODBias = -1 }, Colors.Red, language + "/negative-bias", 4);
                Add(shader, mips, drawing, new(0, 0, 1, 1), new() { MinLOD = .5f, MIPFilter = SamplerFilter.Linear },
                    new(.5f, .5f, 0, 1), language + "/linear-mip", 4);
                Add(shader, mips, drawing, new(0, 0, 1, 1), new() { MinLOD = .75f }, green, language + "/nearest-mip", 4);

                var linear = SamplerFilter.Linear;
                var footprint = new Vector4(0, -1.75f, .25f, 4);
                Add(shader, elongated, drawing, footprint, new() { MinFilter = linear, MagFilter = linear, MIPFilter = linear },
                    green, language + "/isotropic-mip");
                Add(shader, elongated, drawing, footprint, new() { MinFilter = linear, MagFilter = linear, MIPFilter = linear, UseAnisotropy = true, AnisotropyMax = 16 },
                    Colors.Red, language + "/anisotropic-mip");
                if (filteringDriver == "direct3d12")
                    Add(shader, texture, drawing, new(.375f, .25f, 0, 0), new() { MinFilter = linear, UseAnisotropy = true, AnisotropyMax = 4 },
                        Colors.Red, language + "/nearest-mag-with-anisotropy-request");
            }
        }
        private void Add(Shader basis, Resource input, Texture drawing, Vector4 coordinates, RDSamplerState? state, Color expected, string name, int size = 8, string binding = "pixels", int bindingIndex = 0)
        {
            using (state)
            {
                var shader = Own((Shader)basis.Duplicate()); shader.SetTextureSamplerState(binding, state, bindingIndex);
                if (binding == "pixels" && shader.GetShaderUniformList().Any(p => p.Name == "pixels"))
                {
                    if (name.Contains("descriptor-element", StringComparison.Ordinal)) { shader.SetDefaultTextureParameter("pixels", (Texture)input, 0); shader.SetDefaultTextureParameter("pixels", (Texture)input, 1); }
                    else if (input is TextureLayered layered) shader.SetDefaultLayeredTextureParameter("pixels", layered);
                    else shader.SetDefaultTextureParameter("pixels", (Texture)input);
                }
                var material = Own(new ShaderMaterial { Shader = shader });
                if (shader.GetShaderUniformList().Any(p => p.Name == "Coordinates")) material.SetShaderParameter("Coordinates", coordinates);
                var position = new Vector2i((_case % 16) * 16 + 2, (_case / 16) * 24 + 2); _case++;
                var quad = new Quad(drawing, size) { Position = (Vector2)position, Material = material };
                Parent!.AddChild(quad); _quads.Add(quad);
                _pixels.Add((position + new Vector2i(size / 2, size / 2), expected, name));
            }
        }
        private void Blit(Shader shader, Texture input, RDSamplerState state, Vector4 coordinates, Color expected, string name, string binding = "SOURCE_TEXTURE0")
        {
            using (state) shader.SetTextureSamplerState(binding, state);
            if (binding == "pixels") shader.SetDefaultTextureParameter(binding, input);
            var material = Own(new ShaderMaterial { Shader = shader }); material.SetShaderParameter("Coordinates", coordinates);
            using var target = new DrawableTexture(); target.Setup(4, 4, DrawableTexture.DrawableFormat.RGBA8, Colors.Black);
            target.BlitRect(new(0, 0, 4, 4), input, material: material);
            using var image = target.GetImage()!; Match(image.GetPixel(2, 2), expected, name);
        }
        protected override void OnProcess(double delta)
        {
            _frames++;
            if (_frames == 4)
            {
                using var image = ((Window)Parent!).GetTexture().GetImage()!;
                foreach (var pixel in _pixels)
                {
                    var actual = image.GetPixel(pixel.Position.X, pixel.Position.Y);
                    Match(actual, pixel.Expected, pixel.Name);
                    if (filteringDriver is not null)
                        Console.WriteLine(FormattableString.Invariant(
                            $"{pixel.Name} at ({pixel.Position.X},{pixel.Position.Y}): actual ({actual.R:R},{actual.G:R},{actual.B:R},{actual.A:R}), expected ({pixel.Expected.R:R},{pixel.Expected.G:R},{pixel.Expected.B:R},{pixel.Expected.A:R})"));
                }
                if (filteringDriver is not null)
                {
                    Console.WriteLine($"{filteringDriver}: {_pixels.Count} GLSL/HLSL filtering pixels matched on {RenderingServer.GetRenderingDevice()!.GetDeviceName()}.");
                    Tree!.Quit();
                    return;
                }
                image.SavePNG(output); Console.WriteLine($"Native shader samplers: {_pixels.Count} canvas/particle probes plus six blits passed.");
            }
            if (filteringDriver is not null) return;
            WarmFrame();
        }
        protected override void Dispose(bool disposing) { CleanupExtended(); foreach (var resource in _resources) resource.Dispose(); base.Dispose(disposing); }
    }
    private sealed class Quad(Texture texture, int size) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, size, size), false); }
    private static void Match(Color actual, Color expected, string name)
    {
        if (MathF.Abs(actual.R - expected.R) > .02f || MathF.Abs(actual.G - expected.G) > .02f || MathF.Abs(actual.B - expected.B) > .02f || MathF.Abs(actual.A - expected.A) > .02f)
            throw new InvalidOperationException($"{name}: actual {actual}, expected {expected}.");
    }
}
