namespace Electron2D.Binding.Box2D.Dynamics
{
    /// <summary>
    /// Implement this class to provide collision filtering. In other words, you can implement
    /// this class if you want finer control over contact creation.
    /// </summary>
    public interface IContactFilter
    {
        /// <summary>
        /// Return true if contact calculations should be performed between these two shapes.
        /// @warning for performance reasons this is only called when the AABBs begin to overlap.
        /// </summary>
        bool ShouldCollide(Fixture fixtureA, Fixture fixtureB);
    }
}