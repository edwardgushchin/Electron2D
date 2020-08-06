using System.Numerics;

namespace Electron2D.Binding.Box2D.Collision.Collider
{
    /// <summary>
    /// Ray-cast output data. The ray hits at p1 + fraction * (p2 - p1), where p1 and p2
    /// come from b2RayCastInput.
    /// </summary>
    public struct RayCastOutput
    {
        public Vector2 Normal;

        public float Fraction;
    }
}