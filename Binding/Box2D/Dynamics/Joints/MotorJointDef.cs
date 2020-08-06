using System.Numerics;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Dynamics.Joints
{
    /// <summary>
    /// Motor joint definition.
    /// </summary>
    public class MotorJointDef : JointDef
    {
        /// <summary>
        /// The bodyB angle minus bodyA angle in radians.
        /// </summary>
        public float AngularOffset;

        /// <summary>
        /// Position correction factor in the range [0,1].
        /// </summary>
        public float CorrectionFactor;

        /// <summary>
        /// Position of bodyB minus the position of bodyA, in bodyA's frame, in meters.
        /// </summary>
        public Vector2 LinearOffset;

        /// <summary>
        /// The maximum motor force in N.
        /// </summary>
        public float MaxForce;

        /// <summary>
        /// The maximum motor torque in N-m.
        /// </summary>
        public float MaxTorque;

        public MotorJointDef()
        {
            JointType = JointType.MotorJoint;
            LinearOffset.SetZero();
            AngularOffset = 0.0f;
            MaxForce = 1.0f;
            MaxTorque = 1.0f;
            CorrectionFactor = 0.3f;
        }

        /// <summary>
        /// Initialize the bodies and offsets using the current transforms.
        /// </summary>
        public void Initialize(Body bA, Body bB)
        {
            BodyA = bA;
            BodyB = bB;
            var xB = BodyB.GetPosition();
            LinearOffset = BodyA.GetLocalPoint(xB);

            var angleA = BodyA.GetAngle();
            var angleB = BodyB.GetAngle();
            AngularOffset = angleB - angleA;
        }
    }
}