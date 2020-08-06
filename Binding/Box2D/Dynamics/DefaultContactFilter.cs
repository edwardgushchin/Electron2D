namespace Electron2D.Binding.Box2D.Dynamics
{
    /// <summary>
    /// Implement this class to provide collision filtering. In other words, you can implement
    /// this class if you want finer control over contact creation.
    /// </summary>
    public sealed class DefaultContactFilter : IContactFilter
    {
        /// <summary>
        /// Return true if contact calculations should be performed between these two shapes.
        /// @warning for performance reasons this is only called when the AABBs begin to overlap.
        /// </summary>
        public bool ShouldCollide(Fixture fixtureA, Fixture fixtureB)
        {
            var filterA = fixtureA.Filter;
            var filterB = fixtureB.Filter;

            if (filterA.GroupIndex == filterB.GroupIndex && filterA.GroupIndex != 0)
            {
                return filterA.GroupIndex > 0;
            }

            var collide = (filterA.MaskBits & filterB.CategoryBits) != 0
                       && (filterA.CategoryBits & filterB.MaskBits) != 0;
            return collide;
        }
    }
}