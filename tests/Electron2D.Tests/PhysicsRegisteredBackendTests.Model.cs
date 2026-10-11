using Electron2D;

internal static partial class PhysicsRegisteredBackendTests
{
    private sealed partial class Solver
    {
        internal readonly Dictionary<RID, Body> Bodies = [];
        internal readonly Dictionary<RID, Constraint> Joints = [];
        internal readonly HashSet<(RID From, RID To)> Exceptions = [];
        internal override void SetCollisionExceptions(ReadOnlySpan<(RID From, RID To)> pairs) { Exceptions.Clear(); foreach (var p in pairs) Exceptions.Add(p); }
        internal RID Space;
        internal bool Ready, Finished, FailSolve;
        internal int Steps, Flushes, Synchronizations, Pairs;
        internal RID ContactFailure;
        internal Action? StepHook;
        internal PhysicsAreaFieldConfiguration Fields;
        internal override bool HasFailure => false;
        internal override bool ResultsReady => Ready;
        internal override int PreparedBodyCapacity => Bodies.Count;
        internal override void Initialize() { }
        internal override void SetActive(bool active) { }
        internal override void SetSleepSettings(in PhysicsSleepSettings settings) { }
        internal override void SetContactSettings(in PhysicsContactSettings settings) { }
        internal override void SetSolverIterations(int value) { }
        internal override void SetConstraintDefaultBias(float value) { }
        internal override void PrepareSolverCapacity(int bodies, int shapes, int joints) { }
        internal override void PrepareMonitoringCapacity(int bodies, int shapes) { }
        internal override PhysicsBackendStatistics ReadStatistics()
        {
            var active = 0; foreach (var b in Bodies.Values) if (b.Awake && b.Dynamic) active++;
            return new(active, Pairs, active);
        }
        internal override PhysicsBackendCollider CreateCollider(RID collider, bool area)
        { var b = new Body(this, collider, area); Bodies.Add(collider, b); return b; }
        internal override PhysicsBackendJoint CreateJoint(RID joint) { var created = new Constraint(this, joint); Joints.Add(joint, created); return created; }
        internal override void BeginStep(double delta) => Ready = false;
        internal override void PrepareWorld(double delta, in PhysicsAreaFieldConfiguration fields) => Fields = fields;
        internal override void PrepareBodies(double delta) { }
        internal override void Solve(double delta)
        {
            StepHook?.Invoke();
            Pairs = 0;
            foreach (var b in Bodies.Values) { b.ContactPeer = default; b.Advance((float)delta); }
            foreach (var a in Bodies.Values)
                foreach (var b in Bodies.Values)
                {
                    if (a.RID.GetID() >= b.RID.GetID() || a.IsArea || b.IsArea || a.Shapes == 0 || b.Shapes == 0 ||
                        (a.Mask & b.Layer) == 0 && (b.Mask & a.Layer) == 0 || Exceptions.Contains((a.RID, b.RID)) || Exceptions.Contains((b.RID, a.RID))) continue;
                    var difference = b.Position - a.Position; var distance = difference.Length(); var extent = a.Radius + b.Radius;
                    if (distance > extent || a.InverseMass + b.InverseMass == 0) continue;
                    var normal = distance == 0 ? Vector2.Right : difference / distance; var inverse = a.InverseMass + b.InverseMass;
                    a.Position -= normal * (extent - distance) * (a.InverseMass / inverse);
                    b.Position += normal * (extent - distance) * (b.InverseMass / inverse);
                    var impulse = normal * MathF.Max(0, -(b.Velocity - a.Velocity).Dot(normal) / inverse);
                    a.ApplyImpulse(-impulse, 0); b.ApplyImpulse(impulse, 0);
                    a.ContactPeer = b.RID; a.ContactNormal = -normal; a.ContactImpulse = -impulse;
                    b.ContactPeer = a.RID; b.ContactNormal = normal; b.ContactImpulse = impulse; Pairs++;
                }
            if (FailSolve) throw new InvalidOperationException("Injected independent solver failure.");
            foreach (var j in Joints.Values) j.Solve((float)delta);
            Steps++;
        }
        internal override void SyncResults() => Ready = true;
        internal override void CompleteBody(RID body) { }
        internal override void CollectContacts() { }
        internal override void ScanAreas() { }
        internal override int GetAreaOverlapCount(RID area) => ReadAreaOverlaps(area, Span<PhysicsBackendOverlap>.Empty);

        internal override int ReadAreaOverlaps(RID area, Span<PhysicsBackendOverlap> output)
        {
            var self = Bodies[area]; var count = 0;
            foreach (var other in Bodies.Values)
                if (other != self && other.Shapes > 0 && self.Shapes > 0 && (self.Mask & other.Layer) != 0 &&
                    (!other.IsArea || other.Monitorable) && self.Position.DistanceTo(other.Position) < self.Radius + other.Radius)
                { if (count < output.Length) output[count] = new(other.RID, 0, 0); count++; }
            return count;
        }
        internal override bool AreaContainsPoint(RID area, Vector2 point, uint mask) { var b = Bodies[area]; return (b.Layer & mask) != 0 && point.DistanceTo(b.Position) <= b.Radius; }
        internal override void EndStep(Exception? failure) { }
        internal override void FlushQueries() => Flushes++;
        internal override void EndSync(Exception? failure) => Synchronizations++;
        internal override void Finish() { Finished = true; Bodies.Clear(); Joints.Clear(); }
        internal override void PrepareCheckpoint() { }
        internal override PhysicsBackendCheckpoint CreateCheckpoint() => new History(this);
        internal override void PreparePortableCapture() { }
        internal override void ReadPortableBodies(ReadOnlySpan<RID> colliders, bool capture) { }
        internal override void InvalidateCollisionExceptions(RID collider) { }
        internal override void SynchronizeCollisionExceptions() { }
        internal override float ReadPortableSleepTime(RID collider) => Bodies[collider].SleepTime;
        internal override void ApplyPortableSleep(RID collider, float time, bool canSleep, bool sleeping)
        { var b = Bodies[collider]; b.SleepTime = time; b.Awake = !sleeping; b.CanSleep = canSleep; }
        internal override void BeginPortableRestore() { }
        internal override void CompletePortableRestore(ReadOnlySpan<PhysicsBackendOneWay> pairs, ulong tick) { }
        internal override int GetPortableOneWayCount() => 0;
        internal override int CapturePortableOneWays(Span<PhysicsBackendOneWay> destination) => 0;
    }
    private sealed partial class Body(Solver solver, RID rid, bool area)
    {
        internal RID ContactPeer;
        internal Vector2 ContactNormal, ContactImpulse;
        internal RID RID => rid;
        internal bool IsArea => area;
        internal bool Monitoring, Monitorable = true;
        internal uint Layer, Mask;
        internal ulong Canvas;
        internal Vector2 Position, Velocity, Force, Surface;
        internal float Rotation, Angular, Torque, SurfaceAngular, Mass = 1, Inertia = 1, Radius, SleepTime;
        internal bool Awake = true, CanSleep = true, Locked, Detached, Omit;
        internal PhysicsServer.BodyMode Mode;
        private Vector2 _center;
        private Transform _saved;
        private PhysicsBodyFields _fields;
        internal bool Dynamic => !area && Mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        internal void Advance(float dt)
        {
            if (!Awake || area || Mode == PhysicsServer.BodyMode.Static) return;
            if (Dynamic && !Omit)
            {
                Velocity += (_fields.Gravity + Force / Mass) * dt;
                if (!Locked) Angular += Torque / Inertia * dt;
                Velocity /= 1 + _fields.LinearDamp * dt; Angular /= 1 + _fields.AngularDamp * dt;
            }
            Position += Velocity * dt; if (!Locked) Rotation += Angular * dt;
            Force = default; Torque = 0;
        }
        internal override int ShapeCount => Shapes;
        internal int Shapes;
        internal override void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
        { Position = position; Rotation = rotation; Mode = configuration.Mode; Velocity = configuration.LinearVelocity; Angular = configuration.AngularVelocity; Awake = !configuration.Sleeping; CanSleep = configuration.CanSleep; Locked = configuration.LockRotation; }
        internal override void Detach() { Detached = true; solver.Bodies.Remove(rid); }
        internal override void PrepareDefaultForces(in PhysicsBodyForceConfiguration configuration)
        {
            Omit = configuration.OmitForces;
            if (Omit) { Force = default; Torque = 0; return; }
            Force += configuration.PendingForce; if (!Locked) Torque += configuration.PendingTorque;
            if (configuration.ApplyPersistentForces) { Force += configuration.ConstantForce; if (!Locked) Torque += configuration.ConstantTorque; }
        }
        internal override bool HasMotionMode(PhysicsServer.BodyMode mode) => Mode == mode || mode == PhysicsServer.BodyMode.Rigid && Mode == PhysicsServer.BodyMode.RigidLinear;
        internal override void SetMotionMode(PhysicsServer.BodyMode mode) => Mode = mode;
        internal override void SetContactReporting(bool enabled) { }
        internal override void PrepareContactPolicy() { }
        internal override void PrepareIntegration(in PhysicsBodyIntegrationConfiguration configuration, double delta)
        {
            Locked = configuration.LockRotation; Omit = configuration.OmitForces;
            var f = solver.Fields;
            _fields = new(f.GravityVector * f.Gravity * configuration.GravityScale,
                configuration.LinearDamp + f.LinearDamp, configuration.AngularDamp + f.AngularDamp, true);
        }
        internal override void SetAreaMonitoring(bool monitoring, bool monitorable) { Monitoring = monitoring; Monitorable = monitorable; }
        internal override void SetAreaFields(in PhysicsAreaFieldConfiguration configuration) { }
        internal override PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current) => _fields;
        internal override void RefreshIdentity(ulong objectID, ulong canvasID) => Canvas = canvasID;
        internal override void SetCollisionPriority(float value) { }
        internal override void UpdateFilter(uint layer, uint mask, bool wakeBody) { Layer = layer; Mask = mask; }
        internal override void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce)
        {
            Shapes = 0; Layer = layer; Mask = mask;
            foreach (ref readonly var s in slots)
                if (s.Enabled) { if (s.Shape is not CircleShape circle) throw new NotSupportedException("The independent test solver exercises circles only."); Radius = circle.Radius; Shapes++; }
        }
        internal override (Vector2 Position, float Rotation) GetPose() => (Position, Rotation);
        internal override Transform GetTransform() => new(Rotation, Position);
        internal override Transform SampleJointLocalFrame(Vector2 point, float angle) => GetTransform().AffineInverse() * new Transform(angle, point);
        internal override (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion() => (Velocity, Angular, !Awake);
        internal override Vector2 LinearVelocity => Mode == PhysicsServer.BodyMode.Static ? Surface : Velocity;
        internal override float AngularVelocity => Mode == PhysicsServer.BodyMode.Static ? SurfaceAngular : Angular;
        internal override bool IsAwake => Awake;
        internal override void SetPose(Vector2 position, float rotation) { Position = position; Rotation = rotation; }
        internal override void SetTargetPose(Transform pose, double delta) { Velocity = (pose.Origin - Position) / (float)delta; Angular = (pose.Rotation - Rotation) / (float)delta; }
        internal override void SavePose() => _saved = GetTransform();
        internal override void RestorePose() => SetPose(_saved.Origin, _saved.Rotation);
        internal override void SetLinearVelocity(Vector2 velocity) => Velocity = velocity;
        internal override void SetAngularVelocity(float velocity) => Angular = velocity;
        internal override void ClearVelocity() { Velocity = default; Angular = 0; }
        internal override void SetAwake(bool awake) { Awake = awake; if (!awake) ClearVelocity(); }
        internal override void SetCanSleep(bool canSleep) => CanSleep = canSleep;
        internal override void SetRotationLocked(bool locked) => Locked = locked;
        internal override void SetGravityScale(float scale) { }
        internal override void ApplyCentralForce(Vector2 force, bool wake) { Force += force; Awake |= wake; }
        internal override void ApplyTorque(float torque, bool wake) { Torque += torque; Awake |= wake; }
        internal override bool RotationLocked => Locked;
        internal override Vector2 CenterOfMass => Position + _center.Rotated(Rotation);
        internal override Vector2 CenterOfMassLocal => _center;
        internal override float InverseMass => Dynamic ? 1 / Mass : 0;
        internal override float InverseInertia => Dynamic && !Locked ? 1 / Inertia : 0;
        internal override PhysicsMass.Properties ApplyMassProfile(float mass, float inertia, Vector2? center)
        { Mass = mass; Inertia = inertia > 0 ? inertia : MathF.Max(.001f, mass * Radius * Radius / 2); _center = center ?? default; return new(Mass, Inertia, _center); }
        internal override void WakeTouching() { }
        internal override Vector2 GetPointVelocity(Vector2 offset) => LinearVelocity + new Vector2(-offset.Y, offset.X) * AngularVelocity;
        internal override Vector2 GetWorldPointVelocity(Vector2 point) => GetPointVelocity(point - CenterOfMass);
        internal override void ApplyImpulse(Vector2 impulse, float moment) { Velocity += impulse * InverseMass; Angular += moment * InverseInertia; Awake = true; }
        internal override void SetSurfaceVelocity(Vector2 linear, float angular) { Surface = linear; SurfaceAngular = angular; }
        internal override int ReadContacts(Span<PhysicsBodyContact> contacts)
        {
            if (solver.ContactFailure == rid) throw new InvalidOperationException("Injected contact publication failure.");
            if (contacts.IsEmpty || !ContactPeer.IsValid()) return 0;
            var peer = solver.Bodies[ContactPeer]; var point = Position - ContactNormal * Radius;
            contacts[0] = new(rid, 0, peer.RID, 0, point, point, ContactNormal, GetWorldPointVelocity(point), peer.GetWorldPointVelocity(point), ContactImpulse);
            return 1;
        }
        internal override Transform PortablePose => GetTransform();
        internal override void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping,
            Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized)
        { SetPose(pose.Origin, pose.Rotation); Velocity = linear; Angular = angular; SleepTime = sleepTime; CanSleep = canSleep; Awake = !sleeping; Surface = surface; SurfaceAngular = surfaceAngular; Force = force; Torque = torque; _fields = new(gravity, linearDamp, angularDamp, fieldsInitialized); }
        internal override void ValidatePortablePiece(int slot, int piece) { if (piece != 0 || slot < 0 || slot >= Shapes) throw new InvalidDataException("Invalid independent circle piece."); }
    }
    private sealed class Constraint(Solver solver, RID rid) : PhysicsBackendJoint
    {
        internal PhysicsJointConfiguration Settings;
        private RID _first, _second;
        private bool _attached;
        internal override bool IsAttached => _attached;
        internal override void Attach(RID first, RID second, in PhysicsJointConfiguration settings) { _first = first; _second = second; Settings = settings; _attached = true; }
        internal override void Detach() { _attached = false; solver.Joints.Remove(rid); }
        internal override void ApplySolverPolicy(in PhysicsJointConfiguration settings) => Settings = settings;
        internal override void ApplyPortableFrames(Transform first, Transform second) => Settings = Settings with { FrameA = first, FrameB = second };
        internal override void SetCollideConnected(bool value) => Settings = Settings with { DisableCollision = !value };
        internal override void SetPinLimits(bool enabled, float lower, float upper) => Settings = Settings with { PinLimitEnabled = enabled, PinLimitLower = lower, PinLimitUpper = upper };
        internal override void SetPinMotorEnabled(bool value) => Settings = Settings with { PinMotorEnabled = value };
        internal override void SetPinMotorVelocity(float value) => Settings = Settings with { PinMotorVelocity = value };
        internal override void SetPinMotorMaxTorque(float value) => Settings = Settings with { PinMotorMaxTorque = value };
        internal override void SetGrooveLimits(float lower, float upper) => Settings = Settings with { LowerTranslation = lower, UpperTranslation = upper };
        internal void Solve(float dt)
        {
            if (Settings.Type != PhysicsServer.JointType.DampedSpring) return;
            var a = solver.Bodies[_first]; var b = solver.Bodies[_second];
            var separation = b.GetTransform() * Settings.FrameB.Origin - a.GetTransform() * Settings.FrameA.Origin;
            var length = separation.Length(); if (length == 0) return;
            var direction = separation / length;
            var force = (length - Settings.EffectiveSpringRestLength) * Settings.SpringStiffness + (b.Velocity - a.Velocity).Dot(direction) * Settings.SpringDamping;
            var impulse = direction * Mathf.Clamp(force, -Settings.MaxForce, Settings.MaxForce) * dt;
            a.ApplyImpulse(impulse, 0); b.ApplyImpulse(-impulse, 0);
        }
    }
    private sealed class History(Solver solver) : PhysicsBackendCheckpoint
    {
        private readonly (Body Body, Vector2 Position, Vector2 Velocity, float Angular, float Rotation)[] _states = new (Body, Vector2, Vector2, float, float)[solver.Bodies.Count];
        internal override long DeviceCapacityBytes => 0;
        internal override void Capture()
        {
            var i = 0; foreach (var b in solver.Bodies.Values) _states[i++] = (b, b.Position, b.Velocity, b.Angular, b.Rotation);
        }
        internal override void ValidateRestore() { foreach (var s in _states) if (s.Body.Detached) throw new InvalidOperationException("Independent history attachment expired."); }
        internal override void Restore() { foreach (var s in _states) { s.Body.Position = s.Position; s.Body.Velocity = s.Velocity; s.Body.Angular = s.Angular; s.Body.Rotation = s.Rotation; } }
        public override void Dispose() { }
    }
}
