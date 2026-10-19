using Electron2D;

internal static partial class ParticleServerTests
{
    private static void MaterialBehavior()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner); using var mesh = Quad();
        var textures = new List<ImageTexture>();
        using var drawable = new DrawableTexture(); using var deviceView = new TextureRD();
        ImageTexture Texture(params Color[] colors)
        {
            using var image = Image.CreateEmpty(colors.Length, 1, false, Image.Format.Rgbaf);
            for (var i = 0; i < colors.Length; i++) image.SetPixel(i, 0, colors[i]);
            var texture = ImageTexture.CreateFromImage(image); textures.Add(texture); return texture;
        }
        ImageTexture Grid()
        {
            using var image = Image.CreateEmpty(2, 8, false, Image.Format.Rgbaf);
            for (var y = 0; y < 8; y++) for (var x = 0; x < 2; x++) image.SetPixel(x, y, y == 0 ? new(1, 0, 0, 1) : new(5, 4, 0, 1));
            var texture = ImageTexture.CreateFromImage(image); textures.Add(texture); return texture;
        }
        static Vector2 Position(ParticleStateLayout p) => new(p.TransformX.W, p.TransformY.W);
        static Vector2 Velocity(ParticleStateLayout p) => new(p.VelocityAge.X, p.VelocityAge.Y);
        static void Near(float actual, float expected) => Check(Math.Abs(actual - expected) < .0002f, $"Actual {actual}, expected {expected}.");
        static void Vector(Vector2 actual, Vector2 expected) => Check(actual.DistanceTo(expected) < .0002f, $"Actual {actual}, expected {expected}.");
        var cases = new List<(string Name, Action<ParticleProcessMaterial> Setup, Action<ParticleStateLayout> Verify, bool Position)>
        {
            ("linear force", m => { m.InitialVelocityMin = 10; m.LinearAccelMin = 10; }, p => { Vector(Velocity(p), new(11, 0)); Vector(Position(p), new(21.1f, 20)); }, true),
            ("radial force", m => m.RadialAccelMin = 10, p => Vector(Velocity(p), Vector2.One.Normalized()), true),
            ("tangent force", m => m.TangentialAccelMin = 10, p => Vector(Velocity(p), new Vector2(-1, 1).Normalized()), true),
            ("quadratic friction", m => { m.InitialVelocityMin = 10; m.DampingMin = 2; m.ParticleFlagDampingAsFriction = true; }, p => Vector(Velocity(p), new(9, 0)), true),
            ("orbit displacement", m => m.OrbitVelocityMin = 1, p => { var angle = Mathf.Tau * .1f; Vector(Position(p), new(20 * (MathF.Cos(angle) + MathF.Sin(angle)), 20 * (MathF.Cos(angle) - MathF.Sin(angle)))); }, true),
            ("radial pivot", m => { m.VelocityPivot = new(10, 20); m.RadialVelocityMin = 5; }, p => Vector(Position(p), new(20.5f, 20)), true),
            ("inward radial clamp", m => { m.VelocityPivot = new(10, 20); m.RadialVelocityMax = -100; }, p => Vector(Position(p), new(10, 20)), true),
            ("directional texture", m => { m.DirectionalVelocityMin = 2; m.DirectionalVelocityCurve = Texture(new Color(3, 4, 0, 1)); }, p => Vector(Velocity(p), new(6, 8)), true),
            ("two-dimensional motion curve", m => m.DirectionalVelocityCurve = Grid(), p => Vector(Velocity(p), new(2.2f, 1.2f)), true),
            ("two-dimensional appearance curve", m => m.ScaleCurve = Grid(), p => { Near(p.TransformX.X, 2.2f); Near(p.TransformY.Y, 1.2f); }, true),
            ("GPU texture source", m => { drawable.Setup(13, 2, DrawableTexture.DrawableFormat.RGBAF, new Color(2, 2, 2, 1)); drawable.DisplaySize = new(26, 4); m.InitialVelocityMin = 10; m.VelocityLimitCurve = drawable; }, p => Vector(Velocity(p), new(2, 0)), true),
            ("device texture source", m => { deviceView.TextureRDRID = drawable.EnsureImage(); m.ScaleCurve = deviceView; }, p => { Near(p.TransformX.X, 2); Near(p.TransformY.Y, 2); }, true),
            ("all texture channels", m =>
            {
                for (var channel = 1; channel < (int)ParticleProcessMaterial.Parameter.Max; channel++)
                    if (channel is not (13 or 14)) m.SetParamTexture((ParticleProcessMaterial.Parameter)channel, Texture(Colors.White));
                m.AlphaCurve = Texture(Colors.White); m.EmissionCurve = Texture(Colors.White); m.ColorRamp = Texture(Colors.White); m.ColorInitialRamp = Texture(Colors.White); m.VelocityLimitCurve = Texture(Colors.White);
            }, p => { Vector(Velocity(p), Vector2.One.Normalized()); Near(p.Custom.Z, 1); Near(p.Color.X, 1.998f); Near(p.Color.Y, 1.998f); Near(p.Color.Z, 2); }, true),
            ("velocity limit and scale", m => { m.InitialVelocityMin = 10; m.VelocityLimitCurve = Texture(new Color(2, 0, 0, 1)); m.ScaleMin = 2; m.ScaleOverVelocityMax = 4; m.ScaleOverVelocityCurve = Texture(new Color(.5f, .5f, .5f, 1), new(1.5f, 1.5f, 1.5f, 1)); }, p => { Vector(Velocity(p), new(2, 0)); Near(p.TransformX.X, 2); Near(p.TransformY.Y, 2); }, true),
            ("curved angular accumulation", m => { m.AngleMin = 60; m.AngleCurve = Texture(new Color(.5f, .5f, .5f, 1)); m.AngularVelocityMin = 2; m.AngularVelocityCurve = Texture(new Color(.5f, .5f, .5f, 1)); }, p => Near(p.Custom.X, MathF.PI / 6 + .1f), true),
            ("vector scale", m => { m.ScaleMin = 2; m.ScaleCurve = Texture(new Color(2, 3, 1, 1)); }, p => { Near(p.TransformX.X, 4); Near(p.TransformY.Y, 6); }, true),
            ("small base scale", m => { m.ScaleMax = .0001f; m.ScaleCurve = Texture(new Color(100, 100, 100, 1)); }, p => { Near(p.TransformX.X, .1f); Near(p.TransformY.Y, .1f); }, true),
            ("align velocity", m => { m.InitialVelocityMin = 10; m.ParticleFlagAlignY = true; }, p => { Near(p.TransformX.X, 0); Near(p.TransformX.Y, 1); Near(p.TransformY.X, -1); Near(p.TransformY.Y, 0); }, true),
            ("animation phase", m => { m.AnimOffsetMin = .1f; m.AnimOffsetCurve = Texture(new Color(.2f, 0, 0, 1)); m.AnimSpeedMin = 2; m.AnimSpeedCurve = Texture(new Color(.5f, 0, 0, 1)); }, p => Near(p.Custom.Z, .4f), true),
            ("color channels", m => { m.ColorRamp = Texture(new Color(.5f, .4f, .3f, .8f)); m.ColorInitialRamp = Texture(new Color(.5f, .5f, .5f, .5f)); m.AlphaCurve = Texture(new Color(.5f, 0, 0, 1)); m.EmissionCurve = Texture(new Color(1, 0, 0, 1)); }, p => { Near(p.Color.X, .4997f); Near(p.Color.Y, .3997f); Near(p.Color.Z, .3f); Near(p.Color.W, .2f); }, true),
            ("point offset", m => m.EmissionShapeOffset = new(20, 20), p => Vector(Position(p), new(20, 20)), false),
            ("rectangle scale", m => { m.EmissionShape = ParticlesEmissionShape.Rectangle; m.EmissionRectExtents = new(2, 3); m.EmissionShapeScale = new(2, 3); m.EmissionShapeOffset = new(20, 20); }, p => Check(Position(p).X is >= 16 and <= 24 && Position(p).Y is >= 11 and <= 29, "Rectangle bounds and authored scale."), false),
            ("sphere projection", m => { m.EmissionShape = ParticlesEmissionShape.Sphere; m.EmissionSphereRadius = 3; m.EmissionShapeOffset = new(20, 20); }, p => Check(Position(p).DistanceTo(new(20, 20)) <= 3, "Projected sphere bounds."), false),
            ("sphere surface projection", m => { m.EmissionShape = ParticlesEmissionShape.SphereSurface; m.EmissionSphereRadius = 3; m.EmissionShapeOffset = new(20, 20); }, p => Check(Position(p).DistanceTo(new(20, 20)) <= 3, "Projected surface bounds."), false),
            ("ring area", m => { m.EmissionShape = ParticlesEmissionShape.Ring; m.EmissionRingInnerRadius = 2; m.EmissionRingRadius = 3; m.EmissionShapeOffset = new(20, 20); }, p => Check(Position(p).DistanceTo(new(20, 20)) is >= 2 and <= 3, "Ring radii."), false),
            ("directed points", m => { m.EmissionShape = ParticlesEmissionShape.DirectedPoints; m.InitialVelocityMin = 10; m.EmissionPointCount = 2; m.EmissionPointTexture = Texture(new Color(2, 3, 0, 1), new(4, 5, 0, 1)); m.EmissionNormalTexture = Texture(new Color(0, 1, 0, 1), new(0, 1, 0, 1)); m.EmissionColorTexture = Texture(Colors.Green, Colors.Green); m.EmissionShapeOffset = new(20, 20); }, p => { Vector(Velocity(p), new(0, 10)); Check(Position(p).DistanceTo(new(22, 24)) < .0002f || Position(p).DistanceTo(new(24, 26)) < .0002f, "Packed point coordinates and directed normal."); Near(p.Color.Y, 1); }, false),
        };
        var current = 0; ParticleProcessMaterial? material = null; RID particles = default, item = default;
        void Start()
        {
            material = new() { Gravity = Vector2.Zero, Spread = 0, Color = Colors.White };
            cases[current].Setup(material);
            particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetFixedFPS(particles, 10); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 1); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID()); item = Attach(owner, particles, mesh);
            State(particles).Seed = 123; RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.White, Colors.White, cases[current].Position ? 1 : 0); RenderingServer.ParticlesRequestProcessTime(particles, .1f);
        }
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); Start();
            RenderingServer.FramePostDraw += () =>
            {
                var state = State(particles); var particle = state.GPU!.Read(state.GPU.State, 1)[0];
                try { cases[current].Verify(particle); }
                catch (Exception error) { throw new InvalidOperationException(cases[current].Name, error); }
                RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); material!.Dispose(); material = null;
                if (++current == cases.Count) root.Tree!.Quit(); else Start();
            };
        };
        try { Engine.Run(root); Check(current == cases.Count, "Every native material behavior case completed."); }
        finally { material?.Dispose(); foreach (var texture in textures) texture.Dispose(); }
        Console.WriteLine($"{current} native material force, curve, appearance and distribution cases passed.");
    }
}
