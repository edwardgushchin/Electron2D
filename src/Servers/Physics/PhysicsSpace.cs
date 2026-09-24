using Box2D.NET;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace Electron2D;

internal sealed class PhysicsSpace : IDisposable
{
    internal const float MetersPerUnit = 0.01f;
    internal const float UnitsPerMeter = 100f;
    private const ulong RoughMaterial = 1;
    private const ulong AbsorbentMaterial = 2;

    private readonly List<PhysicsBody> _bodies = [];
    private readonly B2WorldId _worldID;
    private bool _stepping;
    private bool _disposed;

    internal PhysicsSpace()
    {
        var definition = b2DefaultWorldDef();
        definition.gravity = new B2Vec2(0, 9.8f);
        definition.restitutionThreshold = 0;
        definition.frictionCallback = CombineFriction;
        definition.restitutionCallback = CombineBounce;
        _worldID = b2CreateWorld(definition);
    }

    internal B2WorldId WorldID => _worldID;

    internal void Add(PhysicsBody body)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (_stepping) throw new InvalidOperationException("Physics bodies cannot enter a world while it is stepping.");
        _bodies.EnsureCapacity(_bodies.Count + 1);
        body.AttachBackend(this);
        _bodies.Add(body);
    }

    internal void Remove(PhysicsBody body)
    {
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("Physics bodies cannot leave a world while it is stepping.");
        if (_bodies.Remove(body)) body.DetachBackend();
    }

    internal void Step(double delta)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PhysicsSpace));
        if (delta == 0 || _bodies.Count == 0) return;
        if (_stepping) throw new InvalidOperationException("A physics world cannot step recursively.");
        _stepping = true;
        try
        {
            foreach (var body in _bodies) body.PrepareBackend();
            b2World_Step(_worldID, (float)delta, 4);
            List<Exception>? errors = null;
            foreach (var body in _bodies)
            {
                try { body.CompleteBackend(); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
            if (errors is not null) throw new AggregateException("Physics-body synchronization failed.", errors);
        }
        finally { _stepping = false; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_stepping) throw new InvalidOperationException("A physics world cannot be disposed during a step.");
        foreach (var body in _bodies) body.DetachBackend();
        _bodies.Clear();
        b2DestroyWorld(_worldID);
        _disposed = true;
    }

    internal static void SetMaterial(ref B2ShapeDef definition, PhysicsMaterial? material)
    {
        var friction = material?.ComputedFriction ?? 1f;
        var bounce = material?.ComputedBounce ?? 0f;
        definition.material.friction = MathF.Abs(friction);
        definition.material.restitution = MathF.Abs(bounce);
        definition.material.userMaterialId = (friction < 0 ? RoughMaterial : 0) |
            (bounce < 0 ? AbsorbentMaterial : 0);
    }

    private static float CombineFriction(float a, ulong aFlags, float b, ulong bFlags) =>
        MathF.Abs(MathF.Min((aFlags & RoughMaterial) != 0 ? -a : a,
            (bFlags & RoughMaterial) != 0 ? -b : b));

    private static float CombineBounce(float a, ulong aFlags, float b, ulong bFlags) =>
        Math.Clamp(((aFlags & AbsorbentMaterial) != 0 ? -a : a) +
            ((bFlags & AbsorbentMaterial) != 0 ? -b : b), 0f, 1f);
}
