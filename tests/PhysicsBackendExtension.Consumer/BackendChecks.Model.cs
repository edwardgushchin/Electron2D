using Electron2D;

public static partial class BackendChecks
{
    private sealed partial class Solver
    {
        internal readonly Dictionary<RID, Body> Bodies = [];
        internal readonly Dictionary<RID, Constraint> Joints = [];
        internal readonly HashSet<(RID From, RID To)> Exceptions = [];
        protected override void SetCollisionExceptions(ReadOnlySpan<(RID From, RID To)> pairs) { Exceptions.Clear(); foreach (var p in pairs) Exceptions.Add(p); }
        internal RID Space => SpaceRID;
        internal RID[] ReadExceptions(RID body) => GetCollisionExceptions(body);
        internal bool Ready, Finished, FailSolve, FailInit, FailFinish, FailDisposal;
        internal int Steps, Flushes, Synchronizations, Pairs, DebugReads, StatePreparations;
        internal bool BadDebugCount, BadDebugPoint, NegativeDebugCount, FailDebugPreparation;
        internal int DebugLimit, DebugSets, DebugPointCount, DebugLastCount;
        internal Vector2[] DebugPoints = [];
        internal Action<int>? DebugPolicyHook;
        internal Action? DebugReadHook;
        protected override void SetDebugContacts(int maximum)
        {
            DebugPolicyHook?.Invoke(maximum);
            if (maximum > 0 && FailDebugPreparation) throw new ApplicationException("Injected diagnostic preparation failure.");
            if (DebugPoints.Length < maximum) DebugPoints = new Vector2[maximum];
            DebugLimit = maximum; DebugPointCount = 0; DebugLastCount = 0; DebugSets++;
        }
        private void AddDebugPoint(Vector2 point)
        {
            if (DebugPointCount < DebugLimit) DebugPoints[DebugPointCount++] = point;
        }
        internal RID ContactFailure;
        internal Action<RID>? IdentityHook;
        internal Action<RID>? ShapeHook, FilterHook;
        internal Action<RID>? BodyHook;
        internal Action<RID>? JointHook;
        internal Action<RID>? JointFactoryHook, JointReleaseHook;
        internal bool FailJointFactory, NullJointFactory, FailJointAttach, FailJointDetach;
        internal PhysicsBackendJoint? ReusedJoint;
        internal Action<RID>? BodyReleaseHook;
        internal Action? InitHook, FinishHook;
        internal Func<RID, PhysicsDirectSpaceState>? SpaceViewFactory;
        internal Func<RID, PhysicsDirectBodyState>? BodyViewFactory;
        internal int SpaceViewCreations, BodyViewCreations;
        internal float Delta;
        protected override PhysicsDirectSpaceState CreateDirectSpaceState(RID space)
        { SpaceViewCreations++; return SpaceViewFactory is { } factory ? factory(space) : base.CreateDirectSpaceState(space); }
        protected override PhysicsDirectBodyState CreateDirectBodyState(RID body)
        { BodyViewCreations++; return BodyViewFactory is { } factory ? factory(body) : base.CreateDirectBodyState(body); }
        internal Action<LifecyclePhase>? LifecycleHook;
        internal Exception? EndFailure;
        internal PhysicsBackendStatistics? StatisticsOverride;
        internal bool FailBodyDetach;
        internal RID MotionFailure, ShapeFailure;
        internal Action<RID>? PolicyHook;
        internal Action? PriorityHook;
        internal Action? StepHook, StatusHook;
        internal Action? FlushHook, SyncHook;
        internal Action<RID>? MotionHook;
        internal Action? WorldPolicyHook;
        internal Action? CapacityHook;
        internal int WorldPolicyCalls;
        internal PhysicsAreaFieldConfiguration Fields;
        protected override bool HasFailure { get { StatusHook?.Invoke(); return false; } }
        protected override bool ResultsReady => Ready;
        protected override int PreparedBodyCapacity => Bodies.Count;
        protected override void Initialize() { LifecycleHook?.Invoke(LifecyclePhase.Initialize); InitHook?.Invoke(); if (FailInit) throw new InvalidOperationException("Initialization failure"); }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && FailDisposal) throw new InvalidOperationException("Disposal failure");
        }
        protected override void SetActive(bool active) { LifecycleHook?.Invoke(LifecyclePhase.Active); WorldPolicy(); }
        private PhysicsSleepSettings _sleepSettings;
        protected override void SetSleepSettings(in PhysicsSleepSettings settings)
        {
            WorldPolicy(); _sleepSettings = settings;
            foreach (var body in Bodies.Values) if (!body.IsArea && body.Mode != PhysicsServer.BodyMode.Static) { body.Awake = true; body.SleepTime = 0; }
        }
        protected override void SetContactSettings(in PhysicsContactSettings settings) => WorldPolicy();
        internal int Iterations = 16;
        internal float ConstraintBias = .2f;
        protected override void SetSolverIterations(int value) { WorldPolicy(); Iterations = value; }
        protected override void SetConstraintDefaultBias(float value) { WorldPolicy(); ConstraintBias = value; }
        private void WorldPolicy() { WorldPolicyCalls++; WorldPolicyHook?.Invoke(); }
        protected override void PrepareSolverCapacity(int bodies, int shapes, int joints) => CapacityHook?.Invoke();
        protected override void PrepareMonitoringCapacity(int bodies, int shapes) { }
        protected override PhysicsBackendStatistics ReadStatistics()
        {
            LifecycleHook?.Invoke(LifecyclePhase.Statistics);
            if (StatisticsOverride is { } value) return value;
            var active = 0; foreach (var b in Bodies.Values) if (b.Awake && b.Dynamic) active++;
            return new(active, Pairs, active);
        }
        protected override PhysicsBackendCollider CreateCollider(RID collider, bool area)
        { var b = new Body(this, collider, area); Bodies.Add(collider, b); return b; }
        protected override PhysicsBackendJoint CreateJoint(RID joint)
        {
            if (FailJointFactory) throw new InvalidOperationException("Joint factory failure");
            if (NullJointFactory) return null!;
            if (ReusedJoint is { } reused) return reused;
            var created = new Constraint(this, joint); Joints.Add(joint, created); JointFactoryHook?.Invoke(joint); return created;
        }
        protected override void BeginStep(double delta) { LifecycleHook?.Invoke(LifecyclePhase.Begin); Ready = false; }
        protected override void PrepareWorld(double delta, in PhysicsAreaFieldConfiguration fields) { Fields = fields; PrepareFieldAreas(); }
        protected override void PrepareBodies(double delta) { }
        protected override void Solve(double delta)
        {
            LifecycleHook?.Invoke(LifecyclePhase.Solve); StepHook?.Invoke();
            Pairs = 0; DebugPointCount = 0; Delta = (float)delta;
            _framePairs.Clear();
            foreach (var b in Bodies.Values)
            {
                if (!b.IsArea) b.ReportedLimit = PhysicsServer.BodyGetMaxContactsReported(b.RID);
                if (!b.IsArea) b.ReportedDepth = PhysicsServer.BodyGetContactsReportedDepthThreshold(b.RID);
                b.Contacts.Clear();
            }
            if (HasContinuousBodies()) SolveContinuous((float)delta);
            else
            {
                foreach (var b in Bodies.Values) b.Advance((float)delta);
                SolveContacts(); SolveJoints((float)delta, true); UpdateSleep((float)delta);
            }
            Steps++;
        }
        private readonly HashSet<(RID, RID)> _framePairs = [];
        private void SolveContacts()
        {
            foreach (var a in Bodies.Values)
                foreach (var b in Bodies.Values)
                {
                    if (a.RID.GetID() >= b.RID.GetID() || a.IsArea || b.IsArea || a.Shapes == 0 || b.Shapes == 0 ||
                        (a.Mask & b.Layer) == 0 && (b.Mask & a.Layer) == 0 || Exceptions.Contains((a.RID, b.RID)) || Exceptions.Contains((b.RID, a.RID))) continue;
                    if (!AllowsContact(a.RID, b.RID) || a.Mode == PhysicsServer.BodyMode.Static && b.Mode == PhysicsServer.BodyMode.Static ||
                        a.InvMass + b.InvMass == 0 && a.ReportedLimit == 0 && b.ReportedLimit == 0 ||
                        !Contact(a, b, out var normal, out var depth, out var diagnosticPoint)) continue;
                    if (depth > 0 && (a.Awake || b.Awake))
                    {
                        var half = normal * (depth * .5f);
                        AddDebugPoint(diagnosticPoint - half); AddDebugPoint(diagnosticPoint + half);
                    }
                    var inverse = a.InvMass + b.InvMass;
                    var active = a.Dynamic && a.Awake || b.Dynamic && b.Awake ||
                        a.Mode == PhysicsServer.BodyMode.Kinematic && (a.Velocity != Vector2.Zero || a.Angular != 0) ||
                        b.Mode == PhysicsServer.BodyMode.Kinematic && (b.Velocity != Vector2.Zero || b.Angular != 0);
                    var impulse = Vector2.Zero;
                    if (active && inverse > 0)
                    {
                        a.Position -= normal * depth * (a.InvMass / inverse);
                        b.Position += normal * depth * (b.InvMass / inverse);
                        Contact(a, b, out _, out _, out var solvedPoint);
                        impulse = SolveContact(a, b, normal, solvedPoint);
                    }
                    Contact(a, b, out _, out _, out var contactPoint);
                    a.RecordContact(b, contactPoint, -normal, -impulse, depth);
                    b.RecordContact(a, contactPoint, normal, impulse, depth);
                    if (_framePairs.Add((a.RID, b.RID))) Pairs++;
                }
            if (FailSolve) throw new InvalidOperationException("Injected independent solver failure.");
        }
        private void SolveJoints(float delta, bool begin)
        {
            if (begin) foreach (var j in Joints.Values) j.BeginSolve();
            for (var i = 0; i < Iterations; i++)
                foreach (var j in Joints.Values) j.Solve(delta, begin && i == 0);
        }
        protected override void SyncResults()
        { foreach (var body in Bodies.Values) body.FreezeContacts(); Ready = true; LifecycleHook?.Invoke(LifecyclePhase.Sync); }
        protected override void CompleteBody(RID body) { }
        protected override void CollectContacts() { }
        protected override void ScanAreas() { }
        protected override int GetAreaOverlapCount(RID area) => ReadAreaOverlaps(area, Span<PhysicsBackendOverlap>.Empty);

        protected override int ReadAreaOverlaps(RID area, Span<PhysicsBackendOverlap> output)
        {
            var self = Bodies[area]; var count = 0;
            foreach (var other in Bodies.Values)
                if (other != self && other.Shapes > 0 && self.Shapes > 0 && (self.Mask & other.Layer) != 0 &&
                    (!other.IsArea || other.Monitorable) && Overlap(self, other))
                { if (count < output.Length) output[count] = new(other.RID, 0, 0); count++; }
            return count;
        }
        protected override bool AreaContainsPoint(RID area, Vector2 point, uint mask) { var b = Bodies[area]; return (b.Layer & mask) != 0 && Annulus.Contains(point, b.Position, b.Hole, b.Radius); }
        private static bool Overlap(Body a, Body b)
        {
            var d = a.Position.DistanceTo(b.Position);
            return d <= a.Radius + b.Radius && d + b.Radius >= a.Hole && d + a.Radius >= b.Hole;
        }
        private static bool Contact(Body a, Body b, out Vector2 normal, out float depth, out Vector2 point)
        {
            if (a.Hole > 0 && b.Hole > 0) throw new NotSupportedException("The fixture exercises custom annulus/circle contacts.");
            if (b.Hole > 0) { var hit = Annulus.Contact(b.Position, b.Hole, b.Radius, a.Position, a.Radius, out normal, out depth, out point); normal = -normal; return hit; }
            return Annulus.Contact(a.Position, a.Hole, a.Radius, b.Position, b.Radius, out normal, out depth, out point);
        }
        protected override void EndStep(Exception? failure) { }
        protected override void FlushQueries() { LifecycleHook?.Invoke(LifecyclePhase.Flush); Flushes++; FlushHook?.Invoke(); }
        protected override void EndSync(Exception? failure) { EndFailure = failure; LifecycleHook?.Invoke(LifecyclePhase.End); Synchronizations++; SyncHook?.Invoke(); }
        protected override void Finish() { LifecycleHook?.Invoke(LifecyclePhase.Finish); FinishHook?.Invoke(); Finished = true; Bodies.Clear(); Joints.Clear(); if (FailFinish) throw new InvalidOperationException("Finish failure"); }
        protected override void PrepareCheckpoint() { }
        protected override PhysicsBackendCheckpoint CreateCheckpoint() => new History(this);
        protected override void PreparePortableCapture() { }
        protected override void ReadPortableBodies(ReadOnlySpan<RID> colliders, bool capture) { }
        protected override void InvalidateCollisionExceptions(RID collider) { }
        protected override void SynchronizeCollisionExceptions() { }
        protected override float ReadPortableSleepTime(RID collider) => Bodies[collider].SleepTime;
        protected override void ApplyPortableSleep(RID collider, float time, bool canSleep, bool sleeping)
        { var b = Bodies[collider]; b.SleepTime = time; b.Awake = !sleeping; b.CanSleep = canSleep; }
        protected override void BeginPortableRestore() { }
        private bool AllowsContact(RID a, RID b)
        {
            foreach (var pair in OneWays)
                if (pair.A == a && pair.B == b || pair.A == b && pair.B == a) return pair.Allowed;
            return true;
        }
        internal readonly List<PhysicsBackendOneWay> OneWays = [];
        internal int OneWayCapacity = -1, OneWayReturn = -1, OneWaySpanLength;
        protected override void CompletePortableRestore(ReadOnlySpan<PhysicsBackendOneWay> pairs, ulong tick)
        { OneWays.Clear(); OneWays.EnsureCapacity(pairs.Length); foreach (var pair in pairs) OneWays.Add(pair); }

        protected override int GetPortableOneWayCount() => OneWayCapacity < 0 ? OneWays.Count : OneWayCapacity;
        protected override int CapturePortableOneWays(Span<PhysicsBackendOneWay> destination)
        {
            OneWaySpanLength = destination.Length;
            for (var i = 0; i < Math.Min(destination.Length, OneWays.Count); i++) destination[i] = OneWays[i];
            return OneWayReturn < 0 ? OneWays.Count : OneWayReturn;
        }
    }
    private sealed partial class Body(Solver solver, RID rid, bool area)
    {
        internal float Hole;
        internal RID RID => rid;
        internal float InvMass => InverseMass;
        internal float InvInertia => InverseInertia;
        internal Vector2 Center => CenterOfMass;
        internal Vector2 LocalCenter => CenterOfMassLocal;
        internal PhysicsBodyFields Fields => _fields;
        internal int ReportedLimit;
        internal Transform Pose => GetTransform();
        internal void AddImpulse(Vector2 impulse, float moment)
        {
            Velocity += impulse * InverseMass; Angular += moment * InverseInertia;
            if (Dynamic && (impulse != Vector2.Zero || moment != 0) && !Awake) { Awake = true; SleepTime = 0; }
        }
        internal Vector2 PointVelocity(Vector2 point) => GetWorldPointVelocity(point);
        internal bool IsArea => area;
        internal bool Monitoring, Monitorable = true;
        internal uint Layer, Mask;
        internal PhysicsColliderShape LastShape;
        internal ulong Canvas, ObjectID;
        internal float Priority;
        internal Vector2 Position, Velocity, Force, Surface;
        internal float Rotation, Angular, Torque, SurfaceAngular, Mass = 1, Inertia = 1, Radius, SleepTime;
        internal float Friction = 1, Bounce;
        internal CCDMode ContinuousMode;
        internal bool Awake = true, CanSleep = true, Locked, Detached, Omit;
        internal bool SleepVisited;
        internal PhysicsServer.BodyMode Mode;
        private Vector2 _center;
        private Transform _saved;
        private PhysicsBodyFields _fields;
        internal PhysicsAreaFieldConfiguration AreaFields;
        internal bool Dynamic => !area && Mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
        internal void Advance(float dt) { if (PrepareAdvance(dt)) Move(dt); }
        internal bool PrepareAdvance(float dt)
        {
            if (!Awake || area || Mode == PhysicsServer.BodyMode.Static) return false;
            if (Dynamic && !Omit)
            {
                Velocity += (_fields.Gravity + Force / Mass) * dt;
                if (!Locked) Angular += Torque / Inertia * dt;
                Velocity /= 1 + _fields.LinearDamp * dt; Angular /= 1 + _fields.AngularDamp * dt;
            }
            Force = default; Torque = 0; return true;
        }
        internal void Move(float dt)
        {
            if (!Awake || area || Mode == PhysicsServer.BodyMode.Static) return;
            Position += Velocity * dt; if (!Locked) Rotation += Angular * dt;
        }
        internal bool FrameForcesApplied;
        protected override int ShapeCount => Shapes;
        internal int Shapes;
        protected override void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
        { Position = position; Rotation = rotation; Mode = configuration.Mode; Velocity = configuration.LinearVelocity; Angular = configuration.AngularVelocity; Awake = !configuration.Sleeping; CanSleep = configuration.CanSleep; Locked = configuration.LockRotation; }
        protected override void Detach()
        {
            try { solver.BodyReleaseHook?.Invoke(rid); if (solver.FailBodyDetach) throw new InvalidOperationException("Collider detach failure"); }
            finally { Detached = true; solver.Bodies.Remove(rid); }
        }
        protected override void PrepareDefaultForces(in PhysicsBodyForceConfiguration configuration)
        {
            solver.StatePreparations++;
            Omit = configuration.OmitForces;
            if (Omit) { Force = default; Torque = 0; return; }
            Force += configuration.PendingForce; if (!Locked) Torque += configuration.PendingTorque;
            if (configuration.ApplyPersistentForces) { Force += configuration.ConstantForce; if (!Locked) Torque += configuration.ConstantTorque; }
        }
        protected override bool HasMotionMode(PhysicsServer.BodyMode mode) => Mode == mode || mode == PhysicsServer.BodyMode.Rigid && Mode == PhysicsServer.BodyMode.RigidLinear;
        protected override void SetMotionMode(PhysicsServer.BodyMode mode) => Mode = mode;
        protected override void SetContactReporting(bool enabled) { }
        protected override void PrepareContactPolicy() => solver.PolicyHook?.Invoke(rid);
        protected override void PrepareIntegration(in PhysicsBodyIntegrationConfiguration configuration, double delta)
        {
            ContinuousMode = configuration.CCDMode;
            Locked = configuration.LockRotation; Omit = configuration.OmitForces;
            var f = solver.ResolveFields(this);
            _fields = new(f.Gravity * configuration.GravityScale,
                configuration.LinearDamp + (configuration.LinearDampMode == RigidBody.DampMode.Combine ? f.LinearDamp : 0),
                configuration.AngularDamp + (configuration.AngularDampMode == RigidBody.DampMode.Combine ? f.AngularDamp : 0), true);
        }
        protected override void SetAreaMonitoring(bool monitoring, bool monitorable) { Monitoring = monitoring; Monitorable = monitorable; }
        protected override void SetAreaFields(in PhysicsAreaFieldConfiguration configuration) => AreaFields = configuration;
        protected override PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current) => _fields;
        protected override void RefreshIdentity(ulong objectID, ulong canvasID)
        { solver.IdentityHook?.Invoke(rid); ObjectID = objectID; Canvas = canvasID; }
        protected override void SetCollisionPriority(float value)
        { solver.PriorityHook?.Invoke(); Priority = value; }
        protected override void UpdateFilter(uint layer, uint mask, bool wakeBody) { solver.FilterHook?.Invoke(rid); Layer = layer; Mask = mask; }
        protected override void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce)
        {
            solver.ShapeHook?.Invoke(rid);
            if (solver.ShapeFailure == rid) throw new InvalidOperationException("Shape retirement failure");
            Shapes = 0; Layer = layer; Mask = mask;
            Friction = friction; Bounce = bounce;
            LastShape = slots.IsEmpty ? default : slots[^1];
            foreach (ref readonly var s in slots)
                if (s.Enabled) { var dimensions = Annulus.Dimensions(s.Shape.GetGeometryData()); Radius = dimensions.Outer; Hole = dimensions.Inner; Shapes++; }
        }
        protected override (Vector2 Position, float Rotation) GetPose() => (Position, Rotation);
        protected override Transform GetTransform()
        {
            if (solver.MotionFailure == rid) throw new InvalidOperationException("Motion capture failure");
            return new(Rotation, Position);
        }
        protected override Transform SampleJointLocalFrame(Vector2 point, float angle) => GetTransform().AffineInverse() * new Transform(angle, point);
        protected override (Vector2 LinearVelocity, float AngularVelocity, bool Sleeping) GetSolverMotion() => (Velocity, Angular, !Awake);
        protected override Vector2 LinearVelocity => Mode == PhysicsServer.BodyMode.Static ? Surface : Velocity + (Mode == PhysicsServer.BodyMode.Kinematic ? Surface : Vector2.Zero);
        protected override float AngularVelocity => Mode == PhysicsServer.BodyMode.Static ? SurfaceAngular : Angular + (Mode == PhysicsServer.BodyMode.Kinematic ? SurfaceAngular : 0);
        protected override bool IsAwake => Awake;
        protected override void SetPose(Vector2 position, float rotation) { Position = position; Rotation = rotation; }
        protected override void SetTargetPose(Transform pose, double delta) { Velocity = (pose.Origin - Position) / (float)delta; Angular = (pose.Rotation - Rotation) / (float)delta; }
        protected override void SavePose() => _saved = GetTransform();
        protected override void RestorePose() => SetPose(_saved.Origin, _saved.Rotation);
        protected override void SetLinearVelocity(Vector2 velocity) { solver.BodyHook?.Invoke(rid); Velocity = velocity; }
        protected override void SetAngularVelocity(float velocity) => Angular = velocity;
        protected override void ClearVelocity() { Velocity = default; Angular = 0; }
        protected override void SetAwake(bool awake) { Awake = awake; SleepTime = 0; if (!awake) ClearVelocity(); else solver.WakeConnected(this); }
        protected override void SetCanSleep(bool canSleep) { CanSleep = canSleep; if (!canSleep) { Awake = true; SleepTime = 0; solver.WakeConnected(this); } }
        protected override void SetRotationLocked(bool locked) => Locked = locked;
        protected override void SetGravityScale(float scale) { }
        protected override void ApplyCentralForce(Vector2 force, bool wake) { Force += force; Awake |= wake; }
        protected override void ApplyTorque(float torque, bool wake) { Torque += torque; Awake |= wake; }
        protected override bool RotationLocked => Locked;
        protected override Vector2 CenterOfMass => Position + _center.Rotated(Rotation);
        protected override Vector2 CenterOfMassLocal => _center;
        protected override float InverseMass => Dynamic ? 1 / Mass : 0;
        protected override float InverseInertia => Dynamic && !Locked ? 1 / Inertia : 0;
        protected override PhysicsMassProperties ApplyMassProfile(float mass, float inertia, Vector2? center)
        { Mass = mass; Inertia = inertia > 0 ? inertia : MathF.Max(.001f, mass * (Radius * Radius + Hole * Hole) / 2); _center = center ?? default; return new(Mass, Inertia, _center); }
        protected override void WakeTouching() => solver.WakeConnected(this);
        protected override Vector2 GetPointVelocity(Vector2 offset) => LinearVelocity + new Vector2(-offset.Y, offset.X) * AngularVelocity;
        protected override Vector2 GetWorldPointVelocity(Vector2 point) => GetPointVelocity(point - CenterOfMass);
        protected override void ApplyImpulse(Vector2 impulse, float moment) { AddImpulse(impulse, moment); if (Dynamic) { Awake = true; SleepTime = 0; solver.WakeConnected(this); } }
        protected override void SetSurfaceVelocity(Vector2 linear, float angular)
        { if (Surface == linear && SurfaceAngular == angular) return; Surface = linear; SurfaceAngular = angular; solver.WakeConnected(this); }
        protected override int ReadContacts(Span<PhysicsBodyContact> contacts)
        {
            if (solver.ContactFailure == rid) throw new InvalidOperationException("Injected contact publication failure.");
            return CopyContacts(contacts);
        }
        protected override Transform PortablePose => GetTransform();
        protected override void ApplyPortableMotion(Transform pose, Vector2 linear, float angular, float sleepTime, bool canSleep, bool sleeping,
            Vector2 surface, float surfaceAngular, Vector2 force, float torque, Vector2 gravity, float linearDamp, float angularDamp, bool fieldsInitialized)
        { SetPose(pose.Origin, pose.Rotation); Velocity = linear; Angular = angular; SleepTime = sleepTime; CanSleep = canSleep; Awake = !sleeping; Surface = surface; SurfaceAngular = surfaceAngular; Force = force; Torque = torque; _fields = new(gravity, linearDamp, angularDamp, fieldsInitialized); }
        protected override void ValidatePortablePiece(int slot, int piece) { if (piece != 0 || slot < 0 || slot >= Shapes) throw new InvalidDataException("Invalid independent circle piece."); }
    }
    private sealed partial class Constraint(Solver solver, RID rid) : PhysicsBackendJoint
    {
        internal PhysicsJointConfiguration Settings;
        private RID _first, _second;
        private bool _attached;
        protected override bool IsAttached => _attached;
        protected override void Attach(RID first, RID second, in PhysicsJointConfiguration settings)
        {
            _first = first; _second = second; Settings = settings; _attached = true; solver.JointHook?.Invoke(rid);
            if (solver.FailJointAttach) throw new InvalidOperationException("Joint attachment failure");
        }
        protected override void Detach()
        {
            try { solver.JointReleaseHook?.Invoke(rid); if (solver.FailJointDetach) throw new InvalidOperationException("Joint detach failure"); }
            finally { _attached = false; solver.Joints.Remove(rid); }
        }
        protected override void ApplySolverPolicy(in PhysicsJointConfiguration settings) { Settings = settings; solver.JointHook?.Invoke(rid); }
        protected override void ApplyPortableFrames(Transform first, Transform second) => Settings = Settings with { FrameA = first, FrameB = second };
        protected override void SetCollideConnected(bool value) { Settings = Settings with { DisableCollision = !value }; solver.JointHook?.Invoke(rid); }
        protected override void SetPinLimits(bool enabled, float lower, float upper) { Settings = Settings with { PinLimitEnabled = enabled, PinLimitLower = lower, PinLimitUpper = upper }; solver.JointHook?.Invoke(rid); }
        protected override void SetPinMotorEnabled(bool value) { Settings = Settings with { PinMotorEnabled = value }; solver.JointHook?.Invoke(rid); }
        protected override void SetPinMotorVelocity(float value) { Settings = Settings with { PinMotorVelocity = value }; solver.JointHook?.Invoke(rid); }
        protected override void SetPinMotorMaxTorque(float value) { Settings = Settings with { PinMotorMaxTorque = value }; solver.JointHook?.Invoke(rid); }
        protected override void SetGrooveLimits(float lower, float upper) { Settings = Settings with { LowerTranslation = lower, UpperTranslation = upper }; solver.JointHook?.Invoke(rid); }
        private void SolveSpring(float dt)
        {
            if (Settings.Type != PhysicsServer.JointType.DampedSpring) return;
            var a = solver.Bodies[_first]; var b = solver.Bodies[_second];
            var separation = b.Pose * Settings.FrameB.Origin - a.Pose * Settings.FrameA.Origin;
            var length = separation.Length(); if (length == 0) return;
            var direction = separation / length;
            var force = (length - Settings.EffectiveSpringRestLength) * Settings.SpringStiffness + (b.Velocity - a.Velocity).Dot(direction) * Settings.SpringDamping;
            var impulse = direction * Mathf.Clamp(force, -Settings.MaxForce, Settings.MaxForce) * dt;
            a.AddImpulse(impulse, 0); b.AddImpulse(-impulse, 0);
        }
    }
}
