using System.Diagnostics;
using System.Text.Json;
using Electron2D;

internal static partial class PhysicsSandboxTests
{
    private static void RunServerSmashProfile()
    {
        const int count = 65536, warmup = 32, samples = 64;
        const float scale = 20;
        var columns = (int)MathF.Ceiling(MathF.Sqrt(count * 1.5f));
        var rows = (count + columns - 1) / columns;
        var size = MathF.Min(2.4f, 360f / columns * .85f) * scale;
        using var fragment = new RectangleShape { Size = new(size, size) };
        using var block = new RectangleShape { Size = new(64 * scale, 64 * scale) };
        var shapes = new List<Shape>();
        var bodies = new RID[count + 5];
        var space = PhysicsServer.SpaceCreate();
        PhysicsServer.SpaceSetActive(space, true);
        PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, .02f); PhysicsServer.AreaSetAngularDamp(space, 0);
        try
        {
            for (var i = 0; i < 4; i++)
            {
                (float x, float y, float w, float h) = i switch { 0 => (576, 638, 840, 18), 1 => (158, 367, 18, 536), 2 => (994, 367, 18, 536), _ => (576, 94, 840, 12) };
                var shape = new RectangleShape { Size = new Vector2(w, h) * scale }; shapes.Add(shape);
                var body = bodies[count + 1 + i] = PhysicsServer.BodyCreate();
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(body, shape.GetRID(), Transform.Identity);
                PhysicsServer.BodySetTransform(body, new Transform(0, new Vector2(x, y) * scale));
                PhysicsServer.BodySetSpace(body, space);
            }
            for (var i = 0; i <= count; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate();
                PhysicsServer.BodyAddShape(body, (i == 0 ? block : fragment).GetRID(), Transform.Identity);
                PhysicsServer.BodySetMass(body, i == 0 ? 12 : .0045f);
                PhysicsServer.BodySetFriction(body, .05f); PhysicsServer.BodySetBounce(body, i == 0 ? .15f : .1f);
                PhysicsServer.BodySetAngularDamp(body, i == 0 ? 0 : .05f);
                var position = i == 0 ? new Vector2(230, 361) * scale : new Vector2(625 * scale + ((i - 1) % columns - (columns - 1) * .5f) * (size / .85f), 361 * scale + ((i - 1) / columns - (rows - 1) * .5f) * (size / .85f));
                PhysicsServer.BodySetTransform(body, new Transform(0, position));
                PhysicsServer.BodySetSpace(body, space);
                using var state = PhysicsServer.BodyGetDirectState(body)!;
                state.Sleeping = i != 0;
            }
            PhysicsServer.BodySetLinearVelocity(bodies[0], new(600 * scale, 0));
            var world = PhysicsServer.Service.BodyRuntime(bodies[0]).Space!;
            PhysicsSpace.ProfilingEnabled = true;
            var times = new double[samples]; var phases = new double[8]; var poses = new Transform[count + 1];
            for (var frame = 0; frame < warmup; frame++) Tick(frame);
            var before = GC.GetTotalAllocatedBytes(true);
            for (var frame = 0; frame < samples; frame++)
            {
                var start = Stopwatch.GetTimestamp(); Tick(frame + warmup);
                for (var i = 0; i <= count; i++) poses[i] = PhysicsServer.BodyGetTransform(bodies[i]);
                times[frame] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                for (var phase = 0; phase < phases.Length; phase++) phases[phase] += world.ProfileMS[phase];
            }
            var bytes = GC.GetTotalAllocatedBytes(true) - before;
            var result = new { bodies = count + 1, warmup, samples, meanMS = times.Average(), p95MS = Percentile(times), allThreadBytes = bytes, phaseMS = phases.Select(p => p / samples).ToArray() };
            Directory.CreateDirectory("bin/physics-sandbox");
            File.WriteAllText("bin/physics-sandbox/profile-server-smash.json", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine(JsonSerializer.Serialize(result));
            Check(poses.All(p => p.IsFinite()), "Server Smash retains finite poses.");
            void Tick(int frame)
            {
                if (frame % 48 == 0) PhysicsServer.BodyApplyCentralImpulse(bodies[0], new Vector2(frame % 96 == 0 ? 80 : -80, -40) * scale);
                PhysicsServer.SpaceStep(space, 1d / 60);
            }
        }
        finally
        {
            PhysicsServer.FreeRID(space);
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            foreach (var shape in shapes) shape.Dispose();
        }
    }
}
