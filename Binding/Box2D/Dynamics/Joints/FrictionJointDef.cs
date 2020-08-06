using System.Numerics;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Dynamics.Joints
{
    /// <summary>
    /// Friction joint definition.
    /// </summary>
    public class FrictionJointDef : JointDef
    {
        /// <summary>
        /// The local anchor point relative to bodyA's origin.
        /// </summary>
        public Vector2 LocalAnchorA;

        /// <summary>
        /// The local anchor point relative to bodyB's origin.
        /// </summary>
        public Vector2 LocalAnchorB;

        /// <summary>
        /// The maximum friction force in N.
        /// </summary>
        public float MaxForce;

        /// <summary>
        /// The maximum friction torque in N-m.
        /// </summary>
        public float MaxTorque;

        public FrictionJointDef()
        {
            JointType = JointType.FrictionJoint;
            LocalAnchorA.SetZero();
            LocalAnchorB.SetZero();
            MaxForce = 0.0f;
            MaxTorque = 0.0f;
        }

        // Point-to-point constraint
        // Cdot = v2 - v1
        //      = v2 + cross(w2, r2) - v1 - cross(w1, r1)
        // J = [-I -r1_skew I r2_skew ]
        // Identity used:
        // w k % (rx i + ry j) = w * (-ry i + rx j)

        // Angle constraint
        // Cdot = w2 - w1
        // J = [0 0 -1 0 0 1]
        // K = invI1 + invI2
        /// <summary>
        /// Initialize the bodies, anchors, axis, and reference angle using the world
        /// anchor and world axis.
        /// </summary>
        public void Initialize(Body bA, Body bB, Vector2 anchor)
        {
            BodyA = bA;
            BodyB = bB;
            LocalAnchorA = BodyA.GetLocalPoint(anchor);
            LocalAnchorB = BodyB.GetLocalPoint(anchor);
        }
    }
}