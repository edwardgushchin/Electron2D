using System.Numerics;
using Electron2D;

namespace PhysicsUnits;

public sealed class Floor : Node
{
    private BoxCollider _collider = null!;
    private Rigidbody _rigidbody = null!;

    public Floor(string name) : base(name)
    {
    }

    protected override void Ready()
    {
        _rigidbody = AddComponent<Rigidbody>();
        _rigidbody.BodyType = PhysicsBodyType.Static;

        _collider = AddComponent<BoxCollider>();
        _collider.Size = new Vector2(20f, 1f);
    }
}