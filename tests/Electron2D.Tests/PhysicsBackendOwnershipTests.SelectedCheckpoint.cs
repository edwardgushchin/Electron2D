using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private sealed class WrappedCheckpoint(ViewBackend owner, PhysicsBackendCheckpoint inner) : PhysicsBackendCheckpoint
    {
        internal override long DeviceCapacityBytes => inner.DeviceCapacityBytes;
        internal override void Capture() { owner.CheckpointCaptures++; owner.CheckpointCaptureHook?.Invoke(); inner.Capture(); }
        internal override void ValidateRestore() { owner.CheckpointValidations++; owner.CheckpointValidationHook?.Invoke(); inner.ValidateRestore(); }
        internal override void Restore() { owner.CheckpointRestores++; owner.CheckpointRestoreHook?.Invoke(); inner.Restore(); }
        public override void Dispose()
        {
            owner.CheckpointReleases++;
            try { owner.CheckpointReleaseHook?.Invoke(); }
            finally { inner.Dispose(); }
        }
    }

    private static void RequireBorrowRejection(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new Exception("Expected borrowed context rejection before mutation");
    }

    private static void SelectedCheckpoint(PhysicsServer.Backend backend)
    {
        ViewBackend selected = null!;
        using var world = new World(new WorldRuntime(false, backend, factory: space => selected = new(space, backend)));
        var space = PhysicsServer.Service.GetSceneSpace(world.Space);
        var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
            PhysicsServer.BodySetSpace(body, world.Space); space.Step(1d / 60);
            var tick = PhysicsServer.SpaceGetTick(world.Space);
            selected.CheckpointPreparationHook = () =>
            {
                RequireBorrowRejection(() => PhysicsServer.SpaceCreateCheckpoint(world.Space));
                RequireBorrowRejection(() => PhysicsServer.FreeRID(world.Space));
                RequireBorrowRejection(() => PhysicsServer.BodySetSpace(body, default));
                throw new InvalidOperationException("Expected selected checkpoint preparation failure");
            };
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreateCheckpoint(world.Space));
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick && PhysicsServer.BodyGetSpace(body) == world.Space && !space.HasBackendFailure, "Rejected checkpoint preparation preserves the running world");
            selected.CheckpointPreparationHook = null;
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            var before = PhysicsServer.BodyGetTransform(body);
            void BorrowGuards()
            {
                RequireBorrowRejection(checkpoint.Dispose);
                RequireBorrowRejection(checkpoint.Capture);
                RequireBorrowRejection(() => PhysicsServer.FreeRID(world.Space));
                RequireBorrowRejection(() => space.Step(1d / 60));
            }
            selected.CheckpointCaptureHook = () => { BorrowGuards(); throw new InvalidOperationException("Expected selected capture failure"); };
            Reject<InvalidOperationException>(checkpoint.Capture);
            Reject<InvalidOperationException>(() => _ = checkpoint.Tick);
            Reject<InvalidOperationException>(checkpoint.Restore);
            selected.CheckpointCaptureHook = null; checkpoint.Capture();
            PhysicsServer.BodySetTransform(body, new(0, new(30, 0)));
            selected.CheckpointValidationHook = () => { BorrowGuards(); throw new InvalidOperationException("Expected selected validation failure"); };
            Reject<InvalidOperationException>(checkpoint.Restore);
            Check(MathF.Abs(PhysicsServer.BodyGetTransform(body).Origin.X - 30) < .001f && !space.HasBackendFailure, "Preflight rejection mutates no body and preserves world access");
            selected.CheckpointValidationHook = BorrowGuards; selected.CheckpointRestoreHook = BorrowGuards;
            checkpoint.Restore();
            Check(PhysicsServer.BodyGetTransform(body).Origin.DistanceTo(before.Origin) < .001f && selected.CheckpointPreparations == 4,
                $"Actual selected solver restores motion after recoverable prepare/capture/validation failures: before={before.Origin}, actual={PhysicsServer.BodyGetTransform(body).Origin}, preparations={selected.CheckpointPreparations}");
            selected.CheckpointValidationHook = selected.CheckpointRestoreHook = null;
            Collect();
            for (var i = 0; i < 32; i++) { checkpoint.Capture(); checkpoint.Restore(); }
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) { checkpoint.Capture(); checkpoint.Restore(); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Selected checkpoint capture/restore allocated {owner}/{all} bytes");
            selected.CheckpointReleaseHook = () => { BorrowGuards(); throw new InvalidOperationException("Expected selected release failure"); };
            Reject<InvalidOperationException>(checkpoint.Dispose);
            Check(checkpoint.IsDisposed && selected.CheckpointReleases == 1, "Failed selected release retires the common checkpoint exactly once");
            checkpoint.Dispose(); space.Step(1d / 60);
            Console.WriteLine($"{backend}: selected checkpoint phases, recoverable failures, borrowed lifetime, real restore and 64 warm capture/restore cycles: {owner}/{all} B.");
        }
        finally { PhysicsServer.FreeRID(body); }
    }
}
