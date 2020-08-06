using System.Numerics;

namespace Electron2D.Binding.Box2D.Collision.Collider
{
    /// <summary>
    /// A manifold point is a contact point belonging to a contact
    /// manifold. It holds details related to the geometry and dynamics
    /// of the contact points.
    /// The local point usage depends on the manifold type:
    /// -e_circles: the local center of circleB
    /// -e_faceA: the local center of cirlceB or the clip point of polygonB
    /// -e_faceB: the clip point of polygonA
    /// This structure is stored across time steps, so we keep it small.
    /// Note: the impulses are used for internal caching and may not
    /// provide reliable contact forces, especially for high speed collisions.
    /// </summary>
    public struct ManifoldPoint
    {
        /// <summary>
        /// usage depends on manifold type
        /// </summary>
        public Vector2 LocalPoint;

        /// <summary>
        /// the non-penetration impulse
        /// </summary>
        public float NormalImpulse;

        /// <summary>
        /// /// the friction impulse
        /// </summary>
        public float TangentImpulse;

        /// <summary>
        /// uniquely identifies a contact point between two shapes
        /// </summary>
        public ContactId Id;
    }
}