using System.Numerics;
using Electron2D.Binding.Box2D.Common;
using Electron2D.Binding.Box2D.Dynamics.Joints;

namespace Electron2D.Binding.Box2D.Ropes
{
    /// <summary>
    /// Rope joint definition. This requires two body anchor points and
    /// a maximum lengths.
    /// Note: by default the connected objects will not collide.
    /// see collideConnected in b2JointDef.
    /// </summary>
    public class RopeJointDef : JointDef
    {
        public RopeJointDef()
        {
            JointType = JointType.RopeJoint;
            LocalAnchorA.Set(-1.0f, 0.0f);
            LocalAnchorB.Set(1.0f, 0.0f);
            MaxLength = 0.0f;
        }

        /// <summary>
        /// The local anchor point relative to bodyA's origin.
        /// </summary>
        public Vector2 LocalAnchorA;

        /// <summary>
        /// The local anchor point relative to bodyB's origin.
        /// </summary>
        public Vector2 LocalAnchorB;

        /// <summary>
        /// The maximum length of the rope.
        /// Warning: this must be larger than b2_linearSlop or
        /// the joint will have no effect.
        /// </summary>
        public float MaxLength;
    };
}