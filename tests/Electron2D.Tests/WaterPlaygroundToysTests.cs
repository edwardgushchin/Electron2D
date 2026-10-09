using Electron2D;
using Electron2D.Examples.WaterPlayground;

internal static partial class WaterPlaygroundTests
{
    private static void CheckLift()
    {
        using var water = new WaterSimulation(256) { FaucetFlow = 0 };
        water.AddToy(WaterToy.Wheel);
        Steps(water, 90);
        var before = water.ActorPose(WaterSimulation.PlatformSlot).Origin.Y;
        var rotor = PhysicsServer.BodyGetDirectState(water.ActorBody(WaterSimulation.WheelSlot))!;
        PhysicsServer.BodyApplyTorqueImpulse(water.ActorBody(WaterSimulation.WheelSlot), -5 / rotor.InverseInertia);
        Steps(water, 100);
        Console.WriteLine($"lift: before={before:F1} after={water.ActorPose(WaterSimulation.PlatformSlot).Origin.Y:F1}");
        Check(water.ActorPose(WaterSimulation.PlatformSlot).Origin.Y < before - 10, "Rotating the wheel raises its force-coupled lift.");

    }
    private static void CheckToys(RenderingDevice device, bool gpu)
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_TOY_CASE") is null or "buoyancy")
            using (var water = new WaterSimulation(16384) { FaucetFlow = 0 })
            {
                water.AddToy(WaterToy.Ball); Put(water, WaterSimulation.BallSlot, new(500, 650)); PhysicsServer.BodySetMode(water.ActorBody(WaterSimulation.BallSlot), PhysicsServer.BodyMode.Static);
                water.SeedPoolForTest(); water.RemoveSolidParticlesForTest(WaterSimulation.BallSlot); water.SetUseGPU(gpu, device);
                Steps(water, 120); var support = 0d;
                for (var i = 0; i < 60; i++) { water.Step(1d / 60); support += water.SupportMassForTest(WaterSimulation.BallSlot); }
                support /= 60; var displacedMass = Math.PI * .28 * .28 * 100;
                Console.WriteLine($"buoyancy {gpu}: support={support:F3} kg expected={displacedMass:F3} kg ratio={support / displacedMass:F3}");
                Check(support > displacedMass * .9 && support < displacedMass * 1.1, "Hydrostatic force agrees with displaced water mass within particle resolution tolerance.");
            }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_TOY_CASE") is null or "boat")
            using (var water = new WaterSimulation(16384) { FaucetFlow = 0 })
            {
                water.SeedPoolForTest(); water.SpawnBoatForTest();
                PhysicsServer.BodySetTransform(water.Boat, new Transform(.2f, new(600, 515)));
                water.RemoveSolidParticlesForTest(1); water.SetUseGPU(gpu, device); Steps(water, 240);
                var angle = Math.Abs(Mathf.Wrap(water.BoatPose.Rotation, -MathF.PI, MathF.PI));
                Console.WriteLine($"boat equilibrium {gpu}: heel={angle:F3}, position={water.BoatPose.Origin}");
                Check(angle < .15 && water.BoatPose.Origin.Y is > 470 and < 550, "A lightly heeled boat returns toward upright flotation in a calm pool.");
            }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_TOY_CASE") is null or "fluid")
            using (var water = new WaterSimulation(4096) { FaucetFlow = 0 })
            {
                water.SeedPoolForTest(); var energy = water.LiquidEnergyForTest(); water.SetUseGPU(gpu, device);
                Steps(water, 180);
                var final = water.LiquidEnergyForTest();
                var density = water.InteriorDensityForTest();
                Console.WriteLine($"resting water {gpu}: energy ratio={final / energy:F4}, interior density/rest={density:F4}");
                Check(density is > .97 and < 1.05, "Settled interior water retains its rest density within resolution tolerance.");
                Check(final <= energy * 1.02 && water.Positions.All(p => p.Y > 430), "A resting pool does not generate energy or launch spontaneous high splashes.");
            }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_TOY_CASE") is null or "dam")
            using (var water = new WaterSimulation(16384) { FaucetFlow = 0 })
            {
                water.SeedPoolForTest(leftOnly: true); var initial = water.LiquidEnergyForTest(); var peak = initial;
                water.SetUseGPU(gpu, device); var front = 0f;
                for (var i = 0; i < 240; i++)
                {
                    water.Step(1d / 60); peak = Math.Max(peak, water.LiquidEnergyForTest());
                    if (i == 29) front = water.Positions.Max(p => p.X);
                }
                Console.WriteLine($"dam release {gpu}: front at 0.5s={front:F1}, peak energy/initial={peak / initial:F4}, final={water.LiquidEnergyForTest() / initial:F4}");
                Check(front > 1020 && peak < initial * 1.03, "A released water column flows across the basin without generating excess mechanical energy.");
            }
        if (!gpu)
        {
            using var inlet = new WaterSimulation();
            var peak = inlet.PeakInletDensityForTest();
            Console.WriteLine($"inlet peak density/rest={peak:F3}");
            Check(peak < 1.1f, "Newly emitted water does not begin in a strongly compressed overlapping packet.");
            CheckLift();
            using var jobs = new WaterSimulation(4096); jobs.CheckWorkerFailureForTest();
        }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_TOY_CASE") is null or "float")
            using (var water = new WaterSimulation(16384) { FaucetFlow = 0 })
            {
                water.SeedPoolForTest(); water.AddToy(WaterToy.Ball); water.AddToy(WaterToy.Wood); water.AddToy(WaterToy.Steel);
                Put(water, WaterSimulation.BallSlot, new(220, 685)); Put(water, WaterSimulation.CargoStart, new(430, 600)); Put(water, WaterSimulation.CargoStart + 1, new(760, 600));
                water.SetUseGPU(gpu, device); Steps(water, 150);
                Console.WriteLine($"toys {gpu}: ball={water.ActorPose(WaterSimulation.BallSlot).Origin.Y:F1} wood={water.ActorPose(WaterSimulation.CargoStart).Origin.Y:F1} steel={water.ActorPose(WaterSimulation.CargoStart + 1).Origin.Y:F1}");
                Check(water.ActorPose(WaterSimulation.BallSlot).Origin.Y < 625 && water.ActorPose(WaterSimulation.CargoStart).Origin.Y < 640 && water.ActorPose(WaterSimulation.CargoStart + 1).Origin.Y > 740, "Buoyant ball and wood rise; steel sinks in both liquid backends.");
                var count = water.ActiveCount; water.DrainOpen = true; Steps(water, 45);
                Check(water.ActiveCount < count && water.StoredCount + water.ActiveCount == water.Count, "Drain returns water to the fixed reservoir.");
                var drained = water.ActiveCount; water.DrainOpen = false; water.FaucetFlow = 1; Steps(water, 45);
                Check(water.ActiveCount > drained && water.ActiveCount <= water.Count, "The faucet recirculates stored particles without creating mass.");
                water.FaucetFlow = 0; var stopped = water.ActiveCount; Steps(water, 3);
                Check(water.ActiveCount == stopped, "Closing the faucet stops emission.");
            }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_TOY_CASE") is null or "bucket")
            using (var water = new WaterSimulation(16384) { FaucetFlow = 0 })
            {
                water.AddToy(WaterToy.Bucket); Put(water, WaterSimulation.BucketSlot, new(280, 350));
                water.SeedPoolForTest(true); var loaded = water.BucketWaterCount(); water.SetUseGPU(gpu, device);
                water.BeginDrag(new(280, 350)); water.MovePointer(new(280, 230)); Steps(water, 90);
                var carried = water.BucketWaterCount(); Console.WriteLine($"bucket {gpu}: loaded={loaded} carried={carried} pose={water.ActorPose(WaterSimulation.BucketSlot).Origin}");
                Check(water.ActorPose(WaterSimulation.BucketSlot).Origin.Y < 285 && carried > loaded * .4, "A raised hollow bucket carries a substantial part of its actual water payload.");
                water.TiltHeld(4); Steps(water, 180);
                Console.WriteLine($"bucket {gpu}: poured remainder={water.BucketWaterCount()} angle={water.ActorPose(WaterSimulation.BucketSlot).Rotation}");
                Check(water.BucketWaterCount() < carried * .7, "Tilting the held bucket pours out its payload.");
            }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_TOY_CASE") is null or "wheel")
            using (var water = new WaterSimulation(16384))
            {
                water.AddToy(WaterToy.Wheel); var start = water.ActorPose(WaterSimulation.PlatformSlot).Origin;
                water.SetUseGPU(gpu, device); Steps(water, 180);
                Console.WriteLine($"wheel {gpu}: turns={water.WheelTurns:F3} platform={water.ActorPose(WaterSimulation.PlatformSlot).Origin}");
                Check(water.WheelTurns < -.02 && Math.Abs(water.ActorPose(WaterSimulation.PlatformSlot).Origin.Y - start.Y) > 2, "The real jet turns the pinned wheel against the lift load.");
                Check(water.ActorPose(WaterSimulation.WheelSlot).Origin.DistanceTo(new(water.FaucetX + 42, 438)) < 3, "Wheel axle remains pinned.");
            }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_TOY_CASE") is null or "gate")
            using (var water = new WaterSimulation(16384) { FaucetFlow = 0 })
            {
                water.AddToy(WaterToy.Gate); Steps(water, 180); water.SeedPoolForTest(leftOnly: true); water.SetUseGPU(gpu, device); Steps(water, 40);
                var closed = water.Positions.Take(water.ActiveCount).Count(p => p.X > 842 && p.Y > 600);
                water.BeginDrag(new(water.Size.X * .72f, 430)); water.MovePointer(new(water.Size.X * .72f, 70)); Steps(water, 130); water.EndDrag();
                var open = water.Positions.Take(water.ActiveCount).Count(p => p.X > 842 && p.Y > 600);
                Console.WriteLine($"gate {gpu}: closed={closed} open={open} y={water.ActorPose(WaterSimulation.GateSlot).Origin.Y:F1}");
                Check(closed < 20 && open > closed + 300, "Closed gate retains the pool; lifting it releases a wave through the opening.");
            }
        if (Environment.GetEnvironmentVariable("ELECTRON2D_WATER_TOY_CASE") is null or "cargo")
            using (var water = new WaterSimulation(16384) { FaucetFlow = 0 })
            {
                water.SeedPoolForTest(); water.SpawnBoatForTest();
                water.SetUseGPU(gpu, device); Steps(water, 120);
                var boat = water.BoatPose;
                Check(boat.Origin.Y is > 450 and < 640, "The unloaded boat is afloat before measuring cargo.");
                double before = 0; for (var i = 0; i < 30; i++) { water.Step(1d / 60); before += water.BoatPose.Origin.Y; }
                before /= 30;
                boat = water.BoatPose; water.AddToy(WaterToy.Wood); water.AddToy(WaterToy.Wood);
                Put(water, WaterSimulation.CargoStart, boat.Origin + new Vector2(-24, -32)); Put(water, WaterSimulation.CargoStart + 1, boat.Origin + new Vector2(24, -32)); Steps(water, 90);
                double after = 0; for (var i = 0; i < 30; i++) { water.Step(1d / 60); after += water.BoatPose.Origin.Y; }
                after /= 30;
                Console.WriteLine($"cargo {gpu}: mean unloaded={before:F1} loaded={after:F1} draft change={after - before:F1}");
                Check(after > before + 2.5, "Two balanced dry cargo blocks increase the mean boat draft through rigid contacts.");
            }
    }
    private static void Put(WaterSimulation water, int slot, Vector2 point)
    { PhysicsServer.BodySetTransform(water.ActorBody(slot), new Transform(0, point)); PhysicsServer.BodySetLinearVelocity(water.ActorBody(slot), Vector2.Zero); }
    private static void Steps(WaterSimulation water, int count) { for (var i = 0; i < count; i++) water.Step(1d / 60); }
}

namespace Electron2D.Examples.WaterPlayground
{
    internal sealed partial class WaterSimulation
    {
        internal void CheckWorkerFailureForTest()
        {
            ActiveCount = Count;
            try { ParticlePass(i => { if (i == 1) throw new ArithmeticException("worker test"); }); throw new Exception("Particle worker failure was lost."); }
            catch (ArithmeticException error) when (error.Message == "worker test") { }
            var completed = 0; ParticlePass(_ => Interlocked.Increment(ref completed));
            if (completed != (Count + Chunk - 1) / Chunk) throw new Exception("Particle workers did not recover after a failed pass.");
        }
        internal double InteriorDensityForTest()
        {
            BuildGrid(); double sum = 0; var samples = 0;
            for (var i = 0; i < ActiveCount; i++)
            {
                var p = Positions[i]; if (p.X < 100 || p.X > Size.X - 100 || p.Y < 610 || p.Y > 700) continue;
                Density(i); sum += _density[i] / RestDensity; samples++;
            }
            return sum / samples;
        }
        internal void RemoveSolidParticlesForTest(int slot)
        {
            var inverse = ActorPose(slot).AffineInverse();
            for (var i = 0; i < ActiveCount;)
                if (Distance((inverse * (XY(_state[i]) * 100)) * .01f, _kinds[slot]).Z < Spacing * .0045f) _state[i] = _state[--ActiveCount]; else i++;
            Capture();
        }
        internal double SupportMassForTest(int slot)
        {
            double impulse = 0;
            if (UseGPU) impulse = _summaries[slot].Y;
            else { for (var i = 0; i < ActiveCount; i++) impulse += _reactions[slot * Count + i].Y; }
            return -impulse / _settings.World.Z / 9.8;
        }
        internal float PeakInletDensityForTest()
        {
            Emit(1d / 60); BuildGrid(); var peak = 0f;
            for (var i = 0; i < ActiveCount; i++) { Density(i); peak = Math.Max(peak, _density[i] / RestDensity); }
            return peak;
        }
        internal double LiquidEnergyForTest()
        {
            double energy = 0;
            for (var i = 0; i < ActiveCount; i++) energy += _mass * (Velocity(_state[i]).LengthSquared() * .5 + 9.8 * (Size.Y * .01 - _state[i].Y));
            return energy;
        }
        internal void SpawnBoatForTest()
        {
            _actors[1] = Body(new(600, 450), Own(new ConvexPolygonShape { Points = Hull }), 10);
            PhysicsServer.BodySetCenterOfMass(Boat, new(0, 18)); PhysicsServer.BodySetAngularDamp(Boat, .5f);
        }
        internal void SeedPoolForTest(bool bucket = false, bool leftOnly = false)
        {
            ActiveCount = Count;
            var i = 0;
            if (bucket)
                for (float y = 318; y < 383; y += Spacing)
                    for (float x = 239; x < 322; x += Spacing) _state[i++] = new(x * .01f, y * .01f, 0, 0);
            var width = leftOnly ? 820 : Size.X - 8;
            var columns = (int)(width / Spacing);
            for (var n = 0; i < Count; n++, i++)
            { var x = 4 + (n % columns + .5f) * Spacing; var y = Size.Y - 4 - (n / columns + .5f) * Spacing; _state[i] = new(x * .01f, y * .01f, 0, 0); }
            Capture();
        }
    }
}
