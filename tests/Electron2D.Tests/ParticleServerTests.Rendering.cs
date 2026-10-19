using Electron2D;

internal static partial class ParticleServerTests
{
    private static void Rendering()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64), MSAA2D = Viewport.MSAA.Samples4X, UseHDR2D = true }; var owner = new Entity(); root.AddChild(owner);
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [new(-5, -1), new(5, -1), new(5, 1), new(-5, 1)], Indices = [0, 1, 2, 0, 2, 3] });
        RID particles = default, item = default; var frame = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 2);
            RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetSpeedScale(particles, 0);
            RenderingServer.ParticlesSetDrawOrder(particles, RenderingServer.ParticlesDrawOrder.Lifetime); item = Attach(owner, particles, mesh);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), new(0, 1), Colors.Red, Colors.White, 31); RenderingServer.ParticlesRequestProcessTime(particles, .25f);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = root.GetTexture().GetImage()!;
                if (frame == 0)
                {
                    Pixel(image, 20, 20, Colors.Red); RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), new(0, 1), Colors.Blue, Colors.White, 31); RenderingServer.ParticlesRequestProcessTime(particles, .01f);
                }
                else if (frame == 1)
                { Pixel(image, 20, 20, Colors.Blue); RenderingServer.ParticlesSetDrawOrder(particles, RenderingServer.ParticlesDrawOrder.ReverseLifetime); }
                else if (frame == 2)
                { Pixel(image, 20, 20, Colors.Red); Pixel(image, 24, 20, Colors.Red); Pixel(image, 20, 24, Colors.Black); RenderingServer.ParticlesSetTransformAlign(particles, RenderingServer.ParticlesTransformAlign.YToVelocity); RenderingServer.ParticlesSetTransformAlignAxis(particles, RenderingServer.ParticlesTransformAlignAxis.X); }
                else
                { Pixel(image, 24, 20, Colors.Black); Pixel(image, 20, 24, Colors.Red); RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit(); }
                frame++;
            };
        };
        Engine.Run(root); Check(frame == 4, "Four ordered/aligned native HDR/MSAA frames completed.");
        Console.WriteLine("Particle age ordering, selected velocity axis, HDR and MSAA pixel checks passed.");
    }
    private static void Allocations(bool builtIn = false)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner);
        using var mesh = Quad(); using var shader = builtIn ? null : ProcessShader();
        using Material material = builtIn ? new ParticleProcessMaterial { Gravity = Vector2.Zero, TurbulenceEnabled = true } : new ShaderMaterial { Shader = shader };
        if (material is ShaderMaterial custom) { custom.SetShaderParameter("Tint", Colors.White); custom.SetShaderParameter("Velocity", Vector2.Zero); custom.SetShaderParameter("Operation", 1u); }
        RID particles = default, item = default; var frames = 0; long start = 0, bytes = 0;
        root.Ready += _ =>
        {
            particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, 8); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true);
            RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetExplosivenessRatio(particles, 1); RenderingServer.ParticlesSetEmitting(particles, true); RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID());
            RenderingServer.ParticlesSetDrawOrder(particles, RenderingServer.ParticlesDrawOrder.Lifetime); item = Attach(owner, particles, mesh);
            RenderingServer.FramePreDraw += () => start = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePostDraw += () =>
            {
                if (frames >= 64) bytes += GC.GetAllocatedBytesForCurrentThread() - start;
                if (++frames != 96) return;
                RenderingServer.FreeRID(item); RenderingServer.FreeRID(particles); root.Tree!.Quit();
            };
        };
        Engine.Run(root); Console.WriteLine("32 warmed GPU particle render/process/sort frames: " + bytes + " managed bytes.");
        Check(frames == 96 && bytes == 0, "Warmed particle frames allocate zero managed bytes: " + bytes);
    }
    private sealed class BoundsCallbackMesh : Mesh
    {
        internal Action? Callback;
        protected override int OnGetSurfaceCount() => 0;
        protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
        protected override int OnSurfaceGetArrayLen(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
        protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
        protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
        protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
        protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
        protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
        protected override Rect2 OnGetAABB() { Callback?.Invoke(); return new(-2, -2, 4, 4); }
    }
    private static void Ownership()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64), Visible = false };
        using var wrongShader = ReadShader("InstanceCustom", Shader.Mode.CanvasItem); using var wrongProgram = new ShaderMaterial { Shader = wrongShader };
        using var wrongMaterial = new CanvasItemMaterial(); using var mesh = Quad(); using var callbackMesh = new BoundsCallbackMesh(); RID particles = default; var frames = 0;
        root.Ready += _ =>
        {
            particles = RenderingServer.ParticlesCreate(); Check(RenderingServer.ParticlesIsInactive(particles), "Fresh zero-capacity storage is inactive.");
            void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
            Reject<InvalidOperationException>(() => RenderingServer.ParticlesEmit(particles, Transform.Identity, Vector2.Zero, Colors.White, Colors.White, 31));
            Reject<ArgumentException>(() => RenderingServer.ParticlesGetEmitting(root.GetViewportRID()));
            Reject<InvalidOperationException>(() => Task.Run(() => RenderingServer.ParticlesGetEmitting(particles)).GetAwaiter().GetResult());
            Reject<NotSupportedException>(() => RenderingServer.ParticlesSetProcessMaterial(particles, wrongMaterial.GetRID()));
            Reject<NotSupportedException>(() => RenderingServer.ParticlesSetProcessMaterial(particles, wrongProgram.GetRID()));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ParticlesSetLifetime(particles, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ParticlesSetAmount(particles, int.MaxValue));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ParticlesSetDrawOrder(particles, (RenderingServer.ParticlesDrawOrder)3));
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ParticlesSetTrails(particles, true, 0));
            RenderingServer.ParticlesSetAmount(particles, 5_000_000); RenderingServer.ParticlesSetFixedFPS(particles, int.MaxValue);
            RenderingServer.ParticlesSetTrailBindPoses(particles, [Transform.Identity, Transform.Identity]); RenderingServer.ParticlesSetTrails(particles, true, 10); RenderingServer.ParticlesRequestProcess(particles);
            Reject<ArgumentOutOfRangeException>(() => RenderingServer.ForceDraw(false)); Check(RenderingServer.ParticlesGetCurrentAABB(particles) == default, "Overflowing history rejects before native state allocation.");
            RenderingServer.ParticlesSetTrails(particles, false, 1); RenderingServer.ParticlesSetFixedFPS(particles, 0);
            RenderingServer.ParticlesSetAmount(particles, 1); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true);
            RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetDrawPasses(particles, 1); RenderingServer.ParticlesSetDrawPassMesh(particles, 0, mesh.GetRID());
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.Red, Colors.White, 31);
            RenderingServer.ParticlesEmit(particles, new Transform(0, new Vector2(50, 50)), Vector2.Zero, Colors.Blue, Colors.White, 31);
            RenderingServer.ParticlesRequestProcessTime(particles, .01f);
            RenderingServer.FramePostDraw += () =>
            {
                var bounds = RenderingServer.ParticlesGetCurrentAABB(particles); Check(Math.Abs(bounds.Position.X - 16) < .01f && Math.Abs(bounds.Position.Y - 16) < .01f, "Hidden, unattached explicit request runs; bounded queue drops overflow.");
                RenderingServer.ParticlesSetDrawPassMesh(particles, 0, callbackMesh.GetRID());
                callbackMesh.Callback = () =>
                {
                    Reject<InvalidOperationException>(() => RenderingServer.ParticlesCreate());
                    Reject<InvalidOperationException>(() => RenderingServer.ParticlesSetAmount(particles, 2));
                    Reject<InvalidOperationException>(() => RenderingServer.FreeRID(particles));
                };
                Check(RenderingServer.ParticlesGetCurrentAABB(particles) == bounds, "Mesh callbacks cannot create, mutate or release particle storage during bounds preparation.");
                RenderingServer.FreeRID(particles); Reject<ArgumentException>(() => RenderingServer.ParticlesGetEmitting(particles)); frames++; root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(frames == 1 && !mesh.IsDisposed && !wrongMaterial.IsDisposed, "Session borrows caller resources.");
        try { RenderingServer.ParticlesGetEmitting(particles); } catch (InvalidOperationException) { Console.WriteLine("Particle owner/thread/identity/domain/finite/capacity/hidden-request/borrowed-resource checks passed."); return; }
        throw new InvalidOperationException("Particle query survived renderer shutdown.");
    }
}
