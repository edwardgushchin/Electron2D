using Electron2D;

internal static partial class DefaultChecks
{
    private static void RectangularRender(string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(256, 128), UseHDR2D = args.Contains("hdr"), Title = "Rectangular matrix uniforms" };
        window.AddChild(new RectangularScene(args[1])); Engine.Run(window);
        Check(window.IsDisposed && !RenderingServer.IsAvailable, "Rectangular consumer retires its rendering session.");
    }
    private sealed class RectangularPainter : Entity
    {
        internal int Draws;
        protected override void OnDraw() { Draws++; DrawRect(new(0, 0, 16, 16), Colors.White); }
    }
    private sealed class RectangularScene(string folder) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<ShaderMaterial> _materials = [];
        private readonly List<(RID Shader, RID Material)> _native = [];
        private readonly List<(RectangularPainter Item, bool Paired)> _painters = [];
        private readonly List<GPUParticles> _particles = [];
        private readonly List<(DrawableTexture Target, ShaderMaterial Material)> _blits = [];
        private int _frame; private long _owner, _all;
        private T Own<T>(T value) where T : Resource { _owned.Add(value); return value; }
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); var texture = Own(ImageTexture.CreateFromImage(image));
            foreach (var language in new[] { "glsl", "hlsl" })
            {
                foreach (var paired in new[] { false, true })
                {
                    var fragment = RectangularCode(folder, language); var vertex = RectangularCode(folder, language, "vertex");
                    var shader = Own(paired ? Shader.CreateFromSPIRV(vertex, fragment) : Shader.CreateFromSPIRV(fragment));
                    var painter = new RectangularPainter { Position = new(8 + _painters.Count * 40, 16) }; AddChild(painter); _painters.Add((painter, paired));
                    if (paired)
                    {
                        var nativeShader = RenderingServer.ShaderCreate(); RenderingServer.ShaderSetSPIRV(nativeShader, vertex, fragment);
                        var material = RenderingServer.MaterialCreate(); _native.Add((nativeShader, material)); RenderingServer.MaterialSetShader(material, nativeShader);
                        RenderingServer.CanvasItemSetMaterial(painter.GetCanvasItem(), material);
                    }
                    else { var material = Own(new ShaderMaterial { Shader = shader }); painter.Material = material; _materials.Add(material); }
                    foreach (var field in RectangularFields)
                    {
                        Check(RenderingServer.ShaderGetMatrixParameterDefault(shader.GetRID(), field.Name).SequenceEqual(field.Initial), "Borrowed shader matrix defaults include every rectangular component.");
                        var destination = new float[field.Initial.Length]; RenderingServer.ShaderGetMatrixParameterDefault(shader.GetRID(), field.Name, destination); Check(destination.SequenceEqual(field.Initial), "Caller-buffer defaults have the exact shape.");
                    }
                }
                var processShader = Own(Shader.CreateFromSPIRV(RectangularCode(folder, language, "particles"), Shader.Mode.Particles));
                var process = Own(new ShaderMaterial { Shader = processShader }); _materials.Add(process);
                var particle = new GPUParticles { Amount = 1, Emitting = false, Lifetime = 1000, FixedFPS = 8, SpeedScale = 0, Interpolate = false, LocalCoords = true, Position = new(32 + _particles.Count * 48, 80), Texture = texture, ProcessMaterial = process };
                AddChild(particle); _particles.Add(particle); particle.EmitParticle(Transform.Identity, Vector2.Zero, Colors.White, Colors.White, (ParticlesEmitFlags)31); particle.RequestParticlesProcess(.125f);
                RectangularBlit(folder, language);
                var blitShader = Own(Shader.CreateFromSPIRV(RectangularCode(folder, language, "blit"), Shader.Mode.TextureBlit));
                var blitMaterial = Own(new ShaderMaterial { Shader = blitShader }); _materials.Add(blitMaterial);
                var blitTarget = Own(new DrawableTexture()); blitTarget.Setup(208, 1, DrawableTexture.DrawableFormat.RGBAF, Colors.Magenta); _blits.Add((blitTarget, blitMaterial));
            }
            foreach (var columns in new[] { 3, 4 })
            {
                using var shader = Shader.CreateFromSPIRV(RectangularCode(folder, "glsl", "global" + columns), Shader.Mode.TextureBlit);
                using var material = new ShaderMaterial { Shader = shader }; using var target = new DrawableTexture(); target.Setup(1, 1, DrawableTexture.DrawableFormat.RGBAF, Colors.Black);
                RenderingServer.GlobalShaderParameterAdd("rectangular_global", columns == 3 ? RenderingServer.GlobalShaderParameterType.Mat3 : RenderingServer.GlobalShaderParameterType.Mat4, new float[columns * columns]);
                try { Reject<ArgumentException>(() => target.BlitRect(new(0, 0, 1, 1), null, material: material)); }
                finally { RenderingServer.GlobalShaderParameterRemove("rectangular_global"); }
            }
            RenderingServer.FramePostDraw += AfterDraw;
        }
        protected override void OnProcess(double delta)
        {
            _frame++;
            if (_frame is 4 or 6 or (> 30 and <= 94))
            {
                var alternate = _frame == 4 || _frame > 30 && _frame % 2 != 0;
                foreach (var material in _materials) { if (_frame == 6) RectangularReset(material); else RectangularSet(material, alternate); }
                foreach (var value in _native)
                {
                    foreach (var field in RectangularFields)
                        if (_frame == 6) RenderingServer.MaterialSetParam(value.Material, field.Name, default(RID));
                        else RenderingServer.MaterialSetMatrixParam(value.Material, field.Name, alternate ? field.Alternate : field.Initial);
                    RenderingServer.MaterialSetParam(value.Material, "phase", alternate ? 1f : 0f);
                }
                foreach (var particle in _particles) particle.RequestParticlesProcess(.125f);
            }
            if (_frame <= 94) foreach (var blit in _blits) blit.Target.BlitRect(new(0, 0, 208, 1), null, material: blit.Material);
        }
        private void AfterDraw()
        {
            if (_frame is 1 or 4 or 6)
            {
                using var image = GetWindow()!.GetTexture().GetImage()!; var expected = new Color(.25f, .5f, .75f);
                foreach (var (item, paired) in _painters)
                {
                    Near(image.GetPixel((int)item.Position.X + 12, (int)item.Position.Y + 8), expected, "Resource/RID rectangular uniforms reach both canvas stages");
                    if (paired)
                    {
                        var shift = _frame == 4 ? 8 : 4;
                        Near(image.GetPixel((int)item.Position.X + shift - 1, (int)item.Position.Y + 8), Colors.Black, "Affine matrix moves the left edge");
                        Near(image.GetPixel((int)item.Position.X + shift + 15, (int)item.Position.Y + 8), expected, "Affine matrix moves the right edge");
                    }
                }
                foreach (var particle in _particles) Near(image.GetPixel((int)particle.Position.X, (int)particle.Position.Y), GetWindow()!.UseHDR2D ? expected.SRGBToLinear() : expected, "Particle matrix buffers update and reset");
                foreach (var blit in _blits) RectangularBlitValues(blit.Target, _frame == 4);
                foreach (var native in _native) foreach (var field in RectangularFields) Check(RenderingServer.MaterialGetMatrixParam(native.Material, field.Name).SequenceEqual(_frame == 4 ? field.Alternate : field.Initial), "Owned RID matrix values remain copied and typed.");
            }
            if (_frame is 30 or 110) { _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all;
                Console.WriteLine($"Rectangular {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} bytes.");
                Check(owner == 0 && all == 0, "Prepared rectangular material/RID/particle updates allocate zero managed bytes.");
                foreach (var item in _painters) Check(item.Item.Draws == 1, "Matrix changes preserve recorded geometry.");
                if (_frame == 174) Tree!.Quit();
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && RenderingServer.IsAvailable)
            {
                RenderingServer.FramePostDraw -= AfterDraw;
                foreach (var item in _native) { RenderingServer.FreeRID(item.Material); RenderingServer.FreeRID(item.Shader); }
            }
            try { base.Dispose(disposing); }
            finally { if (disposing) for (var index = _owned.Count - 1; index >= 0; index--) _owned[index].Dispose(); }
        }
    }
    private static void RectangularBlit(string folder, string language)
    {
        using var shader = Shader.CreateFromSPIRV(RectangularCode(folder, language, "blit"), Shader.Mode.TextureBlit);
        using var material = new ShaderMaterial { Shader = shader }; using var target = new DrawableTexture(); target.Setup(208, 1, DrawableTexture.DrawableFormat.RGBAF, Colors.Magenta);
        foreach (var phase in new[] { 0, 1, 0 })
        {
            if (phase == 1) RectangularSet(material, true); else RectangularReset(material);
            target.BlitRect(new(0, 0, 208, 1), null, material: material);
            RectangularBlitValues(target, phase == 1);
        }
        Console.WriteLine("Rectangular RGBAF: 208 signed/unclamped components, both storage orders, scalar/array defaults, live values and reset passed: " + language);
    }
    private static void RectangularBlitValues(DrawableTexture target, bool alternate)
    {
        using var image = target.GetImage()!; var index = 0;
        foreach (var field in RectangularFields)
            foreach (var expected in alternate ? field.Alternate : field.Initial)
            {
                var pixel = image.GetPixel(index++, 0);
                Check(pixel.R == expected && pixel.G == expected && pixel.B == 1 && pixel.A == 1, $"Raw rectangular alternate={alternate} component {index}: {pixel} vs {expected}.");
            }
    }

}
