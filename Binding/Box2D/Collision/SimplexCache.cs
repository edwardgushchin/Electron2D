using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Collision
{
    /// <summary>
    /// Used to warm start b2Distance.
    /// Set count to zero on first call.
    /// </summary>
    public struct SimplexCache
    {
        /// <summary>
        /// length or area
        /// </summary>
        public float Metric;

        public ushort Count;

        /// <summary>
        /// vertices on shape A
        /// </summary>
        public FixedArray3<byte> IndexA;

        /// <summary>
        /// vertices on shape B
        /// </summary>
        public FixedArray3<byte> IndexB;
    }
}