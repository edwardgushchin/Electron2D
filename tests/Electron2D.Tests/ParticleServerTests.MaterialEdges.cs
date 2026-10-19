using Electron2D;

internal static partial class ParticleServerTests
{
    private static void MaterialEdges()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner); using var mesh = Quad();
        using var image = Image.CreateFromData(1, 1, false, Image.Format.Rgbaf, new byte[16]); var invalid = image.GetData(); System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(invalid, float.NaN); System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(invalid.AsSpan(12), 1);
        using var nanImage = Image.CreateFromData(1, 1, false, Image.Format.Rgbaf, invalid); using var nanTexture = ImageTexture.CreateFromImage(nanImage);
        static Vector2 Velocity(ParticleStateLayout p) => new(p.VelocityAge.X, p.VelocityAge.Y);
        static void Near(Vector2 actual, Vector2 expected) => Check(actual.DistanceTo(expected) < .0002f, $"Actual {actual}, expected {expected}.");
        var cases = new (string Name, Action<ParticleProcessMaterial> Configure, bool Local, int Flags, float Time, Action<ParticleStateLayout> Verify)[]
        {
            ("world supplied velocity", m => m.InheritVelocityRatio = .5f, false, 5, 0, p => Near(Velocity(p), new(-4, 5))),
            ("world initial velocity", m => { m.InitialVelocityMin = 5; m.InheritVelocityRatio = .5f; }, false, 1, 0, p => Near(Velocity(p), new(2, 13))),
            ("local supplied velocity", m => m.InheritVelocityRatio = .5f, true, 5, 0, p => Near(Velocity(p), new(3, 5))),
            ("zero direction", m => { m.Direction = Vector2.Zero; m.InitialVelocityMin = 5; }, true, 1, 0, p => Near(Velocity(p), new(5, 0))),
            ("turbulence full influence", m => { m.InitialVelocityMin = 10; m.TurbulenceEnabled = true; m.TurbulenceInfluenceMin = 1; }, true, 1, .125f, p => Check(Velocity(p).IsFinite() && Math.Abs(Velocity(p).Length() - 10) < .001f, "Turbulence redirects finite velocity while retaining full-influence speed.")),
            ("turbulence zero influence", m => { m.InitialVelocityMin = 10; m.TurbulenceEnabled = true; m.TurbulenceInfluenceMax = 0; m.TurbulenceNoiseSpeed = new(1, 2, 3); }, true, 1, .125f, p => Near(Velocity(p), new(10, 0))),
            ("turbulence initial displacement", m => { m.TurbulenceEnabled = true; m.TurbulenceInitialDisplacementMin = 2; m.EmissionShapeOffset = new(20, 20); }, true, 0, 0, p => { var position = new Vector2(p.TransformX.W, p.TransformY.W); Check(position.IsFinite() && position.DistanceTo(new(20, 20)) is > .00001f and < .21f, "Initial turbulence displaces a generated birth in the numeric noise field."); }),
            ("zero noise scale", m => { m.TurbulenceEnabled = true; m.TurbulenceNoiseScale = 0; m.InitialVelocityMin = 10; }, true, 1, .125f, p => Check(p.TransformX.IsFinite() && p.TransformY.IsFinite() && p.VelocityAge.IsFinite(), "Zero noise control remains finite.")),
            ("nonfinite scale texture", m => m.ScaleCurve = nanTexture, true, 1, 0, p => Check((p.State.X & 1) == 0 || p.TransformX.IsFinite() && p.TransformY.IsFinite(), "Nonfinite source samples cannot leave an active nonfinite transform.")),
            ("nonfinite velocity texture", m => m.DirectionalVelocityCurve = nanTexture, true, 1, 0, p => Check((p.State.X & 1) == 0, "Nonfinite derived velocity deactivates the record.")),
        };
        var current = 0; ParticleProcessMaterial? material = null; RID particles = default, item = default;
        void Start()
        {
            var test = cases[current]; material = new() { Gravity = Vector2.Zero, Spread = 0 }; test.Configure(material);
            particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetFixedFPS(particles, 8); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 10); RenderingServer.ParticlesSetUseLocalCoordinates(particles, test.Local);
            RenderingServer.ParticlesSetEmissionTransform(particles, new Transform(new(0, 2), new(-3, 0), new(10, 12))); RenderingServer.ParticlesSetEmitterVelocity(particles, new(4, 6)); RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID()); item = Attach(owner, particles, mesh);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), new(1, 2), Colors.White, Colors.White, test.Flags); if (test.Time > 0) RenderingServer.ParticlesRequestProcessTime(particles, test.Time);
        }
        root.Ready += _ =>
        {
            Start(); RenderingServer.FramePostDraw += () =>
            {
                var test = cases[current]; var state = State(particles); var particle = state.GPU!.Read(state.GPU.State, 1)[0];
                try { test.Verify(particle); } catch (Exception error) { throw new InvalidOperationException(test.Name, error); }
                RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); material!.Dispose(); material = null;
                if (++current == cases.Length) root.Tree!.Quit(); else Start();
            };
        };
        try { Engine.Run(root); Check(current == cases.Length, "All material edge frames completed."); }
        finally { material?.Dispose(); }
        Console.WriteLine($"{current} native material emission-basis, turbulence, zero and nonfinite edge cases passed.");
    }
}
