namespace Electron2D;

internal sealed class RenderingParticleState(RenderingServer owner, GPUCanvasBackend backend)
{
    internal readonly RenderingServer Owner = owner;
    internal readonly GPUCanvasBackend Backend = backend;
    internal bool Released, Preparing, Requested, Clear = true, Inactive = true;
    internal bool Emitting, OneShot, FractionalDelta, Interpolate = true, LocalCoordinates, Trails;
    internal int Amount, FixedFPS = 30;
    internal float Lifetime = 1, AmountRatio = 1, Explosiveness, Randomness, SpeedScale = 1, Preprocess, InterpToEnd;
    internal float CollisionBaseSize = .01f, TrailLength = 1;
    internal Rect2 CustomAABB = new(-4, -4, 8, 8);
    internal Transform Emission = Transform.Identity, PreviousEmission = Transform.Identity;
    internal Vector2 EmitterVelocity;
    internal RenderingServer.ParticlesDrawOrder DrawOrder;
    internal RenderingServer.ParticlesTransformAlign Align;
    internal RenderingServer.ParticlesTransformAlignAxis AlignAxis = RenderingServer.ParticlesTransformAlignAxis.Y;
    internal Transform[] BindPoses = [];
    internal Mesh?[] DrawPasses = [];
    internal Material? ProcessMaterial;
    internal MaterialState? PreparedProcess;
    internal RenderingParticleState? Subemitter;
    internal ParticleStateLayout[] Manual = [];
    internal int ManualCount;
    internal double Phase, Remainder, InactiveTime, RequestTime, RequestResidual;
    internal uint Cycle, Seed = unchecked((uint)System.Random.Shared.NextInt64());
    internal long LastFrame = -1, Revision, SubemissionFrame = -1;
    internal int IncomingLinks;
    internal Transform CanvasBasis = Transform.Identity;
    internal ParticleGPUState? GPU;
    internal void Changed() { Revision++; Owner.NotifyParticleChanged(); }
    internal void Restart()
    {
        Clear = true; Inactive = false; InactiveTime = 0; Phase = Remainder = 0; Cycle = 0;
        Changed();
    }
    internal void Allocate(int amount)
    {
        if (amount < 0 || ((long)amount + 1) * ParticleStateLayout.ByteSize > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount == Amount) return;
        GPU?.Dispose(); GPU = null; Amount = amount; Manual = []; ManualCount = 0; Restart();
    }
    internal void Emit(Transform transform, Vector2 velocity, Color color, Color custom, int flags)
    {
        if (Amount == 0) throw new InvalidOperationException("Allocate particles before emitting.");
        if (Manual.Length == 0) Manual = new ParticleStateLayout[checked(Amount + 1)];
        if (Emitting) { Clear = true; Emitting = false; }
        Inactive = false; InactiveTime = 0;
        if (ManualCount < Amount)
        {
            Manual[++ManualCount] = new()
            {
                TransformX = new(transform.X.X, transform.Y.X, 0, transform.Origin.X),
                TransformY = new(transform.X.Y, transform.Y.Y, 0, transform.Origin.Y),
                VelocityAge = new(velocity.X, velocity.Y, 0, 0),
                Color = new(color.R, color.G, color.B, color.A),
                Custom = new(custom.R, custom.G, custom.B, custom.A),
                State = new(flags, 0, 0, 0)
            };
        }
        Changed();
    }
    internal void Release()
    {
        Released = true; GPU?.Dispose(); GPU = null; DrawPasses = []; ProcessMaterial = null; Subemitter = null; Manual = []; BindPoses = [];
    }
}
