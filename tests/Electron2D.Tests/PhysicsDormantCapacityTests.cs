using Electron2D;

internal static class PhysicsDormantCapacityTests
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            using (var warm = new Rig(backend, 2)) { warm.Sleep(); warm.Wake(); }
            foreach (var count in new[] { 16, 17, 33, 65 })
            {
                using var rig = new Rig(backend, count);
                var allocation = rig.Sleep(); rig.Wake();
                Console.WriteLine($"Dormant {backend}, threaded={threaded}: {count} bodies/{count - 1} pins, first sleep after 128 active warm frames: {allocation.Owner}/{allocation.All} managed B; cold preparation {rig.Preparation.Owner}/{rig.Preparation.All} B.");
                Check(allocation == (0, 0), "First sleep of a prepared warm island allocates managed memory");
            }
            using (var rig = new Rig(backend, 65))
                foreach (var group in new[] { 1, 2, 3, 5, 9, 17, 33, 65, 9, 2, 65 })
                {
                    rig.Partition(group); var allocation = rig.Sleep(); rig.Wake();
                    Console.WriteLine($"Dormant partition {backend}: group={group}, {allocation.Owner}/{allocation.All} B.");
                    Check(allocation == (0, 0), "Changed dormant island partitions allocate managed memory");
                }
            using (var rig = new Rig(backend, 65, contacts: true))
            {
                Check(PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs) > 0, "The contact island has real solved collision pairs");
                Check(rig.Sleep() == (0, 0), "The contact island allocates while entering sleep");
                rig.VerifyReplay();
            }
            using (var rig = new Rig(backend, 128, contacts: true, pins: false)) rig.VerifyPartitionRestore();
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class Rig : IDisposable
    {
        internal (long Owner, long All) Preparation;
        private readonly RID _space;
        private readonly RID[] _bodies, _joints;
        private readonly Vector2[] _positions;
        private readonly CircleShape _shape = new() { Radius = 1 };
        internal Rig(PhysicsServer.Backend backend, int count, bool contacts = false, bool pins = true)
        {
            _shape.Radius = contacts ? 2 : 1;
            _space = PhysicsServer.SpaceCreate(backend); _bodies = new RID[count]; _joints = new RID[count - 1];
            _positions = new Vector2[count];
            PhysicsServer.AreaSetGravity(_space, 0); PhysicsServer.AreaSetLinearDamp(_space, 0); PhysicsServer.AreaSetAngularDamp(_space, 0);
            PhysicsServer.SpaceSetBodyTimeToSleep(_space, .05f);
            for (var i = 0; i < count; i++)
            {
                var body = _bodies[i] = PhysicsServer.BodyCreate();
                PhysicsServer.BodySetMass(body, 1); PhysicsServer.BodySetInertia(body, 1);
                PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetCollisionLayer(body, contacts ? 1u : 0); PhysicsServer.BodySetCollisionMask(body, contacts ? 1u : 0);
                _positions[i] = new(i * 4, 0);
                PhysicsServer.BodyAddShape(body, _shape.GetRID()); PhysicsServer.BodySetTransform(body, new(0, _positions[i]));
                PhysicsServer.BodySetSpace(body, _space);
                if (i == 0 || !pins) continue;
                var joint = _joints[i - 1] = PhysicsServer.JointCreate();
                PhysicsServer.JointMakePin(joint, new(i * 4 - 2, 0), _bodies[i - 1], body);
                PhysicsServer.JointDisableCollisionsBetweenBodies(joint, false);
            }
            PhysicsServer.SpaceSetActive(_space, true);
            for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(_space, 1d / 60);
        }
        internal void Partition(int group)
        {
            for (var i = 0; i < _bodies.Length; i++)
            {
                PhysicsServer.BodySetCanSleep(_bodies[i], false); PhysicsServer.BodySetSleeping(_bodies[i], false);
                PhysicsServer.BodySetTransform(_bodies[i], new(0, new(i * 4, 0)));
                PhysicsServer.BodySetLinearVelocity(_bodies[i], Vector2.Zero); PhysicsServer.BodySetAngularVelocity(_bodies[i], 0);
            }
            for (var i = 1; i < _bodies.Length; i++)
            {
                if (i % group == 0)
                {
                    if (_joints[i - 1].IsValid()) { PhysicsServer.FreeRID(_joints[i - 1]); _joints[i - 1] = default; }
                }
                else if (!_joints[i - 1].IsValid())
                {
                    var joint = _joints[i - 1] = PhysicsServer.JointCreate();
                    PhysicsServer.JointMakePin(joint, new(i * 4 - 2, 0), _bodies[i - 1], _bodies[i]);
                }
            }
            for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(_space, 1d / 60);
        }
        internal (long Owner, long All) Sleep(int frames = 16)
        {
            var preparationOwner = GC.GetAllocatedBytesForCurrentThread(); var preparationAll = GC.GetTotalAllocatedBytes(true);
            foreach (var body in _bodies) PhysicsServer.BodySetCanSleep(body, true);
            Preparation = (GC.GetAllocatedBytesForCurrentThread() - preparationOwner, GC.GetTotalAllocatedBytes(true) - preparationAll);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < frames; i++) PhysicsServer.SpaceStep(_space, 1d / 60);
            var result = (GC.GetAllocatedBytesForCurrentThread() - owner, GC.GetTotalAllocatedBytes(true) - all);
            for (var i = 0; i < _bodies.Length; i++)
            {
                if (!PhysicsServer.BodyGetSleeping(_bodies[i])) throw new InvalidOperationException($"Body {i}/{_bodies.Length} awake after sleep window: pose={PhysicsServer.BodyGetTransform(_bodies[i])}, velocity={PhysicsServer.BodyGetLinearVelocity(_bodies[i])}, angular={PhysicsServer.BodyGetAngularVelocity(_bodies[i])}");
                Check(PhysicsServer.BodyGetTransform(_bodies[i]).Origin.DistanceTo(_positions[i]) < .001f, "Sleep preserves each constrained pose");
            }
            return result;
        }
        internal void Wake()
        {
            PhysicsServer.BodyApplyCentralImpulse(_bodies[0], Vector2.Right);
            PhysicsServer.SpaceStep(_space, 1d / 60);
            Check(!PhysicsServer.BodyGetSleeping(_bodies[0]), "A real impulse wakes the sleeping island");
            var momentum = Vector2.Zero;
            foreach (var body in _bodies) momentum += PhysicsServer.BodyGetLinearVelocity(body);
            Check(momentum.DistanceTo(Vector2.Right) < .001f, "Wake preserves the complete island's linear momentum");
        }
        internal void VerifyReplay()
        {
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(_space);
            for (var i = 0; i < 128; i++) { Wake(); checkpoint.Restore(); }
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++)
            {
                Wake(); checkpoint.Restore();
                foreach (var body in _bodies) Check(PhysicsServer.BodyGetSleeping(body), "Checkpoint restores sleeping body identity and state");
            }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Console.WriteLine($"Dormant contact checkpoint: 65 bodies/64 pins, 128 complete wake/restore cycles: {owner}/{all} B.");
            Check(owner == 0 && all == 0, "Dormant contact checkpoint wake/restore cycles allocate managed memory");
        }
        internal void VerifyPartitionRestore()
        {
            void SetPartition(int singles)
            {
                for (var i = 0; i < _bodies.Length; i++)
                {
                    PhysicsServer.BodySetCanSleep(_bodies[i], false); PhysicsServer.BodySetSleeping(_bodies[i], false);
                    var index = i - singles;
                    _positions[i] = new(i < singles ? -1000 - i * 16 : index / 5 * 32 + index % 5 * 4, 0);
                    PhysicsServer.BodySetTransform(_bodies[i], new(0, _positions[i]));
                    PhysicsServer.BodySetLinearVelocity(_bodies[i], Vector2.Zero); PhysicsServer.BodySetAngularVelocity(_bodies[i], 0);
                }
                for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(_space, 1d / 60);
                // At most 128 pending island splits plus the quiet timer must complete.
                Check(Sleep(256) == (0, 0), "Changing contact islands allocates during natural sleep");
            }
            SetPartition(0); using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(_space);
            var saved = (Vector2[])_positions.Clone(); SetPartition(28);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            checkpoint.Restore();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Console.WriteLine($"Different dormant contact partition restore: 128 bodies, {owner}/{all} B.");
            Check(owner == 0 && all == 0, "Preparing a different saved dormant partition allocates managed memory");
            for (var i = 0; i < _bodies.Length; i++)
                Check(PhysicsServer.BodyGetSleeping(_bodies[i]) && PhysicsServer.BodyGetTransform(_bodies[i]).Origin.DistanceTo(saved[i]) < .001f,
                    "Different sleeping partition restores every body identity and pose");
        }
        public void Dispose()
        {
            foreach (var joint in _joints) if (joint.IsValid()) PhysicsServer.FreeRID(joint);
            foreach (var body in _bodies) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(_space); _shape.Dispose();
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
