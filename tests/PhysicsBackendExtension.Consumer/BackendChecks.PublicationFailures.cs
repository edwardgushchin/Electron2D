using Electron2D;

public static partial class BackendChecks
{
    private enum PublicationFault { Complete, Contacts, Areas, EndStep, Begin, BeginAndEnd }
    public static void RunPublicationFailures(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("PublicationFailures", () => solver = new());
        try
        {
            if (backend is null)
            {
                for (var index = 0; index < 5; index++) Fault(PublicationFault.Complete, index);
                foreach (var fault in new[] { PublicationFault.Contacts, PublicationFault.Areas, PublicationFault.EndStep, PublicationFault.Begin, PublicationFault.BeginAndEnd }) Fault(fault);
                for (var index = 0; index < 5; index++) Fault(PublicationFault.Complete, index, reported: true);
                Fault(PublicationFault.EndStep, reported: true);
            }
            using var f = new PublicationFixture(backend);
            var complete = 0; var ended = 0;
            if (solver is not null)
            {
                solver.CompleteBodyHook = _ => complete++;
                solver.EndStepHook = failure => { Check(failure is null, "Healthy numerical retirement receives no failure"); ended++; };
            }
            var fail = true; var marker = new ApplicationException("User state callback failure");
            PhysicsServer.BodySetStateSyncCallback(f.IDs[0], _ => { f.Callbacks++; if (fail) { fail = false; throw marker; } });
            var tick = PhysicsServer.SpaceGetTick(f.World.Space);
            try { f.Tick(); throw new InvalidOperationException("Expected state callback failure"); }
            catch (AggregateException error) { Check(error.Flatten().InnerExceptions.Contains(marker), "The original gameplay callback error survives"); }
            Check(PhysicsServer.SpaceGetTick(f.World.Space) == tick + 1 && f.Callbacks == 4, "Later gameplay observers run after an earlier observer fails");
            f.Tick(); Check(f.Callbacks == 8, "The next distinct frame remains usable after gameplay callback failure");
            Collect();
            for (var i = 0; i < 64; i++) f.Tick();
            var start = PhysicsServer.BodyGetTransform(f.IDs[0]).Origin; var callbacks = f.Callbacks; var publications = complete; var retirements = ended;
            var measuredTick = PhysicsServer.SpaceGetTick(f.World.Space);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) f.Tick();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && f.Callbacks - callbacks == 512, "Healthy publication and gameplay observers allocate zero owner/all-thread bytes");
            var moved = PhysicsServer.BodyGetTransform(f.IDs[0]).Origin.X - start.X;
            // Four CPU substeps at 3 m contribute 512 half-ULPs, plus scene-unit conversion.
            Check(PhysicsServer.SpaceGetTick(f.World.Space) == measuredTick + 128, "Every measured frame advances exactly one world tick");
            Check(MathF.Abs(moved - 128f / 120) < .00625f, $"Every measured frame advances the actual selected world: moved={moved:R}, expected={128f / 120:R}");
            if (solver is not null) Check(complete - publications == 640 && ended - retirements == 128, "Each measured selected body publishes and the interval retires exactly once");
            Console.WriteLine($"Publication boundaries {backend?.ToString() ?? "registered"}, separate={threaded}: 128 full moving frames, 512 observers, owner/all={owner}/{all} B.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        void Fault(PublicationFault fault, int target = 0, bool reported = false)
        {
            using var f = new PublicationFixture(null); var selected = solver!;
            f.Tick(); f.Callbacks = 0;
            var tick = PhysicsServer.SpaceGetTick(f.World.Space); var steps = selected.Steps;
            var marker = new ApplicationException("Selected publication failure"); var cleanup = new ApplicationException("Selected retirement failure");
            Exception? retiredFailure = null; var completed = 0; var contacts = 0; var areas = 0; var ended = 0; var flushed = 0; var synced = 0;
            var owner = Environment.CurrentManagedThreadId;
            selected.LifecycleHook = phase =>
            {
                if (phase == LifecyclePhase.Begin && fault is PublicationFault.Begin or PublicationFault.BeginAndEnd) throw marker;
                if (phase == LifecyclePhase.Flush) flushed++;
                if (phase == LifecyclePhase.End) synced++;
            };
            selected.CompleteBodyHook = id =>
            {
                Check(Environment.CurrentManagedThreadId == owner, "Selected body completion remains on the owner thread");
                Reject<InvalidOperationException>(() => f.Tick());
                completed++;
                if (fault == PublicationFault.Complete && id == f.IDs[target])
                { if (reported) selected.ReportedFailure = true; else throw marker; }
            };
            selected.ContactsHook = () => { contacts++; if (fault == PublicationFault.Contacts) throw marker; };
            selected.AreasHook = () => { areas++; if (fault == PublicationFault.Areas) throw marker; };
            selected.EndStepHook = failure =>
            {
                ended++; retiredFailure = failure;
                if (fault == PublicationFault.EndStep) { if (reported) selected.ReportedFailure = true; else throw marker; }
                if (fault == PublicationFault.BeginAndEnd) throw cleanup;
            };
            Exception? observed = null;
            try
            {
                try { f.Tick(); throw new InvalidOperationException("Expected selected publication failure"); }
                catch (AggregateException error)
                {
                    var errors = error.Flatten().InnerExceptions;
                    observed = errors[0];
                    Check((reported ? observed is InvalidOperationException : errors.Contains(marker)) &&
                        (fault != PublicationFault.BeginAndEnd || errors.Contains(cleanup)), "Selected and secondary retirement errors survive aggregation");
                }
                Check(ended == 1 && flushed == 1 && synced == 1, "Retirement, notification flush and end-sync each run once after failure");
                Console.WriteLine($"Publication observation {fault}/{target}: completed={completed}, contacts={contacts}, areas={areas}, observers={f.Callbacks}, retirementFailure={retiredFailure?.Message ?? "none"}");
                Check(ReferenceEquals(retiredFailure, fault == PublicationFault.EndStep ? null : reported ? observed : marker), "Scene body completion failure was reported as a successful end step");
                Check(ReferenceEquals(selected.EndFailure, reported ? observed : marker), "End-sync observes the original interval or publication failure");
                var beganOnly = fault is PublicationFault.Begin or PublicationFault.BeginAndEnd;
                Check(selected.Steps == steps + (beganOnly ? 0 : 1), "Preparation and publication failures never replay the numerical interval");
                Check(f.Callbacks == 0, "An unsuccessful selected publication exposes no gameplay state callbacks");
                if (fault == PublicationFault.Complete) Check(completed == target + 1 && contacts == 0 && areas == 0, "No later selected publication runs after a failed body completion");
                selected.LifecycleHook = null; selected.CompleteBodyHook = null; selected.ContactsHook = selected.AreasHook = null; selected.EndStepHook = null; selected.ReportedFailure = false;
                if (fault == PublicationFault.Begin)
                {
                    Check(PhysicsServer.SpaceGetTick(f.World.Space) == tick, "Failed preparation preserves the last completed tick");
                    _ = PhysicsServer.BodyGetTransform(f.IDs[0]); f.Tick();
                    Check(selected.Steps == steps + 1 && f.Callbacks == 4, "A preparation failure with successful retirement permits a new interval");
                }
                else
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetTick(f.World.Space));
                    Reject<AggregateException>(() => f.Tick(0));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyGetTransform(f.IDs[0]));
                    Check(selected.Steps == steps + (beganOnly ? 0 : 1), "Terminal publication rejects future work without hidden solving");
                }
            }
            finally { selected.LifecycleHook = null; selected.CompleteBodyHook = null; selected.ContactsHook = selected.AreasHook = null; selected.EndStepHook = null; selected.ReportedFailure = false; }
            Console.WriteLine($"Selected publication fault {fault}, target={target}, reported={reported}: original errors, order, callback suppression and terminal/recoverable boundary passed.");
        }
    }
    private sealed class PublicationFixture : IDisposable
    {
        internal readonly World World;
        private readonly SceneTree _tree;
        private readonly CircleShape _shape = new() { Radius = 2 };
        internal readonly RID[] IDs = new RID[5];
        internal int Callbacks;
        internal PublicationFixture(PhysicsServer.Backend? backend)
        {
            World = backend is { } selected ? new World(selected) : new World("PublicationFailures");
            var root = new SubViewport { World = World };
            for (var i = 0; i < 2; i++)
            {
                var body = new RigidBody { Name = "Publication" + i, Position = new(100 * (i + 1), 0), GravityScale = 0, CanSleep = false, LinearVelocity = Vector2.Right, LinearDampMode = RigidBody.DampMode.Replace };
                body.AddChild(new CollisionShape { Shape = _shape }); root.AddChild(body); IDs[i + 3] = body.GetRID();
            }
            _tree = new SceneTree(root);
            for (var i = 0; i < 2; i++)
            {
                var id = IDs[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodyAddShape(id, _shape.GetRID());
                PhysicsServer.BodySetTransform(id, new(0, new Vector2(100 * (i + 3), 0))); PhysicsServer.BodySetGravityScale(id, 0);
                PhysicsServer.BodySetCanSleep(id, false); PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearVelocity(id, Vector2.Right); PhysicsServer.BodySetSpace(id, World.Space);
            }
            IDs[2] = PhysicsServer.AreaCreate(); PhysicsServer.AreaSetSpace(IDs[2], World.Space);
            foreach (var id in IDs) if (id != IDs[2]) PhysicsServer.BodySetStateSyncCallback(id, _ => Callbacks++);
        }
        internal void Tick(double delta = 1d / 120) => _tree.PhysicsFrame(delta);
        public void Dispose()
        {
            for (var i = 0; i < 3; i++) PhysicsServer.FreeRID(IDs[i]);
            _tree.Dispose(); World.Dispose(); _shape.Dispose();
        }
    }
}
