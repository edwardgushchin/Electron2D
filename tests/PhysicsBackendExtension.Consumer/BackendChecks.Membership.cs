using Electron2D;

public static partial class BackendChecks
{
    public static void RunMembership(bool includeGPU = false, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var providers = new List<Solver>(); PhysicsServerManager.RegisterServer("Membership", () => { var s = new Solver(); providers.Add(s); return s; });
        PhysicsServer.Backend[] choices = includeGPU ? [PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU, PhysicsServer.Backend.Extension] : [PhysicsServer.Backend.CPU, PhysicsServer.Backend.Extension];
        var routes = 0; var transfers = 0; var faults = 0; var departures = 0; var rollbacks = 0; var preflights = 0; var callbacks = 0; var frames = 0; var owner = Thread.CurrentThread;
        using var shape = new CircleShape { Radius = 1 }; using var identity = new Node();
        try
        {
            foreach (var sourceKind in choices) foreach (var targetKind in choices)
                {
                    using var source = Create(sourceKind, out var from); using var target = Create(targetKind, out var to);
                    var sourceSpace = source.Space; var targetSpace = target.Space;
                    using var root = new SubViewport { World = source }; var secondary = new SubViewport { World = target }; root.AddChild(secondary);
                    CollisionObject[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone(), new Area()];
                    for (var i = 0; i < nodes.Length; i++)
                    {
                        nodes[i].Position = new(i * 40, 0); nodes[i].AddChild(new CollisionShape { Shape = shape, Disabled = true }); nodes[i].AddChild(new CollisionShape { Shape = shape }); root.AddChild(nodes[i]);
                    }
                    using var tree = new SceneTree(root); _ = secondary.FindWorld(); var raw = PhysicsServer.BodyCreate(); var sensor = PhysicsServer.AreaCreate();
                    RID[] ids = [.. nodes.Select(n => n.GetRID()), raw, sensor]; var held = new PhysicsDirectBodyState?[8];
                    using var point = new PhysicsPointQueryParameters { CollideWithAreas = true }; var hits = new PhysicsPointResult[1];
                    try
                    {
                        for (var i = 0; i < ids.Length; i++)
                        {
                            var id = ids[i];
                            if (i >= 6)
                            {
                                if (IsArea(i)) { PhysicsServer.AreaAddShape(id, shape.GetRID(), disabled: true); PhysicsServer.AreaAddShape(id, shape.GetRID()); }
                                else { PhysicsServer.BodyAddShape(id, shape.GetRID(), disabled: true); PhysicsServer.BodyAddShape(id, shape.GetRID()); }
                                Bind(i, sourceSpace);
                            }
                            if (IsArea(i))
                            {
                                PhysicsServer.AreaSetTransform(id, new(.2f, new Vector2(i * 40, 0))); PhysicsServer.AreaAttachObject(id, i < 6 ? nodes[i] : identity);
                                PhysicsServer.AreaAttachCanvasInstanceID(id, (1UL << 50) + (ulong)i); PhysicsServer.AreaSetCollisionLayer(id, 1); PhysicsServer.AreaSetCollisionMask(id, 3);
                                PhysicsServer.AreaSetGravity(id, 3); PhysicsServer.AreaSetGravityVector(id, new(.2f, .8f)); PhysicsServer.AreaSetPriority(id, 7);
                                PhysicsServer.AreaSetLinearDamp(id, .25f); PhysicsServer.AreaSetAngularDamp(id, .5f);
                            }
                            else
                            {
                                PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetTransform(id, new(.2f, new Vector2(i * 40, 0)));
                                PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false); PhysicsServer.BodySetMass(id, 2); PhysicsServer.BodySetInertia(id, 3);
                                PhysicsServer.BodySetFriction(id, .3f); PhysicsServer.BodySetBounce(id, .2f); PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace);
                                PhysicsServer.BodySetLinearDamp(id, .25f); PhysicsServer.BodySetAngularDamp(id, .5f); PhysicsServer.BodySetContinuousCollisionDetectionMode(id, CCDMode.CastShape);
                                PhysicsServer.BodySetCollisionLayer(id, 1); PhysicsServer.BodySetCollisionMask(id, 3); PhysicsServer.BodySetCollisionPriority(id, 2.5f);
                                PhysicsServer.BodyAttachCanvasInstanceID(id, (1UL << 50) + (ulong)i); if (i >= 6) PhysicsServer.BodyAttachObject(id, identity);
                                PhysicsServer.BodySetLinearVelocity(id, new(1, .5f)); PhysicsServer.BodySetAngularVelocity(id, .1f); PhysicsServer.BodySetConstantForce(id, new(.2f, .1f)); PhysicsServer.BodySetConstantTorque(id, .3f);
                                held[i] = PhysicsServer.BodyGetDirectState(id);
                            }
                        }
                        for (var i = 0; i < ids.Length; i++)
                        {
                            var before = Snapshot(i);
                            Task.Run(() => Reject<InvalidOperationException>(() => Bind(i, targetSpace))).GetAwaiter().GetResult();
                            Check(Space(i) == sourceSpace, "A foreign-thread transfer rejects before source departure"); CheckState(i, before); preflights++;
                        }
                        if (from is not null && targetKind != PhysicsServer.Backend.Extension)
                        {
                            using var unsupported = new CustomShape(Annulus.Create(1, 2));
                            for (var i = 0; i < ids.Length; i++)
                            {
                                Replace(unsupported); var before = Snapshot(i); var view = IsArea(i) ? null : PhysicsServer.BodyGetDirectState(ids[i]);
                                Reject<NotSupportedException>(() => Bind(i, targetSpace));
                                Check(Space(i) == sourceSpace && (view is null || ReferenceEquals(view, PhysicsServer.BodyGetDirectState(ids[i]))), "Unsupported target geometry preserves the current source attachment and view");
                                CheckState(i, before); Replace(shape); preflights++;
                                void Replace(Shape replacement)
                                {
                                    if (i < 6) ((CollisionShape)nodes[i].GetChildren()[1]).Shape = replacement;
                                    else if (IsArea(i)) PhysicsServer.AreaSetShape(ids[i], 1, replacement.GetRID());
                                    else PhysicsServer.BodySetShape(ids[i], 1, replacement.GetRID());
                                }
                            }
                        }
                        var states = Enumerable.Range(0, 8).Select(Snapshot).ToArray();
                        var oldTick = PhysicsServer.SpaceGetTick(sourceSpace); var nextTick = PhysicsServer.SpaceGetTick(targetSpace);
                        for (var i = 0; i < ids.Length; i++)
                        {
                            Bind(i, targetSpace); transfers++; CheckState(i, states[i]); Query(target, i);
                            point.Position = Pose(i).Origin; Check(source.DirectSpaceState.IntersectPoint(point, hits) == 0, "Departure removes the physical RID from source query membership");
                            if (held[i] is { } old) Reject<ObjectDisposedException>(() => _ = old.LinearVelocity);
                            var current = IsArea(i) ? null : PhysicsServer.BodyGetDirectState(ids[i]); Bind(i, targetSpace);
                            Check(current is null || ReferenceEquals(current, PhysicsServer.BodyGetDirectState(ids[i])) && ReferenceEquals(current.GetSpaceState(), target.DirectSpaceState), "Unchanged membership retains one fresh target view and its actual query owner");
                            if (i < 6) Check(nodes[i].Parent == root && nodes[i].Tree == tree && ReferenceEquals(nodes[i].GetWorld(), source), "Explicit physical transfer preserves scene parent, tree and world descriptor");
                        }
                        Check(PhysicsServer.SpaceGetTick(sourceSpace) == oldTick && PhysicsServer.SpaceGetTick(targetSpace) == nextTick, "Attachment changes never advance either world clock");
                        foreach (var i in Enumerable.Range(0, 8).Where(i => !IsArea(i)))
                            foreach (var mode in new[] { PhysicsServer.BodyMode.Static, PhysicsServer.BodyMode.Kinematic, PhysicsServer.BodyMode.Rigid, PhysicsServer.BodyMode.RigidLinear })
                            {
                                PhysicsServer.BodySetMode(ids[i], mode); PhysicsServer.BodySetLinearVelocity(ids[i], new(2, 1)); PhysicsServer.BodySetAngularVelocity(ids[i], mode == PhysicsServer.BodyMode.RigidLinear ? 0 : .2f);
                                var state = Snapshot(i); Bind(i, sourceSpace); transfers++; CheckState(i, state); Bind(i, targetSpace); transfers++; CheckState(i, state);
                                PhysicsServer.BodySetMode(ids[i], PhysicsServer.BodyMode.Rigid);
                            }
                        for (var i = 0; i < ids.Length; i++)
                            if (!IsArea(i))
                            {
                                PhysicsServer.BodySetCanSleep(ids[i], true); PhysicsServer.BodySetSleeping(ids[i], true);
                                var before = Snapshot(i); Bind(i, sourceSpace); CheckState(i, before); Bind(i, targetSpace); CheckState(i, before); transfers += 2;
                                PhysicsServer.BodySetCanSleep(ids[i], false);
                            }
                        if (to is not null)
                            for (var i = 0; i < ids.Length; i++)
                                foreach (var phase in new[] { "factory", "identity", "shape", "capacity" })
                                {
                                    Bind(i, sourceSpace); var before = Snapshot(i); var id = ids[i]; var failure = new ApplicationException("Target membership preparation failure");
                                    to.ColliderFactoryHook = phase == "factory" ? current => { if (current == id) throw failure; } : null;
                                    to.IdentityHook = phase == "identity" ? current => { if (current == id) throw failure; } : null;
                                    to.ShapeHook = phase == "shape" ? current => { if (current == id) throw failure; } : null;
                                    to.CapacityHook = phase == "capacity" ? () => { if (to.Bodies.ContainsKey(id)) throw failure; } : null;
                                    Exception[] errors;
                                    try { errors = ReleaseErrors(() => Bind(i, targetSpace)); }
                                    finally { to.ColliderFactoryHook = null; to.IdentityHook = null; to.ShapeHook = null; to.CapacityHook = null; }
                                    Check(errors.Contains(failure) && Space(i) == sourceSpace && !to.Bodies.ContainsKey(id), "Failed target binding retires the partial target and restores the source identity");
                                    CheckState(i, before); Query(source, i); Bind(i, targetSpace); faults++;
                                }
                        if (from is not null)
                            for (var i = 0; i < ids.Length; i++)
                            {
                                Bind(i, sourceSpace); var before = Snapshot(i); var id = ids[i]; var failure = new ApplicationException("Source membership departure failure");
                                from.BodyReleaseHook = current => { if (current == id) throw failure; }; Exception[] errors;
                                try { errors = ReleaseErrors(() => Bind(i, targetSpace)); } finally { from.BodyReleaseHook = null; }
                                Check(errors.Contains(failure) && Space(i) == default && !from.Bodies.ContainsKey(id) && (to is null || !to.Bodies.ContainsKey(id)), "A failed source departure commits observable detachment and preserves its original error");
                                CheckState(i, before); Bind(i, targetSpace); departures++;
                            }
                        if (from is not null && to is not null)
                            for (var i = 0; i < ids.Length; i++)
                            {
                                Bind(i, sourceSpace); var before = Snapshot(i); var id = ids[i]; var first = new ApplicationException("Target rollback trigger"); var second = new ArithmeticException("Source restoration failure");
                                to.IdentityHook = current => { if (current == id) throw first; }; to.FailBodyDetach = true; from.IdentityHook = current => { if (current == id) throw second; }; Exception[] errors;
                                try { errors = ReleaseErrors(() => Bind(i, targetSpace)); }
                                finally { to.IdentityHook = null; to.FailBodyDetach = false; from.IdentityHook = null; }
                                Check(errors.Contains(first) && errors.Contains(second) && errors.Any(error => !ReferenceEquals(error, first) && !ReferenceEquals(error, second)) && Space(i) == default && !from.Bodies.ContainsKey(id) && !to.Bodies.ContainsKey(id),
                                    "Failed target cleanup and source restoration preserve every error and retire both partial bindings");
                                CheckState(i, before); Bind(i, targetSpace); rollbacks++;
                            }
                        for (var i = 0; i < ids.Length; i++)
                        {
                            var before = Snapshot(i); Bind(i, default); Check(Space(i) == default, "Explicit detachment exposes the empty actual space"); CheckState(i, before); Bind(i, targetSpace); CheckState(i, before); transfers += 2;
                            if (!IsArea(i))
                            {
                                PhysicsServer.BodySetConstantForce(ids[i], Vector2.Zero); PhysicsServer.BodySetConstantTorque(ids[i], 0); PhysicsServer.BodySetLinearDamp(ids[i], 0); PhysicsServer.BodySetAngularDamp(ids[i], 0);
                                PhysicsServer.BodySetAngularVelocity(ids[i], 0); PhysicsServer.BodySetStateSyncCallback(ids[i], _ => { Check(Thread.CurrentThread == owner, "Transferred callbacks remain on the owning scene thread"); callbacks++; });
                            }
                        }
                        if (from is not null) from.StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "The source retains its captured numerical lane after transfers");
                        if (to is not null) to.StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "The target owns its captured numerical lane after transfers");
                        for (var f = 0; f < 128; f++) Frame(); var calls = callbacks; Collect();
                        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                        for (var f = 0; f < 128; f++) Frame();
                        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                        Check(own == 0 && all == 0 && callbacks == calls + 768, $"Post-transfer complete frames allocated {own}/{all} B or lost target callbacks"); frames += 128; routes++;
                        Check(source.PhysicsBackend == sourceKind && target.PhysicsBackend == targetKind && source.PhysicsFallbackReason is null && target.PhysicsFallbackReason is null, "Ordered mixed routes retain both actual requested owners without fallback");
                        void Frame()
                        {
                            for (var i = 0; i < ids.Length; i++) if (!IsArea(i)) { PhysicsServer.BodySetTransform(ids[i], new(.2f, new Vector2(i * 40, 0))); PhysicsServer.BodySetLinearVelocity(ids[i], Vector2.Right); }
                            var first = PhysicsServer.SpaceGetTick(sourceSpace); var second = PhysicsServer.SpaceGetTick(targetSpace); tree.PhysicsFrame(.01);
                            Check(PhysicsServer.SpaceGetTick(sourceSpace) == first + 1 && PhysicsServer.SpaceGetTick(targetSpace) == second + 1, "The original scene driver steps each distinct source and target world exactly once");
                            for (var i = 0; i < ids.Length; i++)
                            {
                                var pose = Pose(i); Check(MathF.Abs(pose.Origin.X - i * 40 - (IsArea(i) ? 0 : .01f)) < .0001f, "Transferred dynamic bodies move only under the target solver"); Query(target, i);
                            }
                        }
                    }
                    finally
                    {
                        if (from is not null) from.StepHook = null; if (to is not null) { to.StepHook = null; to.ColliderFactoryHook = null; to.IdentityHook = null; to.ShapeHook = null; to.CapacityHook = null; to.FailBodyDetach = false; }
                        if (from is not null) { from.BodyReleaseHook = null; from.IdentityHook = null; }
                        PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(sensor);
                    }
                    bool IsArea(int i) => i is 5 or 7;
                    RID Space(int i) => IsArea(i) ? PhysicsServer.AreaGetSpace(ids[i]) : PhysicsServer.BodyGetSpace(ids[i]);
                    void Bind(int i, RID space) { if (IsArea(i)) PhysicsServer.AreaSetSpace(ids[i], space); else PhysicsServer.BodySetSpace(ids[i], space); }
                    Transform Pose(int i) => IsArea(i) ? PhysicsServer.AreaGetTransform(ids[i]) : PhysicsServer.BodyGetTransform(ids[i]);
                    void Query(World world, int i) { point.CanvasInstanceID = IsArea(i) ? PhysicsServer.AreaGetCanvasInstanceID(ids[i]) : PhysicsServer.BodyGetCanvasInstanceID(ids[i]); point.Position = Pose(i).Origin; var found = world.DirectSpaceState.IntersectPoint(point, hits); if (!(found == 1 && hits[0].ColliderRID == ids[i] && hits[0].ShapeIndex == 1)) throw new InvalidOperationException($"Selected query {sourceKind}->{targetKind}, owner {i}, at {point.Position}, expected {ids[i]}: {found}, hit {hits[0].ColliderRID}/{hits[0].ShapeIndex}"); }
                    (ulong Object, ulong Canvas, uint Layer, uint Mask, RID First, RID Second, float[] Values) Snapshot(int i)
                    {
                        var id = ids[i]; var pose = Pose(i);
                        return IsArea(i) ? (PhysicsServer.AreaGetObjectInstanceID(id), PhysicsServer.AreaGetCanvasInstanceID(id), PhysicsServer.AreaGetCollisionLayer(id), PhysicsServer.AreaGetCollisionMask(id), PhysicsServer.AreaGetShape(id, 0), PhysicsServer.AreaGetShape(id, 1),
                            [pose.Origin.X, pose.Origin.Y, pose.Rotation, PhysicsServer.AreaGetGravity(id), PhysicsServer.AreaGetGravityVector(id).X, PhysicsServer.AreaGetGravityVector(id).Y, PhysicsServer.AreaGetLinearDamp(id), PhysicsServer.AreaGetAngularDamp(id), PhysicsServer.AreaGetPriority(id)]) :
                            (PhysicsServer.BodyGetObjectInstanceID(id), PhysicsServer.BodyGetCanvasInstanceID(id), PhysicsServer.BodyGetCollisionLayer(id), PhysicsServer.BodyGetCollisionMask(id), PhysicsServer.BodyGetShape(id, 0), PhysicsServer.BodyGetShape(id, 1),
                            [pose.Origin.X, pose.Origin.Y, pose.Rotation, PhysicsServer.BodyGetLinearVelocity(id).X, PhysicsServer.BodyGetLinearVelocity(id).Y, PhysicsServer.BodyGetAngularVelocity(id), (float)PhysicsServer.BodyGetMode(id),
                                PhysicsServer.BodyGetSleeping(id) ? 1 : 0, PhysicsServer.BodyGetCanSleep(id) ? 1 : 0, PhysicsServer.BodyGetMass(id), PhysicsServer.BodyGetInertia(id), PhysicsServer.BodyGetFriction(id), PhysicsServer.BodyGetBounce(id), PhysicsServer.BodyGetGravityScale(id),
                                PhysicsServer.BodyGetLinearDamp(id), PhysicsServer.BodyGetAngularDamp(id), (float)PhysicsServer.BodyGetLinearDampMode(id), (float)PhysicsServer.BodyGetAngularDampMode(id), (float)PhysicsServer.BodyGetContinuousCollisionDetectionMode(id), PhysicsServer.BodyGetCollisionPriority(id),
                                PhysicsServer.BodyGetConstantForce(id).X, PhysicsServer.BodyGetConstantForce(id).Y, PhysicsServer.BodyGetConstantTorque(id)]);
                    }
                    void CheckState(int i, (ulong Object, ulong Canvas, uint Layer, uint Mask, RID First, RID Second, float[] Values) expected)
                    {
                        var actual = Snapshot(i); Check((actual.Object, actual.Canvas, actual.Layer, actual.Mask, actual.First, actual.Second) == (expected.Object, expected.Canvas, expected.Layer, expected.Mask, expected.First, expected.Second), "Mixed transfer preserves exact common identity, filters and shared shape resources");
                        for (var k = 0; k < actual.Values.Length; k++) Check(MathF.Abs(actual.Values[k] - expected.Values[k]) < .0001f, $"Membership state mismatch {sourceKind}->{targetKind}, owner {i}, field {k}: {actual.Values[k]} != {expected.Values[k]}");
                    }
                }
            Console.WriteLine($"Membership {(includeGPU ? "mixed" : "cpu-extension")}/{(threaded ? "separate" : "owner")}: {routes} ordered routes, {transfers} role/identity transfers, {preflights} preflight rejections, {faults} target failures, {departures} departure failures, {rollbacks} compound rollbacks; {frames} complete frames and {frames * 6} owner callbacks 0/0 B passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        World Create(PhysicsServer.Backend backend, out Solver? solver) { var world = backend == PhysicsServer.Backend.Extension ? new World("Membership") : new World(backend); _ = world.Space; solver = backend == PhysicsServer.Backend.Extension ? providers[^1] : null; return world; }
    }
}
