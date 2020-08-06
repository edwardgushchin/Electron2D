using System.Numerics;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Dynamics.Joints
{
    /// <summary>
    /// Wheel joint definition. This requires defining a line of
    /// motion using an axis and an anchor point. The definition uses local
    /// anchor points and a local axis so that the initial configuration
    /// can violate the constraint slightly. The joint translation is zero
    /// when the local anchor points coincide in world space. Using local
    /// anchors and a local axis helps when saving and loading a game.
    /// </summary>
    public class WheelJointDef : JointDef
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
        /// The local translation axis in bodyA.
        /// </summary>
        public Vector2 LocalAxisA;

        /// <summary>
        /// Enable/disable the joint limit.
        /// </summary>
        public bool EnableLimit;

        /// <summary>
        /// The lower translation limit, usually in meters.
        /// </summary>
        public float LowerTranslation;

        /// <summary>
        /// The upper translation limit, usually in meters.
        /// </summary>
        public float UpperTranslation;

        /// <summary>
        /// Enable/disable the joint motor.
        /// </summary>
        public bool EnableMotor;

        /// <summary>
        /// The maximum motor torque, usually in N-m.
        /// </summary>
        public float MaxMotorTorque;

        /// <summary>
        /// The desired motor speed in radians per second.
        /// </summary>
        public float MotorSpeed;

        /// <summary>
        /// Suspension stiffness. Typically in units N/m.
        /// </summary>
        public float Stiffness;

        /// <summary>
        /// Suspension damping. Typically in units of N*s/m.
        /// </summary>
        public float Damping;

        public WheelJointDef()
        {
            JointType = JointType.WheelJoint;
            LocalAnchorA.SetZero();
            LocalAnchorB.SetZero();
            LocalAxisA.Set(1.0f, 0.0f);
            EnableLimit = false;
            LowerTranslation = 0.0f;
            UpperTranslation = 0.0f;
            EnableMotor = false;
            MaxMotorTorque = 0.0f;
            MotorSpeed = 0.0f;
            Stiffness = 0.0f;
            Damping = 0.0f;
        }

        /// <summary>
        /// Initialize the bodies, anchors, axis, and reference angle using the world
        /// anchor and world axis.
        /// </summary>
        public void Initialize(Body bA, Body bB, Vector2 anchor, Vector2 axis)
        {
            BodyA = bA;
            BodyB = bB;
            LocalAnchorA = BodyA.GetLocalPoint(anchor);
            LocalAnchorB = BodyB.GetLocalPoint(anchor);
            LocalAxisA = BodyA.GetLocalVector(axis);
        }
    }
}