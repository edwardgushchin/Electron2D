using Electron2D;

internal static partial class ParticleServerTests
{
    private static Shader ProcessShader()
    {
        using var source = typeof(ParticleServerTests).Assembly.GetManifestResourceStream("TestShaders.ParticleServerProcess.comp.spv")!;
        using var bytes = new MemoryStream(); source.CopyTo(bytes); return Shader.CreateFromSPIRV(bytes.ToArray(), Shader.Mode.Particles);
    }
    private static ArrayMesh Quad()
    {
        var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)], Indices = [0, 1, 2, 0, 2, 3] }); return mesh;
    }
    private static RID Attach(Entity owner, RID particles, Mesh mesh)
    {
        RenderingServer.ParticlesSetCustomAABB(particles, new(0, 0, 64, 64)); RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, mesh.GetRID());
        var item = RenderingServer.CanvasItemCreate(); RenderingServer.CanvasItemSetParent(item, owner.GetCanvasItem()); RenderingServer.CanvasItemAddParticles(item, particles); return item;
    }
    private static void Simulation()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner);
        using var mesh = Quad(); using var shader = ProcessShader(); using var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("Tint", Colors.White); material.SetShaderParameter("Velocity", Vector2.Zero); material.SetShaderParameter("Operation", 1u);
        RID particles = default, item = default; var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); particles = RenderingServer.ParticlesCreate();
            Check(RenderingServer.ParticlesIsInactive(particles) && !RenderingServer.ParticlesGetEmitting(particles), "Raw inactive/emitting defaults.");
            RenderingServer.ParticlesSetAmount(particles, 8); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true);
            RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 1); RenderingServer.ParticlesSetFractionalDelta(particles, true);
            RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID()); RenderingServer.ParticlesSetEmitting(particles, true); RenderingServer.ParticlesSetPreProcessTime(particles, .25f);
            item = Attach(owner, particles, mesh); RenderingServer.ParticlesRequestProcess(particles);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!;
                void Active(int count) { for (var index = 0; index < 8; index++) Pixel(image, 8 + index * 6, 20, index < count ? Colors.White : Colors.Black); }
                if (frame == 0)
                {
                    Active(2); Check(RenderingServer.ParticlesGetEmitting(particles), "Preprocess executes at zero automatic speed.");
                    RenderingServer.ParticlesSetPreProcessTime(particles, 0); RenderingServer.ParticlesRequestProcessTime(particles, .25f);
                }
                else if (frame == 1)
                {
                    Active(4); RenderingServer.ParticlesSetAmountRatio(particles, .5f); RenderingServer.ParticlesSetExplosivenessRatio(particles, 1);
                    RenderingServer.ParticlesRestart(particles); RenderingServer.ParticlesRequestProcessTime(particles, .001f);
                }
                else if (frame == 2)
                {
                    Active(4); RenderingServer.ParticlesSetAmountRatio(particles, 0); RenderingServer.ParticlesRestart(particles); RenderingServer.ParticlesRequestProcessTime(particles, .001f);
                }
                else if (frame == 3)
                {
                    Active(0); RenderingServer.ParticlesSetAmountRatio(particles, 1); RenderingServer.ParticlesSetOneShot(particles, true); RenderingServer.ParticlesSetLifetime(particles, .25f);
                    material.SetShaderParameter("Operation", 3u); RenderingServer.ParticlesRestart(particles); RenderingServer.ParticlesRequestProcessTime(particles, .25f);
                }
                else if (frame == 4)
                {
                    Check(!RenderingServer.ParticlesGetEmitting(particles), "One-shot emission stops after the queued cycle.");
                    RenderingServer.ParticlesRequestProcessTime(particles, 0, .3f);
                }
                else if (frame == 5)
                {
                    Active(0); Check(!RenderingServer.ParticlesGetEmitting(particles), "Residual processing drains state with emission disabled.");
                    material.SetShaderParameter("Operation", 9u); RenderingServer.ParticlesSetOneShot(particles, false); RenderingServer.ParticlesSetLifetime(particles, 1); RenderingServer.ParticlesSetInterpToEnd(particles, .75f);
                    RenderingServer.ParticlesSetEmitterVelocity(particles, new(.5f, 0)); RenderingServer.ParticlesSetCollisionBaseSize(particles, .25f); RenderingServer.ParticlesSetEmitting(particles, true);
                    RenderingServer.ParticlesRestart(particles); RenderingServer.ParticlesRequestProcessTime(particles, .001f);
                }
                else if (frame == 6)
                {
                    Pixel(image, 8, 20, new(.75f, .5f, .25f, 1)); material.SetShaderParameter("Operation", 33u);
                    RenderingServer.ParticlesSetExplosivenessRatio(particles, 0); RenderingServer.ParticlesRestart(particles); RenderingServer.ParticlesRequestProcessTime(particles, .875f);
                }
                else if (frame == 7)
                {
                    for (var index = 0; index < 8; index++) Pixel(image, 8 + index * 6, 20, index < 7 ? Colors.Red : Colors.Black);
                    RenderingServer.ParticlesRequestProcessTime(particles, .25f);
                }
                else
                {
                    Pixel(image, 8, 20, Colors.Blue); for (var index = 1; index < 8; index++) Pixel(image, 8 + index * 6, 20, Colors.Red);
                    RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit();
                }
                frame++;
            };
        };
        Engine.Run(root); Check(frame == 9, "Nine deterministic particle timing frames completed.");
        Console.WriteLine("Particle fixed/preprocess/zero-speed/phase/ratio/explosiveness/one-shot/residual/process-input pixel checks passed.");
    }
}
