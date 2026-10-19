using Electron2D;

internal static partial class ParticleServerTests
{
    private static void Inputs()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner);
        using var mesh = Quad(); using var shader = ProcessShader(); using var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("Tint", Colors.White); material.SetShaderParameter("Velocity", Vector2.Zero); material.SetShaderParameter("Operation", 17u);
        RID particles = default, item = default; var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 4);
            RenderingServer.ParticlesSetFixedFPS(particles, 1); RenderingServer.ParticlesSetInterpolate(particles, false); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true);
            RenderingServer.ParticlesSetEmitting(particles, true); RenderingServer.ParticlesSetFractionalDelta(particles, true); RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID()); item = Attach(owner, particles, mesh);
            RenderingServer.ParticlesRequestProcessTime(particles, .3f);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!;
                if (frame == 0)
                {
                    Pixel(image, 8, 20, new(.3f, 0, 0, 1)); Pixel(image, 14, 20, new(.05f, 0, 0, 1));
                    RenderingServer.ParticlesSetFractionalDelta(particles, false); RenderingServer.ParticlesRestart(particles); RenderingServer.ParticlesRequestProcessTime(particles, .3f);
                }
                else if (frame == 1)
                {
                    Pixel(image, 14, 20, new(.3f, 0, 0, 1)); material.SetShaderParameter("Operation", 64u); RenderingServer.ParticlesSetEmissionTransform(particles, new Transform(0, new Vector2(.25f, 0))); RenderingServer.ParticlesRequestProcessTime(particles, .01f);
                }
                else if (frame == 2)
                {
                    Pixel(image, 8, 20, Colors.Blue); RenderingServer.ParticlesSetUseLocalCoordinates(particles, false); RenderingServer.CanvasItemSetTransform(item, new Transform(0, new Vector2(.25f, 0))); RenderingServer.ParticlesRequestProcessTime(particles, .01f);
                }
                else if (frame == 3)
                {
                    Pixel(image, 8, 20, new(.25f, 0, 0, 1)); RenderingServer.ParticlesSetEmissionTransform(particles, new Transform(0, new Vector2(.5f, 0))); RenderingServer.CanvasItemSetTransform(item, new Transform(0, new Vector2(.5f, 0))); RenderingServer.ParticlesRequestProcessTime(particles, .01f);
                }
                else
                { Pixel(image, 8, 20, new(.5f, .25f, 0, 1)); RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit(); }
                frame++;
            };
        };
        Engine.Run(root); Check(frame == 5, "Five native process input frames completed."); Console.WriteLine("Particle fractional newborn delta and local/world/previous emission frame pixels passed.");
    }
    private static void Sheet()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        using var material = new CanvasItemMaterial { ParticlesAnimation = true, ParticlesAnimHFrames = 2, ParticlesAnimLoop = false }; var owner = new Entity { Material = material }; root.AddChild(owner);
        using var pixels = Image.CreateEmpty(8, 4, false, Image.Format.Rgba8); pixels.Fill(Colors.Red); for (var y = 0; y < 4; y++) for (var x = 4; x < 8; x++) pixels.SetPixel(x, y, Colors.Blue);
        using var texture = ImageTexture.CreateFromImage(pixels); using var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [new(-4, -4), new(4, -4), new(4, 4), new(-4, 4)], UVs = [Vector2.Zero, new(1, 0), Vector2.One, new(0, 1)], Indices = [0, 1, 2, 0, 2, 3] });
        RID particles = default, item = default; var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 100);
            RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, mesh.GetRID()); item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, owner.GetCanvasItem()); RenderingServer.CanvasItemSetUseParentMaterial(item, true); RenderingServer.CanvasItemAddParticles(item, particles, texture.GetRID());
            void Emit(float phase) { RenderingServer.ParticlesRestart(particles); RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.White, new(0, 0, phase, 0), 31); }
            Emit(1.25f);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!; Pixel(image, 20, 20, frame == 0 ? Colors.Blue : Colors.Red);
                if (frame++ == 0) { material.ParticlesAnimLoop = true; Emit(1.25f); }
                else { RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit(); }
            };
        };
        Engine.Run(root); Check(frame == 2, "Two particle sprite sheet frames completed."); Console.WriteLine("Particle texture/sprite sheet, inherited material, phase clamp/loop pixels passed.");
    }
}
