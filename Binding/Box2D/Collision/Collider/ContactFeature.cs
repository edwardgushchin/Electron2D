namespace Electron2D.Binding.Box2D.Collision.Collider
{
    /// <summary>
    /// The features that intersect to form the contact point
    /// This must be 4 bytes or less.
    /// </summary>
    public struct ContactFeature
    {
        public enum FeatureType: byte
        {
            Vertex = 0,

            Face = 1
        }

        /// <summary>
        /// Feature index on shapeA
        /// </summary>
        public byte IndexA;

        /// <summary>
        /// Feature index on shapeB
        /// </summary>
        public byte IndexB;

        /// <summary>
        /// The feature type on shapeA
        /// </summary>
        public byte TypeA;

        /// <summary>
        /// The feature type on shapeB
        /// </summary>
        public byte TypeB;
    }
}