using Electron2D;

public static partial class BackendChecks
{
    public static void RunPortableCapture(bool threaded = false, PhysicsServer.Backend? backend = null)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("PortableCapture", () => new Solver());
            for (var registered = 0; registered < 2; registered++)
                for (var edit = 0; edit < 12; edit++)
                {
                    using var rig = new PortableContactRig(registered == 0 ? new World(PhysicsServer.Backend.CPU) :
                        backend is { } selected ? new World(selected) : new World("PortableCapture"));
                    Check(rig.World.PhysicsBackend == (registered == 0 ? PhysicsServer.Backend.CPU : backend ?? PhysicsServer.Backend.Extension) &&
                        rig.World.PhysicsFallbackReason is null, "Topology capture uses the selected owner without startup fallback");
                    if (edit >= 10) rig.Mover.ContactMonitor = false;
                    rig.Contact();
                    Check(rig.State.GetContactCount() == 1 && rig.Character.GetSlideCollisionCount() == 1,
                        "Capture topology checks begin with actual solved contact and character slide history");
                    var snapshot = new PhysicsSnapshot(65536); rig.Map.Capture(snapshot);
                    var before = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(before); var events = rig.Events;
                    var impulse = rig.State.GetContactImpulse(0); var shape = PhysicsServer.BodyGetShape(rig.Mover.GetRID(), 1);
                    switch (edit)
                    {
                        case 0: PhysicsServer.BodyRemoveShape(rig.Mover.GetRID(), 1); break;
                        case 1: PhysicsServer.BodyRemoveShape(rig.Peer.GetRID(), 2); break;
                        case 2: PhysicsServer.BodyRemoveShape(rig.Character.GetRID(), 1); break;
                        case 3: rig.RemoveAreaShape(); break;
                        case 4: PhysicsServer.BodyClearShapes(rig.Peer.GetRID()); break;
                        case 5: Reindex(rig.Mover.GetRID()); break;
                        case 6: Reindex(rig.Peer.GetRID()); break;
                        case 7: Reindex(rig.Character.GetRID()); break;
                        case 8: rig.ReindexAreaShapes(); break;
                        case 9:
                            PhysicsServer.BodyRemoveShape(rig.Mover.GetRID(), 1);
                            PhysicsServer.BodyAddShape(rig.Mover.GetRID(), shape); break;
                        case 10: Reindex(rig.Mover.GetRID()); break;
                        case 11:
                            PhysicsServer.BodyRemoveShape(rig.Peer.GetRID(), 1);
                            PhysicsServer.BodyAddShape(rig.Peer.GetRID(), shape); break;
                    }
                    Check(rig.State.GetContactCount() == 1 && rig.State.GetContactImpulse(0) == impulse && rig.Character.GetSlideCollisionCount() == 1,
                        "Structural edits preserve sampled public contact and slide values until their ordinary history refresh");
                    Reject<InvalidOperationException>(() => rig.Map.Capture(snapshot));
                    var after = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(after);
                    Check(before.AsSpan().SequenceEqual(after) && rig.Events == events,
                        "Removing a referenced logical slot rejects capture before replacing the last complete packet or delivering history events");
                    rig.Tree.PhysicsFrame(.01); rig.Character.Velocity = Vector2.Zero; rig.Character.MoveAndSlide();
                    rig.Map.Capture(snapshot); var updated = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(updated);
                    var decoded = new PhysicsSnapshot(65536); decoded.ReadFrom(updated);
                    Check(decoded.Tick == PhysicsServer.SpaceGetTick(rig.World.Space),
                        "Capture succeeds once real simulation/movement refreshes the retired contact and slide histories");

                    void Reindex(RID body)
                    {
                        var resource = PhysicsServer.BodyGetShape(body, 1);
                        PhysicsServer.BodyRemoveShape(body, 0);
                        PhysicsServer.BodyAddShape(body, resource, new(0, new(20, 0)), disabled: true);
                    }
                }
            Console.WriteLine($"Portable capture CPU/{backend?.ToString() ?? "registered"}, threaded={threaded}: twelve real topology edits reject stale history and preserve complete packets and public samples.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
