using Electron2D;

internal static partial class ParticleServerTests
{
    private static void Streams()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner); using var mesh = Quad(); var completed = false;
        root.Ready += _ =>
        {
            var a = RenderingServer.ParticlesCreate(); var b = RenderingServer.ParticlesCreate();
            foreach (var id in new[] { a, b }) { RenderingServer.ParticlesSetAmount(id, 1); RenderingServer.ParticlesSetUseLocalCoordinates(id, true); RenderingServer.ParticlesSetFixedFPS(id, 0); RenderingServer.ParticlesSetSpeedScale(id, 0); RenderingServer.ParticlesSetLifetime(id, 100); }
            var itemA = Attach(owner, a, mesh); var itemB = Attach(owner, b, mesh);
            RenderingServer.ParticlesEmit(a, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.Red, Colors.White, 31);
            RenderingServer.ParticlesEmit(b, new Transform(0, new Vector2(40, 20)), Vector2.Zero, Colors.Blue, Colors.White, 31); RenderingServer.ForceDraw(false);
            var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>(); var lighting = new CanvasLightingFrame(); lighting.ConfigureSDF(root, root.Size);
            void Append(RID id, Rect2i? clip) => new CanvasParticles(State(id)).Append(vertices, batches, null, Transform.Identity, Colors.White, null, BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, clip, false, false, owner);
            void Draw() { var backend = State(a).Backend; backend.Draw(backend.FindTarget(root)!, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(vertices), System.Runtime.InteropServices.CollectionsMarshal.AsSpan(batches), Colors.Black, true, RenderingServer.Service!.CanvasTime, [], lighting); }
            Append(a, new Rect2i(0, 0, 0, 0)); Append(b, null); Draw();
            using (var image = root.GetTexture().GetImage()!) { Pixel(image, 20, 20, Colors.Black); Pixel(image, 40, 20, Colors.Blue); }
            vertices.Clear(); batches.Clear(); Append(b, new Rect2i(0, 0, 0, 0)); Append(a, null); Draw();
            using (var image = root.GetTexture().GetImage()!) { Pixel(image, 20, 20, Colors.Red); Pixel(image, 40, 20, Colors.Black); }
            RenderingServer.FreeRID(itemA); RenderingServer.FreeRID(itemB); RenderingServer.FreeRID(a); RenderingServer.FreeRID(b); completed = true; root.Tree!.Quit();
        };
        Engine.Run(root); Check(completed, "Multiple native particle streams completed."); Console.WriteLine("Particle generated stream identity survives empty clipping and reversed batch order.");
    }
}
