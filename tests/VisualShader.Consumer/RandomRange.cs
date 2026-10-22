using Electron2D;
using V = Electron2D.VisualShader;
using P = Electron2D.VisualShaderNode.PortType;

internal static partial class GraphChecks
{
    internal static void RandomRangeCPU()
    {
        using var node = new VisualShaderNodeRandomRange();
        Check(node.GetInputPortDefaultValue<Vector3>(0) == Vector3.One && node.GetInputPortDefaultValue<float>(1) == 0 && node.GetInputPortDefaultValue<float>(2) == 1, "Fixed seed and scalar bounds have their declared defaults.");
        Reject<ArgumentException>(() => node.SetInputPortDefaultValue(0, 1f));
        Reject<ArgumentException>(() => node.SetInputPortDefaultValue(1, Vector3.One));
        Reject<ArgumentOutOfRangeException>(() => node.SetInputPortDefaultValue(3, 0f));
        Reject<ArgumentException>(() => node.SetInputPortDefaultValue(0, new Vector3(float.NaN, 0, 0)));
        using var copy = (VisualShaderNodeRandomRange)node.Duplicate(true);
        copy.SetInputPortDefaultValue(1, .25f); Check(node.GetInputPortDefaultValue<float>(1) == 0, "Copied defaults are independent.");
        using var graph = new V(); graph.AddNode(V.Type.Fragment, node, default, 2); graph.AddNode(V.Type.Fragment, node, default, 3);
        var file = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-random-" + Guid.NewGuid().ToString("N") + ".e2dres");
        try
        {
            ResourceSaver.Save(graph, file, SaverFlags.BundleResources | SaverFlags.Compress | SaverFlags.SaveBigEndian);
            using var loaded = ResourceLoader.Load<V>(file, ResourceLoader.CacheMode.IgnoreDeep);
            Check(loaded.GetNode(V.Type.Fragment, 2) is VisualShaderNodeRandomRange && ReferenceEquals(loaded.GetNode(V.Type.Fragment, 2), loaded.GetNode(V.Type.Fragment, 3)), "Compiled factory restores the concrete definition and shared aliases.");
        }
        finally { System.IO.File.Delete(file); }
        Console.WriteLine("RandomRange: defaults, type/finite/port guards, duplicate and compressed big-endian shared archive passed.");
    }
    internal static void AuthorRandomRange(string folder)
    {
        Directory.CreateDirectory(folder);
        Vector3[] seeds = [Vector3.One, Vector3.Zero, new(.125f, .5f, .875f), new(-.25f, -.5f, -.875f), new(-1.5f, 2.25f, -.75f), new(.00001f, -.00002f, .00003f)];
        foreach (var stage in ParticleStages.Concat([V.Type.Vertex, V.Type.Fragment, V.Type.TextureBlit]))
            for (var sample = 0; sample < seeds.Length; sample++)
            {
                using var graph = new V(); graph.SetMode(stage == V.Type.TextureBlit ? Shader.Mode.TextureBlit : stage is V.Type.Vertex or V.Type.Fragment ? Shader.Mode.CanvasItem : Shader.Mode.Particles);
                using var node = new VisualShaderNodeRandomRange(); using var zero = new VisualShaderNodeRandomRange(); using var equal = new VisualShaderNodeRandomRange(); using var reversed = new VisualShaderNodeRandomRange();
                zero.SetInputPortDefaultValue(0, Vector3.Zero); zero.SetInputPortDefaultValue(1, .25f); zero.SetInputPortDefaultValue(2, .75f);
                equal.SetInputPortDefaultValue(1, -.5f); equal.SetInputPortDefaultValue(2, -.5f);
                reversed.SetInputPortDefaultValue(0, Vector3.Zero); reversed.SetInputPortDefaultValue(1, .75f); reversed.SetInputPortDefaultValue(2, .25f);
                using var seed = new VisualShaderNodeVec3Parameter { ParameterName = "range_seed", DefaultValueEnabled = true, DefaultValue = seeds[sample] };
                using var low = new VisualShaderNodeFloatParameter { ParameterName = "range_low", DefaultValueEnabled = true, DefaultValue = -2f };
                using var high = new VisualShaderNodeFloatParameter { ParameterName = "range_high", DefaultValueEnabled = true, DefaultValue = 3f };
                // Independent pinned shader function keeps the row-vector multiplication oracle outside the node generator.
                using var reference = new VisualShaderNodeGlobalExpression { Expression = "vec3 e2d_random_range(vec3 p){return vec3(50.0);} vec3 range_reference(vec3 p){p*=mat3(vec3(127.1,311.7,-53.7),vec3(269.5,183.3,77.1),vec3(-301.7,27.3,215.3));return 2.0*fract(fract(p)*4375.55)-1.;}" };
                using var observe = new VisualShaderNodeExpression();
                for (var port = 0; port < 5; port++) observe.AddInputPort(port, P.Scalar, "v" + port);
                observe.AddInputPort(5, P.Vector3, "seed_value"); observe.AddInputPort(6, P.Scalar, "low_value"); observe.AddInputPort(7, P.Scalar, "high_value");
                observe.AddOutputPort(0, stage == V.Type.TextureBlit ? P.Vector4 : P.Vector3, "observed");
                observe.Expression = "bool a=abs(v0-range_reference(vec3(1.0)).x)<0.00001;bool b=abs(v1-mix(low_value,high_value,range_reference(seed_value).x))<0.00001;bool c=v2==-0.25 && v3==-0.5 && v4==1.25;bool valid=a&&b&&c; observed=" + (stage == V.Type.TextureBlit ? "vec4(valid?vec3(.25,.5,.75):vec3(a?0.0:1.0,b?0.0:1.0,c?0.0:1.0),1.0);" : "valid?vec3(.25,.5,.75):vec3(a?0.0:1.0,b?0.0:1.0,c?0.0:1.0);");
                if (stage == V.Type.TextureBlit) observe.Expression = "observed=vec4(v1,mix(low_value,high_value,range_reference(seed_value).x),(abs(v0-range_reference(vec3(1.0)).x)<0.00001 && v2==-0.25 && v3==-0.5 && v4==1.25)?v2:50.0,1.0);";
                VisualShaderNode[] nodes = [node, node, zero, equal, reversed, seed, low, high, reference, observe];
                for (var i = 0; i < nodes.Length; i++) graph.AddNode(stage, nodes[i], default, i + 2);
                for (var i = 0; i < 5; i++) graph.ConnectNodes(stage, i + 2, 0, 11, i);
                graph.ConnectNodes(stage, 7, 0, 3, 0); graph.ConnectNodes(stage, 8, 0, 3, 1); graph.ConnectNodes(stage, 9, 0, 3, 2);
                graph.ConnectNodes(stage, 7, 0, 11, 5); graph.ConnectNodes(stage, 8, 0, 11, 6); graph.ConnectNodes(stage, 9, 0, 11, 7);
                graph.ConnectNodes(stage, 11, 0, 0, stage == V.Type.Vertex ? 2 : stage is V.Type.Fragment or V.Type.TextureBlit ? 0 : ColorPort(stage));
                var prefix = stage == V.Type.TextureBlit ? "blit" : stage is V.Type.Vertex or V.Type.Fragment ? "canvas" : "particle";
                SaveDomain(graph, folder, $"{prefix}-random-range-{(int)stage}-{sample}");
            }
        Console.WriteLine("Authored 48 RandomRange programs across eight stages, each checking default/connected seeds and unclamped/equal/reversed bounds.");
    }
    private static void CheckRandomBlit(string name, ShaderMaterial material)
    {
        using var target = new DrawableTexture(); target.Setup(8, 8, DrawableTexture.DrawableFormat.RGBAF, Colors.Magenta);
        var original = 0f;
        for (var phase = 0; phase < 4; phase++)
        {
            if (phase == 1) { material.SetShaderParameter("range_seed", Vector3.Zero); material.SetShaderParameter("range_low", .75f); material.SetShaderParameter("range_high", -.25f); }
            if (phase == 2) { material.SetShaderParameter("range_seed", new Vector3(-.25f, .5f, -.875f)); material.SetShaderParameter("range_low", -.5f); material.SetShaderParameter("range_high", -.5f); }
            if (phase == 3) { material.SetShaderParameter("range_seed", (Texture?)null); material.SetShaderParameter("range_low", (Texture?)null); material.SetShaderParameter("range_high", (Texture?)null); }
            target.BlitRectMulti(new(0, 0, 8, 8), [], [], material: material);
            using var image = target.GetImage()!; var value = image.GetPixel(3, 3);
            Check(float.IsFinite(value.R) && MathF.Abs(value.R - value.G) < .00001f && value.B == -.25f && value.A == 1f, $"{name} raw float node/reference and unclamped bounds: {value}.");
            if (phase == 0) { original = value.R; if (name.EndsWith("-1", StringComparison.Ordinal)) Check(value.R == -7f, "Zero uniform seed extrapolates below the negative first bound."); }
            if (phase == 1) Check(value.R == 1.75f, "Live zero seed with reversed bounds retains extrapolation.");
            if (phase == 2) Check(value.R == -.5f, "Live equal negative bounds retain their value.");
            if (phase == 3) Check(value.R == original, "Removing overrides restores the repeatable seed and original bounds.");
        }
    }
    private sealed partial class DomainScene
    {
        private GPUParticles[] _randomParticles = [];
        private void PrepareRandomParticles(string folder, Texture texture)
        {
            var files = Directory.GetFiles(folder, "particle-random-range-*.e2dres").Order(StringComparer.Ordinal).ToArray();
            _randomParticles = new GPUParticles[files.Length];
            if (files.Length == 0) return;
            var contour = OwnDomain(new OccluderPolygon { Polygon = [new(16, 32), new(112, 32), new(112, 112), new(16, 112)] });
            AddChild(new LightOccluder { Occluder = contour });
            for (var i = 0; i < files.Length; i++)
            {
                var shader = OwnDomain(ResourceLoader.Load<V>(files[i], ResourceLoader.CacheMode.IgnoreDeep));
                var material = OwnDomain(new ShaderMaterial { Shader = shader });
                var particle = new GPUParticles { Amount = 1, Emitting = false, Lifetime = 1000, FixedFPS = 8, Interpolate = false, LocalCoords = true, SpeedScale = 0, ProcessMaterial = material, Texture = texture };
                _randomParticles[i] = particle; AddChild(particle);
                particle.EmitParticle(new Transform(0, new Vector2(24 + i % 6 * 16, 40 + i / 6 * 16)), Vector2.Zero, Colors.White, Colors.White, (ParticlesEmitFlags)31);
                particle.RequestParticlesProcess(.125f);
            }
        }
        private void CheckRandomParticles(Image image)
        {
            for (var i = 0; i < _randomParticles.Length; i++)
            {
                var position = _randomParticles[i].CaptureRect().GetCenter(); var actual = image.GetPixel((int)position.X, (int)position.Y);
                var expected = new Color(.25f, .5f, .75f); if (GetWindow()!.UseHDR2D) expected = expected.SRGBToLinear();
                Check(MathF.Abs(actual.R - expected.R) < .005f && MathF.Abs(actual.G - expected.G) < .005f && MathF.Abs(actual.B - expected.B) < .005f, $"RandomRange particle {i} native pixel {actual} at {position}.");
            }
            if (_randomParticles.Length != 0) Console.WriteLine($"{_randomParticles.Length} RandomRange particle programs: actual pixels across all five lifecycle stages passed.");
        }
    }
    private sealed partial class GraphScene
    {
        private readonly List<(ShaderMaterial Material, Vector2 Position)> _randomCases = [];
        private void ProcessRandomRange()
        {
            if (_frame is not (4 or 6) && _frame is not (> 30 and <= 94)) return;
            foreach (var item in _randomCases)
            {
                if (_frame == 6) { item.Material.SetShaderParameter("range_seed", (Texture?)null); item.Material.SetShaderParameter("range_low", (Texture?)null); item.Material.SetShaderParameter("range_high", (Texture?)null); }
                else { item.Material.SetShaderParameter("range_seed", new Vector3(-.75f, _frame * .125f, .625f)); item.Material.SetShaderParameter("range_low", .75f); item.Material.SetShaderParameter("range_high", -.25f); }
            }
        }
        private void CheckRandomRange()
        {
            if (_randomCases.Count == 0 || _frame is not (5 or 7)) return;
            using var image = GetWindow()!.GetTexture().GetImage()!;
            foreach (var item in _randomCases) Check(EffectPixelsMatch(image.GetPixel((int)item.Position.X + 8, (int)item.Position.Y + 8), new(.25f, .5f, .75f), GetWindow()!.UseHDR2D), "Live RandomRange seed/bounds update and reset.");
        }
    }
}
