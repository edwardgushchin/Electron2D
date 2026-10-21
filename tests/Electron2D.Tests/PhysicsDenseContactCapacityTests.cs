using Electron2D;

internal static class PhysicsDenseContactCapacityTests
{
    private const double Step = 1d / 60;
    internal static void Run(PhysicsServer.Backend backend, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            foreach (var slots in new[] { 16, 17, 32 })
            {
                using var rig = new Rig(backend, slots);
                Measure(rig.Cycle, "natural sleep/wake");
                using var checkpoint = rig.Checkpoint();
                Measure(() => { rig.Wake(); checkpoint.Restore(); rig.VerifySleeping(); }, "wake/checkpoint restore");

                void Measure(Action cycle, string operation)
                {
                    for (var i = 0; i < 128; i++) cycle();
                    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
                    var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 128; i++) cycle();
                    owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                    Console.WriteLine($"Dense contact capacity {backend}, threaded={threaded}: 2 bodies, {slots * slots} contacts, 128 {operation} cycles: {owner}/{all} B.");
                    Check(owner == 0 && all == 0, "A warmed dense contact island allocates during repeated sleep/wake or restoration");
                }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class Rig : IDisposable
    {
        private readonly RID _space, _body, _floor;
        private readonly CircleShape _circle = new() { Radius = 2 };
        private readonly RectangleShape _ground = new() { Size = new(200, 2) };
        private readonly PhysicsDirectBodyState _view;
        private readonly bool[] _pairs;
        private readonly int _slots;
        internal Rig(PhysicsServer.Backend backend, int slots)
        {
            _slots = slots; _pairs = new bool[slots * slots];
            _space = PhysicsServer.SpaceCreate(backend);
            PhysicsServer.AreaSetGravity(_space, 980); PhysicsServer.AreaSetLinearDamp(_space, 0); PhysicsServer.AreaSetAngularDamp(_space, 0);
            PhysicsServer.SpaceSetBodyTimeToSleep(_space, .05f);
            _floor = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(_floor, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetTransform(_floor, new(0, new(0, 3)));
            for (var i = 0; i < slots; i++) PhysicsServer.BodyAddShape(_floor, _ground.GetRID());
            PhysicsServer.BodySetSpace(_floor, _space);
            _body = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMode(_body, PhysicsServer.BodyMode.RigidLinear);
            PhysicsServer.BodySetMass(_body, 1); PhysicsServer.BodySetInertia(_body, 1);
            for (var i = 0; i < slots; i++) PhysicsServer.BodyAddShape(_body, _circle.GetRID());
            PhysicsServer.BodySetMaxContactsReported(_body, _pairs.Length);
            PhysicsServer.BodySetSpace(_body, _space); PhysicsServer.SpaceSetActive(_space, true);
            _view = PhysicsServer.BodyGetDirectState(_body)!;
            for (var i = 0; i < 32; i++) PhysicsServer.SpaceStep(_space, Step);
            Check(PhysicsServer.SpaceGetBackend(_space) == backend, "Dense contact capacity preserves the selected backend");
            VerifySleeping();
        }
        internal void Cycle()
        {
            Wake();
            for (var i = 0; i < 16; i++) PhysicsServer.SpaceStep(_space, Step);
            VerifySleeping();
        }
        internal PhysicsCheckpoint Checkpoint() => PhysicsServer.SpaceCreateCheckpoint(_space);
        internal void VerifySleeping()
        {
            Check(PhysicsServer.BodyGetSleeping(_body), "Dense contact island returns to natural sleep");
            Check(_view.Transform.Origin.Length() < .01f && _view.LinearVelocity.Length() < .001f, "Dense support preserves resting pose and velocity");
            Check(ReferenceEquals(_view, PhysicsServer.BodyGetDirectState(_body)), "Dense contact cycles preserve direct-view identity");
        }
        internal void Wake()
        {
            PhysicsServer.BodySetSleeping(_body, false); PhysicsServer.SpaceStep(_space, Step);
            Check(!PhysicsServer.BodyGetSleeping(_body), "Explicit wake activates the dense contact island");
            var count = _view.GetContactCount();
            if (count != _pairs.Length) throw new InvalidOperationException($"Expected {_pairs.Length} distinct contact pairs, got {count}");
            Array.Clear(_pairs); var impulse = Vector2.Zero;
            for (var i = 0; i < count; i++)
            {
                var local = _view.GetContactLocalShape(i); var other = _view.GetContactColliderShape(i);
                Check((uint)local < _slots && (uint)other < _slots, "Dense contact shape indices remain in range");
                var pair = local * _slots + other;
                Check(!_pairs[pair] && _view.GetContactCollider(i) == _floor, "Dense contact pairs preserve unique shape slots and floor identity");
                _pairs[pair] = true;
                Check(_view.GetContactLocalNormal(i).DistanceTo(Vector2.Up) < .001f, "Every dense contact retains its physical support normal");
                impulse += _view.GetContactImpulse(i);
            }
            Check(impulse.DistanceTo(new(0, -980 * (float)Step)) < .01f, "Complete dense contact impulse balances gravity");
        }
        public void Dispose()
        {
            _view.Dispose(); PhysicsServer.FreeRID(_body); PhysicsServer.FreeRID(_floor); PhysicsServer.FreeRID(_space);
            _circle.Dispose(); _ground.Dispose();
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
