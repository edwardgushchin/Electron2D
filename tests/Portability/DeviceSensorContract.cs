using Electron2D;

internal static class DeviceSensorContract
{
    internal static void Run()
    {
        var saved = Read();
        var timeScale = Engine.TimeScale;
        Check(saved == default && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable,
            "Fresh headless device values start at zero.");
        try
        {
            Input.SetAccelerometer(new(1, 2, 3));
            Check(Input.GetGravity() == default && Input.GetGyroscope() == default && Input.GetMagnetometer() == default,
                "The four device channels are independent.");
            Input.SetGravity(new(4, 5, 6)); Input.SetGyroscope(new(7, 8, 9)); Input.SetMagnetometer(new(10, 11, 12));
            Check(Read() == (new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9), new Vector3(10, 11, 12)),
                "Each getter observes its corresponding setter.");
            var value = Input.GetAccelerometer(); value.X = 100;
            Check(Input.GetAccelerometer().X == 1, "Getters return independent values.");
            Input.SetAccelerometer(value); value.Y = 100;
            Check(Input.GetAccelerometer().Y == 2, "Setters copy the supplied value.");
            Check(Input.GetJoyAccelerometer(int.MaxValue) == default && Input.GetJoyGravity(int.MaxValue) == default &&
                Input.GetJoyGyroscope(int.MaxValue) == default, "Device values do not masquerade as controller samples.");
            foreach (var scalar in new[] { 0f, BitConverter.Int32BitsToSingle(int.MinValue), float.Epsilon,
                float.MaxValue, float.MinValue, float.PositiveInfinity, float.NegativeInfinity,
                BitConverter.Int32BitsToSingle(0x7fc01234) })
            {
                var sample = new Vector3(scalar, -scalar, BitConverter.Int32BitsToSingle(int.MinValue));
                Write(sample);
                var channels = Read();
                Same(channels.Acceleration, sample); Same(channels.Gravity, sample);
                Same(channels.Gyroscope, sample); Same(channels.MagneticField, sample);
            }
            if (!OperatingSystem.IsBrowser()) ConcurrentSnapshots();
            Engine.TimeScale = 1;
            SceneFrames();
            Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "Device simulation never opens a native host.");
            Console.WriteLine("Device sensor values passed: four independent bit-preserving channels, copied values, thread-safe snapshots and headless scene consumption.");
        }
        finally
        {
            Input.SetAccelerometer(saved.Acceleration); Input.SetGravity(saved.Gravity);
            Input.SetGyroscope(saved.Gyroscope); Input.SetMagnetometer(saved.MagneticField);
            Engine.TimeScale = timeScale;
        }
    }

    private static void ConcurrentSnapshots()
    {
        Write(new(0, 0, .5f));
        using var start = new ManualResetEventSlim();
        var writer = new Thread(() =>
        {
            start.Wait();
            for (var i = 0; i < 4096; i++) Write(new(i, -i, i + .5f));
        });
        writer.Start(); start.Set();
        try
        {
            for (var i = 0; i < 4096; i++)
            {
                var sample = Read();
                Coherent(sample.Acceleration); Coherent(sample.Gravity);
                Coherent(sample.Gyroscope); Coherent(sample.MagneticField);
            }
        }
        finally { Check(writer.Join(5000), "The bounded device writer terminates."); }
    }

    private static void SceneFrames()
    {
        using var root = new Node();
        var actor = new SensorActor { InputEnabled = true }; root.AddChild(actor);
        using var tree = new SceneTree(root);
        Engine.Start(tree);
        try
        {
            if (!OperatingSystem.IsBrowser())
            {
                var writer = new Thread(() => Write(new(2, 1, .5f)));
                writer.Start(); Check(writer.Join(5000), "Device values can be supplied off-owner while the Engine runs.");
            }
            else Write(new(2, 1, .5f));
            Engine.AdvanceFrame(1d / 60);
            Check(actor.Position.DistanceTo(new(2f / 60, 1f / 60)) < .00001f && MathF.Abs(actor.Rotation - .5f / 60) < .00001f &&
                actor.Gravity == new Vector3(2, 1, .5f) && actor.MagneticField == actor.Gravity && actor.InputEvents == 0,
                "A real scene consumes motion and field values without synthesized input callbacks.");
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 128; i++) ActiveFrame(i);
            var before = actor.Position; var frames = actor.Frames;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 256; i++) ActiveFrame(i);
            var ownBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownBytes == 0 && allBytes == 0, $"Active device/scene frames allocated {ownBytes}/{allBytes} owner/all-thread bytes.");
            Check(actor.Position.X > before.X + 1 && actor.Frames == frames + 256, "Measured frames execute actual Entity motion.");
            Write(default);
            for (var i = 0; i < 64; i++) Engine.AdvanceFrame(1d / 60);
            before = actor.Position; var rotation = actor.Rotation;
            owner = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 256; i++) Engine.AdvanceFrame(1d / 60);
            ownBytes = GC.GetAllocatedBytesForCurrentThread() - owner; allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownBytes == 0 && allBytes == 0 && actor.Position == before && actor.Rotation == rotation && actor.InputEvents == 0,
                $"Idle device/scene frames retain state and allocate zero bytes: {ownBytes}/{allBytes}.");
            Console.WriteLine("Device values: 256 active and 256 idle Engine/SceneTree/Entity frames at 0/0 owner/all-thread managed bytes.");
            Write(new(3, 4, 5));
        }
        finally { Engine.Stop(); }
        var retained = (new Vector3(3, 4, 5), new Vector3(3, 4, 5), new Vector3(3, 4, 5), new Vector3(3, 4, 5));
        Check(Read() == retained, "Stopping the Engine preserves every nonzero device value.");
        using var restartRoot = new Node();
        using var restartTree = new SceneTree(restartRoot);
        Engine.Start(restartTree);
        try { Check(Read() == retained, "Restarting the Engine retains every device channel."); }
        finally { Engine.Stop(); }
    }

    private sealed class SensorActor : Entity
    {
        internal Vector3 Gravity, MagneticField;
        internal int Frames, InputEvents;
        protected override void OnProcess(double delta)
        {
            var acceleration = Input.GetAccelerometer(); var gyro = Input.GetGyroscope();
            Gravity = Input.GetGravity(); MagneticField = Input.GetMagnetometer();
            Position += new Vector2(acceleration.X, acceleration.Y) * (float)delta;
            Rotation += gyro.Z * (float)delta;
            Frames++;
        }
        protected override void OnInput(InputEvent inputEvent) => InputEvents++;
    }

    private static void ActiveFrame(int index)
    {
        Write(new(index % 4 + 1, .5f, .25f));
        Engine.AdvanceFrame(1d / 60);
    }

    private static void Write(Vector3 value)
    {
        Input.SetAccelerometer(value); Input.SetGravity(value);
        Input.SetGyroscope(value); Input.SetMagnetometer(value);
    }

    private static (Vector3 Acceleration, Vector3 Gravity, Vector3 Gyroscope, Vector3 MagneticField) Read() =>
        (Input.GetAccelerometer(), Input.GetGravity(), Input.GetGyroscope(), Input.GetMagnetometer());

    private static void Coherent(Vector3 value) => Check(value.Y == -value.X && value.Z == value.X + .5f, "A getter never tears a vector across concurrent writes.");
    private static void Same(Vector3 actual, Vector3 expected) => Check(
        BitConverter.SingleToInt32Bits(actual.X) == BitConverter.SingleToInt32Bits(expected.X) &&
        BitConverter.SingleToInt32Bits(actual.Y) == BitConverter.SingleToInt32Bits(expected.Y) &&
        BitConverter.SingleToInt32Bits(actual.Z) == BitConverter.SingleToInt32Bits(expected.Z), "Raw sensor component bits are preserved.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
