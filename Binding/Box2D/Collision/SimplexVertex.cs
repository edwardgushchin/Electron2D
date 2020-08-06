using System.Numerics;

namespace Electron2D.Binding.Box2D.Collision
{
    public struct SimplexVertex
    {
        public Vector2 Wa; // support point in proxyA

        public Vector2 Wb; // support point in proxyB

        public Vector2 W; // wB - wA

        public float A; // barycentric coordinate for closest point

        public int IndexA; // wA index

        public int IndexB; // wB index
    }
}