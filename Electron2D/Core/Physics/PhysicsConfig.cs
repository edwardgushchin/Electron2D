using System.Numerics;

namespace Electron2D;

public sealed class PhysicsConfig
{
    public Vector2 Gravity { get; set; } = new(0, -9.81f);

    /// <summary>Fixed step (секунды).</summary>
    public float FixedDelta { get; set; } = 1f / 60f;
}