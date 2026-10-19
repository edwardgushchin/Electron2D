using Electron2D;

public static partial class BackendChecks
{
    public static void RunBodyStates(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("BodyStates", () => new Solver());
            using var world = backend is { } selected ? new World(selected) : new World("BodyStates");
            using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 1 };
            PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
            var raw = PhysicsServer.BodyCreate(); var owner = Thread.CurrentThread; var callbacks = 0; var frames = 0; var queries = 0; var cold = 0;
            var modes = new[] { PhysicsServer.BodyMode.Static, PhysicsServer.BodyMode.Kinematic, PhysicsServer.BodyMode.Rigid, PhysicsServer.BodyMode.RigidLinear };
            RID[] ids = [.. nodes.Select(n => n.GetRID()), raw];
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            try
            {
                for (var i = 0; i < nodes.Length; i++)
                {
                    nodes[i].Position = new(i * 80, 0); nodes[i].AddChild(new CollisionShape { Shape = shape, Disabled = true }); nodes[i].AddChild(new CollisionShape { Shape = shape }); root.AddChild(nodes[i]);
                }
                PhysicsServer.BodyAddShape(raw, shape.GetRID(), disabled: true); PhysicsServer.BodyAddShape(raw, shape.GetRID());
                foreach (var id in ids)
                {
                    PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static);
                    if (!PhysicsServer.BodyGetSleeping(id)) throw new InvalidOperationException($"Detached static state lost inactivity for owner {Array.IndexOf(ids, id)}.");
                    PhysicsServer.BodySetSleeping(id, false); Check(PhysicsServer.BodyGetSleeping(id), "Static explicit wake does not change inactive participation"); cold++;
                    var n = Array.IndexOf(ids, id);
                    if (n < nodes.Length && nodes[n] is RigidBody rigid) Check(rigid.Sleeping, "Direct static scene state reports inactivity");
                }
                using var frozen = new RigidBody { Freeze = true };
                using var bone = new PhysicalBone();
                Check(frozen.Sleeping && PhysicsServer.BodyGetSleeping(frozen.GetRID()) && bone.Sleeping && PhysicsServer.BodyGetSleeping(bone.GetRID()), "Detached native frozen/inactive roles expose inactivity in scene and server reads");
                frozen.Freeze = false; Check(!frozen.Sleeping, "Static reads preserve the stored dynamic wake request");
                frozen.Sleeping = true; frozen.Freeze = true; _ = frozen.Sleeping; frozen.Freeze = false;
                Check(frozen.Sleeping, "Static reads preserve an existing dynamic sleep request");
                foreach (var requestedSleep in new[] { false, true })
                {
                    using var authored = new RigidBody { CanSleep = false, Sleeping = requestedSleep, Freeze = true };
                    using var packed = new PackedScene(); packed.Pack(authored);
                    using var copy = (RigidBody)packed.Instantiate();
                    Check(!copy.CanSleep && copy.Freeze && copy.Sleeping, "Packed inactive bodies preserve sleep permission and current static participation");
                    copy.Freeze = false; Check(copy.Sleeping == requestedSleep, "Packed bodies retain the dynamic sleep request independently of static inactivity"); cold++;
                }
                using var tree = new SceneTree(root); PhysicsServer.BodySetSpace(raw, world.Space);
                Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "State checks retain the requested selected backend without fallback");
                foreach (var control in new PhysicsBody[] { new AnimatableBody(), new CharacterBody() })
                {
                    control.DisableMode = CollisionDisableMode.MakeStatic; control.ProcessMode = ProcessMode.Disabled; root.AddChild(control);
                    PhysicsServer.BodySetSpace(control.GetRID(), default);
                    Check(PhysicsServer.BodyGetMode(control.GetRID()) == PhysicsServer.BodyMode.Static && PhysicsServer.BodyGetSleeping(control.GetRID()), "Detached disabled scene roles retain effective static inactivity");
                    root.RemoveChild(control); Check(!PhysicsServer.BodyGetSleeping(control.GetRID()), "Leaving disabled static participation restores the native kinematic role"); control.Dispose(); cold++;
                }

                foreach (var id in ids)
                {
                    var view = PhysicsServer.BodyGetDirectState(id)!; Check(view.Sleeping && PhysicsServer.BodyGetSleeping(id), "Attached static state and view agree");
                    PhysicsServer.BodySetSpace(id, default); Reject<ObjectDisposedException>(() => _ = view.Sleeping);
                    Check(PhysicsServer.BodyGetSleeping(id), "Static detachment preserves inactive state");
                    PhysicsServer.BodySetSpace(id, world.Space); Check(PhysicsServer.BodyGetSleeping(id), "Static reentry preserves inactive state");
                    PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCollisionMask(id, 0);
                    PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace);
                    PhysicsServer.BodySetLinearDamp(id, 0); PhysicsServer.BodySetAngularDamp(id, 0);
                    PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetCanSleep(id, true); PhysicsServer.BodySetSleeping(id, true);
                    Check(PhysicsServer.BodyGetSleeping(id) && PhysicsServer.BodyGetLinearVelocity(id) == Vector2.Zero && PhysicsServer.BodyGetAngularVelocity(id) == 0, "Dynamic sleep clears both motion channels");
                    PhysicsServer.BodySetCanSleep(id, false); Check(!PhysicsServer.BodyGetSleeping(id), "Disabling automatic sleep wakes the dynamic profile");
                    PhysicsServer.BodySetStateSyncCallback(id, state => { Check(Thread.CurrentThread == owner && state.GetSpaceState() == world.DirectSpaceState, "State callbacks retain owner thread and actual query space"); callbacks++; });
                    cold++;
                }
                for (var i = 0; i < 64; i++) Frame(); Collect();
                var calls = callbacks; var startQueries = queries; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0 && callbacks == calls + 576 && queries == startQueries + 768, $"Complete state frames allocated {own}/{all} bytes or lost callbacks/queries: {callbacks - calls}/{queries - startQueries}");
                Console.WriteLine($"Body states {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: 6 owners, 4 roles, {cold} lifecycle cases; 128 changed full frames, 576 owner callbacks, 768 queries 0/0 B passed.");
                void Frame()
                {
                    var index = frames++; var mode = modes[index & 3]; var canSleep = (index & 4) == 0;
                    for (var i = 0; i < ids.Length; i++)
                    {
                        var id = ids[i]; PhysicsServer.BodySetMode(id, mode); PhysicsServer.BodySetCanSleep(id, canSleep); PhysicsServer.BodySetSleeping(id, false);
                        PhysicsServer.BodySetTransform(id, new(.2f, new Vector2(i * 80, 0)));
                        PhysicsServer.BodySetLinearVelocity(id, mode == PhysicsServer.BodyMode.Kinematic ? Vector2.Zero : new(2, 1));
                        PhysicsServer.BodySetAngularVelocity(id, mode is PhysicsServer.BodyMode.Kinematic or PhysicsServer.BodyMode.RigidLinear ? 0 : .2f);
                        if (mode == PhysicsServer.BodyMode.Kinematic) PhysicsServer.BodySetTransform(id, new(.202f, new Vector2(i * 80 + .02f, .01f)));
                    }
                    var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(.01); Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Complete state frames advance exactly one fixed tick");
                    for (var i = 0; i < ids.Length; i++)
                    {
                        var id = ids[i]; var pose = PhysicsServer.BodyGetTransform(id); var stationary = mode == PhysicsServer.BodyMode.Static;
                        var target = new Vector2(i * 80 + (stationary ? 0 : .02f), stationary ? 0 : .01f);
                        var angle = mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.Kinematic ? .202f : .2f;
                        var velocity = PhysicsServer.BodyGetLinearVelocity(id); var angular = PhysicsServer.BodyGetAngularVelocity(id);
                        var velocityTolerance = mode == PhysicsServer.BodyMode.Kinematic ? .002f + 4 * (MathF.BitIncrement(MathF.Abs(target.X)) - MathF.Abs(target.X)) / .01f : .002f;
                        if (!(pose.Origin.DistanceTo(target) < .0002f && MathF.Abs(pose.Rotation - angle) < .0002f && velocity.DistanceTo(new(2, 1)) < velocityTolerance &&
                            MathF.Abs(angular - (mode == PhysicsServer.BodyMode.RigidLinear ? 0 : .2f)) < .002f && PhysicsServer.BodyGetCanSleep(id) == canSleep))
                            throw new InvalidOperationException($"State motion {backend}/{mode}, frame {index}, owner {i}: pose={pose.Origin}/{pose.Rotation}, expected={target}/{angle}; velocity={velocity}/{angular}, canSleep={PhysicsServer.BodyGetCanSleep(id)}/{canSleep}.");
                        if (stationary) Check(PhysicsServer.BodyGetSleeping(id), "Static full frames retain inactivity");
                        else if (mode != PhysicsServer.BodyMode.Kinematic) Check(!PhysicsServer.BodyGetSleeping(id), "Explicit dynamic motion remains awake");
                        point.Position = pose.Origin; Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == id && hits[0].ShapeIndex == 1, "Selected state motion retains the active logical shape identity"); queries++;
                    }
                }
            }
            finally { PhysicsServer.FreeRID(raw); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
