using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    internal static void RenderLightGraphs(string folder, string[] args)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (args.Contains("msaa")) ProjectSettings.Set(ProjectSettings.MSAA2D, Viewport.MSAA.Samples4X);
        using var window = new Window { Size = new(256, 416), Title = "Authored canvas lights", UseHDR2D = args.Contains("hdr") };
        var scene = new LightGraphScene(folder); window.AddChild(scene); Engine.Run(window);
        Check(scene.Complete && window.IsDisposed && !RenderingServer.IsAvailable, "The authored-light host retires its renderer.");
    }

    private sealed class LightGraphPatch(Texture? texture = null) : Entity
    {
        protected override void OnDraw()
        { if (texture is null) DrawRect(new(0, 0, 16, 16), Colors.White); else DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    }

    private sealed class LightGraphScene(string folder) : Node
    {
        private readonly List<Resource> _owned = [];
        private readonly List<(string Name, int Row)> _patches = [];
        private DirectionalLight _first = null!, _second = null!;
        private PointLight _point = null!;
        private LightOccluder _occluder = null!;
        private ShaderMaterial _parameters = null!; private LightGraphPatch _parameterPatch = null!;
        private CanvasModulate _modulate = null!;
        private int _frame; private long _owner, _all; internal bool Complete;
        protected override void OnReady()
        {
            OS.LowProcessorUsageMode = false; RenderingServer.SetDefaultClearColor(Colors.Black);
            _first = new DirectionalLight { Color = Colors.Red, Energy = .25f, Height = 1 };
            _second = new DirectionalLight { Color = Colors.Blue, Energy = .5f, Height = 1, Enabled = false };
            _modulate = new CanvasModulate { Color = Colors.Black };
            AddChild(_first); AddChild(_second); AddChild(_modulate);
            ImageTexture Pixel(Color color)
            {
                using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(color);
                var texture = ImageTexture.CreateFromImage(image); _owned.Add(texture); return texture;
            }
            var white = Pixel(Colors.White); var normal = new CanvasTexture { DiffuseTexture = white, NormalTexture = Pixel(new(.8f, .5f, 1)) }; _owned.Add(normal);
            var specular = new CanvasTexture { DiffuseTexture = white, SpecularTexture = Pixel(new(.5f, .25f, 1, .5f)), SpecularColor = new(.2f, .4f, .6f), SpecularShininess = .5f }; _owned.Add(specular);
            _point = new PointLight { Texture = white, Position = new(128, 128), TextureScale = 1024, Height = 2, Energy = .25f, Enabled = false, ShadowColor = new(.8f, .4f, .2f, 1) }; AddChild(_point);
            var polygon = new OccluderPolygon { Closed = false, Polygon = [new(16, 0), new(16, 416)] }; _owned.Add(polygon);
            _occluder = new LightOccluder { Occluder = polygon, Visible = false }; AddChild(_occluder);
            RenderingServer.GlobalShaderParameterAdd("light_global", RenderingServer.GlobalShaderParameterType.Float, .25f);
            foreach (var name in LightGraphPrograms)
            {
                var shader = ResourceLoader.Load<VisualShader>(IOPath.Combine(folder, "light-" + name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
                var material = new ShaderMaterial { Shader = shader }; _owned.Add(shader); _owned.Add(material);
                var row = _patches.Count * 24 + 4; _patches.Add((name, row));
                var patch = new LightGraphPatch(name == "normal" ? normal : name == "specular" ? specular : null) { Position = new(4, row), Material = material }; AddChild(patch);
                if (name == "parameters") { _parameters = material; _parameterPatch = patch; material.SetShaderParameter("light_gain", .125f); patch.SetInstanceShaderParameter<float>("light_item", .375f); }
            }
            RenderingServer.FramePostDraw += AfterLightDraw;
        }
        protected override void OnProcess(double delta)
        {
            if (_frame is >= 30 and < 94)
            {
                var even = (_frame & 1) == 0; _first.Energy = even ? .25f : .5f;
                _parameters.SetShaderParameter("light_gain", even ? .125f : .25f);
                RenderingServer.GlobalShaderParameterSet("light_global", even ? .25f : .125f);
                _parameterPatch.SetInstanceShaderParameter<float>("light_item", even ? .375f : .25f);
            }
        }
        private void AfterLightDraw()
        {
            _frame++;
            if (_frame is 2 or 5 or 8 or 11 or 14 or 17 or 22 or 25) VerifyLightPixels();
            if (_frame == 3) _second.Enabled = true;
            if (_frame == 6) { _first.Enabled = _second.Enabled = false; _modulate.Color = Colors.White; }
            if (_frame == 9) { _first.Enabled = true; _first.Color = Colors.Green; _first.Energy = .5f; _modulate.Color = Colors.Black; }
            if (_frame == 12) { _first.Enabled = false; _point.Enabled = true; }
            if (_frame == 15) { _point.ShadowEnabled = true; _occluder.Visible = true; }
            if (_frame == 18) { _first.Enabled = true; _point.Enabled = false; _occluder.Visible = false; }
            if (_frame == 20)
                foreach (var shader in _owned.OfType<Shader>()) shader.SetSPIRV(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex), shader.GetSPIRV());
            if (_frame == 23) _first.Energy = 0;
            if (_frame == 26) _first.Energy = .5f;
            if (_frame is 30 or 110) { _owner = GC.GetAllocatedBytesForCurrentThread(); _all = GC.GetTotalAllocatedBytes(true); }
            if (_frame is 94 or 174)
            {
                var owner = GC.GetAllocatedBytesForCurrentThread() - _owner; var all = GC.GetTotalAllocatedBytes(true) - _all;
                Console.WriteLine($"Authored lights {(_frame == 94 ? "active" : "idle")}: 64 frames, owner={owner}, all={all} managed bytes.");
                Check(owner == 0 && all == 0, "Prepared per-light graph execution allocates no managed bytes."); VerifyLightPixels();
                if (_frame == 174) { Complete = true; Tree!.Quit(); }
            }
        }
        private void VerifyLightPixels()
        {
            using var image = GetWindow()!.GetTexture().GetImage()!;
            foreach (var (name, row) in _patches)
            {
                var expected = ExpectedLightPixel(name);
                var actual = image.GetPixel(8, row + 8);
                Check(MathF.Abs(actual.R - expected.R) < .006f && MathF.Abs(actual.G - expected.G) < .006f && MathF.Abs(actual.B - expected.B) < .006f,
                    $"Light graph {name}, frame {_frame}: {actual}, expected {expected}.");
            }
            Console.WriteLine($"Authored light phase {_frame}: {_patches.Count} actual pixel observations passed.");
        }
        private Color ExpectedLightPixel(string name)
        {
            var opaque = name is "builtins" or "global" or "parameters" or "normal" or "specular";
            if (name == "carry") return _frame == 8 ? new(.16f, .32f, .48f) : _frame == 17 ? new(.32f, .16f, .08f) : _frame == 5 ? new(.2f, .4f, .6f) : new(.1f, .2f, .3f);
            if (_frame == 17) return name == "shadow" ? new(.2f, .05f, .075f) : opaque ? new(.8f, .4f, .2f) : new(.4f, .2f, .1f);
            if (_frame == 8) return name == "parameters" ? new(.125f, .125f, .125f) : Colors.White;
            if (name is "builtins" or "global") return _frame == 25 ? Colors.Black : _frame == 5 ? new(.25f, 0, .5f) : _frame == 14 ? new(.25f, .25f, .25f) : _frame >= 11 ? new(0, .5f, 0) : new(.25f, 0, 0);
            if (name == "early" && _frame != 14) return Colors.Black;
            var result = name switch
            {
                "parameters" => _frame >= 94 ? new Color(.25f, .125f, .25f) : new(.125f, .25f, .375f),
                "normal" => new(.4f, .25f, .45f),
                "specular" => GetWindow()!.UseHDR2D ? new(.00827619f, .1592734f, .125f) : new(.05f, .3f, .125f),
                _ => new Color(.125f, .25f, .375f)
            };
            return _frame == 5 ? result * 2 : result;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) RenderingServer.FramePostDraw -= AfterLightDraw;
            if (disposing && RenderingServer.IsAvailable) RenderingServer.GlobalShaderParameterRemove("light_global");
            try { base.Dispose(disposing); }
            finally { if (disposing) for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose(); }
        }
    }
}
