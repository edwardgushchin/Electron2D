using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private static void GuardPresence(PhysicsServer.Backend backend)
    {
        var before = PhysicsColliderBackend.AttachmentCount;
        var space = PhysicsServer.SpaceCreate(backend);
        using var parent = new Node();
        var body = new RigidBody();
        using var shape = new InputShape();
        var owner = new CollisionShape { Shape = shape };
        body.AddChild(owner); parent.AddChild(body);
        var builds = 0;
        shape.BuildHook = () =>
        {
            builds++;
            Check(CollisionObject.HasSpaceGeometryBorrows && PhysicsColliderBackend.AttachmentCount == before,
                "Source geometry remains guarded before the first attachment is counted");
            Reject<InvalidOperationException>(body.Dispose);
            Reject<InvalidOperationException>(() => parent.RemoveChild(body));
            Reject<InvalidOperationException>(() => owner.Shape = null);
        };
        shape.Invalidate();
        try
        {
            PhysicsServer.BodySetSpace(body.GetRID(), space); shape.BuildHook = null;
            Check(builds > 0 && PhysicsColliderBackend.AttachmentCount == before + 1 && !CollisionObject.HasSpaceGeometryBorrows,
                "Completed attachment retains its count and releases the geometry borrow");
            PhysicsServer.BodySetSpace(body.GetRID(), default);
            Check(PhysicsColliderBackend.AttachmentCount == before, "Detach balances the hierarchy guard presence count");
            shape.BuildHook = () => throw new ApplicationException("Injected source geometry failure"); shape.Invalidate();
            Reject<ApplicationException>(() => PhysicsServer.BodySetSpace(body.GetRID(), space)); shape.BuildHook = null;
            Check(PhysicsColliderBackend.AttachmentCount == before && !CollisionObject.HasSpaceGeometryBorrows,
                "Failed preflight releases its borrow without inventing an attachment");
            PhysicsServer.BodySetSpace(body.GetRID(), space);
        }
        finally { shape.BuildHook = null; PhysicsServer.FreeRID(space); }
        Check(PhysicsColliderBackend.AttachmentCount == before, "World retirement releases the attachment count");
    }
}
