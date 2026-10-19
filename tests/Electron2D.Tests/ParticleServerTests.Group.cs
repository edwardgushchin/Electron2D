using Electron2D;

internal static partial class ParticleServerTests
{
    private sealed class ParticleGroup : CanvasGroup
    {
        internal RID Particles;
        protected override void OnDraw() { if (Particles.IsValid()) RenderingServer.CanvasItemAddParticles(GetCanvasItem(), Particles); }
    }
    private sealed class GroupFill : Entity { protected override void OnDraw() => DrawRect(new(8, 8, 24, 24), Colors.Green); }
    private static void Group()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var group = new ParticleGroup { SelfModulate = new(1, 1, 1, .5f), FitMargin = 0, ClearMargin = 0 }; group.AddChild(new GroupFill()); root.AddChild(group);
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new()
        {
            Vertices = [new(-4, -4), new(4, -4), new(4, 4), new(-4, 4)],
            Indices = [0, 1, 2, 0, 2, 3],
            Colors = [new(1, 1, 1, .5f), new(1, 1, 1, .5f), new(1, 1, 1, .5f), new(1, 1, 1, .5f)]
        }); var completed = false;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); var particles = RenderingServer.ParticlesCreate(); group.Particles = particles;
            RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetLifetime(particles, 100);
            RenderingServer.ParticlesSetCustomAABB(particles, new(0, 0, 64, 64)); RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, mesh.GetRID()); group.QueueRedraw();
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, new(1, 1, 1, .5f), Colors.White, 31);
            RenderingServer.ForceDraw(false); using var image = root.GetTexture().GetImage()!; Pixel(image, 20, 20, new(0, .125f, 0, 1)); Pixel(image, 30, 20, Colors.Black);
            RenderingServer.FreeRID(particles); group.Particles = default; completed = true; root.Tree!.Quit();
        };
        Engine.Run(root); Check(completed, "Particle canvas group frame completed."); Console.WriteLine("Particle group-owner geometry uses generated screen UVs and one final premultiplication of mesh/particle/item opacity.");
    }
    private static void WorldGroup()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var group = new CanvasGroup { FitMargin = 0, ClearMargin = 0 }; root.AddChild(group); using var mesh = Quad(); var completed = false;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); var particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1);
            RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 100);
            RenderingServer.ParticlesSetCustomAABB(particles, new(-4, 16, 8, 8)); RenderingServer.ParticlesSetEmissionTransform(particles, new Transform(0, new Vector2(40, 0)));
            RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, mesh.GetRID());
            var item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, group.GetCanvasItem()); RenderingServer.CanvasItemSetTransform(item, new Transform(0, new Vector2(40, 0))); RenderingServer.CanvasItemAddParticles(item, particles);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(40, 20)), Vector2.Zero, Colors.Green, Colors.White, 31);
            RenderingServer.ForceDraw(false); using var image = root.GetTexture().GetImage()!; Pixel(image, 40, 20, Colors.Green); Pixel(image, 2, 20, Colors.Black);
            RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); completed = true; root.Tree!.Quit();
        };
        Engine.Run(root); Check(completed, "World particle group frame completed."); Console.WriteLine("World particle custom visibility bounds fit the actual group in emitter-local coordinates.");
    }

}
