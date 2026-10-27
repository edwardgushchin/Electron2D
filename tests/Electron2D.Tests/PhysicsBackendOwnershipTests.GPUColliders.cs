using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private static void GPUColliderLookup()
    {
        var first = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU);
        var second = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU);
        var left = PhysicsServer.Service.GetSceneSpace(first).GPUStore!;
        var right = PhysicsServer.Service.GetSceneSpace(second).GPUStore!;
        var body = PhysicsServer.BodyCreate();
        var facade = PhysicsServer.Service.BodyRuntime(body).Backend;
        try
        {
            Reject<InvalidOperationException>(() => left.NativeCollider(facade));
            PhysicsServer.BodySetSpace(body, first);
            var retired = left.NativeCollider(facade);
            Check(ReferenceEquals(retired, facade.Implementation), "Built-in lookup returns the current concrete attachment");
            Reject<InvalidOperationException>(() => right.NativeCollider(facade));
            PhysicsServer.BodySetSpace(body, second);
            Check(ReferenceEquals(right.NativeCollider(facade), facade.Implementation), "Transfer resolves only the new store's attachment");
            Reject<InvalidOperationException>(() => left.NativeCollider(facade));
            PhysicsServer.BodySetSpace(body, first);
            var current = left.NativeCollider(facade);
            retired.Detach();
            Check(!ReferenceEquals(retired, current) && ReferenceEquals(left.NativeCollider(facade), current),
                "Repeated retirement cannot replace the current attachment");
            PhysicsServer.BodySetSpace(body, default);
            Reject<InvalidOperationException>(() => left.NativeCollider(facade));
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second); }
        Reject<InvalidOperationException>(() => left.NativeCollider(facade));
        Console.WriteLine("GPU native lookup: detached, current, foreign, transfer, repeated retirement and disposed-store guards passed.");
    }
}
