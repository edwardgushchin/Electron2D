using System.Numerics;

namespace Electron2D.Binding.Box2D.Collision.Collider
{
    /// <summary>
    /// Used for computing contact manifolds.
    /// </summary>
    public struct ClipVertex
    {
        public Vector2 Vector;

        public ContactId Id;
    }
}