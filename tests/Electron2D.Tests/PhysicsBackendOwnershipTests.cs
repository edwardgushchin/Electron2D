using System.Diagnostics;
using Electron2D;
using static Box2D.NET.B2Worlds;

internal static class PhysicsBackendOwnershipTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        ColliderAttachment(backend);
        QueryOwnership(backend);
        CallbackBorrow(backend);
        var first = PhysicsServer.SpaceCreate(backend); var second = PhysicsServer.SpaceCreate(backend);
        var left = PhysicsServer.Service.GetSceneSpace(first); var right = PhysicsServer.Service.GetSceneSpace(second);
        var implementation = left.BackendImplementation;
        var cpuID = backend == PhysicsServer.Backend.CPU ? left.WorldID : default;
        var gpuStore = left.GPUStore;
        try
        {
            Check(!ReferenceEquals(implementation, right.BackendImplementation) && implementation.Kind == backend &&
                implementation.Requested == backend && implementation.FallbackReason is null, "Fresh immutable per-world solver ownership");
            if (backend == PhysicsServer.Backend.CPU)
            {
                Check(implementation is CPUPhysicsWorldBackend && left.GPUStore is null && left.WorldID.index1 != right.WorldID.index1,
                    "CPU implementations own distinct native worlds and no resident GPU store");
            }
            else
            {
                Check(implementation is GPUPhysicsWorldBackend && !ReferenceEquals(left.GPUStore, right.GPUStore), "GPU implementations own distinct resident stores");
                Reject<InvalidOperationException>(() => _ = left.WorldID);
                Reject<InvalidOperationException>(() => left.EnableGPUSolver());
            }
            using var shape = new CircleShape { Radius = 3 };
            const int count = 512;
            var bodies = new RID[count];
            try
            {
                PhysicsServer.SpaceSetActive(first, true);
                for (var i = 0; i < count; i++)
                {
                    var body = bodies[i] = PhysicsServer.BodyCreate();
                    PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetCanSleep(body, false);
                    PhysicsServer.BodySetTransform(body, new(0, new((i % 32) * 40, (i / 32) * 40)));
                    PhysicsServer.BodySetLinearVelocity(body, new(20, 0)); PhysicsServer.BodySetSpace(body, first);
                }
                Collect();
                for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(first, 1d / 60);
                var before = PhysicsServer.BodyGetTransform(bodies[0]).Origin;
                var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(first, 1d / 60);
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"World dispatch allocated {owner}/{all} owner/all-thread bytes");
                Check(left.Tick == 128 && right.Tick == 0 && PhysicsServer.BodyGetTransform(bodies[0]).Origin.X > before.X + 15,
                    "Complete selected solver step advances only its own world and publishes real motion");
                using var query = new PhysicsRayQueryParameters { From = new(-10, before.Y), To = new(200, before.Y), HitFromInside = true };
                _ = PhysicsServer.SpaceGetDirectState(first).IntersectRay(query);
                Console.WriteLine($"{backend}: selected implementation, 512 bodies/64 full warmed steps: {owner}/{all} owner/all-thread B; independent ticks and native lifetime passed.");
            }
            finally { foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); }
        }
        finally { PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second); }
        Reject<ObjectDisposedException>(implementation.EnsureAccess);
        if (backend == PhysicsServer.Backend.CPU) Check(!b2World_IsValid(cpuID), "CPU solver world released by its implementation");
        else Reject<ObjectDisposedException>(() => gpuStore!.Read([], []));
        implementation.Dispose();
        if (backend == PhysicsServer.Backend.CPU) CleanupFailure();
    }
    private static void ColliderAttachment(PhysicsServer.Backend backend)
    {
        using var geometry = new CircleShape { Radius = 5 };
        using var association = new Node();
        using var point = new PhysicsPointQueryParameters { Position = new(10, 20), CollideWithAreas = true };
        var first = PhysicsServer.SpaceCreate(backend); var second = PhysicsServer.SpaceCreate();
        var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            var retained = PhysicsServer.Service.BodyRuntime(body).Backend;
            var initialVersion = retained.AttachmentVersion;
            Reject<ArgumentOutOfRangeException>(() => retained.Attach(PhysicsServer.Service.GetSceneSpace(first), default, 0,
                new((PhysicsServer.BodyMode)999)));
            Check(retained.Space is null && retained.Implementation is null && retained.AttachmentVersion == initialVersion,
                "Rejected native creation leaves the retained collider detached and reusable");
            PhysicsServer.BodyAddShape(body, geometry.GetRID()); PhysicsServer.AreaAddShape(area, geometry.GetRID());
            PhysicsServer.BodySetTransform(body, new(0, point.Position)); PhysicsServer.AreaSetTransform(area, new(0, point.Position));
            PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetInertia(body, 20);
            PhysicsServer.BodySetLinearVelocity(body, new(3, 4)); PhysicsServer.BodySetAngularVelocity(body, .25f);
            PhysicsServer.BodySetConstantForce(body, new(5, 6)); PhysicsServer.BodyAttachObject(body, association);
            PhysicsColliderImplementation? previous = null;
            PhysicsDirectBodyState? stale = null;
            for (var i = 0; i < 4; i++)
            {
                var target = i % 2 == 0 ? first : second;
                PhysicsServer.BodySetSpace(body, target); PhysicsServer.AreaSetSpace(area, target);
                Check(retained.Implementation is not null && !ReferenceEquals(previous, retained.Implementation) && retained.AttachmentVersion == initialVersion + i + 1,
                    "Every attachment comes from its selected world and advances the retained identity epoch");
                if (previous is CPUPhysicsColliderImplementation cpu) Check(cpu.BodyID.index1 == 0, "Retired CPU attachment clears its borrowed native ID");
                if (previous is GPUPhysicsColliderImplementation gpu) Check(gpu.GPUHandle.Generation == 0, "Retired GPU attachment clears its resident handle");
                if (stale is not null) Reject<InvalidOperationException>(() => _ = stale.Transform);
                var view = PhysicsServer.BodyGetDirectState(body)!;
                Check(view.Transform.Origin.IsEqualApprox(point.Position) && view.LinearVelocity.IsEqualApprox(new(3, 4)) && MathF.Abs(view.AngularVelocity - .25f) < .0001f &&
                    MathF.Abs(view.InverseMass - .5f) < .0001f && view.GetConstantForce() == new Vector2(5, 6),
                    "Attachment transfer preserves scene-unit motion, mass and authored force within .0001 rounding tolerance");
                var hits = PhysicsServer.SpaceGetDirectState(target).IntersectPoint(point);
                Check(hits.Length == 2 && hits[0].ColliderRID == body && hits[0].ColliderObject == association && hits[1].ColliderRID == area,
                    "Body/Area geometry and sampled object association use the selected attachment with stable public RIDs");
                previous = retained.Implementation; stale = view;
            }
            PhysicsServer.BodySetSpace(body, default); PhysicsServer.AreaSetSpace(area, default);
            Check(retained.Space is null && retained.Implementation is null && retained.ShapeCount == 0 && retained.RID == body,
                "Detachment retires concrete storage while retaining common resource identity");
            Console.WriteLine($"{backend}: body/Area attachment creation failure, four world transfers, public state/identity and native retirement passed.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second); }
    }
    private static void QueryOwnership(PhysicsServer.Backend backend)
    {
        using var geometry = new CircleShape { Radius = 5 };
        using var queryGeometry = new CircleShape { Radius = 2 };
        using var ray = PhysicsRayQueryParameters.Create(new(-20, 0), new(20, 0));
        using var point = new PhysicsPointQueryParameters();
        using var shape = new PhysicsShapeQueryParameters { Shape = queryGeometry };
        using var sweep = new PhysicsShapeQueryParameters { Shape = queryGeometry, Transform = new(0, new(-20, 0)), Motion = new(40, 0) };
        var points = new PhysicsPointResult[2]; var shapes = new PhysicsShapeResult[2]; var contacts = new Vector2[5];
        contacts[^1] = new(999, 999);
        var first = PhysicsServer.SpaceCreate(backend); var second = PhysicsServer.SpaceCreate(backend);
        var body = PhysicsServer.BodyCreate(); var other = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetMode(other, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(body, geometry.GetRID()); PhysicsServer.BodyAddShape(other, geometry.GetRID());
            PhysicsServer.BodySetTransform(other, new(0, new(100, 0)));
            PhysicsServer.BodySetSpace(body, first); PhysicsServer.BodySetSpace(other, second);
            var direct = PhysicsServer.SpaceGetDirectState(first); var separate = PhysicsServer.SpaceGetDirectState(second);
            Check(direct.IntersectPoint(point).Length == 1 && direct.IntersectShape(shape).Length == 1 && direct.CollideShape(shape).Length > 0,
                "Copied results execute the selected query family");
            Evaluate(direct, true); Evaluate(separate, false);
            Task.Run(() => GuardAll<InvalidOperationException>(direct)).GetAwaiter().GetResult();
            if (backend == PhysicsServer.Backend.CPU)
            {
                var world = b2GetWorldFromId(PhysicsServer.Service.GetSceneSpace(first).WorldID);
                world.locked = true;
                try { GuardAll<InvalidOperationException>(direct); }
                finally { world.locked = false; }
            }
            Collect();
            for (var i = 0; i < 64; i++) { Evaluate(direct, true); Evaluate(separate, false); }
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { Evaluate(direct, true); Evaluate(separate, false); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Complete query dispatch allocated {owner}/{all} owner/all-thread bytes");
            ray.Exclude = [body]; point.Exclude = [body]; shape.Exclude = [body]; sweep.Exclude = [body];
            Evaluate(direct, false);
            ray.Exclude = []; point.Exclude = []; shape.Exclude = []; sweep.Exclude = [];
            Evaluate(direct, true);
            direct.Dispose(); GuardAll<ObjectDisposedException>(direct);
            var reopened = PhysicsServer.SpaceGetDirectState(first);
            Check(!ReferenceEquals(direct, reopened), "Disposed view reopens on the same selected world");
            Evaluate(reopened, true);
            if (backend == PhysicsServer.Backend.GPU)
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid);
                PhysicsServer.SpaceSetActive(first, true);
                Reject<AggregateException>(() => PhysicsServer.SpaceStep(first, float.MaxValue));
                GuardAll<InvalidOperationException>(reopened);
            }
            PhysicsServer.FreeRID(first); first = default;
            GuardAll<ArgumentException>(reopened);
            Evaluate(separate, false);
            Console.WriteLine($"{backend}: six query families, alternating hit/miss worlds, all overload guards and 64 warmed cycles: {owner}/{all} owner/all-thread B passed.");

            void Evaluate(PhysicsDirectSpaceState view, bool hit)
            {
                var rayHit = view.IntersectRay(ray);
                Check(hit ? rayHit is { ColliderRID: var rid } && rid == body && MathF.Abs(rayHit.Value.Position.X + 5) < .05f : rayHit is null,
                    "Ray uses its own world and preserves scene-unit geometry within .05 units");
                var pointCount = view.IntersectPoint(point, points); var shapeCount = view.IntersectShape(shape, shapes);
                Check(pointCount == (hit ? 1 : 0) && shapeCount == pointCount && (!hit || points[0].ColliderRID == body && shapes[0].ColliderRID == body),
                    "Point and shape dispatch preserve physical RID identity");
                var pairCount = view.CollideShape(shape, contacts);
                Check((pairCount > 0) == hit && contacts[^1] == new Vector2(999, 999), "Contact pairs preserve unused odd storage");
                var rest = view.GetRestInfo(shape);
                Check(hit ? rest is { ColliderRID: var restRID } && restRID == body && rest.Value.Point.IsFinite() : rest is null,
                    "Rest information uses the selected geometry");
                var fractions = view.CastMotion(sweep);
                Check(hit ? fractions.SafeFraction is > .2f and < .5f && fractions.UnsafeFraction >= fractions.SafeFraction : fractions == (1f, 1f),
                    "Motion fractions bracket the new collision or preserve the no-hit sentinel");
            }
            void GuardAll<T>(PhysicsDirectSpaceState view) where T : Exception
            {
                Reject<T>(() => view.IntersectRay(ray));
                Reject<T>(() => view.IntersectPoint(point)); Reject<T>(() => view.IntersectPoint(point, points));
                Reject<T>(() => view.IntersectPoint(point, Span<PhysicsPointResult>.Empty));
                Reject<T>(() => view.IntersectShape(shape)); Reject<T>(() => view.IntersectShape(shape, shapes));
                Reject<T>(() => view.IntersectShape(shape, Span<PhysicsShapeResult>.Empty));
                Reject<T>(() => view.CastMotion(sweep));
                Reject<T>(() => view.CollideShape(shape)); Reject<T>(() => view.CollideShape(shape, contacts));
                Reject<T>(() => view.CollideShape(shape, Span<Vector2>.Empty));
                Reject<T>(() => view.GetRestInfo(shape));
            }
        }
        finally
        {
            PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(other);
            if (first.IsValid()) PhysicsServer.FreeRID(first);
            PhysicsServer.FreeRID(second);
        }
    }
    private static void CallbackBorrow(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 5 };
        var space = PhysicsServer.SpaceCreate(backend); var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        var calls = 0;
        try
        {
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.AreaAddShape(area, shape.GetRID());
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.AreaSetSpace(area, space);
            PhysicsServer.AreaSetMonitorCallback(area, (_, _, _, _, _) =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(space));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
                Check(PhysicsServer.SpaceGetBackend(space) == backend, "Rejected callback release preserves the registry and implementation");
                calls++;
            });
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(calls > 0, "A real monitor callback exercises the borrowed world guard");
            PhysicsServer.AreaSetMonitorCallback(area, null);
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(space); }
    }
    private static void CleanupFailure()
    {
        var rid = PhysicsServer.SpaceCreate(); var space = PhysicsServer.Service.GetSceneSpace(rid); var id = space.WorldID;
        var bodies = new RID[512];
        try
        {
            PhysicsServer.SpaceSetActive(rid, true);
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodySetCanSleep(body, false); PhysicsServer.BodySetSpace(body, rid);
            }
            PhysicsServer.SpaceStep(rid, 1d / 60);
            foreach (var body in bodies) PhysicsServer.FreeRID(body);
            Array.Clear(bodies);
            space.BackendImplementation.Tasks.Dispose();
            Reject<AggregateException>(() => PhysicsServer.FreeRID(rid));
            Check(!b2World_IsValid(id), "Worker cleanup failure cannot skip native world destruction");
            Reject<ArgumentException>(() => PhysicsServer.SpaceGetBackend(rid));
            Reject<ObjectDisposedException>(space.BackendImplementation.EnsureAccess);
            Console.WriteLine("Injected worker cleanup failure is aggregated after native world release.");
        }
        finally
        {
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            if (b2World_IsValid(id)) PhysicsServer.FreeRID(rid);
        }
    }
    internal static void RunNoDevice()
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(typeof(PhysicsBackendOwnershipTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_BACKEND_OWNERSHIP"] = "no-device-child";
        start.Environment["SDL_GPU_DRIVER"] = "electron2d-no-such-driver"; start.Environment["SDL_VIDEODRIVER"] = "dummy";
        start.Environment["XDG_SESSION_TYPE"] = "unspecified";
        start.Environment.Remove("DISPLAY"); start.Environment.Remove("WAYLAND_DISPLAY");
        using var child = Process.Start(start)!;
        var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(60_000)) { child.Kill(true); throw new InvalidOperationException("Backend ownership no-device child timed out."); }
        Check(child.ExitCode == 0, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
        Console.Write(stdout.GetAwaiter().GetResult());
    }
    internal static void RunNoDeviceChild()
    {
        Run(PhysicsServer.Backend.CPU);
        Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU));
        var fallback = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU, allowCPUFallback: true);
        try
        {
            var space = PhysicsServer.Service.GetSceneSpace(fallback);
            Check(space.BackendImplementation is CPUPhysicsWorldBackend && space.RequestedBackend == PhysicsServer.Backend.GPU &&
                space.ActualBackend == PhysicsServer.Backend.CPU && !string.IsNullOrWhiteSpace(space.BackendFallbackReason), "Startup fallback owns an actual CPU implementation with retained request diagnostics");
        }
        finally { PhysicsServer.FreeRID(fallback); }
        Console.WriteLine("Fresh CPU implementation, strict unavailable GPU and explicit startup fallback passed without a graphics device.");
    }
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
