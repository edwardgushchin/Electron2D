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
        internal bool Ready, Finished, FailSolve, FailInit, FailFinish, FailDisposal, ReportedFailure;
        internal Action? DisposalHook;
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
        internal Action<RID>? IdentityHook, ColliderFactoryHook, ModeHook, RotationHook, SurfaceHook, PoseHook, AngularHook, AwakeHook, CanSleepHook, MassHook;
        internal Action<RID>? ShapeHook, FilterHook;
        internal Action<RID, float>? GravityHook;
        internal bool UsePublishedGravityScale;
        internal Action<RID>? AreaFieldsHook;
        internal Action? DefaultFieldsHook;
        internal Action<RID, uint, uint, bool>? FilterPolicyHook;
        internal Action<RID>? BodyHook;
        internal Action<RID>? JointHook;
        internal Action<RID>? JointCollisionHook;
        internal Action<RID>? JointParameterHook;
        internal Action<RID>? ExceptionWakeHook;
        internal Action<RID>? JointFactoryHook, JointReleaseHook;
        internal bool FailJointFactory, NullJointFactory, FailJointAttach, FailJointDetach;
        internal PhysicsBackendJoint? ReusedJoint;
        internal bool NullColliderFactory;
        internal PhysicsBackendCollider? ReusedCollider;
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
        internal Action<RID>? CompleteBodyHook;
        internal Action? ContactsHook, AreasHook;
        internal Action<Exception?>? EndStepHook;
        internal Exception? EndFailure;
        internal PhysicsBackendStatistics? StatisticsOverride;
        internal bool FailBodyDetach;
        internal RID MotionFailure, ShapeFailure;
        internal Action<RID>? PolicyHook;
        internal Action? PriorityHook;
        internal Action? StepHook, StatusHook;
        internal Action? FlushHook, SyncHook;
        internal Action<RID>? MotionHook;
        internal Action? WorldPolicyHook, WorldPolicyPublishedHook;
        internal Action? CapacityHook;
        internal Action? MonitorCapacityHook;
        internal Action<RID>? AreaMonitoringHook;
        internal Func<RID, int?>? AreaCountOverride;
        internal delegate int? AreaReader(RID area, Span<PhysicsBackendOverlap> output);
        internal AreaReader? AreaReadOverride;
        internal int WorldPolicyCalls;
        internal PhysicsAreaFieldConfiguration Fields;
        protected override bool HasFailure { get { StatusHook?.Invoke(); return ReportedFailure; } }
        protected override bool ResultsReady => Ready;
        protected override int PreparedBodyCapacity => Bodies.Count;
        protected override void Initialize()
        {
            LifecycleHook?.Invoke(LifecyclePhase.Initialize); InitHook?.Invoke();
            if (FailInit) throw new InvalidOperationException("Initialization failure");
            _sleepSettings = new(PhysicsServer.SpaceGetBodyLinearVelocitySleepThreshold(SpaceRID),
                PhysicsServer.SpaceGetBodyAngularVelocitySleepThreshold(SpaceRID), PhysicsServer.SpaceGetBodyTimeToSleep(SpaceRID));
            Iterations = PhysicsServer.SpaceGetSolverIterations(SpaceRID); ConstraintBias = PhysicsServer.SpaceGetConstraintDefaultBias(SpaceRID);
            _contactSettings = new(PhysicsServer.SpaceGetContactDefaultBias(SpaceRID), PhysicsServer.SpaceGetContactMaxAllowedPenetration(SpaceRID),
                PhysicsServer.SpaceGetContactRecycleRadius(SpaceRID), PhysicsServer.SpaceGetContactMaxSeparation(SpaceRID));
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) DisposalHook?.Invoke();
            if (disposing && FailDisposal) throw new InvalidOperationException("Disposal failure");
        }
        internal bool SelectedActive;
        internal Action? ActivePublishedHook;
        protected override void SetActive(bool active)
        { LifecycleHook?.Invoke(LifecyclePhase.Active); WorldPolicy(); SelectedActive = active; ActivePublishedHook?.Invoke(); }
        private PhysicsSleepSettings _sleepSettings;
        protected override void SetSleepSettings(in PhysicsSleepSettings settings)
        {
            WorldPolicy(); _sleepSettings = settings;
            foreach (var body in Bodies.Values) if (!body.IsArea && body.Mode != PhysicsServer.BodyMode.Static) { body.Awake = true; body.SleepTime = 0; }
            WorldPolicyPublishedHook?.Invoke();
        }
        private PhysicsContactSettings _contactSettings;
        protected override void SetContactSettings(in PhysicsContactSettings settings)
        { WorldPolicy(); if (_contactSettings != settings) { _contactSettings = settings; WakeDynamics(); } WorldPolicyPublishedHook?.Invoke(); }
        private void WakeDynamics() { foreach (var body in Bodies.Values) if (body.Dynamic) { body.Awake = true; body.SleepTime = 0; } }
        internal int Iterations = 16;
        internal float ConstraintBias = .2f;
        protected override void SetSolverIterations(int value) { WorldPolicy(); if (Iterations != value) { Iterations = value; WakeDynamics(); } WorldPolicyPublishedHook?.Invoke(); }
        protected override void SetConstraintDefaultBias(float value) { WorldPolicy(); ConstraintBias = value; WorldPolicyPublishedHook?.Invoke(); }
        internal float ReadWorldPolicy(int index) => index switch
        {
            0 => _sleepSettings.LinearThreshold,
            1 => _sleepSettings.AngularThreshold,
            2 => _sleepSettings.TimeToSleep,
            3 => _contactSettings.RecycleRadius,
            4 => _contactSettings.MaxSeparation,
            5 => _contactSettings.Bias,
            6 => _contactSettings.AllowedPenetration,
            7 => Iterations,
            8 => ConstraintBias,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        private void WorldPolicy() { WorldPolicyCalls++; WorldPolicyHook?.Invoke(); }
        protected override void PrepareSolverCapacity(int bodies, int shapes, int joints) => CapacityHook?.Invoke();
        protected override void PrepareMonitoringCapacity(int bodies, int shapes) => MonitorCapacityHook?.Invoke();
        protected override PhysicsBackendStatistics ReadStatistics()
        {
            LifecycleHook?.Invoke(LifecyclePhase.Statistics);
            if (StatisticsOverride is { } value) return value;
            var active = 0; var islands = 0; var sensorPairs = 0;
            foreach (var b in Bodies.Values)
            {
                b.SleepVisited = false;
                if (!b.IsArea && b.Awake && b.Mode != PhysicsServer.BodyMode.Static) active++;
            }
            foreach (var seed in Bodies.Values)
            {
                if (!seed.Dynamic || seed.SleepVisited) continue;
                Group(seed, _sleepGroup);
                var awake = false; var constrained = false;
                foreach (var b in _sleepGroup)
                {
                    awake |= b.Awake;
                    foreach (var peer in Bodies.Values)
                        if (peer != b && !peer.IsArea && Connected(b, peer)) constrained = true;
                }
                if (awake && constrained) islands++;
            }
            _sleepGroup.Clear();
            foreach (var area in Bodies.Values)
                if (area.IsArea)
                    foreach (var peer in Bodies.Values)
                    {
                        if (peer == area || peer.IsArea && area.RID.GetID() > peer.RID.GetID() ||
                            (area.Mask & peer.Layer) == 0 && (!peer.IsArea || (peer.Mask & area.Layer) == 0)) continue;
                        for (var i = 0; i < area.Shapes; i++) for (var j = 0; j < peer.Shapes; j++)
                                if (PartsOverlap(area, area.Parts[i], peer, peer.Parts[j])) sensorPairs++;
                    }
            return new(active, checked(Pairs + sensorPairs), islands);
        }
        protected override PhysicsBackendCollider CreateCollider(RID collider, bool area)
        {
            ColliderFactoryHook?.Invoke(collider);
            if (NullColliderFactory) return null!;
            if (ReusedCollider is { } reused) return reused;
            var b = new Body(this, collider, area); Bodies.Add(collider, b); return b;
        }
        protected override PhysicsBackendJoint CreateJoint(RID joint)
        {
            if (FailJointFactory) throw new InvalidOperationException("Joint factory failure");
            if (NullJointFactory) return null!;
            if (ReusedJoint is { } reused) return reused;
            var created = new Constraint(this, joint); Joints.Add(joint, created);
            try { JointFactoryHook?.Invoke(joint); return created; }
            catch { Joints.Remove(joint); throw; }
        }
        protected override void BeginStep(double delta) { LifecycleHook?.Invoke(LifecyclePhase.Begin); Ready = false; }
        protected override void PrepareWorld(double delta, in PhysicsAreaFieldConfiguration fields) { Fields = fields; DefaultFieldsHook?.Invoke(); PrepareFieldAreas(); }
        protected override void PrepareBodies(double delta) { }
        internal bool CountSolveAllocations;
        internal long SolveAllocatedBytes;
        protected override void Solve(double delta)
        {
            if (!SelectedActive) return;
            var allocated = CountSolveAllocations ? GC.GetAllocatedBytesForCurrentThread() : 0;
            try
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
                if (Joints.Count != 0 || HasContinuousBodies()) SolveContinuous((float)delta);
                else
                {
                    foreach (var b in Bodies.Values) b.Advance((float)delta);
                    SolveConstraints((float)delta, true); UpdateSleep((float)delta);
                }
                Steps++;
            }
            finally
            {
                if (CountSolveAllocations) Interlocked.Add(ref SolveAllocatedBytes, GC.GetAllocatedBytesForCurrentThread() - allocated);
            }
        }
        private readonly HashSet<(RID, int, RID, int)> _framePairs = [];
        protected override void SyncResults()
        { foreach (var body in Bodies.Values) body.FreezeContacts(); Ready = true; LifecycleHook?.Invoke(LifecyclePhase.Sync); }
        protected override void CompleteBody(RID body) { CompleteBodyHook?.Invoke(body); }
        protected override void CollectContacts() { ContactsHook?.Invoke(); }
        protected override void ScanAreas() { AreasHook?.Invoke(); }
        protected override int GetAreaOverlapCount(RID area) => AreaCountOverride?.Invoke(area) ?? ReadAreaOverlaps(area, Span<PhysicsBackendOverlap>.Empty);

        protected override int ReadAreaOverlaps(RID area, Span<PhysicsBackendOverlap> output)
        {
            if (AreaReadOverride?.Invoke(area, output) is { } supplied) return supplied;
            var self = Bodies[area]; var count = 0;
            foreach (var other in Bodies.Values)
            {
                if (other == self || (self.Mask & other.Layer) == 0 || other.IsArea && !other.Monitorable) continue;
                for (var i = 0; i < self.Shapes; i++) for (var j = 0; j < other.Shapes; j++)
                        if (PartsOverlap(self, self.Parts[i], other, other.Parts[j]) && !PriorAreaPair(self, i, other, j))
                        { if (count < output.Length) output[count] = new(other.RID, other.Parts[j].Index, self.Parts[i].Index); count++; }
            }
            return count;
        }
        protected override bool AreaContainsPoint(RID area, Vector2 point, uint mask)
        {
            var b = Bodies[area]; if ((b.Layer & mask) == 0) return false;
            for (var i = 0; i < b.Shapes; i++) if (ContainsPart(b.Parts[i], new(b.Rotation, b.Position), point)) return true;
            return false;
        }
        protected override void EndStep(Exception? failure) { EndStepHook?.Invoke(failure); }
        protected override void FlushQueries() { LifecycleHook?.Invoke(LifecyclePhase.Flush); Flushes++; FlushHook?.Invoke(); }
        protected override void EndSync(Exception? failure) { EndFailure = failure; LifecycleHook?.Invoke(LifecyclePhase.End); Synchronizations++; SyncHook?.Invoke(); }
        protected override void Finish() { LifecycleHook?.Invoke(LifecyclePhase.Finish); FinishHook?.Invoke(); Finished = true; Bodies.Clear(); Joints.Clear(); if (FailFinish) throw new InvalidOperationException("Finish failure"); }
        protected override void PrepareCheckpoint() { }
        internal WeakReference<PhysicsBackendCheckpoint>? HistoryReference;
        internal bool FailHistoryRelease;
        internal Action? RestoreExecutionHook;
        protected override PhysicsBackendCheckpoint CreateCheckpoint()
        {
            var history = new History(this); HistoryReference = new(history); return history;
        }
        protected override void PreparePortableCapture() { }
        protected override void ReadPortableBodies(ReadOnlySpan<RID> colliders, bool capture) { }
        protected override void InvalidateCollisionExceptions(RID collider) { }
        protected override void SynchronizeCollisionExceptions() { }
        protected override float ReadPortableSleepTime(RID collider) => Bodies[collider].SleepTime;
        protected override void ApplyPortableSleep(RID collider, float time, bool canSleep, bool sleeping)
        { var b = Bodies[collider]; b.SleepTime = time; b.Awake = !sleeping; b.CanSleep = canSleep; }
        protected override void BeginPortableRestore() { }
        internal readonly List<PhysicsBackendOneWay> OneWays = [];
        internal int OneWayCapacity = -1, OneWayReturn = -1, OneWaySpanLength;
        protected override void CompletePortableRestore(ReadOnlySpan<PhysicsBackendOneWay> pairs, ulong tick)
        { OneWays.Clear(); OneWays.EnsureCapacity(pairs.Length); foreach (var pair in pairs) OneWays.Add(pair); RestoreExecutionHook?.Invoke(); }

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
        internal RID RID => rid;
        internal float InvMass => InverseMass;
        internal float InvInertia => InverseInertia;
        internal Vector2 Center => Position + CenterOfMass;
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
        internal float Rotation, Angular, Torque, SurfaceAngular, Mass = 1, Inertia = 1, SleepTime;
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
                Velocity = Velocity * MathF.Max(0, 1 - _fields.LinearDamp * dt) + (_fields.Gravity + Force / Mass) * dt;
                if (!Locked) Angular = Angular * MathF.Max(0, 1 - _fields.AngularDamp * dt) + Torque * InverseInertia * dt;
            }
            Force = default; Torque = 0; return true;
        }
        internal void Move(float dt)
        {
            if (!Awake || area || Mode == PhysicsServer.BodyMode.Static) return;
            var next = Locked ? Rotation : Rotation + Angular * dt;
            Position += Velocity * dt + (_center.Rotated(Rotation) - _center.Rotated(next));
            Rotation = next;
        }
        internal bool FrameForcesApplied;
        protected override int ShapeCount => Shapes;
        internal int Shapes;
        protected override void Attach(Vector2 position, float rotation, in PhysicsBodyConfiguration configuration)
        { Position = position; Rotation = rotation; Mode = configuration.Mode; PublishedGravityScale = configuration.GravityScale; Velocity = configuration.LinearVelocity; Angular = configuration.AngularVelocity; Awake = !configuration.Sleeping; CanSleep = configuration.CanSleep; Locked = configuration.LockRotation; }
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
        protected override void SetMotionMode(PhysicsServer.BodyMode mode) { solver.ModeHook?.Invoke(rid); Mode = mode; }
        protected override void SetContactReporting(bool enabled) { }
        protected override void PrepareContactPolicy()
        {
            solver.PolicyHook?.Invoke(rid); var changed = false;
            for (var i = 0; i < Shapes; i++)
            {
                var part = Parts[i]; if (part.Source is not { IsDisposed: false } shape) continue;
                var bias = shape.CustomSolverBias; if (part.Bias == bias) continue;
                Parts[i] = part with { Bias = bias }; changed = true;
            }
            if (changed) solver.WakeConnected(this);
        }
        protected override void PrepareIntegration(in PhysicsBodyIntegrationConfiguration configuration, double delta)
        {
            ContinuousMode = configuration.CCDMode;
            Locked = configuration.LockRotation; Omit = configuration.OmitForces;
            var f = solver.ResolveFields(this);
            var fields = new PhysicsBodyFields(f.Gravity * (solver.UsePublishedGravityScale && !configuration.OmitForces ? PublishedGravityScale : configuration.GravityScale),
                configuration.LinearDamp + (configuration.LinearDampMode == RigidBody.DampMode.Combine ? f.LinearDamp : 0),
                configuration.AngularDamp + (configuration.AngularDampMode == RigidBody.DampMode.Combine ? f.AngularDamp : 0), true);
            if (_fields.Initialized && _fields != fields && Dynamic) SetAwake(true);
            _fields = fields;
        }
        protected override void SetAreaMonitoring(bool monitoring, bool monitorable) { Monitoring = monitoring; Monitorable = monitorable; solver.AreaMonitoringHook?.Invoke(RID); }
        protected override void SetAreaFields(in PhysicsAreaFieldConfiguration configuration) { AreaFields = configuration; solver.AreaFieldsHook?.Invoke(rid); }
        protected override PhysicsBodyFields ReadResolvedFields(in PhysicsBodyFields current) => _fields;
        protected override void RefreshIdentity(ulong objectID, ulong canvasID)
        { solver.IdentityHook?.Invoke(rid); ObjectID = objectID; Canvas = canvasID; }
        protected override void SetCollisionPriority(float value)
        { solver.PriorityHook?.Invoke(); Priority = value; }
        protected override void UpdateFilter(uint layer, uint mask, bool wakeBody)
        {
            solver.FilterPolicyHook?.Invoke(rid, layer, mask, wakeBody);
            solver.FilterHook?.Invoke(rid);
            if (wakeBody) solver.WakeConnected(this);
            Layer = layer; Mask = mask;
        }
        protected override void RebuildShapes(ReadOnlySpan<PhysicsColliderShape> slots, uint layer, uint mask, bool sensor, float density, float friction, float bounce)
        {
            solver.ShapeHook?.Invoke(rid);
            if (solver.ShapeFailure == rid) throw new InvalidOperationException("Shape retirement failure");
            Shapes = 0; Layer = layer; Mask = mask;
            Friction = friction; Bounce = bounce;
            LastShape = slots.IsEmpty ? default : slots[^1];
            RebuildParts(slots);
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
        protected override bool IsAwake => Mode != PhysicsServer.BodyMode.Static && Awake;
        protected override void SetPose(Vector2 position, float rotation)
        {
            solver.PoseHook?.Invoke(rid);
            if (Position == position && Rotation == rotation) return;
            Position = position; Rotation = rotation; solver.WakeConnected(this);
        }
        protected override void SetTargetPose(Transform pose, double delta) { Velocity = (pose.Origin - Position + _center.Rotated(pose.Rotation) - _center.Rotated(Rotation)) / (float)delta; Angular = (pose.Rotation - Rotation) / (float)delta; }
        protected override void SavePose() => _saved = GetTransform();
        protected override void RestorePose() { Position = _saved.Origin; Rotation = _saved.Rotation; }
        protected override void SetLinearVelocity(Vector2 velocity) { solver.BodyHook?.Invoke(rid); Velocity = velocity; }
        protected override void SetAngularVelocity(float velocity) { solver.AngularHook?.Invoke(rid); Angular = velocity; }
        protected override void ClearVelocity() { Velocity = default; Angular = 0; }
        protected override void SetAwake(bool awake) { solver.AwakeHook?.Invoke(rid); Awake = awake; SleepTime = 0; if (!awake) ClearVelocity(); else solver.WakeConnected(this); }
        protected override void SetCanSleep(bool canSleep) { solver.CanSleepHook?.Invoke(rid); CanSleep = canSleep; if (!canSleep) { Awake = true; SleepTime = 0; solver.WakeConnected(this); } }
        protected override void SetRotationLocked(bool locked) { solver.RotationHook?.Invoke(rid); Locked = locked; }
        internal float PublishedGravityScale = 1;
        protected override void SetGravityScale(float scale) { PublishedGravityScale = scale; solver.GravityHook?.Invoke(rid, scale); }
        protected override void ApplyCentralForce(Vector2 force, bool wake) { Force += force; Awake |= wake; }
        protected override void ApplyTorque(float torque, bool wake) { Torque += torque; Awake |= wake; }
        protected override bool RotationLocked => Locked;
        protected override Vector2 CenterOfMass => _center.Rotated(Rotation);
        protected override Vector2 CenterOfMassLocal => _center;
        protected override float InverseMass => Dynamic ? 1 / Mass : 0;
        protected override float InverseInertia => Dynamic && !Locked && Inertia > 0 ? 1 / Inertia : 0;
        protected override PhysicsMassProperties ApplyMassProfile(float mass, float inertia, Vector2? center)
        { solver.MassHook?.Invoke(rid); return PartMass(mass, inertia, center); }
        protected override void WakeTouching() { solver.ExceptionWakeHook?.Invoke(rid); solver.WakeConnected(this); }
        protected override Vector2 GetPointVelocity(Vector2 offset)
        {
            var arm = offset - CenterOfMass;
            return LinearVelocity + new Vector2(-arm.Y, arm.X) * AngularVelocity;
        }
        protected override Vector2 GetWorldPointVelocity(Vector2 point) => GetPointVelocity(point - Position);
        protected override void ApplyImpulse(Vector2 impulse, float moment) { AddImpulse(impulse, moment); if (Dynamic) { Awake = true; SleepTime = 0; solver.WakeConnected(this); } }
        protected override void SetSurfaceVelocity(Vector2 linear, float angular)
        { solver.SurfaceHook?.Invoke(rid); if (Surface == linear && SurfaceAngular == angular) return; Surface = linear; SurfaceAngular = angular; solver.WakeConnected(this); }
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
        internal (RID First, RID Second) Endpoints => (_first, _second);
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
        protected override void ApplySolverPolicy(in PhysicsJointConfiguration settings)
        {
            solver.JointParameterHook?.Invoke(rid);
            var changed = Settings != settings; Settings = settings;
            if (changed && _attached) solver.WakeConnected(solver.Bodies[_first]);
            solver.JointHook?.Invoke(rid);
        }
        protected override void ApplyPortableFrames(Transform first, Transform second) => Settings = Settings with { FrameA = first, FrameB = second };
        protected override void SetCollideConnected(bool value) { solver.JointCollisionHook?.Invoke(rid); Settings = Settings with { DisableCollision = !value }; solver.JointHook?.Invoke(rid); }
        protected override void SetPinLimits(bool enabled, float lower, float upper) { solver.JointParameterHook?.Invoke(rid); Settings = Settings with { PinLimitEnabled = enabled, PinLimitLower = lower, PinLimitUpper = upper }; solver.JointHook?.Invoke(rid); }
        protected override void SetPinMotorEnabled(bool value) { solver.JointParameterHook?.Invoke(rid); Settings = Settings with { PinMotorEnabled = value }; solver.JointHook?.Invoke(rid); }
        protected override void SetPinMotorVelocity(float value) { solver.JointParameterHook?.Invoke(rid); Settings = Settings with { PinMotorVelocity = value }; solver.JointHook?.Invoke(rid); }
        protected override void SetPinMotorMaxTorque(float value) { solver.JointParameterHook?.Invoke(rid); Settings = Settings with { PinMotorMaxTorque = value }; solver.JointHook?.Invoke(rid); }
        protected override void SetGrooveLimits(float lower, float upper) { solver.JointParameterHook?.Invoke(rid); Settings = Settings with { LowerTranslation = lower, UpperTranslation = upper }; solver.JointHook?.Invoke(rid); }
        private void SolveSpring(float dt)
        {
            if (Settings.Type != PhysicsServer.JointType.DampedSpring) return;
            var a = solver.Bodies[_first]; var b = solver.Bodies[_second];
            var pointA = a.Pose * Settings.FrameA.Origin; var pointB = b.Pose * Settings.FrameB.Origin;
            var separation = pointB - pointA;
            var length = separation.Length(); if (length == 0) return;
            var direction = separation / length;
            var ra = pointA - a.Center; var rb = pointB - b.Center;
            var crossA = Cross(ra, direction); var crossB = Cross(rb, direction);
            var inverse = (double)a.InvMass + b.InvMass + a.InvInertia * crossA * crossA + b.InvInertia * crossB * crossB;
            if (inverse == 0) return;
            var speed = (Motion(b) - Motion(a)).Dot(direction) + Spin(b) * crossB - Spin(a) * crossA;
            var decay = Math.Exp(-Settings.SpringDamping * dt * inverse);
            var elastic = (Settings.EffectiveSpringRestLength - length) * (double)Settings.SpringStiffness * dt;
            var total = elastic * decay - speed * (1 - decay) / inverse;
            var limit = (double)Settings.MaxForce * dt;
            var impulse = direction * (float)Math.Clamp(total, -limit, limit);
            Apply(a, -impulse, Cross(ra, -impulse)); Apply(b, impulse, Cross(rb, impulse));
        }
    }
}
