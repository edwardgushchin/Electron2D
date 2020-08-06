using System.Numerics;

namespace Electron2D.Binding.Box2D.Collision.Shapes
{
    public struct MassData
    {
        /// <summary>
        /// The mass of the shape, usually in kilograms.
        /// </summary>
        public float Mass;

        /// <summary>
        /// The position of the shape's centroid relative to the shape's origin.
        /// </summary>
        public Vector2 Center;

        /// <summary>
        /// The rotational inertia of the shape about the local origin.
        /// </summary>
        public float RotationInertia;
    }
}