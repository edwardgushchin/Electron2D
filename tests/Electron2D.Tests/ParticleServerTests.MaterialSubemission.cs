using Electron2D;

internal static partial class ParticleServerTests
{
    private static void MaterialSubemission()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(64, 64) }; var owner = new Entity(); root.AddChild(owner); using var mesh = Quad();
        using var parentMaterial = new ParticleProcessMaterial { Gravity = Vector2.Zero, Direction = new(3, 4), Spread = 0, InitialVelocityMin = 5, InitialVelocityMax = 5, InheritVelocityRatio = .5f, SubEmitterMode = ParticleProcessMaterial.SubEmitterModeEnum.AtStart, SubEmitterAmountAtStart = 130, SubEmitterKeepVelocity = true };
        using var childMaterial = new ParticleProcessMaterial { Gravity = Vector2.Zero, Spread = 0, InitialVelocityMin = 2, InitialVelocityMax = 2, Color = Colors.Green };
        RID parent = default, child = default, parentItem = default, childItem = default; var frame = 0;
        RID Create(int amount, Material material)
        {
            var particles = RenderingServer.ParticlesCreate(); RenderingServer.ParticlesSetAmount(particles, amount); RenderingServer.ParticlesSetFixedFPS(particles, 0); RenderingServer.ParticlesSetSpeedScale(particles, 0); RenderingServer.ParticlesSetLifetime(particles, 100); RenderingServer.ParticlesSetUseLocalCoordinates(particles, true); RenderingServer.ParticlesSetProcessMaterial(particles, material.GetRID()); return particles;
        }
        void Emit(float life = 1) => RenderingServer.ParticlesEmit(parent, new Transform(0, new Vector2(20, 20)), Vector2.Zero, Colors.White, new(0, 0, 0, life), life == 1 ? 1 : 17);
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); parent = Create(1, parentMaterial); child = Create(130, childMaterial); RenderingServer.ParticlesSetEmitterVelocity(parent, new(1, 2)); RenderingServer.ParticlesSetSubemitter(parent, child); parentItem = Attach(owner, parent, mesh); childItem = Attach(owner, child, mesh); Emit();
            RenderingServer.FramePostDraw += () =>
            {
                var state = State(child); var particles = state.GPU!.Read(state.GPU.State, 130); var expected = frame switch { 0 or 2 => 130, 1 or 4 or 5 => 0, 6 or 7 => 1, _ => 3 };
                var actual = particles.Count(p => (p.State.X & 1) != 0); Check(actual == expected, $"Subemission frame {frame}: {actual} active children, expected {expected}.");
                foreach (var particle in particles)
                    if ((particle.State.X & 1) != 0)
                    {
                        var velocity = new Vector2(particle.VelocityAge.X, particle.VelocityAge.Y); var wanted = frame is 0 or 2 ? new Vector2(3.5f, 5) : new Vector2(2, 0);
                        Check(velocity.DistanceTo(wanted) < .0001f && (frame >= 4 || new Vector2(particle.TransformX.W, particle.TransformY.W).DistanceTo(new(20, 20)) < .0001f), "Child birth retains only the requested parent fields.");
                    }
                if (frame == 0) { using var image = root.GetTexture().GetImage()!; Pixel(image, 20, 20, Colors.Green); }
                if (++frame == 10)
                {
                    RenderingServer.FreeRID(parentItem); RenderingServer.FreeRID(childItem); RenderingServer.FreeRID(parent); RenderingServer.FreeRID(child); root.Tree!.Quit(); return;
                }
                if (frame is 1 or 2 or 3 or 4 or 7 or 8) RenderingServer.ParticlesRestart(child);
                if (frame == 2) { parentMaterial.SubEmitterAmountAtStart = int.MaxValue; RenderingServer.ParticlesRestart(parent); Emit(); }
                if (frame == 3) { parentMaterial.SubEmitterAmountAtStart = 3; parentMaterial.SubEmitterKeepVelocity = false; RenderingServer.ParticlesRestart(parent); Emit(); }
                if (frame == 4)
                {
                    parentMaterial.SubEmitterMode = ParticleProcessMaterial.SubEmitterModeEnum.Constant; parentMaterial.SubEmitterFrequency = 4;
                    RenderingServer.ParticlesSetLifetime(parent, 1); RenderingServer.ParticlesSetFixedFPS(parent, 10); RenderingServer.ParticlesRestart(parent); Emit(); RenderingServer.ParticlesRequestProcessTime(parent, .1f);
                }
                if (frame is 5 or 6) RenderingServer.ParticlesRequestProcessTime(parent, .1f);
                if (frame == 7) { parentMaterial.SubEmitterFrequency = float.MaxValue; RenderingServer.ParticlesSetFixedFPS(parent, 8); RenderingServer.ParticlesRestart(parent); Emit(); RenderingServer.ParticlesRequestProcessTime(parent, .125f); }
                if (frame == 8) { parentMaterial.SubEmitterMode = ParticleProcessMaterial.SubEmitterModeEnum.AtEnd; parentMaterial.SubEmitterAmountAtEnd = 3; RenderingServer.ParticlesSetFixedFPS(parent, 10); RenderingServer.ParticlesRestart(parent); Emit(.5f); RenderingServer.ParticlesRequestProcessTime(parent, .5f); }
                if (frame == 9) RenderingServer.ParticlesRequestProcessTime(parent, .1f);
            };
        };
        Engine.Run(root); Check(frame == 10, "All bounded subemission birth frames completed.");
        Console.WriteLine("One parent emits 130 children across workgroups, saturates the child pool, honors velocity flags and emits once at start/end and honors ordinary/extreme frequency.");
    }
}
