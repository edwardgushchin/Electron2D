using System.Diagnostics;
using System.Numerics;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Dynamics.Joints
{
    /// <summary>
    /// Pulley joint definition. This requires two ground anchors,
    /// two dynamic body anchor points, and a pulley ratio.
    /// </summary>
    public class PulleyJointDef : JointDef
    {
        /// <summary>
        /// The first ground anchor in world coordinates. This point never moves.
        /// </summary>
        public Vector2 GroundAnchorA;

        /// <summary>
        /// The second ground anchor in world coordinates. This point never moves.
        /// </summary>
        public Vector2 GroundAnchorB;

        /// <summary>
        /// The a reference length for the segment attached to bodyA.
        /// </summary>
        public float LengthA;

        /// <summary>
        /// The a reference length for the segment attached to bodyB.
        /// </summary>
        public float LengthB;

        /// <summary>
        /// The local anchor point relative to bodyA's origin.
        /// </summary>
        public Vector2 LocalAnchorA;

        /// <summary>
        /// The local anchor point relative to bodyB's origin.
        /// </summary>
        public Vector2 LocalAnchorB;

        /// <summary>
        /// The pulley ratio, used to simulate a block-and-tackle.
        /// </summary>
        public float Ratio;

        public PulleyJointDef()
        {
            JointType = JointType.PulleyJoint;

            GroundAnchorA.Set(-1.0f, 1.0f);

            GroundAnchorB.Set(1.0f, 1.0f);

            LocalAnchorA.Set(-1.0f, 0.0f);

            LocalAnchorB.Set(1.0f, 0.0f);

            LengthA = 0.0f;

            LengthB = 0.0f;

            Ratio = 1.0f;

            CollideConnected = true;
        }

        /// <summary>
        /// Initialize the bodies, anchors, lengths, max lengths, and ratio using the world anchors.
        /// </summary>
        public void Initialize(
            Body bA,
            Body bB,
            Vector2 groundA,
            Vector2 groundB,
            Vector2 anchorA,
            Vector2 anchorB,
            float r)
        {
            BodyA = bA;
            BodyB = bB;
            GroundAnchorA = groundA;
            GroundAnchorB = groundB;
            LocalAnchorA = BodyA.GetLocalPoint(anchorA);
            LocalAnchorB = BodyB.GetLocalPoint(anchorB);
            var dA = anchorA - groundA;
            LengthA = dA.Length();
            var dB = anchorB - groundB;
            LengthB = dB.Length();
            Ratio = r;
            System.Diagnostics.Debug.Assert(Ratio > Settings.Epsilon);
        }
    }
}