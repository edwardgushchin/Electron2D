namespace Electron2D.Binding.Box2D.Dynamics.Joints
{
    /// <summary>
    /// Gear joint definition. This definition requires two existing
    /// revolute or prismatic joints (any combination will work).
    /// </summary>
    public class GearJointDef : JointDef
    {
        /// <summary>
        /// The first revolute/prismatic joint attached to the gear joint.
        /// </summary>
        public Joint Joint1;

        /// <summary>
        /// The second revolute/prismatic joint attached to the gear joint.
        /// </summary>
        public Joint Joint2;

        /// <summary>
        /// The gear ratio.
        /// @see b2GearJoint for explanation.
        /// </summary>
        public float Ratio;

        public GearJointDef()
        {
            JointType = JointType.GearJoint;
            Joint1 = null;
            Joint2 = null;
            Ratio = 1.0f;
        }
    }
}