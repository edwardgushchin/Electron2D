using System.Numerics;

namespace Electron2D.Binding.Box2D.Collision
{
    /// <summary>
    /// Output for b2Distance.
    /// </summary>
    public struct DistanceOutput
    {
        /// <summary>
        /// closest point on shapeA
        /// </summary>
        public Vector2 PointA;

        /// <summary>
        /// closest point on shapeB
        /// </summary>
        public Vector2 PointB;

        public float Distance;

        /// <summary>
        /// number of GJK iterations used
        /// </summary>
        public int Iterations;
    }
}