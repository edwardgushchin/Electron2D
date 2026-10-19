using Electron2D;

internal static partial class GPUParticlesNodeTests
{
    private sealed class ParticleWarmWindow : Window
    {
        internal GPUParticles Emitter = null!;
        internal long Before;
        internal bool TickSeen;
        private int _ticks;
        internal ParticleWarmWindow() { PhysicsProcessEnabled = true; }
        protected override void OnPhysicsProcess(double delta)
        {
            Before = GC.GetAllocatedBytesForCurrentThread(); TickSeen = true;
            Emitter.Position = new(20 + _ticks++ % 2, 20); Emitter.RequestParticlesProcess(.125f);
        }
    }
    internal static void Warm()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var oldFPS = Engine.MaxFPS; var oldPhysicsFPS = Engine.PhysicsTicksPerSecond; Engine.MaxFPS = 120; Engine.PhysicsTicksPerSecond = 120;
        using var root = new ParticleWarmWindow { Size = new(64, 64) };
        using var material = new ParticleProcessMaterial { Gravity = Vector2.Zero, TurbulenceEnabled = true, InitialVelocityMin = 2 };
        var node = new GPUParticles { Amount = 8, Lifetime = 100, FixedFPS = 8, LocalCoords = true, Explosiveness = 1, ProcessMaterial = material, TrailEnabled = true, Position = new(20, 20) }; root.AddChild(node); root.Emitter = node;
        long bytes = 0; var frames = 0;
        root.Ready += _ => RenderingServer.FramePostDraw += () =>
        {
            if (!root.TickSeen) return;
            if (frames >= 64) bytes += GC.GetAllocatedBytesForCurrentThread() - root.Before;
            root.TickSeen = false;
            if (++frames == 96) { Check(node.CaptureRect().HasArea(), "Warmed native particles produced actual current bounds."); root.Tree!.Quit(); }
        };
        try { Engine.Run(root); }
        finally { Engine.MaxFPS = oldFPS; Engine.PhysicsTicksPerSecond = oldPhysicsFPS; }
        Console.WriteLine("32 warmed GPU scene physics/movement/seek/process/trail/render intervals: " + bytes + " managed bytes.");
        Check(frames == 96 && bytes == 0, "Warmed GPU scene intervals reuse managed storage: " + bytes);
    }
}
