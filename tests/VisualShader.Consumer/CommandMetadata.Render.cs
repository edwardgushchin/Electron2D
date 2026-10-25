using Electron2D;
using System.Runtime.InteropServices;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderCommandMetadata(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(640, 320), Title = "Command metadata", UseHDR2D = args.Contains("hdr") };
        var reject = args.Contains("reject-signed") ? 1 : args.Contains("reject-unsigned") ? 2 : 0;
        var scene = new CommandMetadataScene(folder, reject); window.AddChild(scene); var rejected = false;
        try { Engine.Run(window); }
        catch (ArgumentException error) when (reject != 0 && error.Message.Contains("require float sampling", StringComparison.Ordinal)) { rejected = true; }
        Check((reject == 0 ? scene.Complete : rejected) && window.IsDisposed && !RenderingServer.IsAvailable, "Command metadata host retires its renderer after success or typed sampler rejection.");
        if (rejected) Console.WriteLine("Integer channels reject the fixed float canvas sampler before frame publication.");
    }
    private sealed class MetadataPatch(Texture texture, int kind, ArrayMesh mesh, MultiMesh? multi, StyleBoxTexture style, RID particles) : Entity
    {
        protected override void OnDraw()
        {
            if (particles.IsValid()) RenderingServer.CanvasItemAddParticles(GetCanvasItem(), particles, texture.GetRID());
            else if (kind == 9) DrawStyleBox(style, new(0, 0, 16, 16));
            else if (kind is 8 or 13) RenderingServer.CanvasItemAddNinePatch(GetCanvasItem(), new(0, 0, 16, 16), kind == 8 ? new(2, 1, 4, 2) : default, texture.GetRID(), Vector2.One, Vector2.One);
            else if (kind == 10) DrawMesh(mesh, texture);
            else if (multi is not null) DrawMultiMesh(multi, texture);
            else if (kind == 5) DrawTextureRect(texture, new(0, 0, 16, 16), true);
            else if (kind is 1 or 2 or 3 or 4) DrawTextureRectRegion(texture, new(0, 0, kind == 2 ? -16 : 16, 16), new(2, 1, kind == 3 ? -4 : 4, 2), transpose: kind == 4);
            else DrawTextureRect(texture, new(0, 0, 16, 16), false);
        }
    }
    private readonly record struct MetadataProbe(string Program, int Kind, Vector2 Origin);
    private sealed class CommandMetadataScene(string folder, int reject) : Node
    {
        private readonly List<Resource> _owned = []; private readonly List<ShaderMaterial> _materials = []; private readonly List<MetadataProbe> _probes = [];
        private readonly List<RID> _deviceImages = []; private readonly List<CanvasTexture> _channels = [];
        private readonly List<RID> _particles = [];
        private CanvasTexture _texture = null!; private ImageTexture _specular = null!;
        private DirectionalLight _first = null!, _second = null!; private CanvasModulate _modulate = null!;
        private RenderingDevice _device = null!; private int _lights;
        private int _frame, _component, _phase; private long _owner, _all; internal bool Complete;
        private T KeepMetadata<T>(T resource) where T : Resource { _owned.Add(resource); return resource; }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            using var white = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8); white.Fill(Colors.White);
            var diffuse = KeepMetadata(ImageTexture.CreateFromImage(white));
            using var image = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8);
            for (var y = 0; y < 4; y++) for (var x = 0; x < 8; x++) image.SetPixel(x, y, new(.2f * (x % 5 + 1), .4f, .6f, .2f * (x % 4 + 1)));
            _specular = KeepMetadata(ImageTexture.CreateFromImage(image));
            _texture = KeepMetadata(new CanvasTexture { DiffuseTexture = diffuse, SpecularTexture = _specular, SpecularColor = new(.5f, .75f, .25f), SpecularShininess = .4f });
            _channels.Add(_texture); _device = RenderingServer.GetRenderingDevice()!;
            CanvasTexture DeviceChannels(RenderingDevice.DataFormat dataFormat, byte[] data)
            {
                using var format = new RDTextureFormat { Width = 1, Height = 1, Format = dataFormat, UsageBits = RenderingDevice.TextureUsageBits.Sampling };
                using var view = new RDTextureView(); var rid = _device.TextureCreate(format, view, [data]); _deviceImages.Add(rid);
                var image = KeepMetadata(new TextureRD { TextureRDRID = rid });
                var channels = KeepMetadata(new CanvasTexture { DiffuseTexture = diffuse, SpecularTexture = image, SpecularColor = _texture.SpecularColor, SpecularShininess = .4f }); _channels.Add(channels); return channels;
            }
            var floating = DeviceChannels(RenderingDevice.DataFormat.R32G32B32A32SFLOAT, MemoryMarshal.AsBytes(new float[] { 2, -1, .5f, .75f }.AsSpan()).ToArray());
            var signed = DeviceChannels(RenderingDevice.DataFormat.R32G32B32A32SINT, MemoryMarshal.AsBytes(new int[] { -2, 3, 1, -1 }.AsSpan()).ToArray());
            var unsigned = DeviceChannels(RenderingDevice.DataFormat.R32G32B32A32UINT, MemoryMarshal.AsBytes(new uint[] { 3, 4, 5, 6 }.AsSpan()).ToArray());
            _first = new DirectionalLight { Energy = .25f, Height = 1, Enabled = false }; _second = new DirectionalLight { Energy = .25f, Height = 1, Enabled = false };
            _modulate = new CanvasModulate { Color = Colors.White }; AddChild(_first); AddChild(_second); AddChild(_modulate);
            var atlas = KeepMetadata(new AtlasTexture { Atlas = _texture, Region = new(2, 1, 4, 2) });
            var nested = KeepMetadata(new AtlasTexture { Atlas = atlas, Region = new(1, 0, 2, 2) });
            var style = KeepMetadata(new StyleBoxTexture { Texture = atlas, TextureMarginLeft = 1, TextureMarginRight = 1, TextureMarginTop = 1, TextureMarginBottom = 1 });
            var mesh = KeepMetadata(new ArrayMesh()); var cpuMesh = KeepMetadata(new ArrayMesh());
            foreach (var target in new[] { mesh, cpuMesh }) target.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new()
            {
                Vertices = [new(0, 0), new(16, 0), new(16, 16), new(0, 16)],
                UVs = [new(0, 0), new(1, 0), new(1, 1), new(0, 1)],
                Indices = [0, 1, 2, 0, 2, 3],
                Bones = target == cpuMesh ? new int[16] : [],
                Weights = target == cpuMesh ? [1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0] : []
            });
            var process = KeepMetadata(ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "support/metadata-process.e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
            var processMaterial = KeepMetadata(new ShaderMaterial { Shader = process });
            for (var row = 0; row < CommandMetadataPrograms.Length; row++)
            {
                var program = CommandMetadataPrograms[row];
                if (reject != 0 && program != "specular-node") continue;
                var shader = KeepMetadata(program.StartsWith("raw-", StringComparison.Ordinal)
                    ? Shader.CreateFromSPIRV(File.ReadAllBytes(IOPath.Combine(folder, "metadata-" + program + ".spv")))
                    : ResourceLoader.Load<Shader>(IOPath.Combine(folder, "metadata-" + program + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep));
                Check(!shader.GetShaderUniformList().Any(p => p.Name is "REGION_RECT" or "CANVAS_SPECULAR"), "Command metadata stays outside user material fields.");
                var material = KeepMetadata(new ShaderMaterial { Shader = shader }); _materials.Add(material);
                for (var kind = reject == 1 ? 15 : reject == 2 ? 16 : 0; kind < (reject == 1 ? 16 : reject == 2 ? 17 : 21); kind++)
                {
                    if (reject == 0 && kind is 15 or 16) continue;
                    var origin = new Vector2(4 + kind * 28, 4 + row * 28); MultiMesh? multi = null;
                    if (kind is 11 or 12 or 20)
                    {
                        multi = KeepMetadata(new MultiMesh { Mesh = kind == 11 ? cpuMesh : mesh, InstanceCount = 1 }); multi.SetInstanceTransform2D(0, Transform.Identity);
                        if (kind == 12)
                        {
                            var buffer = RenderingServer.MultiMeshGetBufferRDRID(multi.GetRID()); var data = multi.Buffer; data[3] = 1;
                            RenderingServer.GetRenderingDevice()!.BufferUpdate(buffer, 0, (uint)(data.Length * sizeof(float)), MemoryMarshal.AsBytes(data.AsSpan()));
                            Check(multi.GetInstanceTransform2D(0).Origin == Vector2.Zero, "Device-written geometry retains independent CPU storage.");
                        }
                    }
                    RID particles = default;
                    if (kind is 17 or 18 or 19)
                    {
                        particles = RenderingServer.ParticlesCreate(); _particles.Add(particles);
                        RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetEmitting(particles, false); RenderingServer.ParticlesSetLifetime(particles, 1000);
                        RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetInterpolate(particles, false);
                        RenderingServer.ParticlesSetUseLocalCoordinates(particles, kind != 19); RenderingServer.ParticlesSetCustomAABB(particles, new(-64, -64, 128, 128));
                        RenderingServer.ParticlesSetProcessMaterial(particles, processMaterial.GetRID()); RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, (kind == 18 ? cpuMesh : mesh).GetRID());
                        if (kind == 18) { RenderingServer.ParticlesSetTrailBindPoses(particles, [Transform.Identity, Transform.Identity]); RenderingServer.ParticlesSetTrails(particles, true, .5f); }
                        var emission = new Transform(.3f, new Vector2(2, 3)); if (kind == 19) RenderingServer.ParticlesSetEmissionTransform(particles, emission);
                        RenderingServer.ParticlesEmit(particles, kind == 19 ? emission : Transform.Identity, Vector2.Zero, Colors.White, Colors.White, 31); RenderingServer.ParticlesRequestProcessTime(particles, .125f);
                    }
                    Texture texture = kind == 0 ? diffuse : kind is 6 or 9 or 13 ? atlas : kind == 7 ? nested : kind == 14 ? floating : kind == 15 ? signed : kind == 16 ? unsigned : _texture;
                    AddChild(new MetadataPatch(texture, kind, mesh, multi, style, particles) { Position = origin, Material = material, TextureFilter = TextureFilter.Nearest });
                    _probes.Add(new(program, kind, origin));
                }
            }
            RenderingServer.FramePostDraw += AfterMetadata;
        }
        protected override void OnProcess(double delta)
        {
            if (_frame is >= 40 and < 104)
            {
                _component = _frame % 4; foreach (var material in _materials) material.SetShaderParameter("component", (float)_component);
                GetWindow()!.CanvasTransform = new Transform(0, (_frame & 1) == 0 ? new Vector2(3, 2) : Vector2.Zero);
            }
        }
        private void AfterMetadata()
        {
            _frame++;
            if (_frame is 2 or 5 or 8 or 11 or 14 or 17 or 20 or 23 or 26 or 29 or 32 or 34 or 37 or 108) VerifyMetadata();
            if (_frame is 3 or 6 or 9) { _component++; foreach (var material in _materials) material.SetShaderParameter("component", (float)_component); }
            if (_frame == 12) { foreach (var texture in _channels) { texture.SpecularColor = new(2, -.5f, .25f); texture.SpecularShininess = -2; } _phase = 1; }
            if (_frame == 15) { _texture.SpecularTexture = null; _phase = 2; }
            if (_frame is 18 or 21 or 24) { _component = (_component + 1) % 4; foreach (var material in _materials) material.SetShaderParameter("component", (float)_component); }
            if (_frame == 27) { _texture.SpecularTexture = _specular; foreach (var texture in _channels) { texture.SpecularColor = new(.5f, .75f, .25f); texture.SpecularShininess = .4f; } _phase = 0; }
            if (_frame == 30) foreach (var material in _materials) { var shader = material.Shader!; if (shader is VisualShader) shader.SetSPIRV(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex), shader.GetSPIRV()); else shader.SetSPIRV(shader.GetSPIRV()); }
            if (_frame == 33) { _lights = 1; _first.Enabled = true; _modulate.Color = Colors.Black; }
            if (_frame == 35) { _lights = 2; _second.Enabled = true; }
            if (_frame == 106) { _lights = 0; _first.Enabled = _second.Enabled = false; _modulate.Color = Colors.White; }
            if (_frame is 40 or 120) { _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 104 or 184)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all;
                Console.WriteLine($"Command metadata {(_frame == 104 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes.");
                Check(owner == 0 && all == 0, "Prepared command metadata transport allocates no managed bytes."); VerifyMetadata();
                if (_frame == 184) { Complete = true; Tree!.Quit(); }
            }
        }
        private void VerifyMetadata()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!;
            foreach (var probe in _probes)
            {
                var local = probe.Origin + new Vector2(probe.Kind == 12 ? 5 : 4, 10); var position = GetWindow()!.CanvasTransform * local;
                var u = 4.5f / 16; var v = 10.5f / 16;
                var region = probe.Kind switch
                {
                    1 or 4 or 6 or 8 => new Vector4(.25f, .25f, .5f, .5f),
                    2 or 3 => new(.25f, .25f, -.5f, .5f),
                    5 => new(0, 0, 2, 4),
                    7 => new(.375f, .25f, .25f, .5f),
                    9 => new(.25f, .25f, .5f, .5f),
                    10 or 11 or 12 or >= 17 => new(0, 0, 8, 4),
                    _ => new(0, 0, 1, 1)
                };
                var uvX = probe.Kind switch
                {
                    1 or 6 => .25f + .5f * u,
                    2 or 3 => .25f + .5f * (1 - u),
                    4 => .25f + .5f * v,
                    5 => u * 2 % 1,
                    7 => .375f + .25f * u,
                    8 or 9 => .25f + .125f + (4.5f - 1) / 14 * .25f,
                    13 => .125f + (4.5f - 1) / 14 * .75f,
                    _ => u
                };
                var column = Math.Clamp((int)(uvX * 8), 0, 7);
                var sample = probe.Kind switch { 14 => new Vector4(2, -1, .5f, .75f), 15 => new(-2, 3, 1, -1), 16 => new(3, 4, 5, 6), _ => _phase == 2 ? Vector4.One : new Vector4(.2f * (column % 5 + 1), .4f, .6f, .2f * (column % 4 + 1)) };
                var color = _phase == 0 ? new Color(.5f, .75f, .25f) : new Color(2, -.5f, .25f);
                if (GetWindow()!.UseHDR2D) color = color.SRGBToLinear();
                var specular = probe.Kind == 0 ? Vector4.One : sample * new Vector4(color.R, color.G, color.B, _phase == 0 ? .4f : -2);
                var value = probe.Program is "global" or "custom" or "light" or "raw-glsl" or "raw-hlsl" ? region + specular : probe.Program.StartsWith("region", StringComparison.Ordinal) ? region : specular;
                var component = _component switch { 0 => value.X, 1 => value.Y, 2 => value.Z, _ => value.W };
                var encoded = (component / 256 + .5f) * 255; var red = encoded - MathF.Floor(encoded); var green = MathF.Floor(encoded) / 255;
                var energy = _lights == 0 ? 1 : _lights * (probe.Program == "light" ? .125f : .25f);
                var actual = image.GetPixel((int)position.X, (int)position.Y);
                Check(MathF.Abs(actual.R - red * energy) < .0041f && MathF.Abs(actual.G - green * energy) < .0041f && MathF.Abs(actual.B - .75f * energy) < .0041f,
                    $"Metadata {probe.Program}/{probe.Kind}, frame {_frame}, component {_component}, value {component}: {actual}, expected {red * energy},{green * energy},{.75f * energy}.");
            }
            Console.WriteLine($"Command metadata phase {_frame}: {_probes.Count} geometry/channel observations passed.");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RenderingServer.FramePostDraw -= AfterMetadata;
                if (RenderingServer.IsAvailable)
                {
                    foreach (var rid in _particles) RenderingServer.FreeRID(rid);
                    foreach (var rid in _deviceImages) _device.FreeRID(rid);
                }
            }
            base.Dispose(disposing);
            if (disposing) for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose();
        }
    }
}
