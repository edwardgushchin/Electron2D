using Electron2D;

internal static partial class ParticleServerTests
{
    private static RenderingParticleState State(RID particles)
    {
        var states = (Dictionary<RID, RenderingParticleState>)typeof(RenderingServer).GetField("_particles", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(RenderingServer.Service!)!;
        return states[particles];
    }
    private static Shader ReadShader(string name, Shader.Mode mode)
    {
        using var input = typeof(ParticleServerTests).Assembly.GetManifestResourceStream("TestShaders." + name + ".spv")!;
        using var bytes = new MemoryStream(); input.CopyTo(bytes); return Shader.CreateFromSPIRV(bytes.ToArray(), mode);
    }
    private static void Rollback()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) };
        using var processShader = ReadShader("ParticleRollback.comp", Shader.Mode.Particles); using var drawShader = ReadShader("ParticleRollback.frag", Shader.Mode.CanvasItem);
        using var process = new ShaderMaterial { Shader = processShader }; using var draw = new ShaderMaterial { Shader = drawShader };
        var owner = new Entity { Material = draw }; root.AddChild(owner); using var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new()
        {
            Vertices = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)],
            Indices = [0, 1, 2, 0, 2, 3],
            Bones = new int[16],
            Weights = [1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0]
        }); var completed = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); var particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 1);
            RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetProcessMaterial(particles, process.GetRID());
            RenderingServer.ParticlesSetTrailBindPoses(particles, [Transform.Identity, Transform.Identity]); RenderingServer.ParticlesSetTrails(particles, true, .5f);
            RenderingServer.GlobalShaderParameterAdd("rollbackDrawTint", RenderingServer.GlobalShaderParameterType.Vec4, Vector4.One);
            var item = Attach(owner, particles, mesh); RenderingServer.CanvasItemSetUseParentMaterial(item, true);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.Red, Colors.White, 31); RenderingServer.ParticlesRequestProcessTime(particles, .25f);
            RenderingServer.FramePostDraw += () => completed++;
            void Failed(string name)
            {
                try { RenderingServer.ForceDraw(false); }
                catch (ArgumentException error) when (error.Message.Contains(name, StringComparison.Ordinal))
                { Check(RenderingServer.ParticlesGetCurrentAABB(particles) == default && completed == 0, "Cancelled particle state is unpublished and frame callbacks stay pending."); return; }
                throw new InvalidOperationException("Missing global did not cancel native particle submission: " + name);
            }
            Failed("rollbackProcessTint"); RenderingServer.GlobalShaderParameterAdd("rollbackProcessTint", RenderingServer.GlobalShaderParameterType.Vec4, new Vector4(0, 1, 0, 1));
            RenderingServer.ForceDraw(false);
            var server = RenderingServer.Service!;
            var state = State(particles); var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
            new CanvasParticles(state).Append(vertices, batches, null, Transform.Identity, Colors.White, draw.GetCanvasState(), BlendMode.Mix, TextureFilter.Nearest, TextureRepeat.Disabled, 1, null, false, false, owner);
            var backend = state.Backend; var target = backend.FindTarget(root)!; var lighting = new CanvasLightingFrame(); lighting.ConfigureSDF(root, root.Size);
            void Submit() => backend.Draw(target, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(vertices), System.Runtime.InteropServices.CollectionsMarshal.AsSpan(batches), Colors.Black, true, server.CanvasTime, [], lighting);
            RenderingServer.GlobalShaderParameterRemove("rollbackDrawTint"); var failed = false;
            try { Submit(); } catch (ArgumentException error) when (error.Message.Contains("rollbackDrawTint", StringComparison.Ordinal)) { failed = true; }
            Check(failed && state.GPU!.Initialized && completed == 1, "A later canvas submission cancels after geometry upload without a new simulation checkpoint.");
            RenderingServer.GlobalShaderParameterAdd("rollbackDrawTint", RenderingServer.GlobalShaderParameterType.Vec4, Vector4.One); Submit();
            using var image = root.GetTexture().GetImage()!; Pixel(image, 20, 20, Colors.Green);
            Check(completed == 1 && RenderingServer.ParticlesGetCurrentAABB(particles).Position == new Vector2(16, 16), "Retry preserves pending manual emission, seek, mesh upload and bounds.");
            RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit();
        };
        Engine.Run(root); Check(completed == 1, "One retried particle frame completed."); Console.WriteLine("Particle process/draw native cancellation and retry preserve unpublished state, pending manual queue and geometry uploads.");
    }
}
