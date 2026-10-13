using Electron2D;

public static partial class BackendChecks
{
    public static void RunPortableGeometry(bool threaded = false, PhysicsServer.Backend? backend = null)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            if (backend is null) PhysicsServerManager.RegisterServer("PortableGeometry", () => new Solver());
            for (var direction = 0; direction < 2; direction++)
                for (var edit = 0; edit < 7; edit++)
                {
                    using var source = new PortableContactRig(direction == 0 ? new World(PhysicsServer.Backend.CPU) : PartnerWorld());
                    using var target = new PortableContactRig(direction == 0 ? PartnerWorld() : new World(PhysicsServer.Backend.CPU));
                    var partner = backend ?? PhysicsServer.Backend.Extension;
                    Check(source.World.PhysicsBackend == (direction == 0 ? PhysicsServer.Backend.CPU : partner) &&
                        target.World.PhysicsBackend == (direction == 0 ? partner : PhysicsServer.Backend.CPU) &&
                        source.World.PhysicsFallbackReason is null && target.World.PhysicsFallbackReason is null,
                        "Authoring validation uses the selected numerical owners without startup fallback");
                    using var circle = new CircleShape { Radius = 1.5f };
                    using var rectangle = new RectangleShape { Size = new(2, 2) };
                    source.Contact(); target.Separate();
                    Check(source.State.GetContactCount() == 1 && source.Character.GetSlideCollisionCount() == 1,
                        "Geometry validation begins with actual solved contact and character slide history");
                    var sent = new PhysicsSnapshot(65536); source.Map.Capture(sent);
                    var packet = Encode(sent); var received = new PhysicsSnapshot(65536); received.ReadFrom(packet);
                    var mover = target.Mover.GetRID(); var peer = target.Peer.GetRID(); var character = target.Character.GetRID();
                    var shape = PhysicsServer.BodyGetShape(mover, 1); var pose = PhysicsServer.BodyGetShapeTransform(character, 1);
                    Check(shape != PhysicsServer.BodyGetShape(source.Mover.GetRID(), 1),
                        "Compatible geometry uses distinct process-local resources");
                    Change(true);
                    Check(PhysicsServer.BodyGetShapeCount(mover) == 2 && PhysicsServer.BodyGetShapeCount(peer) == 3 && PhysicsServer.BodyGetShapeCount(character) == 2,
                        "The incompatible profiles retain their original logical body shape counts");
                    var before = new PhysicsSnapshot(65536); target.Map.Capture(before); var bytes = Encode(before); var events = target.Events;
                    Reject<System.IO.InvalidDataException>(() => target.Map.Apply(received));
                    var after = new PhysicsSnapshot(65536); target.Map.Capture(after);
                    Check(bytes.AsSpan().SequenceEqual(Encode(after)) && target.Events == events && packet.AsSpan().SequenceEqual(Encode(received)),
                        "Same-count authored geometry mismatch rejects before changing complete state, history events or the reusable input packet");
                    Change(false); events = target.Events;
                    try { target.Map.Apply(received); }
                    catch (System.IO.InvalidDataException error) { throw new InvalidOperationException($"Restored direction={direction}, edit={edit} authoring must accept the unchanged packet", error); }
                    Check(target.Events == events && PhysicsServer.SpaceGetTick(target.World.Space) == received.Tick &&
                        target.State.GetContactCount() == source.State.GetContactCount() && target.State.GetContactCollider(0) == peer &&
                        target.State.GetContactLocalShape(0) == 1 && target.State.GetContactColliderShape(0) == 1 &&
                        target.State.GetContactImpulse(0) == source.State.GetContactImpulse(0) && target.Character.GetSlideCollisionCount() == 1,
                        "Restoring compatible authoring allows the unchanged packet to import real remapped contact and slide history silently");
                    target.VerifyMoverQuery(); target.VerifyCharacterQuery();
                    var enters = target.MoverEnters; var overlaps = target.AreaEnters; target.Tree.PhysicsFrame(.01);
                    Check(target.MoverEnters == enters && target.AreaEnters == overlaps,
                        "A restored geometry profile continues real contact history without duplicate enters");
                    target.Separate(); Check(target.MoverExits > 0 && target.AreaExits > 0,
                        "Corrected contact and Area histories retain ordinary exits after separation");

                    void Change(bool mismatch)
                    {
                        switch (edit)
                        {
                            case 0: PhysicsServer.BodySetShape(mover, 1, mismatch ? circle.GetRID() : shape); break;
                            case 1: PhysicsServer.BodySetShape(peer, 0, mismatch ? rectangle.GetRID() : shape); break;
                            case 2: PhysicsServer.BodySetShapeTransform(character, 1, mismatch ? new(0, new(3, 0)) : pose); break;
                            case 3: PhysicsServer.BodySetShapeDisabled(peer, 2, mismatch); break;
                            case 4:
                                if (mismatch) PhysicsServer.BodyRemoveShape(mover, 0);
                                else
                                {
                                    PhysicsServer.BodyClearShapes(mover);
                                    PhysicsServer.BodyAddShape(mover, shape, new(0, new(20, 0)), disabled: true);
                                }
                                PhysicsServer.BodyAddShape(mover, shape, mismatch ? new(0, new(20, 0)) : Transform.Identity, disabled: mismatch);
                                if (!mismatch)
                                    for (var slot = 0; slot < 2; slot++) PhysicsServer.BodySetShapeAsOneWayCollision(mover, slot, false, 1);
                                break;
                            case 5: target.SetCircleBias(mismatch ? .25f : 0); break;
                            case 6: target.SetAreaShape(mismatch ? circle.GetRID() : default); break;
                        }
                    }
                }
            Console.WriteLine($"Portable geometry CPU/{backend?.ToString() ?? "registered"}, threaded={threaded}: seven same-count authoring guards, unchanged packet recovery and real contact continuation in both directions passed.");
            World PartnerWorld() => backend is { } selected ? new World(selected) : new World("PortableGeometry");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        static byte[] Encode(PhysicsSnapshot snapshot)
        { var bytes = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(bytes); return bytes; }
    }
}
