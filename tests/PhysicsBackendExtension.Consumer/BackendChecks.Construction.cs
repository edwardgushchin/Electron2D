using Electron2D;

public static partial class BackendChecks
{
    public static void RunConstruction(PhysicsServer.Backend? backend = null)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("Construction", () => solver = new());
        var space = backend is { } selected ? PhysicsServer.SpaceCreate(selected) : PhysicsServer.SpaceCreate("Construction");
        var raw = PhysicsServer.BodyCreate(); var other = PhysicsServer.BodyCreate();
        using var circle = new CircleShape { Radius = 3 };
        try
        {
            Check(raw != other && PhysicsServer.BodyGetSpace(raw) == default && PhysicsServer.BodyGetMode(raw) == PhysicsServer.BodyMode.Rigid &&
                PhysicsServer.BodyGetContinuousCollisionDetectionMode(raw) == CCDMode.Disabled, "Body construction assigns independent unbound identities and configured defaults");
            Check(PhysicsServer.SpaceGetBackend(space) == (backend ?? PhysicsServer.Backend.Extension) && !PhysicsServer.SpaceIsActive(space),
                "Explicit space creation binds the selected implementation but starts inactive");
            PhysicsServer.BodyAddShape(raw, circle.GetRID());
            PhysicsServer.BodySetMass(raw, 4); PhysicsServer.BodySetInertia(raw, 7); PhysicsServer.BodySetCenterOfMass(raw, new(1, 2));
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            foreach (var mode in Enum.GetValues<PhysicsServer.BodyMode>())
            {
                PhysicsServer.BodySetMode(raw, mode);
                foreach (var ccd in Enum.GetValues<CCDMode>())
                {
                    PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, ccd);
                    Check(PhysicsServer.BodyGetMode(raw) == mode && PhysicsServer.BodyGetContinuousCollisionDetectionMode(raw) == ccd,
                        "Detached role and per-body continuous policy retain every applicable value");
                }
            }
            PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodySetSpace(raw, space); PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            PhysicsServer.BodyResetMassProperties(raw);
            var view = PhysicsServer.BodyGetDirectState(raw)!;
            Check(PhysicsServer.BodyGetMass(raw) == 4 && MathF.Abs(PhysicsServer.BodyGetInertia(raw) - 18) < .0001f &&
                PhysicsServer.BodyGetCenterOfMass(raw) == Vector2.Zero && MathF.Abs(view.InverseMass - .25f) < .0001f,
                "Mass reset preserves configured kilograms and recomputes circle inertia/local center through the selected owner");
            if (solver is not null)
            {
                solver.BodyHook = _ =>
                {
                    var mode = PhysicsServer.BodyGetMode(raw); var ccd = PhysicsServer.BodyGetContinuousCollisionDetectionMode(raw);
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetMode(raw, mode));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, ccd));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodyResetMassProperties(raw));
                    Check(PhysicsServer.BodyGetMode(raw) == mode && PhysicsServer.BodyGetContinuousCollisionDetectionMode(raw) == ccd,
                        "Numerical borrowing rejects equal role/CCD and mass-reset writes while keeping authored reads");
                };
                try { PhysicsServer.BodySetLinearVelocity(raw, Vector2.Right); }
                finally { solver.BodyHook = null; }
            }
            foreach (var ccd in Enum.GetValues<CCDMode>())
            {
                PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, ccd); PhysicsServer.SpaceStep(space, 1d / 120);
                Check(PhysicsServer.BodyGetContinuousCollisionDetectionMode(raw) == ccd && PhysicsServer.BodyGetContinuousCollisionDetectionMode(other) == CCDMode.Disabled,
                    "Live continuous policy remains per-body and survives publication");
                if (solver is not null) Check(solver.Bodies[raw].ContinuousMode == ccd, "Selected integration receives exact configured CCD policy");
            }
            PhysicsServer.BodySetSleeping(raw, true);
            var stored = PhysicsServer.BodyGetContinuousCollisionDetectionMode(raw);
            PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, stored);
            Check(PhysicsServer.BodyGetSleeping(raw), "Equal continuous-policy writes preserve sleep");
            PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, stored == CCDMode.Disabled ? CCDMode.CastRay : CCDMode.Disabled);
            Check(!PhysicsServer.BodyGetSleeping(raw), "Changed continuous policy wakes dynamic state");
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetMode(raw, (PhysicsServer.BodyMode)99));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, (CCDMode)99));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, (CCDMode)(-1)));
            Task.Run(() =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Rigid));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, CCDMode.Disabled));
                Reject<InvalidOperationException>(() => PhysicsServer.BodyResetMassProperties(raw));
            }).GetAwaiter().GetResult();
            var currentView = PhysicsServer.BodyGetDirectState(raw)!;
            PhysicsServer.BodySetSpace(raw, default); Reject<ObjectDisposedException>(() => _ = currentView.LinearVelocity);
            Check(PhysicsServer.BodyGetMode(raw) == PhysicsServer.BodyMode.Rigid && PhysicsServer.BodyGetContinuousCollisionDetectionMode(raw) == CCDMode.Disabled,
                "Detachment preserves authored role and continuous policy while retiring attachment views");
            PhysicsServer.BodySetSpace(raw, space);
            for (var i = 0; i < 256; i++) Frame(i);
            Collect(); GC.WaitForPendingFinalizers(); _ = GC.GetTotalAllocatedBytes(true);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(i);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Construction policy full frames allocated {owner}/{all} B");
            Console.WriteLine($"Construction {backend?.ToString() ?? "registered"}: identities, modes/CCD, mass reset, lifetime and 128 changing-CCD full steps {owner}/{all} B passed.");
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(other); PhysicsServer.FreeRID(space); }
        ServerModeChecks(backend);

        void Frame(int i)
        {
            PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, (CCDMode)(i % 3));
            PhysicsServer.BodySetLinearVelocity(raw, Vector2.Right);
            PhysicsServer.SpaceStep(space, 1d / 120);
        }
    }
}
