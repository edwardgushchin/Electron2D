namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly Dictionary<RID, RenderingParticleState> _particles = [];
    private readonly List<RenderingParticleState> _particleOrder = [];
    private readonly List<RenderingParticleState> _particleSchedule = [];
    private bool _particleGraphChanged = true;
    internal List<RenderingParticleState> ParticleSchedule
    {
        get
        {
            if (!_particleGraphChanged) return _particleSchedule;
            _particleSchedule.Clear(); var incoming = new Dictionary<RenderingParticleState, int>(); foreach (var state in _particleOrder) incoming.Add(state, 0);
            foreach (var state in _particleOrder) if (state.Subemitter is { } child) incoming[child]++;
            foreach (var state in _particleOrder) if (incoming[state] == 0) _particleSchedule.Add(state);
            for (var i = 0; i < _particleSchedule.Count; i++) if (_particleSchedule[i].Subemitter is { } child && --incoming[child] == 0) _particleSchedule.Add(child);
            if (_particleSchedule.Count != _particleOrder.Count) throw new InvalidOperationException("Particle subemission graph contains a cycle.");
            _particleGraphChanged = false; return _particleSchedule;
        }
    }
    internal bool ParticleHasIncoming(RenderingParticleState state) => state.IncomingLinks != 0;
    internal long ParticleFrame => _particleFrame;
    internal double ParticleDelta => _particleDelta;
    private long _particleFrame;
    private double _particleDelta;
    internal bool HasParticleSources => _particleOrder.Count != 0;
    internal void NotifyParticleChanged() => MarkChanged();
    private RenderingParticleState Particle(RID rid)
    { EnsureOwner(); return _particles.TryGetValue(rid, out var state) && !state.Released ? state : throw new ArgumentException("The particle identity does not belong to this rendering session.", nameof(rid)); }
    private RenderingParticleState ChangeParticle(RID rid)
    {
        EnsureTextureChange(); var state = Particle(rid);
        foreach (var entry in _particleOrder) if (entry.Preparing) throw new InvalidOperationException("Particle preparation cannot mutate retained particle storage.");
        state.Changed(); return state;
    }
    private RID ParticlesCreateCore()
    {
        EnsureTextureChange(); foreach (var entry in _particleOrder) if (entry.Preparing) throw new InvalidOperationException("Particle preparation cannot create retained particle storage.");
        if (_backend is not GPUCanvasBackend backend) throw new NotSupportedException("Particle simulation requires the GPU renderer.");
        var rid = RID.Allocate(); var state = new RenderingParticleState(this, backend); _particles.Add(rid, state); _particleOrder.Add(state); _particleGraphChanged = true; return rid;
    }
    private bool ReleaseParticleRID(RID rid)
    {
        if (!_particles.TryGetValue(rid, out var state)) return false;
        ChangeParticle(rid); _particles.Remove(rid); _particleOrder.Remove(state);
        foreach (var other in _particleOrder) if (ReferenceEquals(other.Subemitter, state)) other.Subemitter = null;
        if (state.Subemitter is { } previousChild) previousChild.IncomingLinks--;
        state.Release(); _particleGraphChanged = true; return true;
    }
    private void ReleaseParticles()
    { foreach (var state in _particleOrder) state.Release(); _particleOrder.Clear(); _particles.Clear(); }
    private void SetParticleSubemitter(RID rid, RID subemitter)
    {
        var state = ChangeParticle(rid); var child = subemitter.IsValid() ? Particle(subemitter) : null;
        for (var current = child; current is not null; current = current.Subemitter) if (ReferenceEquals(current, state)) throw new ArgumentException("Particle subemitters cannot form a cycle.", nameof(subemitter));
        if (state.Subemitter is { } previous) previous.IncomingLinks--;
        state.Subemitter = child; if (child is not null) child.IncomingLinks++; _particleGraphChanged = true;
    }
    private void SetParticleDrawPass(RID rid, int pass, RID mesh)
    {
        var state = ChangeParticle(rid); if ((uint)pass >= (uint)state.DrawPasses.Length) throw new ArgumentOutOfRangeException(nameof(pass));
        state.DrawPasses[pass] = mesh.IsValid() ? RenderingMeshRegistry.Resolve(mesh) : null;
    }
    private static float ParticleNumber(float value, string name, bool positive = false)
    { if (!float.IsFinite(value) || (positive ? value <= 0 : value < 0)) throw new ArgumentOutOfRangeException(name); return value; }
    private static float ParticleRatio(float value, string name)
    { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(name); return Math.Clamp(value, 0, 1); }
    internal void RequestParticleCanvas(RenderingParticleState state)
    {
        if (state.Released || !ReferenceEquals(state.Owner, this)) return;
        state.Requested = true; _continuousCanvas |= state.Emitting || !state.Inactive;
    }
}
