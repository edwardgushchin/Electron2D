using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Electron2D.Binding.Box2D.Collision.Shapes;
using Electron2D.Binding.Box2D.Common;
using Electron2D.Binding.Box2D.Dynamics.Contacts;
using Electron2D.Binding.Box2D.Dynamics.Joints;

namespace Electron2D.Binding.Box2D.Dynamics
{
    /// <summary>
    /// The body type.
    /// static: zero mass, zero velocity, may be manually moved
    /// kinematic: zero mass, non-zero velocity set by user, moved by solver
    /// dynamic: positive mass, non-zero velocity determined by forces, moved by solver
    /// </summary>
    public enum BodyType
    {
        StaticBody = 0,

        KinematicBody = 1,

        DynamicBody = 2

        // TODO_ERIN
        //b2_bulletBody,
    }

    /// <summary>
    /// A body definition holds all the data needed to construct a rigid body.
    /// You can safely re-use body definitions. Shapes are added to a body after construction.
    /// </summary>
    public struct BodyDef
    {
        private bool? _enabled;

        /// <summary>
        /// Does this body start out enabled?
        /// </summary>
        public bool Enabled
        {
            get => _enabled ?? true;
            set => _enabled = value;
        }

        private bool? _allowSleep;

        /// <summary>
        /// Set this flag to false if this body should never fall asleep. Note that
        /// this increases CPU usage.
        /// </summary>
        public bool AllowSleep
        {
            get => _allowSleep ?? true;
            set => _allowSleep = value;
        }

        /// <summary>
        /// The world angle of the body in radians.
        /// </summary>
        public float Angle;

        /// <summary>
        /// Angular damping is use to reduce the angular velocity. The damping parameter
        /// can be larger than 1.0f but the damping effect becomes sensitive to the
        /// time step when the damping parameter is large.
        /// Units are 1/time
        /// </summary>
        public float AngularDamping;

        /// <summary>
        /// The angular velocity of the body.
        /// </summary>
        public float AngularVelocity;

        private bool? _awake;

        /// <summary>
        /// Is this body initially awake or sleeping?
        /// </summary>
        public bool Awake
        {
            get => _awake ?? true;
            set => _awake = value;
        }

        /// <summary>
        /// The body type: static, kinematic, or dynamic.
        /// Note: if a dynamic body would have zero mass, the mass is set to one.
        /// </summary>
        public BodyType BodyType;

        /// <summary>
        /// Is this a fast moving body that should be prevented from tunneling through
        /// other moving bodies? Note that all bodies are prevented from tunneling through
        /// kinematic and static bodies. This setting is only considered on dynamic bodies.
        /// @warning You should use this flag sparingly since it increases processing time.
        /// </summary>
        public bool Bullet;

        /// <summary>
        /// Should this body be prevented from rotating? Useful for characters.
        /// </summary>
        public bool FixedRotation;

        private float? _gravityScale;

        /// <summary>
        /// Scale the gravity applied to this body.
        /// </summary>
        public float GravityScale
        {
            get => _gravityScale ?? 1.0f;
            set => _gravityScale = value;
        }

        /// <summary>
        /// Linear damping is use to reduce the linear velocity. The damping parameter
        /// can be larger than 1.0f but the damping effect becomes sensitive to the
        /// time step when the damping parameter is large.
        /// Units are 1/time
        /// </summary>
        public float LinearDamping;

        /// <summary>
        /// The linear velocity of the body's origin in world co-ordinates.
        /// </summary>
        public Vector2 LinearVelocity;

        /// <summary>
        /// The world position of the body. Avoid creating bodies at the origin
        /// since this can lead to many overlapping shapes.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Use this to store application specific body data.
        /// </summary>
        public object UserData;
    }

    /// <summary>
    /// A rigid body. These are created via b2World::CreateBody.
    /// </summary>
    public class Body : IDisposable
    {
        /// <summary>
        /// 接触边缘列表
        /// </summary>
        internal readonly LinkedList<ContactEdge> ContactEdges;

        /// <summary>
        /// 夹具列表
        /// </summary>
        public IReadOnlyList<Fixture> FixtureList => Fixtures;

        /// <summary>
        /// 夹具列表
        /// </summary>
        internal readonly List<Fixture> Fixtures;

        /// <summary>
        /// 关节边缘列表
        /// </summary>
        internal readonly LinkedList<JointEdge> JointEdges;

        /// <summary>
        /// 质心的转动惯量
        /// </summary>
        private float _inertia;

        /// <summary>
        /// Get the total mass of the body.
        /// @return the mass, usually in kilograms (kg).
        /// 质量
        /// </summary>
        private float _mass;

        /// <summary>
        /// 物体类型
        /// </summary>
        private BodyType _type;

        /// <summary>
        /// 所属世界
        /// </summary>
        internal World _world;

        /// <summary>
        /// 物体标志
        /// </summary>
        internal BodyFlags Flags;

        /// <summary>
        /// 受力
        /// </summary>
        internal Vector2 Force;

        /// <summary>
        /// 重力系数
        /// </summary>
        internal float GravityScale;

        /// <summary>
        /// 质心的转动惯量倒数
        /// </summary>
        internal float InverseInertia;

        /// <summary>
        /// 质量倒数
        /// </summary>
        internal float InvMass;

        /// <summary>
        /// 岛屿索引
        /// </summary>
        internal int IslandIndex;

        /// <summary>
        /// 链表节点物体
        /// </summary>
        internal LinkedListNode<Body> Node;

        /// <summary>
        /// 扫描
        /// </summary>
        internal Sweep Sweep; // the swept motion for CCD

        /// <summary>
        /// 扭矩
        /// </summary>
        internal float Torque;

        /// <summary>
        /// 物体位置
        /// </summary>
        internal Transform Transform; // the body origin transform

        internal Body(BodyDef def, World world)
        {
            System.Diagnostics.Debug.Assert(def.Position.IsValid());
            System.Diagnostics.Debug.Assert(def.LinearVelocity.IsValid());
            System.Diagnostics.Debug.Assert(def.Angle.IsValid());
            System.Diagnostics.Debug.Assert(def.AngularVelocity.IsValid());
            System.Diagnostics.Debug.Assert(def.AngularDamping.IsValid() && def.AngularDamping >= 0.0f);
            System.Diagnostics.Debug.Assert(def.LinearDamping.IsValid() && def.LinearDamping >= 0.0f);

            Flags = 0;

            if (def.Bullet)
            {
                Flags |= BodyFlags.IsBullet;
            }

            if (def.FixedRotation)
            {
                Flags |= BodyFlags.FixedRotation;
            }

            if (def.AllowSleep)
            {
                Flags |= BodyFlags.AutoSleep;
            }

            if (def.Awake && def.BodyType != BodyType.StaticBody)
            {
                Flags |= BodyFlags.IsAwake;
            }

            if (def.Enabled)
            {
                Flags |= BodyFlags.IsEnabled;
            }

            _world = world;

            Transform.Position = def.Position;
            Transform.Rotation.Set(def.Angle);

            Sweep = new Sweep
            {
                LocalCenter = Vector2.Zero,
                C0 = Transform.Position,
                C = Transform.Position,
                A0 = def.Angle,
                A = def.Angle,
                Alpha0 = 0.0f
            };

            JointEdges = new LinkedList<JointEdge>();
            ContactEdges = new LinkedList<ContactEdge>();
            Fixtures = new List<Fixture>();
            Node = null;

            LinearVelocity = def.LinearVelocity;
            AngularVelocity = def.AngularVelocity;

            LinearDamping = def.LinearDamping;
            AngularDamping = def.AngularDamping;
            GravityScale = def.GravityScale;

            Force.SetZero();
            Torque = 0.0f;

            SleepTime = 0.0f;

            _type = def.BodyType;

            _mass = 0.0f;
            InvMass = 0.0f;

            _inertia = 0.0f;
            InverseInertia = 0.0f;

            UserData = def.UserData;
        }

        public float AngularDamping { get; set; }

        /// <summary>
        /// Get/Set the angular velocity.
        /// the new angular velocity in radians/second.
        /// 角速度
        /// </summary>
        public float AngularVelocity { get; internal set; }

        /// <summary>
        /// Get the rotational inertia of the body about the local origin.
        /// @return the rotational inertia, usually in kg-m^2.
        /// </summary>
        public float Inertia => _inertia + (_mass * Vector2.Dot(Sweep.LocalCenter, Sweep.LocalCenter));

        /// <summary>
        /// Get/Set the linear damping of the body.
        /// </summary>
        public float LinearDamping { get; set; }

        /// <summary>
        /// 线速度
        /// </summary>
        /// Set the linear velocity of the center of mass.
        /// @param v the new linear velocity of the center of mass.
        /// Get the linear velocity of the center of mass.
        /// @return the linear velocity of the center of mass.
        public Vector2 LinearVelocity { get; internal set; }

        public float Mass => _mass;

        /// <summary>
        /// 休眠时间
        /// </summary>
        internal float SleepTime { get; set; }

        /// <summary>
        /// Set the type of this body. This may alter the mass and velocity.
        /// </summary>
        public BodyType BodyType
        {
            get => _type;
            set
            {
                System.Diagnostics.Debug.Assert(!_world.IsLocked);
                if (_world.IsLocked)
                {
                    return;
                }

                if (_type == value)
                {
                    return;
                }

                _type = value;

                ResetMassData();

                if (_type == BodyType.StaticBody)
                {
                    LinearVelocity = Vector2.Zero;
                    AngularVelocity = 0.0f;
                    Sweep.A0 = Sweep.A;
                    Sweep.C0 = Sweep.C;
                    UnsetFlag(BodyFlags.IsAwake);
                    SynchronizeFixtures();
                }

                IsAwake = true;

                Force.SetZero();
                Torque = 0.0f;

                // Delete the attached contacts.
                // 删除所有接触点

                var node = ContactEdges.First;
                while (node != null)
                {
                    var c = node.Value;
                    node = node.Next;
                    _world.ContactManager.Destroy(c.Contact);
                }

                ContactEdges.Clear();

                // Touch the proxies so that new contacts will be created (when appropriate)
                var broadPhase = _world.ContactManager.BroadPhase;
                foreach (var f in Fixtures)
                {
                    var proxyCount = f.ProxyCount;
                    for (var i = 0; i < proxyCount; ++i)
                    {
                        broadPhase.TouchProxy(f.Proxies[i].ProxyId);
                    }
                }
            }
        }

        /// <summary>
        /// Should this body be treated like a bullet for continuous collision detection?
        /// Is this body treated like a bullet for continuous collision detection?
        /// </summary>
        public bool IsBullet
        {
            get => (Flags & BodyFlags.IsBullet) != 0;
            set
            {
                if (value)
                {
                    Flags |= BodyFlags.IsBullet;
                }
                else
                {
                    Flags &= ~BodyFlags.IsBullet;
                }
            }
        }

        /// <summary>
        /// You can disable sleeping on this body. If you disable sleeping, the
        /// body will be woken.
        /// Is this body allowed to sleep
        /// </summary>
        public bool IsSleepingAllowed
        {
            get => (Flags & BodyFlags.AutoSleep) != 0;
            set
            {
                if (value)
                {
                    Flags |= BodyFlags.AutoSleep;
                }
                else
                {
                    Flags &= ~BodyFlags.AutoSleep;
                    IsAwake = true;
                }
            }
        }

        /// <summary>
        /// Set the sleep state of the body. A sleeping body has very
        /// low CPU cost.
        /// @param flag set to true to wake the body, false to put it to sleep.
        /// Get the sleeping state of this body.
        /// @return true if the body is awake.
        /// </summary>
        public bool IsAwake
        {
            get => (Flags & BodyFlags.IsAwake) != 0;
            set
            {
                if (BodyType == BodyType.StaticBody)
                {
                    return;
                }

                if (value)
                {
                    Flags |= BodyFlags.IsAwake;
                    SleepTime = 0.0f;
                }
                else
                {
                    Flags &= ~BodyFlags.IsAwake;
                    SleepTime = 0.0f;
                    LinearVelocity = Vector2.Zero;
                    AngularVelocity = 0.0f;
                    Force.SetZero();
                    Torque = 0.0f;
                }
            }
        }

        /// <summary>
        /// Set the active state of the body. An inactive body is not
        /// simulated and cannot be collided with or woken up.
        /// If you pass a flag of true, all fixtures will be added to the
        /// broad-phase.
        /// If you pass a flag of false, all fixtures will be removed from
        /// the broad-phase and all contacts will be destroyed.
        /// Fixtures and joints are otherwise unaffected. You may continue
        /// to create/destroy fixtures and joints on inactive bodies.
        /// Fixtures on an inactive body are implicitly inactive and will
        /// not participate in collisions, ray-casts, or queries.
        /// Joints connected to an inactive body are implicitly inactive.
        /// An inactive body is still owned by a b2World object and remains
        /// in the body list.
        /// Get the active state of the body.
        /// </summary>
        public bool IsEnabled

        {
            get => (Flags & BodyFlags.IsEnabled) != 0;
            set
            {
                System.Diagnostics.Debug.Assert(!_world.IsLocked);

                if (value == IsEnabled)
                {
                    return;
                }

                if (value)
                {
                    Flags |= BodyFlags.IsEnabled;

                    // Create all proxies.
                    // 激活时创建粗检测代理
                    var broadPhase = _world.ContactManager.BroadPhase;
                    foreach (var f in Fixtures)
                    {
                        f.CreateProxies(broadPhase, Transform);
                    }

                    // Contacts are created at the beginning of the next
                    World.HasNewContacts = true;
                }
                else
                {
                    Flags &= ~BodyFlags.IsEnabled;

                    // Destroy all proxies.
                    // 休眠时销毁粗检测代理
                    var broadPhase = _world.ContactManager.BroadPhase;
                    foreach (var f in Fixtures)
                    {
                        f.DestroyProxies(broadPhase);
                    }

                    // Destroy the attached contacts.
                    // 销毁接触点
                    var node = ContactEdges.First;
                    while (node != null)
                    {
                        var c = node.Value;
                        node = node.Next;
                        _world.ContactManager.Destroy(c.Contact);
                    }

                    ContactEdges.Clear();
                }
            }
        }

        /// <summary>
        /// Set this body to have fixed rotation. This causes the mass
        /// to be reset.
        /// </summary>
        public bool IsFixedRotation
        {
            get => (Flags & BodyFlags.FixedRotation) != 0;
            set
            {
                // 物体已经有固定旋转,不需要设置
                if ((Flags & BodyFlags.FixedRotation) != 0 && value)
                {
                    return;
                }

                if (value)
                {
                    Flags |= BodyFlags.FixedRotation;
                }
                else
                {
                    Flags &= ~BodyFlags.FixedRotation;
                }

                AngularVelocity = 0.0f;

                ResetMassData();
            }
        }

        /// <summary>
        /// Get/Set the user data pointer that was provided in the body definition.
        /// 用户信息
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Get the parent world of this body.
        /// </summary>
        public World World => _world;

        /// <inheritdoc />
        public void Dispose()
        {
            _world = null;
            System.Diagnostics.Debug.Assert(ContactEdges.Count == 0, "ContactEdges.Count == 0");
            System.Diagnostics.Debug.Assert(JointEdges.Count == 0, "JointEdges.Count == 0");
            ContactEdges?.Clear();
            JointEdges?.Clear();
            Fixtures?.Clear();
            GC.SuppressFinalize(this);
        }

        public void SetAngularVelocity(float value)
        {
            if (_type == BodyType.StaticBody) // 静态物体无角速度
            {
                return;
            }

            if (value * value > 0.0f)
            {
                IsAwake = true;
            }

            AngularVelocity = value;
        }

        public void SetLinearVelocity(Vector2 value)
        {
            if (_type == BodyType.StaticBody) // 静态物体无加速度
            {
                return;
            }

            if (Vector2.Dot(value, value) > 0.0f) // 点积大于0时唤醒本物体
            {
                IsAwake = true;
            }

            LinearVelocity = value;
        }

        /// <summary>
        /// Creates a fixture and attach it to this body. Use this function if you need
        /// to set some fixture parameters, like friction. Otherwise you can create the
        /// fixture directly from a shape.
        /// If the density is non-zero, this function automatically updates the mass of the body.
        /// Contacts are not created until the next time step.
        /// @param def the fixture definition.
        /// @warning This function is locked during callbacks.
        /// 创建夹具
        /// </summary>
        /// <param name="def"></param>
        /// <returns></returns>
        public Fixture CreateFixture(FixtureDef def)
        {
            System.Diagnostics.Debug.Assert(!_world.IsLocked);
            if (_world.IsLocked)
            {
                return null;
            }

            var fixture = Fixture.Create(this, def);

            if ((Flags & BodyFlags.IsEnabled) != 0)
            {
                var broadPhase = _world.ContactManager.BroadPhase;
                fixture.CreateProxies(broadPhase, Transform);
            }

            fixture.Body = this;
            Fixtures.Add(fixture);

            // Adjust mass properties if needed.
            if (fixture.Density > 0.0f)
            {
                ResetMassData();
            }

            // Let the world know we have a new fixture. This will cause new contacts
            // to be created at the beginning of the next time step.
            // 通知世界存在新增夹具,在下一个时间步中将自动创建新夹具的接触点
            _world.HasNewContacts = true;

            return fixture;
        }

        /// <summary>
        /// Creates a fixture from a shape and attach it to this body.
        /// This is a convenience function. Use b2FixtureDef if you need to set parameters
        /// like friction, restitution, user data, or filtering.
        /// If the density is non-zero, this function automatically updates the mass of the body.
        /// @param shape the shape to be cloned.
        /// @param density the shape density (set to zero for static bodies).
        /// @warning This function is locked during callbacks.
        /// 创建夹具
        /// </summary>
        public Fixture CreateFixture(Shape shape, float density)
        {
            var def = new FixtureDef {Shape = shape, Density = density};

            return CreateFixture(def);
        }

        /// <summary>
        /// Destroy a fixture. This removes the fixture from the broad-phase and
        /// destroys all contacts associated with this fixture. This will
        /// automatically adjust the mass of the body if the body is dynamic and the
        /// fixture has positive density.
        /// All fixtures attached to a body are implicitly destroyed when the body is destroyed.
        /// @param fixture the fixture to be removed.
        /// @warning This function is locked during callbacks.
        /// 删除夹具
        /// </summary>
        public void DestroyFixture(Fixture fixture)
        {
            if (fixture == default)
            {
                return;
            }

            // 世界锁定时不能删除夹具
            System.Diagnostics.Debug.Assert(!_world.IsLocked);
            if (_world.IsLocked)
            {
                return;
            }

            // 断言夹具所属物体
            System.Diagnostics.Debug.Assert(fixture.Body == this);

            // Remove the fixture from this body's singly linked list.
            System.Diagnostics.Debug.Assert(Fixtures.Count > 0);

            // You tried to remove a shape that is not attached to this body.
            // 确定该夹具存在于物体的夹具列表中
            System.Diagnostics.Debug.Assert(Fixtures.Any(e => e == fixture));

            // Destroy any contacts associated with the fixture.
            // 销毁关联在夹具上的接触点
            var node = ContactEdges.First;
            while (node != null)
            {
                var contactEdge = node.Value;
                node = node.Next;
                if (contactEdge.Contact.FixtureA == fixture || contactEdge.Contact.FixtureB == fixture)
                {
                    // This destroys the contact and removes it from
                    // this body's contact list.
                    _world.ContactManager.Destroy(contactEdge.Contact);
                }
            }

            // 如果物体处于活跃状态,销毁夹具的粗检测代理对象
            if ((Flags & BodyFlags.IsEnabled) != 0)
            {
                var broadPhase = _world.ContactManager.BroadPhase;
                fixture.DestroyProxies(broadPhase);
            }

            Fixtures.Remove(fixture);
            fixture.Body = null;
            Fixture.Destroy(fixture);

            // Reset the mass data.
            // 夹具销毁后重新计算物体质量
            ResetMassData();
        }

        /// <summary>
        /// Set the position of the body's origin and rotation.
        /// Manipulating a body's transform may cause non-physical behavior.
        /// Note: contacts are updated on the next call to b2World::Step.
        /// @param position the world position of the body's local origin.
        /// @param angle the world rotation in radians.
        /// </summary>
        public void SetTransform(Vector2 position, float angle)
        {
            System.Diagnostics.Debug.Assert(!_world.IsLocked);
            if (_world.IsLocked)
            {
                return;
            }

            Transform.Rotation.Set(angle);
            Transform.Position = position;

            Sweep.C = MathUtils.Mul(Transform, Sweep.LocalCenter);
            Sweep.A = angle;

            Sweep.C0 = Sweep.C;
            Sweep.A0 = angle;

            var broadPhase = _world.ContactManager.BroadPhase;
            foreach (var f in Fixtures)
            {
                f.Synchronize(broadPhase, Transform, Transform);
            }
        }

        /// <summary>
        /// Get the body transform for the body's origin.
        /// @return the world transform of the body's origin.
        /// </summary>
        public Transform GetTransform()
        {
            return Transform;
        }

        /// <summary>
        /// Get the world body origin position.
        /// @return the world position of the body's origin.
        /// </summary>
        public Vector2 GetPosition()
        {
            return Transform.Position;
        }

        /// <summary>
        /// Get the angle in radians.
        /// @return the current world rotation angle in radians.
        /// </summary>
        public float GetAngle()
        {
            return Sweep.A;
        }

        /// <summary>
        /// Get the world position of the center of mass.
        /// </summary>
        public Vector2 GetWorldCenter()
        {
            return Sweep.C;
        }

        /// <summary>
        /// Get the local position of the center of mass.
        /// </summary>
        public Vector2 GetLocalCenter()
        {
            return Sweep.LocalCenter;
        }

        /// <summary>
        /// Apply a force at a world point. If the force is not
        /// applied at the center of mass, it will generate a torque and
        /// affect the angular velocity. This wakes up the body.
        /// @param force the world force vector, usually in Newtons (N).
        /// @param point the world position of the point of application.
        /// @param wake also wake up the body
        /// 在指定位置施加作用力
        /// </summary>
        /// <param name="force"></param>
        /// <param name="point"></param>
        /// <param name="wake"></param>
        public void ApplyForce(Vector2 force, Vector2 point, bool wake)
        {
            if (_type != BodyType.DynamicBody)
            {
                return;
            }

            if (wake && (Flags & BodyFlags.IsAwake) == 0)
            {
                IsAwake = true;
            }

            // Don't accumulate a force if the body is sleeping.
            if ((Flags & BodyFlags.IsAwake) != 0)
            {
                Force += force;
                Torque += MathUtils.Cross(point - Sweep.C, force);
            }
        }

        /// <summary>
        /// Apply a force to the center of mass. This wakes up the body.
        /// @param force the world force vector, usually in Newtons (N).
        /// @param wake also wake up the body
        /// 在质心施加作用力
        /// </summary>
        /// <param name="force"></param>
        /// <param name="wake"></param>
        public void ApplyForceToCenter(Vector2 force, bool wake)
        {
            if (_type != BodyType.DynamicBody)
            {
                return;
            }

            if (wake && (Flags & BodyFlags.IsAwake) == 0)
            {
                IsAwake = true;
            }

            // Don't accumulate a force if the body is sleeping
            if ((Flags & BodyFlags.IsAwake) != 0)
            {
                Force += force;
            }
        }

        /// <summary>
        /// Apply a torque. This affects the angular velocity
        /// without affecting the linear velocity of the center of mass.
        /// @param torque about the z-axis (out of the screen), usually in N-m.
        /// @param wake also wake up the body
        /// 施加扭矩
        /// </summary>
        /// <param name="torque"></param>
        /// <param name="wake"></param>
        public void ApplyTorque(float torque, bool wake)
        {
            if (_type != BodyType.DynamicBody)
            {
                return;
            }

            if (wake && (Flags & BodyFlags.IsAwake) == 0)
            {
                IsAwake = true;
            }

            // Don't accumulate a force if the body is sleeping
            if ((Flags & BodyFlags.IsAwake) != 0)
            {
                Torque += torque;
            }
        }

        /// <summary>
        /// Apply an impulse at a point. This immediately modifies the velocity.
        /// It also modifies the angular velocity if the point of application
        /// is not at the center of mass. This wakes up the body.
        /// @param impulse the world impulse vector, usually in N-seconds or kg-m/s.
        /// @param point the world position of the point of application.
        /// @param wake also wake up the body
        /// 在物体指定位置施加线性冲量
        /// </summary>
        /// <param name="impulse"></param>
        /// <param name="point"></param>
        /// <param name="wake"></param>
        public void ApplyLinearImpulse(Vector2 impulse, Vector2 point, bool wake)
        {
            if (_type != BodyType.DynamicBody)
            {
                return;
            }

            if (wake && (Flags & BodyFlags.IsAwake) == 0)
            {
                IsAwake = true;
            }

            // Don't accumulate velocity if the body is sleeping
            if ((Flags & BodyFlags.IsAwake) != 0)
            {
                LinearVelocity += InvMass * impulse;
                AngularVelocity += InverseInertia * MathUtils.Cross(point - Sweep.C, impulse);
            }
        }

        /// <summary>
        /// Apply an impulse to the center of mass. This immediately modifies the velocity.
        /// @param impulse the world impulse vector, usually in N-seconds or kg-m/s.
        /// @param wake also wake up the body
        /// 在质心施加线性冲量
        /// </summary>
        /// <param name="impulse"></param>
        /// <param name="wake"></param>
        public void ApplyLinearImpulseToCenter(Vector2 impulse, bool wake)
        {
            if (_type != BodyType.DynamicBody)
            {
                return;
            }

            if (wake && (Flags & BodyFlags.IsAwake) == 0)
            {
                IsAwake = true;
            }

            // Don't accumulate velocity if the body is sleeping
            if ((Flags & BodyFlags.IsAwake) != 0)
            {
                LinearVelocity += InvMass * impulse;
            }
        }

        /// <summary>
        /// Apply an angular impulse.
        /// @param impulse the angular impulse in units of kg*m*m/s
        /// @param wake also wake up the body
        /// 施加角冲量
        /// </summary>
        /// <param name="impulse"></param>
        /// <param name="wake"></param>
        public void ApplyAngularImpulse(float impulse, bool wake)
        {
            if (_type != BodyType.DynamicBody)
            {
                return;
            }

            if (wake && (Flags & BodyFlags.IsAwake) == 0)
            {
                IsAwake = true;
            }

            // Don't accumulate velocity if the body is sleeping
            if ((Flags & BodyFlags.IsAwake) != 0)
            {
                AngularVelocity += InverseInertia * impulse;
            }
        }

        /// <summary>
        /// Get the mass data of the body.
        /// @return a struct containing the mass, inertia and center of the body.
        /// </summary>
        public void GetMassData(out MassData data)
        {
            data = new MassData
            {
                Mass = _mass,
                RotationInertia = _inertia + (_mass * Vector2.Dot(Sweep.LocalCenter, Sweep.LocalCenter)),
                Center = Sweep.LocalCenter
            };
        }

        /// <summary>
        /// Set the mass properties to override the mass properties of the fixtures.
        /// Note that this changes the center of mass position.
        /// Note that creating or destroying fixtures can also alter the mass.
        /// This function has no effect if the body isn't dynamic.
        /// @param massData the mass properties.
        /// </summary>
        public void SetMassData(MassData massData)
        {
            System.Diagnostics.Debug.Assert(!_world.IsLocked);
            if (_world.IsLocked)
            {
                return;
            }

            if (_type != BodyType.DynamicBody)
            {
                return;
            }

            InvMass = 0.0f;
            _inertia = 0.0f;
            InverseInertia = 0.0f;

            _mass = massData.Mass;
            if (_mass <= 0.0f)
            {
                _mass = 1.0f;
            }

            InvMass = 1.0f / _mass;

            if (massData.RotationInertia > 0.0f && (Flags & BodyFlags.FixedRotation) == 0) // 存在转动惯量且物体可旋转
            {
                _inertia = massData.RotationInertia - (_mass * Vector2.Dot(massData.Center, massData.Center));
                System.Diagnostics.Debug.Assert(_inertia > 0.0f);
                InverseInertia = 1.0f / _inertia;
            }

            // Move center of mass.
            var oldCenter = Sweep.C;
            Sweep.LocalCenter = massData.Center;
            Sweep.C0 = Sweep.C = MathUtils.Mul(Transform, Sweep.LocalCenter);

            // Update center of mass velocity.
            LinearVelocity += MathUtils.Cross(AngularVelocity, Sweep.C - oldCenter);
        }

        /// <summary>
        /// This resets the mass properties to the sum of the mass properties of the fixtures.
        /// This normally does not need to be called unless you called SetMassData to override
        /// the mass and you later want to reset the mass.
        /// 重置质量数据
        /// </summary>
        private void ResetMassData()
        {
            // Compute mass data from shapes. Each shape has its own density.
            // 从所有形状计算质量数据,每个形状都有各自的密度
            _mass = 0.0f;
            InvMass = 0.0f;
            _inertia = 0.0f;
            InverseInertia = 0.0f;
            Sweep.LocalCenter.SetZero();

            // Static and kinematic bodies have zero mass.
            if (_type == BodyType.StaticBody || _type == BodyType.KinematicBody)
            {
                Sweep.C0 = Transform.Position;
                Sweep.C = Transform.Position;
                Sweep.A0 = Sweep.A;
                return;
            }

            System.Diagnostics.Debug.Assert(_type == BodyType.DynamicBody);

            // Accumulate mass over all fixtures.
            var localCenter = Vector2.Zero;
            foreach (var f in Fixtures)
            {
                if (f.Density.Equals(0.0f))
                {
                    continue;
                }

                f.GetMassData(out var massData);
                _mass += massData.Mass;
                localCenter += massData.Mass * massData.Center;
                _inertia += massData.RotationInertia;
            }

            // Compute center of mass.
            if (_mass > 0.0f)
            {
                InvMass = 1.0f / _mass;
                localCenter *= InvMass;
            }

            if (_inertia > 0.0f && (Flags & BodyFlags.FixedRotation) == 0) // 存在转动惯量且物体可旋转
            {
                // Center the inertia about the center of mass.
                _inertia -= _mass * Vector2.Dot(localCenter, localCenter);
                System.Diagnostics.Debug.Assert(_inertia > 0.0f);
                InverseInertia = 1.0f / _inertia;
            }
            else
            {
                _inertia = 0.0f;
                InverseInertia = 0.0f;
            }

            // Move center of mass.
            var oldCenter = Sweep.C;
            Sweep.LocalCenter = localCenter;
            Sweep.C0 = Sweep.C = MathUtils.Mul(Transform, Sweep.LocalCenter);

            // Update center of mass velocity.
            LinearVelocity += MathUtils.Cross(AngularVelocity, Sweep.C - oldCenter);
        }

        /// <summary>
        /// Get the world coordinates of a point given the local coordinates.
        /// @param localPoint a point on the body measured relative the body's origin.
        /// @return the same point expressed in world coordinates.
        /// </summary>
        public Vector2 GetWorldPoint(Vector2 localPoint)
        {
            return MathUtils.Mul(Transform, localPoint);
        }

        /// <summary>
        /// Get the world coordinates of a vector given the local coordinates.
        /// @param localVector a vector fixed in the body.
        /// @return the same vector expressed in world coordinates.
        /// </summary>
        public Vector2 GetWorldVector(Vector2 localVector)
        {
            return MathUtils.Mul(Transform.Rotation, localVector);
        }

        /// <summary>
        /// Gets a local point relative to the body's origin given a world point.
        /// @param a point in world coordinates.
        /// @return the corresponding local point relative to the body's origin.
        /// </summary>
        public Vector2 GetLocalPoint(Vector2 worldPoint)
        {
            return MathUtils.MulT(Transform, worldPoint);
        }

        /// <summary>
        /// Gets a local vector given a world vector.
        /// @param a vector in world coordinates.
        /// @return the corresponding local vector.
        /// </summary>
        public Vector2 GetLocalVector(Vector2 worldVector)
        {
            return MathUtils.MulT(Transform.Rotation, worldVector);
        }

        /// <summary>
        /// Get the world linear velocity of a world point attached to this body.
        /// @param a point in world coordinates.
        /// @return the world velocity of a point.
        /// </summary>
        public Vector2 GetLinearVelocityFromWorldPoint(Vector2 worldPoint)
        {
            return LinearVelocity + MathUtils.Cross(AngularVelocity, worldPoint - Sweep.C);
        }

        /// <summary>
        /// Get the world velocity of a local point.
        /// @param a point in local coordinates.
        /// @return the world velocity of a point.
        /// </summary>
        public Vector2 GetLinearVelocityFromLocalPoint(Vector2 localPoint)
        {
            return GetLinearVelocityFromWorldPoint(GetWorldPoint(localPoint));
        }

        /// <summary>
        /// Dump this body to a log file
        /// </summary>
        public void Dump()
        {
            var bodyIndex = IslandIndex;

            DumpLogger.Log("{");
            DumpLogger.Log("  b2BodyDef bd;");
            DumpLogger.Log($"  bd.type = b2BodyType({_type});");
            DumpLogger.Log($"  bd.position.Set({Transform.Position.X}, {Transform.Position.Y});");
            DumpLogger.Log($"  bd.angle = {Sweep.A};");
            DumpLogger.Log($"  bd.linearVelocity.Set({LinearVelocity.X}, {LinearVelocity.Y});");
            DumpLogger.Log($"  bd.angularVelocity = {AngularVelocity};");
            DumpLogger.Log($"  bd.linearDamping = {LinearDamping};");
            DumpLogger.Log($"  bd.angularDamping = {AngularDamping};");
            DumpLogger.Log($"  bd.allowSleep = bool({(Flags & BodyFlags.AutoSleep) != 0});");
            DumpLogger.Log($"  bd.awake = bool({(Flags & BodyFlags.IsAwake) != 0});");
            DumpLogger.Log($"  bd.fixedRotation = bool({(Flags & BodyFlags.FixedRotation) != 0});");
            DumpLogger.Log($"  bd.bullet = bool({(Flags & BodyFlags.IsBullet) != 0});");
            DumpLogger.Log($"  bd.active = bool({(Flags & BodyFlags.IsEnabled) != 0});");
            DumpLogger.Log($"  bd.gravityScale = {GravityScale};");
            DumpLogger.Log($"  bodies[{IslandIndex}] = m_world.CreateBody(&bd);");
            foreach (var f in Fixtures)
            {
                DumpLogger.Log("  {");
                f.Dump(bodyIndex);
                DumpLogger.Log("  }");
            }

            DumpLogger.Log("}");
        }

        /// <summary>
        /// 同步夹具
        /// </summary>
        internal void SynchronizeFixtures()
        {
            var broadPhase = World.ContactManager.BroadPhase;

            if ((Flags & BodyFlags.IsAwake) != 0)
            {
                var xf1 = new Transform();
                xf1.Rotation.Set(Sweep.A0);
                xf1.Position = Sweep.C0 - MathUtils.Mul(xf1.Rotation, Sweep.LocalCenter);

                for (var index = 0; index < Fixtures.Count; index++)
                {
                    Fixtures[index].Synchronize(broadPhase, xf1, Transform);
                }
            }
            else
            {
                for (var index = 0; index < Fixtures.Count; index++)
                {
                    Fixtures[index].Synchronize(broadPhase, Transform, Transform);
                }
            }

            // var xf1 = new Transform();
            // xf1.Rotation.Set(Sweep.A0);
            // xf1.Position = Sweep.C0 - MathUtils.Mul(xf1.Rotation, Sweep.LocalCenter);
            //
            // var broadPhase = _world.ContactManager.BroadPhase;
            // for (var index = 0; index < Fixtures.Count; index++)
            // {
            //     Fixtures[index].Synchronize(broadPhase, xf1, Transform);
            // }
        }

        /// <summary>
        /// 同步位置
        /// </summary>
        internal void SynchronizeTransform()
        {
            Transform.Rotation.Set(Sweep.A);
            Transform.Position = Sweep.C - MathUtils.Mul(Transform.Rotation, Sweep.LocalCenter);
        }

        /// <summary>
        /// This is used to prevent connected bodies from colliding.
        /// It may lie, depending on the collideConnected flag.
        /// 判断物体之间是否应该检测碰撞
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        internal bool ShouldCollide(Body other)
        {
            // At least one body should be dynamic.
            if (_type != BodyType.DynamicBody && other._type != BodyType.DynamicBody)
            {
                return false;
            }

            // Does a joint prevent collision?
            var node = JointEdges.First;
            while (node != null)
            {
                var joint = node.Value;
                node = node.Next;
                if (joint.Other == other && !joint.Joint.CollideConnected)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 在安全时间段内快进,此时不同步粗检测
        /// </summary>
        /// <param name="alpha"></param>
        internal void Advance(float alpha)
        {
            // Advance to the new safe time. This doesn't sync the broad-phase.
            Sweep.Advance(alpha);
            Sweep.C = Sweep.C0;
            Sweep.A = Sweep.A0;
            Transform.Rotation.Set(Sweep.A);
            Transform.Position = Sweep.C - MathUtils.Mul(Transform.Rotation, Sweep.LocalCenter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFlag(BodyFlags flag)
        {
            Flags |= flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsetFlag(BodyFlags flag)
        {
            Flags &= ~flag;
        }
    }
}