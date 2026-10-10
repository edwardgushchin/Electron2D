using System.Diagnostics;
using Electron2D;
using static Box2D.NET.B2Worlds;

internal static class PhysicsBackendOwnershipTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
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
