using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Collision
{
    /// <summary>
    /// Input for b2Distance.
    /// You have to option to use the shape radii
    /// in the computation. Even
    /// </summary>
    public struct DistanceInput
    {
        public DistanceProxy ProxyA;

        public DistanceProxy ProxyB;

        public Transform TransformA;

        public Transform TransformB;

        public bool UseRadii;
    }
}