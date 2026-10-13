using Electron2D;

public static partial class BackendChecks
{
    public static void RunPortableCapture(bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            PhysicsServerManager.RegisterServer("PortableCapture", () => new Solver());
            for (var registered = 0; registered < 2; registered++)
                for (var edit = 0; edit < 5; edit++)
                {
                    using var rig = new PortableContactRig(registered == 0 ? new World(PhysicsServer.Backend.CPU) : new World("PortableCapture"));
                    rig.Contact();
                    Check(rig.State.GetContactCount() == 1 && rig.Character.GetSlideCollisionCount() == 1,
                        "Capture topology checks begin with actual solved contact and character slide history");
                    var snapshot = new PhysicsSnapshot(65536); rig.Map.Capture(snapshot);
                    var before = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(before); var events = rig.Events;
                    switch (edit)
                    {
                        case 0: PhysicsServer.BodyRemoveShape(rig.Mover.GetRID(), 1); break;
                        case 1: PhysicsServer.BodyRemoveShape(rig.Peer.GetRID(), 2); break;
                        case 2: PhysicsServer.BodyRemoveShape(rig.Character.GetRID(), 1); break;
                        case 3: rig.RemoveAreaShape(); break;
                        case 4: PhysicsServer.BodyClearShapes(rig.Peer.GetRID()); break;
                    }
                    Reject<InvalidOperationException>(() => rig.Map.Capture(snapshot));
                    var after = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(after);
                    Check(before.AsSpan().SequenceEqual(after) && rig.Events == events,
                        "Removing a referenced logical slot rejects capture before replacing the last complete packet or delivering history events");
                    rig.Tree.PhysicsFrame(.01); rig.Character.Velocity = Vector2.Zero; rig.Character.MoveAndSlide();
                    rig.Map.Capture(snapshot); var updated = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(updated);
                    var decoded = new PhysicsSnapshot(65536); decoded.ReadFrom(updated);
                    Check(decoded.Tick == PhysicsServer.SpaceGetTick(rig.World.Space),
                        "Capture succeeds once real simulation/movement refreshes the retired contact and slide histories");
                }
            Console.WriteLine($"Portable capture CPU/registered, threaded={threaded}: five real topology edits reject stale history and preserve complete packets.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
