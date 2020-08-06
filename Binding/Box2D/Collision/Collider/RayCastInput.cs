using System.Numerics;

namespace Electron2D.Binding.Box2D.Collision.Collider
{
    /// <summary>
    /// Ray-cast input data. The ray extends from p1 to p1 + maxFraction * (p2 - p1).
    /// </summary>
    public struct RayCastInput
    {
        public Vector2 P1;

        public Vector2 P2;

        public float MaxFraction;
    }
}