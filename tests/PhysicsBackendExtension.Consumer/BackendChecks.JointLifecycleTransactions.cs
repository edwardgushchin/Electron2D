using Electron2D;

public static partial class BackendChecks
{
    public static void RunJointLifecycleTransactions(bool threaded = false, bool requestsFirst = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (requestsFirst) { JointSceneRequests(threaded); JointClearTransactions(threaded); }
            else { JointClearTransactions(threaded); JointSceneRequests(threaded); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void JointClearTransactions(bool threaded)
    {
        Solver? solver = null; var cases = 0;
        PhysicsServerManager.RegisterServer("JointClearTransactions", () => solver = new());
        for (var role = 0; role < 3; role++)
            for (var scene = 0; scene < 2; scene++)
                for (var combined = 0; combined < 2; combined++) Run(role, scene != 0, combined != 0);
        Console.WriteLine($"Joint clear transactions/{threaded}: {cases} role/scene-raw/detach-invalidation cases passed.");

        void Run(int role, bool scene, bool combined)
        {
            using var world = new World("JointClearTransactions"); using var root = new SubViewport { World = world };
            var first = new RigidBody { Name = "A", GravityScale = 0 }; var second = new StaticBody { Name = "B" };
            root.AddChild(first); root.AddChild(second);
            Joint? node = scene ? role switch { 0 => new PinJoint(), 1 => new GrooveJoint(), _ => new DampedSpringJoint() } : null;
            if (node is not null) { node.NodeA = "../A"; node.NodeB = "../B"; root.AddChild(node); }
            using var tree = new SceneTree(root); var joint = node?.GetRID() ?? PhysicsServer.JointCreate();
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            try
            {
                if (node is null) MakeJoint(joint, role, first.GetRID(), second.GetRID());
                PhysicsServer.JointSetBias(joint, .3f); var failure = new ApplicationException("joint-clear-detach"); var invalidation = new ApplicationException("joint-clear-invalidation");
                solver!.JointReleaseHook = id => { if (id == joint) throw failure; };
                if (combined) solver.ExceptionWakeHook = id => { if (id == first.GetRID()) throw invalidation; };
                var errors = JointParameterErrors(() => PhysicsServer.JointClear(joint));
                Check(errors.Length == (combined ? 2 : 1) && errors.Any(e => ReferenceEquals(e, failure)) && (!combined || errors.Any(e => ReferenceEquals(e, invalidation))), "Clear preserves all original cleanup errors");
                if (PhysicsServer.JointGetType(joint) != PhysicsServer.JointType.Empty)
                    throw new InvalidOperationException("Failed joint clear retained a retired connection role.");
                Check(!solver.Joints.ContainsKey(joint) && !solver.ReadExceptions(first.GetRID()).Contains(second.GetRID()) && PhysicsServer.JointGetBias(joint) == .3f,
                    "Failed clear commits Empty and contribution retirement while retaining identity and common policy");
                solver.JointReleaseHook = solver.ExceptionWakeHook = null;
                Check(world.DirectSpaceState.IntersectPoint(point, hits) == 0, "Live query does not recreate a discarded connection");
                if (node is not null)
                {
                    PhysicsServer.BodySetSpace(first.GetRID(), default); PhysicsServer.BodySetSpace(first.GetRID(), world.Space); tree.PhysicsFrame(.01);
                    Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Empty && !solver.Joints.ContainsKey(joint), "Failed server clear remains authoritative through endpoint reentry");
                }
                PhysicsServer.JointClear(joint);
                if (node is not null)
                {
                    node.NodeA = ""; node.NodeA = "../A"; tree.PhysicsFrame(.01);
                    Check((int)PhysicsServer.JointGetType(joint) == role && solver.Joints.ContainsKey(joint), "An explicit scene path edit reclaims cleared scene ownership");
                }
                cases++;
            }
            finally
            {
                solver!.JointReleaseHook = solver.ExceptionWakeHook = null;
                if (node is null) PhysicsServer.FreeRID(joint); else node.Dispose();
            }
        }
    }

    private static void JointSceneRequests(bool threaded)
    {
        Solver? solver = null; var cases = 0;
        PhysicsServerManager.RegisterServer("JointSceneRequests", () => solver = new());
        for (var role = 0; role < 3; role++)
            for (var failure = 0; failure < 3; failure++) Run(role, failure);
        Console.WriteLine($"Joint scene requests/{threaded}: {cases} role/factory-attach-contribution cases passed.");

        void Run(int role, int failureKind)
        {
            using var world = new World("JointSceneRequests"); using var root = new SubViewport { World = world };
            var first = new RigidBody { Name = "A", GravityScale = 0 }; var second = new StaticBody { Name = "B", Position = new(20, 0) };
            var incoming = new RigidBody { Name = "C", Position = new(100, 0), GravityScale = 0 }; var incomingPeer = new StaticBody { Name = "D", Position = new(200, 0) };
            root.AddChild(first); root.AddChild(second); root.AddChild(incoming); root.AddChild(incomingPeer);
            Joint node = role switch { 0 => new PinJoint(), 1 => new GrooveJoint(), _ => new DampedSpringJoint() };
            node.NodeA = "../A"; node.NodeB = "../B"; root.AddChild(node);
            using var tree = new SceneTree(root); var id = node.GetRID(); var error = new ApplicationException("scene-server-joint-request"); var owner = Thread.CurrentThread;
            try
            {
                solver!.StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Retried scene request uses the actual selected numerical thread");
                if (failureKind == 0) solver.JointFactoryHook = joint => { if (joint == id) throw error; };
                else if (failureKind == 1) solver.JointHook = joint => { if (joint == id) throw error; };
                else solver.ExceptionWakeHook = body => { if (body == incoming.GetRID()) throw error; };
                Check(JointParameterErrors(() => MakeJoint(id, role, incoming.GetRID(), incomingPeer.GetRID())).Any(e => ReferenceEquals(e, error)), "A validated scene server request reports its original selected failure");
                solver.JointFactoryHook = solver.JointHook = solver.ExceptionWakeHook = null;
                PhysicsServer.BodySetSpace(incoming.GetRID(), default); PhysicsServer.BodySetSpace(incoming.GetRID(), world.Space); tree.PhysicsFrame(.01);
                if (!solver.Joints.TryGetValue(id, out var selected) || selected.Endpoints != (incoming.GetRID(), incomingPeer.GetRID()))
                    throw new InvalidOperationException("Failed server joint request lost scene ownership on endpoint reentry.");
                Check((int)PhysicsServer.JointGetType(id) == role && solver.ReadExceptions(incoming.GetRID()).Contains(incomingPeer.GetRID()), "Retried request retains RID, role and independent contribution");
                node.NodeA = ""; node.NodeA = "../A"; tree.PhysicsFrame(.01);
                Check(solver.Joints[id].Endpoints == (first.GetRID(), second.GetRID()), "Explicit scene authoring still reclaims ownership after a failed server request");
                cases++;
            }
            finally { solver!.JointFactoryHook = solver.JointHook = solver.ExceptionWakeHook = null; solver.StepHook = null; }
        }
    }
}
