using Electron2D;

internal static partial class ParticleServerTests
{
    private static void VisualGraphs(string folder)
    {
        CustomNodes.Register();
        using (var graph = ResourceLoader.Load<VisualShader>(System.IO.Path.Combine(folder, "particle-basic.e2dres"), ResourceLoader.CacheMode.IgnoreDeep))
            ParticleShaderTests.VerifyBirthNumbers(graph.GetSPIRV());
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64), SDFOversize = CanvasSDFOversize.Percent100, SDFScale = CanvasSDFScale.Percent100 };
        var owner = new Entity(); root.AddChild(owner); using var quad = Quad();
        using var contour = new OccluderPolygon { Polygon = [new(10, 10), new(30, 10), new(30, 30), new(10, 30)] };
        root.AddChild(new LightOccluder { Occluder = contour });
        var files = Directory.GetFiles(folder, "particle-*.e2dres").Order(StringComparer.Ordinal).ToArray();
        var current = 0; var counterRound = 0; RID particles = default, item = default, child = default; VisualShader? shader = null; ShaderMaterial? material = null;
        string Name() => System.IO.Path.GetFileNameWithoutExtension(files[current]);
        void Emit(int count)
        {
            for (var i = 0; i < count; i++) RenderingServer.ParticlesEmit(particles, new Transform(0, Name() == "particle-collision-2" ? new Vector2(40, 40) : new Vector2(11, 20)), new(2, 0), Colors.White, new(.1f, .2f, .3f, .4f), Name().StartsWith("particle-shape-", StringComparison.Ordinal) || Name().StartsWith("particle-mesh-", StringComparison.Ordinal) ? 6 : 31);
            RenderingServer.ParticlesRequestProcessTime(particles, .125f);
        }
        void Start()
        {
            shader = ResourceLoader.Load<VisualShader>(files[current], ResourceLoader.CacheMode.IgnoreDeep); material = new() { Shader = shader };
            particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, Name() == "particle-number" ? 67 : 1);
            RenderingServer.ParticlesSetEmitting(particles, false); RenderingServer.ParticlesSetFixedFPS(particles, 8); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetCollisionBaseSize(particles, 1); RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID());
            item = Attach(owner, particles, quad);
            if ((Name().StartsWith("particle-emit-", StringComparison.Ordinal) || Name().StartsWith("particle-payload-", StringComparison.Ordinal)))
            {
                child = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(child, 2); RenderingServer.ParticlesSetEmitting(child, false); RenderingServer.ParticlesSetFixedFPS(child, 0); RenderingServer.ParticlesSetSpeedScale(child, 0); RenderingServer.ParticlesSetLifetime(child, 100); RenderingServer.ParticlesSetUseLocalCoordinates(child, true); RenderingServer.ParticlesSetSubemitter(particles, child);
            }
            Emit(Name() == "particle-number" ? 67 : 1);
        }
        void Near(float actual, float expected, string field) => Check(MathF.Abs(actual - expected) < .004f, Name() + " " + field + $": {actual}, expected {expected}.");
        root.Ready += _ =>
        {
            RenderingServer.GlobalShaderParameterAdd("domain_global", RenderingServer.GlobalShaderParameterType.Vec3, new Vector3(.25f, .5f, .75f)); Start(); RenderingServer.FramePostDraw += AfterDraw;
        };
        void AfterDraw()
        {
            var name = Name(); var source = State(particles); var records = source.GPU!.Read(source.GPU.State, source.Amount); var value = records[0];
            if (name == "particle-number")
            {
                var numbers = records.Select(p => (int)p.Color.X).Order().ToArray();
                Check(numbers.SequenceEqual(Enumerable.Range((counterRound == 3 ? 0 : counterRound) * 67, 67)), "Device birth numbers are unique, consecutive and continue when the same slots are reused.");
                Check(records.All(p => (p.State.X & 1) == 0), "The graph's process stage deactivates each newborn after recording its number.");
                if (++counterRound < 4) { if (counterRound == 3) RenderingServer.ParticlesRestart(particles); Emit(67); return; }
            }
            else if (name == "particle-provider-contact")
            { Near(value.Color.X, .75f, "provider-only collide setup x"); Near(value.Color.Y, .5f, "provider-only collide setup y"); Near(value.Color.Z, .25f, "provider-only collide setup z"); }
            else if (name.StartsWith("particle-random-range-", StringComparison.Ordinal))
            { Near(value.Color.X, .25f, "random oracle x"); Near(value.Color.Y, .5f, "random oracle y"); Near(value.Color.Z, .75f, "random oracle z"); }
            else if (name.StartsWith("particle-parameter-", StringComparison.Ordinal))
            {
                var role = int.Parse(name.Split('-')[3]);
                var expected = role switch { 0 => new Vector3(.25f, .25f, .25f), 1 => new Vector3(7f / 16, 7f / 16, 7f / 16), 2 => new Vector3(11f / 16, 11f / 16, 11f / 16), 3 => new Vector3(.125f, .625f, .25f), 4 => new Vector3(.125f, .5f, .875f), 5 => new Vector3(.125f, .625f, .875f), 6 => new Vector3(.75f, .75f, .75f), _ => new Vector3(.2f, .3f, .4f) };
                Near(value.Color.X, expected.X, "parameter x"); Near(value.Color.Y, expected.Y, "parameter y"); Near(value.Color.Z, expected.Z, "parameter z");
            }
            else if (name.StartsWith("particle-custom-", StringComparison.Ordinal) || name.StartsWith("particle-global-", StringComparison.Ordinal) || name.StartsWith("particle-shared-", StringComparison.Ordinal) || name.StartsWith("particle-image-", StringComparison.Ordinal))
            {
                var expected = name.StartsWith("particle-image-", StringComparison.Ordinal) ? new Vector3(64f / 255, 128f / 255, 192f / 255) : name.EndsWith("-1", StringComparison.Ordinal) && name.StartsWith("particle-custom-", StringComparison.Ordinal) ? new Vector3(.125f, .375f, .625f) : new Vector3(.25f, .5f, .75f);
                Near(value.Color.X, expected.X, "domain x"); Near(value.Color.Y, expected.Y, "domain y"); Near(value.Color.Z, expected.Z, "domain z");
            }
            else if (name.StartsWith("particle-restart-", StringComparison.Ordinal))
            {
                var expected = name == "particle-restart-4" || name == "particle-restart-7" ? 1 : 0; Near(value.Color.X, expected, "contextual restart");
            }
            else if (name.StartsWith("particle-collision-", StringComparison.Ordinal))
            {
                if (name.EndsWith("-0", StringComparison.Ordinal)) { Near(value.Color.X, -1, "collision normal x"); Near(value.Color.Y, 0, "collision normal y"); }
                else if (name.EndsWith("-1", StringComparison.Ordinal)) Check(value.Color.X > 1 && value.Color.X < 3, "SDF depth reaches collide graph.");
                else Near(value.Color.X, .25f, "noncontact selects process graph");
                Near(value.Custom.X, .2f, "process custom follows either branch");
            }
            else if (name.StartsWith("particle-transform-", StringComparison.Ordinal))
            {
                if (name.EndsWith("-2", StringComparison.Ordinal)) { Near(value.TransformX.X, 2, "matrix xx"); Near(value.TransformX.Y, 4, "matrix xy"); Near(value.TransformY.X, 3, "matrix yx"); Near(value.TransformX.W, 21, "matrix origin x"); Near(value.TransformY.W, 22, "matrix origin y"); }
                else if (name.EndsWith("-0", StringComparison.Ordinal)) { Near(value.TransformX.X, 0, "start rotation xx"); Near(value.TransformY.X, 2, "positive planar rotation yx"); Near(value.TransformX.W, 11, "start preserves origin"); }
                else { Near(value.TransformX.X, 2, "scale replaces preceding angle matrix"); Near(value.TransformY.Y, 2, "scale yy"); Near(value.TransformX.W, 0, "process replacement resets origin"); }
            }
            else if (name.StartsWith("particle-math-", StringComparison.Ordinal))
            {
                var kind = int.Parse(name.Split('-')[3]); var radial = new Vector2(11, 20).Normalized();
                var expected = kind switch { 0 => new Vector3(1, 0, 0), 1 or 2 => new Vector3(0, -1, 0), 3 => new Vector3(1, 0, 0), 4 => new Vector3(radial.X, radial.Y, 0), 5 => new Vector3(radial.Y, -radial.X, 0), _ => new Vector3(.25f, .25f, .25f) };
                Near(value.Color.X, expected.X, "color.x"); Near(value.Color.Y, expected.Y, "color.y"); Near(value.Color.Z, expected.Z, "color.z");
            }
            else if (name.StartsWith("particle-input-", StringComparison.Ordinal))
            {
                var input = name.Split('-')[3];
                var expected = input switch { "active" or "color" or "transform" or "emission_transform" => new Vector3(1, 1, 1), "custom" => new Vector3(.1f, .2f, .3f), "delta" => new Vector3(.125f, .125f, .125f), "lifetime" => new Vector3(100, 100, 100), "velocity" => new Vector3(2, 0, 0), "random_seed" => new Vector3(source.Seed, source.Seed, source.Seed), "time" => new Vector3(value.Color.X, value.Color.X, value.Color.X), _ => Vector3.Zero };
                Check(float.IsFinite(value.Color.X) && value.Color.X >= 0, "Input values have finite renderer-owned producers.");
                Near(value.Color.X, expected.X, "color.x"); Near(value.Color.Y, expected.Y, "color.y"); Near(value.Color.Z, expected.Z, "color.z");
            }
            else if (name.StartsWith("particle-expression-", StringComparison.Ordinal))
                Near(value.Color.X, 1, "return preserves following native publication and bypasses the graph root");
            else if (name.StartsWith("particle-shape-", StringComparison.Ordinal))
            {
                var position = new Vector2(value.TransformX.W, value.TransformY.W);
                Check(position.IsFinite() && position.Length() <= 10.001f, "Planar emission shapes stay within their numeric radius.");
                if (name.EndsWith("-1", StringComparison.Ordinal)) Check(Math.Abs(position.X) <= .5001f && Math.Abs(position.Y) <= .5001f, "Box input denotes full extents.");
            }
            else if (name.StartsWith("particle-mesh-", StringComparison.Ordinal))
            {
                var kind = int.Parse(name.Split('-')[2]);
                if (kind is 0 or 2) { Near(value.TransformX.W, 0, "empty mesh position"); Near(value.Color.W, 0, "empty mesh alpha"); }
                else if (kind is 1 or 4)
                {
                    Near(value.TransformX.W, 2, "mesh x"); Near(value.TransformY.W, 0, "mesh y"); Near(value.Color.X, 64f / 255, "quantized color"); Near(value.Color.Y, 128f / 255, "quantized color");
                    var normal = ((VisualShaderNodeParticleMeshEmitter)shader!.GetNode(VisualShader.Type.Start, 2)).Mesh!.SurfaceGetArrays(0).Normals[0];
                    Near(value.VelocityAge.X, normal.X, "canonical mesh normal x"); Near(value.VelocityAge.Y, normal.Y, "canonical mesh normal y"); Near(value.Custom.X, .1f, "uv x"); Near(value.Custom.Y, .2f, "uv y"); Near(value.Custom.W, .6f, "uv2 x");
                }
                else Check(value.TransformX.W is 2 or 3, "All surfaces contribute their vertices without reindexing channels.");
            }
            else if (name.StartsWith("particle-random-", StringComparison.Ordinal))
                Check(value.Custom.X is >= -1.00001f and <= 1.00001f && value.Custom.Y is >= -1.00001f and <= 1.00001f, "Every random component follows its authored range.");
            if (child.IsValid())
            {
                var payload = State(child); var births = payload.GPU!.Read(payload.GPU.State, 2).Where(p => (p.State.X & 1) != 0).ToArray();
                Check(births.Length == (name == "particle-payload-0" ? 0 : 1), "Side effects honor the condition and bounded child intake.");
                if (name == "particle-payload-1") { Near(births[0].TransformX.W, 7, "payload x"); Near(births[0].TransformY.W, 8, "payload y"); Near(births[0].VelocityAge.X, 3, "payload velocity"); Near(births[0].Color.X, .25f, "payload color"); Near(births[0].Custom.W, .9f, "payload custom"); }
                else if (births.Length != 0)
                {
                    var flags = int.Parse(name.Split('-')[2]); Near(births[0].TransformX.W, (flags & 1) != 0 ? value.TransformX.W : 0, "mask position"); Near(births[0].VelocityAge.X, (flags & 4) != 0 ? value.VelocityAge.X : 0, "mask velocity"); Near(births[0].Custom.X, (flags & 16) != 0 ? value.Custom.X : 0, "mask custom");
                }
                RenderingServer.FreeRID(child); child = default;
            }
            RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); material!.Dispose(); shader!.Dispose(); material = null; shader = null;
            if (++current == files.Length) root.Tree!.Quit(); else Start();
        }
        try { Engine.Run(root); Check(current == files.Length, "The complete imported particle corpus executed."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= AfterDraw; material?.Dispose(); shader?.Dispose(); }
        Console.WriteLine($"{current} native particle graph programs passed; {counterRound * 67} birth-number observations across slot reuse/reset.");
    }
}
