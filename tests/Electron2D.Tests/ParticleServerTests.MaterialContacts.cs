using Electron2D;
using ContactMode = Electron2D.ParticleProcessMaterial.CollisionModeEnum;

internal static partial class ParticleServerTests
{
    private static void MaterialContacts()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64), SDFOversize = CanvasSDFOversize.Percent100, SDFScale = CanvasSDFScale.Percent100 };
        var owner = new Entity(); root.AddChild(owner); using var mesh = Quad();
        using var polygon = new OccluderPolygon { Polygon = [new(10, 10), new(30, 10), new(30, 30), new(10, 30)] }; root.AddChild(new LightOccluder { Occluder = polygon });
        static void Near(Vector2 actual, Vector2 expected) => Check(actual.DistanceTo(expected) < .03f, $"Contact actual {actual}, expected {expected}.");
        var cases = new (string Name, ContactMode Mode, Vector2 Position, Vector2 Velocity, float Radius, float Friction, float Bounce, float Scale, bool UseScale, bool Alive, Vector2 ExpectedPosition, Vector2 ExpectedVelocity)[]
        {
            ("disabled", ContactMode.Disabled, new(20, 20), Vector2.Zero, 1, 0, 0, 1, false, true, new(20, 20), Vector2.Zero),
            ("hide edge", ContactMode.HideOnContact, new(11, 20), Vector2.Zero, 1, 0, 0, 1, false, false, new(11, 20), Vector2.Zero),
            ("hide center", ContactMode.HideOnContact, new(20, 20), Vector2.Zero, 1, 0, 0, 1, false, false, new(20, 20), Vector2.Zero),
            ("rigid center", ContactMode.Rigid, new(20, 20), Vector2.Zero, 1, 0, 0, 1, false, true, new(31, 20), Vector2.Zero),
            ("rigid bounce", ContactMode.Rigid, new(11, 20), new(-5, 0), 2, 0, 1, 1, false, true, new(8, 20), new(5, 0)),
            ("rigid friction", ContactMode.Rigid, new(11, 20), new(-5, 4), 2, .5f, 1, 1, false, true, new(8, 20), new(5, 2)),
            ("scale radius", ContactMode.Rigid, new(11, 20), Vector2.Zero, 2, 0, 0, 3, true, true, new(4, 20), Vector2.Zero),
            ("nonuniform transform", ContactMode.Rigid, new(11, 20), Vector2.Zero, 2, 0, 0, 1, false, true, new(16, 20), Vector2.Zero),
            ("outside near contact", ContactMode.HideOnContact, new(9, 20), Vector2.Zero, 2, 0, 0, 1, false, false, new(9, 20), Vector2.Zero),
            ("outside tangent", ContactMode.HideOnContact, new(8, 20), Vector2.Zero, 2, 0, 0, 1, false, true, new(8, 20), Vector2.Zero),
            ("subemission query", ContactMode.Disabled, new(20, 20), Vector2.Zero, 1, 0, 0, 1, false, true, new(20, 20), Vector2.Zero),
            ("subemission hide", ContactMode.HideOnContact, new(20, 20), Vector2.Zero, 1, 0, 0, 1, false, false, new(20, 20), Vector2.Zero),
            ("subemission rigid", ContactMode.Rigid, new(11, 20), Vector2.Zero, 2, 0, 0, 1, false, true, new(8, 20), Vector2.Zero),
        };
        using var childMaterial = new ParticleProcessMaterial { Gravity = Vector2.Zero };
        var current = 0; ParticleProcessMaterial? material = null; RID particles = default, item = default, child = default;
        void Start()
        {
            var test = cases[current]; owner.Scale = test.Name == "nonuniform transform" ? new(2, 1) : Vector2.One; material = new() { Gravity = Vector2.Zero, Spread = 0, Color = Colors.Green, CollisionMode = test.Mode, CollisionBounce = test.Bounce, CollisionFriction = test.Friction, CollisionUseScale = test.UseScale, ScaleMin = test.Scale, ScaleMax = test.Scale };
            particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetCollisionBaseSize(particles, test.Radius); RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID()); item = Attach(owner, particles, mesh);
            if (test.Name.StartsWith("subemission", StringComparison.Ordinal))
            {
                material.SubEmitterMode = ParticleProcessMaterial.SubEmitterModeEnum.AtCollision; material.SubEmitterAmountAtCollision = 2;
                child = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(child, 4); RenderingServer.ParticlesSetFixedFPS(child, 0); RenderingServer.ParticlesSetSpeedScale(child, 0); RenderingServer.ParticlesSetUseLocalCoordinates(child, true); RenderingServer.ParticlesSetProcessMaterial(child, childMaterial.GetRID()); RenderingServer.ParticlesSetSubemitter(particles, child);
            }
            var scale = test.UseScale ? test.Scale : 1;
            RenderingServer.ParticlesEmit(particles, new Transform(new(scale, 0), new(0, scale), test.Position), test.Velocity, Colors.White, Colors.White, 7);
        }
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); Start();
            RenderingServer.FramePostDraw += () =>
            {
                var test = cases[current]; var state = State(particles); var result = state.GPU!.Read(state.GPU.State, 1)[0];
                try
                {
                    Check(((result.State.X & 1) != 0) == test.Alive, "Contact active policy.");
                    Near(new(result.TransformX.W, result.TransformY.W), test.ExpectedPosition); Near(new(result.VelocityAge.X, result.VelocityAge.Y), test.ExpectedVelocity);
                    using var image = root.GetTexture().GetImage()!; Pixel(image, (int)(test.ExpectedPosition.X * owner.Scale.X), (int)(test.ExpectedPosition.Y * owner.Scale.Y), test.Alive ? Colors.Green : Colors.Black);
                    if (child.IsValid()) { var childState = State(child); Check(childState.GPU!.Read(childState.GPU.State, 4).Count(p => (p.State.X & 1) != 0) == 2, "Contact subemission reaches an unattached child, including query-only and hidden parents."); }
                }
                catch (Exception error) { throw new InvalidOperationException(test.Name, error); }
                RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); material!.Dispose(); material = null;
                if (child.IsValid()) { RenderingServer.FreeRID(child); child = default; }
                if (++current == cases.Length) root.Tree!.Quit(); else Start();
            };
        };
        try { Engine.Run(root); Check(current == cases.Length, "All material SDF contact cases completed."); }
        finally { material?.Dispose(); }
        Console.WriteLine($"{current} native material SDF contact, friction, bounce and scale cases passed with rendered output.");
    }
}
