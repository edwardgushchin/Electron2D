using Electron2D;

public static partial class BackendChecks
{
    private static void ProcessGroupRoles(PhysicsServer.Backend? backend)
    {
        using var world = backend is { } selected ? new World(selected) : new World("ProcessGroups");
        using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 2 };
        var moving = new RigidBody { GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0) };
        moving.AddChild(new CollisionShape { Shape = shape }); root.AddChild(moving);
        using var tree = new SceneTree(root);
        var owner = Environment.CurrentManagedThreadId;
        Node[] nodes = [new Node(), new Entity(), new Control(), new RigidBody(), new StaticBody(), new AnimatableBody(), new CharacterBody(), new Area()];
        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        foreach (var node in nodes)
        {
            node.Name = "Role" + node.GetType().Name;
            var changes = 0; var messages = 0; var messageThread = 0;
            var probe = new RoleProbe(); node.AddChild(probe);
            var nested = new RoleProbe { ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread }; node.AddChild(nested);
            var expectedRole = Node.ProcessThreadGroupMode.Inherit;
            Action<ElectronObject> changed = sender =>
            {
                changes++;
                Check(ReferenceEquals(sender, node) && node.ProcessThreadGroup == expectedRole && Environment.CurrentManagedThreadId == owner,
                    "Role metadata notification observes the committed node and role on the owner");
                probe.CallDeferredThreadGroup<RoleProbe>(_ => { messages++; messageThread = Environment.CurrentManagedThreadId; });
            };
            node.PropertyListChanged += changed;
            node.ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread;
            node.ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread;
            node.ProcessThreadGroup = Node.ProcessThreadGroupMode.Inherit;
            Check(changes == 0, "Detached role changes are silent");
            root.AddChild(node);
            foreach (var role in new[] { Node.ProcessThreadGroupMode.MainThread, Node.ProcessThreadGroupMode.SubThread, Node.ProcessThreadGroupMode.Inherit })
            {
                expectedRole = role; var before = changes; var delivered = messages;
                node.ProcessThreadGroup = role;
                Check(changes == before + 1 && messages == delivered, "One attached role change synchronously publishes metadata after rebinding");
                node.ProcessThreadGroup = role;
                Check(changes == before + 1, "Equal role writes do not repeat the notification");
                tree.ProcessFrame(.01);
                Check(messages == delivered + 1 && messageThread == probe.Thread && (probe.Thread == owner) == (role != Node.ProcessThreadGroupMode.SubThread) && nested.Thread == owner,
                    "Notification-queued work follows the new inherited group while nested boundaries retain their owner");
                var x = moving.Position.X; tree.PhysicsFrame(.01); query.Position = moving.Position;
                Check(moving.Position.X > x && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == moving.GetRID(),
                    "All role changes preserve actual selected simulation and query identity");
            }
            var count = changes;
            Reject<ArgumentOutOfRangeException>(() => node.ProcessThreadGroup = (Node.ProcessThreadGroupMode)3);
            Exception? foreign = null;
            var worker = new Thread(() => { try { node.ProcessThreadGroup = Node.ProcessThreadGroupMode.Inherit; } catch (Exception error) { foreign = error; } });
            worker.Start(); worker.Join();
            Check(foreign is InvalidOperationException && changes == count && node.ProcessThreadGroup == Node.ProcessThreadGroupMode.Inherit,
                "Invalid and foreign equal writes reject before metadata or group changes");
            node.PropertyListChanged -= changed;

            var failure = new InvalidOperationException("Expected role metadata failure");
            Action<ElectronObject> fail = _ => throw failure; node.PropertyListChanged += fail;
            Check(ReferenceEquals(CaptureCopy(() => node.ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread), failure) && node.ProcessThreadGroup == Node.ProcessThreadGroupMode.SubThread,
                "Observer failure propagates the original error after the new role has committed");
            node.PropertyListChanged -= fail; tree.ProcessFrame(.01);
            Check(probe.Thread != owner && nested.Thread == owner, "Failed metadata delivery leaves coherent replacement group membership");

            var nestedChanges = 0;
            Action<ElectronObject> reenter = _ => { if (++nestedChanges == 1) node.ProcessThreadGroup = Node.ProcessThreadGroupMode.Inherit; };
            node.PropertyListChanged += reenter; node.ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread; node.PropertyListChanged -= reenter;
            tree.ProcessFrame(.01);
            Check(nestedChanges == 2 && node.ProcessThreadGroup == Node.ProcessThreadGroupMode.Inherit && probe.Thread == owner,
                "A synchronous nested role change retains the final committed group");
            node.SetBlockSignals(true); node.PropertyListChanged += changed;
            node.ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread; node.PropertyListChanged -= changed; node.SetBlockSignals(false);
            Check(changes == count, "Signal blocking suppresses role metadata delivery without suppressing rebinding");
            tree.ProcessFrame(.01); Check(probe.Thread != owner, "Blocked metadata still leaves the requested processing owner active");
            root.RemoveChild(node); node.Dispose();
            Reject<ObjectDisposedException>(() => node.ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread);
        }
        Console.WriteLine($"Process group roles {backend?.ToString() ?? "registered"}: eight inherited branches, committed notifications, worker messages, errors, reentry and real physics/query identity passed.");
    }

    private sealed class RoleProbe : Node
    {
        internal int Thread;
        protected override void OnProcess(double delta) => Thread = Environment.CurrentManagedThreadId;
    }
}
