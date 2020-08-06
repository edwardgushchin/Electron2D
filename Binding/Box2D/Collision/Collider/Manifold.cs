using System.Numerics;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Collision.Collider
{
    /// <summary>
    /// A manifold for two touching convex shapes.
    /// Box2D supports multiple types of contact:
    /// - clip point versus plane with radius
    /// - point versus point with radius (circles)
    /// The local point usage depends on the manifold type:
    /// -e_circles: the local center of circleA
    /// -e_faceA: the center of faceA
    /// -e_faceB: the center of faceB
    /// Similarly the local normal usage:
    /// -e_circles: not used
    /// -e_faceA: the normal on polygonA
    /// -e_faceB: the normal on polygonB
    /// We store contacts in this way so that position correction can
    /// account for movement, which is critical for continuous physics.
    /// All contact scenarios must be expressed in one of these types.
    /// This structure is stored across time steps, so we keep it small.
    /// </summary>
    public struct Manifold
    {
        /// <summary>
        /// the points of contact, size Settings.MaxManifoldPoints
        /// </summary>
        public FixedArray2<ManifoldPoint> Points;

        /// <summary>
        /// not use for Type::e_points
        /// </summary>
        public Vector2 LocalNormal;

        /// <summary>
        /// usage depends on manifold type
        /// </summary>
        public Vector2 LocalPoint;

        public ManifoldType Type;

        /// <summary>
        /// the number of manifold points
        /// </summary>
        public int PointCount;
    }
}