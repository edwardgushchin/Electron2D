namespace Electron2D.Binding.Box2D.Collision.Collider
{
    /// <summary>
    /// This is used for determining the state of contact points.
    /// </summary>
    public enum PointState
    {
        /// <summary>
        /// point does not exist
        /// </summary>
        NullState,

        /// <summary>
        /// point was added in the update
        /// </summary>
        AddState,

        /// <summary>
        /// point persisted across the update
        /// </summary>
        PersistState,

        /// <summary>
        /// point was removed in the update
        /// </summary>
        RemoveState
    }
}