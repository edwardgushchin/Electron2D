using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Dynamics.Contacts
{
    /// <summary>
    /// Contact impulses for reporting. Impulses are used instead of forces because
    /// sub-step forces may approach infinity for rigid body collisions. These
    /// match up one-to-one with the contact points in b2Manifold.
    /// </summary>
    public struct ContactImpulse
    {
        public FixedArray2<float> NormalImpulses;

        public FixedArray2<float> TangentImpulses;

        public int Count;
    }
}