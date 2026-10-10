using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private static void Lifecycle(PhysicsServer.Backend backend)
    {
        ViewBackend selected = null!;
        var source = PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, factory: space =>
        {
            selected = new(space, backend);
            selected.InitializeHook = () =>
            {
                Check(PhysicsServer.SpaceGetBackend(space.RID) == backend && !space.IsActive, "Selected initialization sees its registered inactive context");
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(space.RID, true));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(space.RID));
            };
            return selected;
        });
        using var shape = new CircleShape { Radius = 2 };
        using var sensorShape = new CircleShape { Radius = 100 };
        var body = PhysicsServer.BodyCreate(); var nested = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        var guards = true; var callbacks = 0; var nestedDelivered = false; var removedDelivered = false;
        var observerFailure = new InvalidOperationException("Expected observer failure");
        var failObserver = false;
        try
        {
            Check(selected.Initializations == 1 && !selected.IsActive, "Initialization executes once before publication");
            selected.ActiveHook = active =>
            {
                Check(selected.IsActive != active && PhysicsServer.SpaceIsActive(source.RID) == selected.IsActive, "Activation commits only after selected work succeeds");
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(source.RID, active));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(source.RID, 1d / 60));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(source.RID));
            };
            foreach (var rid in new[] { body, nested })
            {
                PhysicsServer.BodyAddShape(rid, shape.GetRID()); PhysicsServer.BodySetCanSleep(rid, false);
                PhysicsServer.BodySetSpace(rid, source.RID);
            }
            PhysicsServer.BodySetTransform(nested, new(0, new(20, 0)));
            PhysicsServer.AreaAddShape(area, sensorShape.GetRID()); PhysicsServer.AreaSetSpace(area, source.RID);
            PhysicsServer.BodySetStateSyncCallback(body, _ =>
            {
                callbacks++;
                Check(selected.IsSynchronizing && !selected.IsFlushingQueries, "Solved body callbacks precede query/event flushing");
            });
            PhysicsServer.AreaSetMonitorCallback(area, (status, rid, _, _, _) =>
            {
                Check(selected.IsFlushingQueries, "Observer delivery runs inside the selected flush phase");
                if (status == PhysicsServer.AreaBodyStatus.Added && rid == nested)
                {
                    PhysicsServer.BodySetSpace(nested, default);
                    Check(selected.IsFlushingQueries, "Nested detach flushing preserves the outer phase");
                    nestedDelivered = true;
                }
                if (status == PhysicsServer.AreaBodyStatus.Removed && rid == body)
                {
                    removedDelivered = true;
                    if (failObserver) throw observerFailure;
                }
            });
            selected.FlushHook = () =>
            {
                Check(selected.IsFlushingQueries, "Selected flush sees its active phase");
                if (!guards) return;
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(source.RID, 0));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreateCheckpoint(source.RID));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(source.RID));
            };
            selected.EndSyncHook = _ =>
            {
                Check(!selected.IsFlushingQueries, "End-sync follows all observer delivery");
                if (!guards) return;
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(source.RID, 0));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(source.RID));
            };
            PhysicsServer.SpaceSetActive(source.RID, true);
            var activations = selected.Activations; PhysicsServer.SpaceSetActive(source.RID, true);
            Check(selected.Activations == activations, "Unchanged activation does not reinvoke implementation work");
            PhysicsServer.SpaceStep(source.RID, 1d / 60);
            Check(source.Tick == 1 && callbacks == 1 && nestedDelivered && selected.Flushes >= 2 && selected.EndSynchronizations == 1,
                "Real solving, callbacks, nested Area delivery and finalization execute in order");
            Check(!selected.IsSynchronizing && !selected.IsFlushingQueries, "Completed phases retire their borrowed state");
            selected.ActiveHook = _ => throw new InvalidOperationException("Expected activation failure");
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(source.RID, false));
            Check(PhysicsServer.SpaceIsActive(source.RID), "Failed activation preserves the previous policy");
            selected.ActiveHook = null;
            PhysicsServer.SpaceSetActive(source.RID, false);
            var ended = selected.EndSynchronizations; var flushed = selected.Flushes;
            PhysicsServer.SpaceStep(source.RID, 1d / 60);
            Check(source.Tick == 1 && selected.EndSynchronizations == ended && selected.Flushes == flushed, "Inactive world executes no solver or publication phases");
            PhysicsServer.SpaceSetActive(source.RID, true);
            var flushFailure = new InvalidOperationException("Expected selected flush failure");
            var endFailure = new InvalidOperationException("Expected selected end-sync failure");
            selected.FlushHook = () => throw flushFailure;
            selected.EndSyncHook = failure => { Check(failure is not null, "End-sync receives earlier publication failure"); throw endFailure; };
            failObserver = true;
            PhysicsServer.AreaSetTransform(area, new(0, new(1000, 0)));
            try { PhysicsServer.SpaceStep(source.RID, 1d / 60); throw new InvalidOperationException("Expected aggregate failure"); }
            catch (AggregateException error)
            {
                var errors = error.Flatten().InnerExceptions;
                Check(errors.Contains(flushFailure) && errors.Contains(observerFailure) && errors.Contains(endFailure),
                    "Selected flush, mandatory observer and finalization failures all survive aggregation");
            }
            Check(removedDelivered && source.Tick == 2 && !selected.IsSynchronizing && !selected.IsFlushingQueries,
                "Failed owner flush still delivers observers and retires phases without replaying the solved tick");
            selected.FlushHook = null; selected.EndSyncHook = null; failObserver = false; guards = false;
            Collect();
            for (var i = 0; i < 32; i++) PhysicsServer.SpaceStep(source.RID, 1d / 60);
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(source.RID, 1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Lifecycle dispatch allocated {owner}/{all} owner/all-thread bytes");
            Console.WriteLine($"{backend}: selected init/activation/sync/flush/end-sync, nested observers, failure aggregation and 64 warm full intervals: {owner}/{all} B.");
        }
        finally
        {
            selected.FlushHook = null; selected.EndSyncHook = null;
            PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(nested); PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(source.RID);
        }
        Reject<ObjectDisposedException>(selected.EnsureAccess);
        ViewBackend failed = null!; RID rejected = default;
        Reject<InvalidOperationException>(() => PhysicsServer.Service.CreateRegisteredSpace(backend, false, true, factory: space =>
        {
            rejected = space.RID; failed = new(space, backend);
            failed.InitializeHook = () => throw new InvalidOperationException("Expected selected initialization failure");
            return failed;
        }));
        Check(failed.Initializations == 1, "Failed initialization is not repeated");
        Reject<ArgumentException>(() => PhysicsServer.SpaceGetBackend(rejected));
        Reject<ObjectDisposedException>(failed.EnsureAccess);
        ViewBackend emptyOwner = null!;
        var empty = PhysicsServer.Service.CreateRegisteredSpace(backend, false, true,
            factory: space => emptyOwner = new(space, backend));
        try
        {
            PhysicsServer.SpaceSetActive(empty.RID, true); PhysicsServer.SpaceStep(empty.RID, 1d / 60);
            Check(empty.Tick == 1 && emptyOwner.Initializations == 1 && emptyOwner.Flushes == 1 && emptyOwner.EndSynchronizations == 1 &&
                !emptyOwner.IsSynchronizing && !emptyOwner.IsFlushingQueries,
                "Empty active worlds execute the actual selected interval and lifecycle once");
            PhysicsServer.SpaceStep(empty.RID, 0); PhysicsServer.SpaceSetActive(empty.RID, false); PhysicsServer.SpaceStep(empty.RID, 1d / 60);
            Check(empty.Tick == 1 && emptyOwner.Flushes == 1 && emptyOwner.EndSynchronizations == 1,
                "Zero-time and inactive empty worlds preserve their completed lifecycle");
        }
        finally { PhysicsServer.FreeRID(empty.RID); }
    }
}
