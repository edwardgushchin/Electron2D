using Electron2D;

public static partial class BackendChecks
{
    public static void RunNodeScheduling(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("NodeScheduling", () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("NodeScheduling");
            foreach (var kind in new[] { "node", "entity", "body" }) SchedulingMembership(world, kind);
            Console.WriteLine($"Node scheduling {backend?.ToString() ?? "registered"}, threaded={threaded}: captured lane membership, priority/tree order, reentry, errors, actual physics and zero-byte frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    private static void SchedulingMembership(World world, string kind)
    {
        using var root = new SubViewport { World = world };
        var first = new ScheduleProbe { Name = "First", ProcessPriority = -10, PhysicsProcessPriority = -10 };
        Node target = kind switch { "node" => new ScheduleProbe(), "entity" => new SpatialScheduleProbe(), _ => new BodyScheduleProbe() };
        target.Name = "Target"; root.AddChild(first); root.AddChild(target);
        using var shape = new CircleShape { Radius = 2 };
        var mover = new RigidBody
        {
            Name = "Mover",
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(6, 0),
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace,
            Position = new(0, 100)
        };
        mover.AddChild(new CollisionShape { Shape = shape }); root.AddChild(mover);
        using var tree = new SceneTree(root);
        var order = new List<(int Node, int Notification)>(8); Action<int>? action = null;
        first.Signal = what => { if (Lane(what)) { order.Add((1, what)); action?.Invoke(what); } };
        ((IScheduleProbe)target).Signal = what => { if (Lane(what)) order.Add((2, what)); };
        foreach (var physics in new[] { false, true })
            foreach (var internalLane in new[] { false, true })
            {
                Reset(); Enable(first, physics, internalLane, true);
                var notification = Notification(physics, internalLane);
                Frame(physics); order.Clear();
                action = _ => { Enable(target, physics, internalLane, true); action = null; };
                Frame(physics);
                Check(order.Count == 1 && order[0] == (1, notification),
                    "A newly enabled node was absent from the captured lane and must wait for its next frame");
                order.Clear(); Frame(physics);
                Check(order.SequenceEqual([(1, notification), (2, notification)]), "Enabling invalidates the next cached lane snapshot");
                action = _ => { Enable(target, physics, internalLane, false); action = null; };
                order.Clear(); Frame(physics); Check(order.SequenceEqual([(1, notification)]), "Disabling a captured later node takes effect immediately");
                order.Clear(); Frame(physics); Check(order.Count == 1, "Disabled membership does not survive a cached snapshot");
                Enable(target, physics, internalLane, true);
                action = _ => { if (physics) target.PhysicsProcessPriority = -20; else target.ProcessPriority = -20; action = null; };
                order.Clear(); Frame(physics); Check(order.SequenceEqual([(1, notification), (2, notification)]), "Priority writes preserve current captured order");
                order.Clear(); Frame(physics); Check(order.SequenceEqual([(2, notification), (1, notification)]), "Priority writes rebuild the next selected lane order");
                target.ProcessPriority = target.PhysicsProcessPriority = 0;
                action = _ => { root.RemoveChild(target); root.AddChild(target); action = null; };
                order.Clear(); Frame(physics);
                Check(order.SequenceEqual([(1, notification)]), "Detach and same-tree reentry cannot revive a removed captured entry");
                order.Clear(); Frame(physics); Check(order.SequenceEqual([(1, notification), (2, notification)]), "Reentered membership resumes on the next snapshot");
                target.ProcessMode = ProcessMode.Disabled;
                action = _ => { target.ProcessMode = ProcessMode.Always; action = null; };
                order.Clear(); Frame(physics);
                Check(order.SequenceEqual([(1, notification), (2, notification)]), "Captured enabled membership rechecks current process-mode eligibility");
            }
        foreach (var physics in new[] { false, true })
        {
            Reset(); Enable(target, physics, true, true);
            var internalNotification = Notification(physics, true); var publicNotification = Notification(physics, false);
            var failure = new InvalidOperationException("Expected internal scheduling failure");
            ((IScheduleProbe)target).Signal = what =>
            {
                if (!Lane(what)) return;
                order.Add((2, what));
                if (what == internalNotification) { Enable(target, physics, false, true); throw failure; }
            };
            var error = CaptureCopy(() => Frame(physics));
            Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure) &&
                order.SequenceEqual([(2, internalNotification), (2, publicNotification)]),
                "An internal member can enable its own public lane immediately, and internal failure still attempts that lane");
            Reset(); Enable(target, physics, true, true); Enable(target, physics, false, true);
            ((IScheduleProbe)target).Signal = what =>
            {
                if (!Lane(what)) return;
                order.Add((2, what));
                if (what == internalNotification) { root.RemoveChild(target); root.AddChild(target); }
            };
            Frame(physics);
            Check(order.SequenceEqual([(2, internalNotification)]), "Internal same-tree reentry suppresses the old attachment's public continuation");
            ((IScheduleProbe)target).Signal = what => { if (Lane(what)) order.Add((2, what)); };
            order.Clear(); Frame(physics);
            Check(order.SequenceEqual([(2, internalNotification), (2, publicNotification)]), "The new attachment resumes both enabled lanes on the next frame");
        }
        Reset(); first.ProcessEnabled = first.PhysicsProcessEnabled = true;
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        action = what => Enable(target, what == Node.NotificationPhysicsProcess, false, true);
        for (var i = 0; i < 128; i++) Active();
        Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Active();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Scheduling membership {kind} full idle/physics/query cycles allocate {owner}/{all} B");
        Console.WriteLine($"Scheduling {kind}: 128 warm/128 measured two-lane frames and real point queries {owner}/{all} B.");

        void Active()
        {
            target.ProcessEnabled = target.PhysicsProcessEnabled = false;
            order.Clear(); tree.ProcessFrame(.01); Check(order.Count == 1 && order[0] == (1, Node.NotificationProcess), "Warmed idle activation retains the captured boundary");
            order.Clear(); var x = mover.Position.X; tree.PhysicsFrame(.01);
            Check(order.Count == 1 && order[0] == (1, Node.NotificationPhysicsProcess) && MathF.Abs(mover.Position.X - x - .06f) < .0001f,
                "Warmed physics activation preserves selected solver motion after callback delivery");
            query.Position = mover.Position;
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == mover.GetRID(), "Scheduling preserves actual moving collider query identity");
        }
        void Reset()
        {
            action = null; order.Clear();
            first.ProcessEnabled = first.PhysicsProcessEnabled = first.InternalProcessEnabled = first.InternalPhysicsProcessEnabled = false;
            target.ProcessEnabled = target.PhysicsProcessEnabled = target.InternalProcessEnabled = target.InternalPhysicsProcessEnabled = false;
            target.ProcessPriority = target.PhysicsProcessPriority = 0; target.ProcessMode = ProcessMode.Inherit;
        }
        void Frame(bool physics) { if (physics) tree.PhysicsFrame(.01); else tree.ProcessFrame(.01); }
    }
    private static bool Lane(int what) => what is Node.NotificationProcess or Node.NotificationPhysicsProcess or Node.NotificationInternalProcess or Node.NotificationInternalPhysicsProcess;
    private static int Notification(bool physics, bool internalLane) => physics
        ? internalLane ? Node.NotificationInternalPhysicsProcess : Node.NotificationPhysicsProcess
        : internalLane ? Node.NotificationInternalProcess : Node.NotificationProcess;
    private static void Enable(Node node, bool physics, bool internalLane, bool value)
    {
        if (physics) { if (internalLane) node.InternalPhysicsProcessEnabled = value; else node.PhysicsProcessEnabled = value; }
        else { if (internalLane) node.InternalProcessEnabled = value; else node.ProcessEnabled = value; }
    }
    private interface IScheduleProbe { Action<int>? Signal { get; set; } }
    private sealed class ScheduleProbe : Node, IScheduleProbe
    {
        public Action<int>? Signal { get; set; }
        protected override void OnNotification(int what) { base.OnNotification(what); Signal?.Invoke(what); }
    }
    private sealed class SpatialScheduleProbe : Entity, IScheduleProbe
    {
        public Action<int>? Signal { get; set; }
        protected override void OnNotification(int what) { base.OnNotification(what); Signal?.Invoke(what); }
    }
    private sealed class BodyScheduleProbe : RigidBody, IScheduleProbe
    {
        public Action<int>? Signal { get; set; }
        protected override void OnNotification(int what) { base.OnNotification(what); Signal?.Invoke(what); }
    }
}
