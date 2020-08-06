using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Xml.XPath;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Dynamics.Joints
{
    public enum JointType
    {
        UnknownJoint,

        RevoluteJoint,

        PrismaticJoint,

        DistanceJoint,

        PulleyJoint,

        MouseJoint,

        GearJoint,

        WheelJoint,

        WeldJoint,

        FrictionJoint,

        RopeJoint,

        MotorJoint
    }

    public enum LimitState
    {
        InactiveLimit,

        AtLowerLimit,

        AtUpperLimit,

        EqualLimits
    }

    /// <summary>
    /// A joint edge is used to connect bodies and joints together
    /// in a joint graph where each body is a node and each joint
    /// is an edge. A joint edge belongs to a doubly linked list
    /// maintained in each attached body. Each joint has two joint
    /// nodes, one for each attached body.
    /// </summary>
    public struct JointEdge : IDisposable
    {
        /// <summary>
        /// provides quick access to the other body attached.
        /// </summary>
        public Body Other;

        /// <summary>
        /// the joint
        /// </summary>
        public Joint Joint;

        public LinkedListNode<JointEdge> Node;

        /// <inheritdoc />
        public void Dispose()
        {
            Other = null;
            Joint = null;
            Node = null;
        }
    }

    /// <summary>
    /// Joint definitions are used to construct joints.
    /// </summary>
    public class JointDef : IDisposable
    {
        /// <summary>
        /// The first attached body.
        /// </summary>
        public Body BodyA;

        /// <summary>
        /// The second attached body.
        /// </summary>
        public Body BodyB;

        /// <summary>
        /// Set this flag to true if the attached bodies should collide.
        /// </summary>
        public bool CollideConnected;

        /// <summary>
        /// The joint type is set automatically for concrete joint types.
        /// </summary>
        public JointType JointType = JointType.UnknownJoint;

        /// <summary>
        /// Use this to attach application specific data to your joints.
        /// </summary>
        public object UserData;

        /// <inheritdoc />
        public virtual void Dispose()
        {
            BodyA = null;
            BodyB = null;
            UserData = null;
        }
    }

    public abstract class Joint
    {
        /// <summary>
        /// A物体
        /// </summary>
        public Body BodyA;

        /// <summary>
        /// B物体
        /// </summary>
        public Body BodyB;

        public readonly bool CollideConnected;

        /// <summary>
        /// 关节A头
        /// </summary>
        public JointEdge EdgeA;

        /// <summary>
        /// 关节B头
        /// </summary>
        public JointEdge EdgeB;

        /// <summary>
        /// 关节索引值,只用于Dump
        /// </summary>
        public int Index;

        /// <summary>
        /// 当前关节是否已经在孤岛中
        /// </summary>
        public bool IslandFlag;

        /// <summary>
        /// 关节链表节点
        /// </summary>
        public LinkedListNode<Joint> Node;

        /// <summary>
        /// 用户数据
        /// </summary>
        public object UserData;

        internal Joint(JointDef def)
        {
            System.Diagnostics.Debug.Assert(def.BodyA != def.BodyB);

            JointType = def.JointType;
            BodyA = def.BodyA;
            BodyB = def.BodyB;
            Index = 0;
            CollideConnected = def.CollideConnected;
            IslandFlag = false;
            UserData = def.UserData;
            EdgeA = new JointEdge();
            EdgeB = new JointEdge();
        }

        /// <summary>
        /// Get the next joint the world joint list.
        /// Short-cut function to determine if either body is inactive.
        /// </summary>
        public bool IsEnabled => BodyA.IsEnabled && BodyB.IsEnabled;

        /// <summary>
        /// Get collide connected.
        /// Note: modifying the collide connect flag won't work correctly because
        /// the flag is only checked when fixture AABBs begin to overlap.
        /// </summary>
        public bool IsCollideConnected => CollideConnected;

        /// <summary>
        /// 关节类型
        /// </summary>
        public JointType JointType { get; }

        /// <summary>
        /// Get the anchor point on bodyA in world coordinates.
        /// </summary>
        public abstract Vector2 GetAnchorA();

        /// <summary>
        /// Get the anchor point on bodyB in world coordinates.
        /// </summary>
        public abstract Vector2 GetAnchorB();

        /// <summary>
        /// Get the reaction force on bodyB at the joint anchor in Newtons.
        /// </summary>
        public abstract Vector2 GetReactionForce(float inv_dt);

        /// <summary>
        /// Get the reaction torque on bodyB in N*m.
        /// </summary>
        public abstract float GetReactionTorque(float inv_dt);

        /// <summary>
        /// Dump this joint to the log file.
        /// </summary>
        public virtual void Dump()
        {
            DumpLogger.Log("// Dump is not supported for this joint type.\n");
        }

        /// <summary>
        /// Shift the origin for any points stored in world coordinates.
        /// </summary>
        public virtual void ShiftOrigin(Vector2 newOrigin)
        { }

        /// <summary>
        /// /// Debug draw this joint
        /// </summary>
        /// <param name="drawer"></param>
        public virtual void Draw(IDrawer drawer)
        {
            var xf1 = BodyA.GetTransform();
            var xf2 = BodyB.GetTransform();
            var x1 = xf1.Position;
            var x2 = xf2.Position;
            var p1 = GetAnchorA();
            var p2 = GetAnchorB();

            var color = Color.FromArgb(0.5f, 0.8f, 0.8f);

            switch (JointType)
            {
            case JointType.DistanceJoint:
                drawer.DrawSegment(p1, p2, color);
                break;

            case JointType.PulleyJoint:
            {
                var pulley = (PulleyJoint)this;
                var s1 = pulley.GetGroundAnchorA();
                var s2 = pulley.GetGroundAnchorB();
                drawer.DrawSegment(s1, p1, color);
                drawer.DrawSegment(s2, p2, color);
                drawer.DrawSegment(s1, s2, color);
            }
                break;

            case JointType.MouseJoint:
            {
                var c = Color.FromArgb(0.0f, 1.0f, 0.0f);
                drawer.DrawPoint(p1, 4.0f, c);
                drawer.DrawPoint(p2, 4.0f, c);

                drawer.DrawSegment(p1, p2, Color.FromArgb(0.8f, 0.8f, 0.8f));
            }
                break;

            default:
                drawer.DrawSegment(x1, p1, color);
                drawer.DrawSegment(p1, p2, color);
                drawer.DrawSegment(x2, p2, color);
                break;
            }
        }

        internal abstract void InitVelocityConstraints(in SolverData data);

        internal abstract void SolveVelocityConstraints(in SolverData data);

        // This returns true if the position errors are within tolerance.
        internal abstract bool SolvePositionConstraints(in SolverData data);

        internal static Joint Create(JointDef jointDef)
        {
            return jointDef switch
            {
                DistanceJointDef def => new DistanceJoint(def),
                WheelJointDef def => new WheelJoint(def),
                MouseJointDef def => new MouseJoint(def),
                WeldJointDef def => new WeldJoint(def),
                PulleyJointDef def => new PulleyJoint(def),
                RevoluteJointDef def => new RevoluteJoint(def),
                RopeJointDef def => new RopeJoint(def),
                FrictionJointDef def => new FrictionJoint(def),
                GearJointDef def => new GearJoint(def),
                MotorJointDef def => new MotorJoint(def),
                PrismaticJointDef def => new PrismaticJoint(def),
                _ => throw new ArgumentOutOfRangeException(nameof(jointDef.JointType)),
            };
        }
    }
}