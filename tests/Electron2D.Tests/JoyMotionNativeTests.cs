using System.Runtime.InteropServices;
using System.Diagnostics;
using Electron2D;
using SDL3;
using static JoyMotionContract;

internal static class JoyMotionNativeTests
{
    internal static void Run()
    {
        var ignore = Input.IgnoreJoypadOnUnfocusedApplication;
        using var display = DisplayServer.Open("Controller motion", new(96, 64));
        Input.IgnoreJoypadOnUnfocusedApplication = false;
        try
        {
            using var pad = new MotionPad(true, true);
            var device = pad.Device;
            Check(Input.HasJoyMotionSensors(device) && Input.GetJoyMotionSensorsRate(device) == 120 &&
                !Input.IsJoyMotionSensorsEnabled(device), "Native sensor discovery and disabled startup.");
            JoyMotionContract.Expect<InvalidOperationException>(() => Input.StartJoyMotionSensorsCalibration(device));
            pad.RejectEnable = true;
            JoyMotionContract.Expect<InvalidOperationException>(() => Input.SetJoyMotionSensorsEnabled(device, true));
            Check(!Input.IsJoyMotionSensorsEnabled(device), "Failed native activation preserves published state.");
            pad.RejectEnable = false;
            Input.SetJoyMotionSensorsEnabled(device, true);
            pad.Sample(new(1, 9.80665f, -2), new(.1f, -.2f, .3f));
            Near(Input.GetJoyAccelerometer(device), new(-1, -9.80665f, 2), "Native acceleration units and axes.");
            Near(Input.GetJoyGyroscope(device), new(.1f, -.2f, .3f), "Native angular units and axes.");
            Check(MathF.Abs(Input.GetJoyGravity(device).Length() - 9.80665f) < .0001f, "Gravity has standard magnitude.");
            Input.StartJoyMotionSensorsCalibration(device);
            Check(Input.IsJoyMotionSensorsCalibrating(device) && !Input.IsJoyMotionSensorsCalibrated(device) &&
                Input.GetJoyMotionSensorsCalibration(device) is null, "Starting calibration resets its completion and bias record.");
            JoyMotionContract.Expect<InvalidOperationException>(() => Input.StartJoyMotionSensorsCalibration(device));
            JoyMotionContract.Expect<InvalidOperationException>(() => Input.SetJoyMotionSensorsCalibration(device, default));
            for (var i = 0; i < 16; i++) pad.Sample(new(0, 9.80665f, 0), new(.1f, -.2f, .3f));
            Input.StopJoyMotionSensorsCalibration(device);
            Near(Input.GetJoyMotionSensorsCalibration(device)!.Value.GyroscopeOffset, new(.1f, -.2f, .3f), "Stationary calibration uses actual native samples.");
            Check(Input.IsJoyMotionSensorsCalibrated(device) && !Input.IsJoyMotionSensorsCalibrating(device), "Completed calibration flags.");
            JoyMotionContract.Expect<InvalidOperationException>(() => Input.StopJoyMotionSensorsCalibration(device));
            pad.Sample(new(0, 9.80665f, 0), new(.1f, -.2f, .3f));
            Near(Input.GetJoyGyroscope(device), default, "Calibration removes stationary drift.");
            Input.ClearJoyMotionSensorsCalibration(device);
            Check(Input.IsJoyMotionSensorsCalibrated(device) && Input.GetJoyMotionSensorsCalibration(device)!.Value.GyroscopeOffset == default,
                "Clear resets offset while preserving the completion flag.");
            Input.SetJoyMotionSensorsCalibration(device, new(new(.2f, .1f, -.3f)));
            pad.Sample(new(0, 9.80665f, 0), new(.3f, .4f, .2f));
            Near(Input.GetJoyGyroscope(device), new(.1f, .3f, .5f), "Restored calibration is applied on the next sample.");
            pad.Sample(new(float.NaN, 0, 0), new(1, 2, 3));
            Near(Input.GetJoyGyroscope(device), new(.1f, .3f, .5f), "Invalid native data does not poison completed readings.");
            JoyMotionContract.Expect<ArgumentOutOfRangeException>(() => Input.SetJoyMotionSensorsCalibration(device, new(new(float.NaN, 0, 0))));
            Near(Input.GetJoyMotionSensorsCalibration(device)!.Value.GyroscopeOffset, new(.2f, .1f, -.3f), "Invalid calibration preserves completed data.");
            Input.SetJoyMotionSensorsEnabled(device, false);
            Check(Input.GetJoyAccelerometer(device) == default && Input.GetJoyGravity(device) == default && Input.GetJoyGyroscope(device) == default,
                "Disabled readings are zero.");
            Input.SetJoyMotionSensorsCalibration(device, default);
            Input.SetJoyMotionSensorsEnabled(device, true);
            pad.Remap();
            Check(Input.HasJoyMotionSensors(device) && !Input.IsJoyMotionSensorsEnabled(device) &&
                Input.GetJoyMotionSensorsCalibration(device) is null, "Native remapping resets sensor ownership without changing the device ID.");
            Input.SetJoyMotionSensorsEnabled(device, true);
            VerifyEqualEnableTiming(pad);
            var actor = new MotionActor(device);
            var root = new Node(); root.AddChild(actor);
            using (var tree = new SceneTree(root))
            {
                Engine.Start(tree);
                try
                {
                    for (var i = 0; i < 128; i++) Frame(pad);
                    var before = actor.Position;
                    var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 256; i++) Frame(pad);
                    var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
                    Check(ownerBytes == 0 && allBytes == 0, $"256 active native sensor/scene intervals allocated {ownerBytes}/{allBytes} owner/all-thread bytes.");
                    Check(actor.Position.X > before.X + 1 && actor.Position.Y > before.Y + .5f, "Native gyroscope readings move the actual scene entity.");
                }
                finally { Engine.Stop(); }
            }
            Task.Run(() =>
            {
                Check(Input.HasJoyMotionSensors(device) && Input.GetJoyGyroscope(device).IsFinite(), "Off-owner snapshots remain readable.");
                JoyMotionContract.Expect<InvalidOperationException>(() => Input.SetJoyMotionSensorsEnabled(device, false));
                JoyMotionContract.Expect<InvalidOperationException>(() => Input.StartJoyMotionSensorsCalibration(device));
                JoyMotionContract.Expect<InvalidOperationException>(() => Input.SetJoyMotionSensorsCalibration(device, default));
            }).GetAwaiter().GetResult();
            DisplayServer.WindowSetMode(WindowMode.Minimized);
            Input.IgnoreJoypadOnUnfocusedApplication = true;
            pad.Sample(new(0, 9.80665f, 0), new(1, 2, 3));
            Check(Input.GetJoyGyroscope(device) == default && Input.GetJoyGravity(device) == default, "Focus policy suppresses sampled readings.");
            Input.IgnoreJoypadOnUnfocusedApplication = false;
            Near(Input.GetJoyGyroscope(device), new(1, .5f, .25f), "Disabling focus suppression immediately exposes the preceding accepted sample.");
            pad.Sample(new(0, 9.80665f, 0), new(.2f, .3f, .4f));
            Near(Input.GetJoyGyroscope(device), new(.2f, .3f, .4f), "Focus-policy restoration resumes live samples.");
            Input.StartJoyMotionSensorsCalibration(device);
            Input.ClearJoyMotionSensorsCalibration(device);
            Check(Input.IsJoyMotionSensorsCalibrating(device), "Clear retains active calibration.");
            pad.Dispose();
            Check(!Input.HasJoyMotionSensors(device) && !Input.IsJoyMotionSensorsCalibrating(device) && Input.GetJoyMotionSensorsCalibration(device) is null,
                "Disconnect retires capability, data and calibration before ID reuse.");
            using var replacement = new MotionPad(false, true);
            Check(replacement.Device == device && Input.GetJoyMotionSensorsCalibration(device) is null, "Reused device identity starts without old calibration.");
            Input.SetJoyMotionSensorsEnabled(device, true);
            replacement.Sample(default, new(.4f, .5f, .6f));
            Near(Input.GetJoyGyroscope(device), new(.4f, .5f, .6f), "Gyroscope-only device samples its available channel.");
            Check(Input.GetJoyAccelerometer(device) == default && Input.GetJoyGravity(device) == default && Input.GetJoyMotionSensorsRate(device) == 120,
                "Gyroscope-only capability has no fabricated acceleration or gravity.");
            replacement.Dispose();
            using var accelerationOnly = new MotionPad(true, false);
            Input.SetJoyMotionSensorsEnabled(accelerationOnly.Device, true);
            accelerationOnly.Sample(new(2, 3, 4), default);
            Near(Input.GetJoyAccelerometer(accelerationOnly.Device), new(-2, -3, -4), "Accelerometer-only device delivers its real channel.");
            Check(Input.GetJoyGyroscope(accelerationOnly.Device) == default, "No fabricated gyroscope on an accelerometer-only device.");
            accelerationOnly.Dispose();
            using var withoutSensors = new MotionPad(false, false);
            Input.SetJoyMotionSensorsEnabled(withoutSensors.Device, true);
            Input.StartJoyMotionSensorsCalibration(withoutSensors.Device);
            Check(!Input.HasJoyMotionSensors(withoutSensors.Device) && !Input.IsJoyMotionSensorsEnabled(withoutSensors.Device), "Connected controller without sensors remains unsupported.");
            Console.WriteLine("Native controller motion: discovery, failures, calibration, scene movement, focus, retirement and 256 full intervals at 0/0 B passed.");
        }
        finally { Input.IgnoreJoypadOnUnfocusedApplication = ignore; }
    }

    private static void Frame(MotionPad pad)
    {
        pad.Sample(new(0, 9.80665f, 0), new(1, .5f, .25f));
        _ = Input.GetJoyMotionSensorsCalibration(pad.Device);
        Engine.AdvanceFrame(1d / 120);
    }

    private static void VerifyEqualEnableTiming(MotionPad pad)
    {
        var firstStart = Stopwatch.GetTimestamp();
        pad.Sample(new(0, 9.80665f, 0), default);
        var firstEnd = Stopwatch.GetTimestamp();
        var before = Input.GetJoyGravity(pad.Device);
        Thread.Sleep(40);
        Input.SetJoyMotionSensorsEnabled(pad.Device, true);
        var secondStart = Stopwatch.GetTimestamp();
        pad.Sample(default, new(0, 0, 1));
        var secondEnd = Stopwatch.GetTimestamp();
        var lower = Stopwatch.GetElapsedTime(firstEnd, secondStart).TotalSeconds;
        var upper = Stopwatch.GetElapsedTime(firstStart, secondEnd).TotalSeconds;
        var angle = (float)((lower + upper) * .5);
        var expected = new Vector3(before.X * MathF.Cos(angle) + before.Y * MathF.Sin(angle),
            -before.X * MathF.Sin(angle) + before.Y * MathF.Cos(angle), before.Z);
        var observed = Input.GetJoyGravity(pad.Device);
        var tolerance = (upper - lower) * .5 * 9.80665 + .0001;
        Check(observed.DistanceTo(expected) <= tolerance,
            $"Equal enable must retain the accepted sample clock: {lower:F6}..{upper:F6}s, expected {expected}, observed {observed}, tolerance {tolerance:F6}.");
    }

    private sealed class MotionActor(int device) : Entity
    {
        protected override void OnProcess(double delta)
        {
            var rate = Input.GetJoyGyroscope(device);
            Position += new Vector2(rate.X, rate.Y) * (float)delta;
        }
    }

    private static void Near(Vector3 actual, Vector3 expected, string message) => Check(actual.DistanceTo(expected) < .0001f, message);

    private sealed class MotionPad : IDisposable
    {
        private readonly SDL.VirtualJoystickDesc _descriptor;
        private readonly nint _name, _sensors;
        private readonly bool _acceleration, _gyro;
        private uint _instance;
        private nint _joystick;
        private ulong _timestamp;
        private string? _originalMapping;
        private bool _disposed;
        internal readonly int Device;
        internal bool RejectEnable;

        internal MotionPad(bool acceleration, bool gyro)
        {
            _acceleration = acceleration; _gyro = gyro;
            _name = Marshal.StringToCoTaskMemUTF8("Motion Test Controller");
            var sensorSize = Marshal.SizeOf<SDL.VirtualJoystickSensorDesc>();
            _sensors = Marshal.AllocHGlobal(sensorSize * 2);
            var count = 0;
            if (acceleration) Marshal.StructureToPtr(new SDL.VirtualJoystickSensorDesc { Type = SDL.SensorType.Accel, Rate = 120 }, _sensors + sensorSize * count++, false);
            if (gyro) Marshal.StructureToPtr(new SDL.VirtualJoystickSensorDesc { Type = SDL.SensorType.Gyro, Rate = 120 }, _sensors + sensorSize * count++, false);
            var descriptor = new SDL.VirtualJoystickDesc(); SDL.InitInterface(ref descriptor);
            descriptor.Name = _name; descriptor.Type = SDL.JoystickType.Gamepad;
            descriptor.NAxes = 6; descriptor.NButtons = 21; descriptor.ButtonMask = (1u << 21) - 1;
            descriptor.NSensors = (ushort)count; descriptor.Sensors = _sensors;
            descriptor.SetSensorsEnabled = (_, enable) => !enable || !RejectEnable;
            _descriptor = descriptor;
            try
            {
                var prior = Input.GetConnectedJoypads();
                _instance = SDL.AttachVirtualJoystick(in descriptor);
                Check(_instance != 0, "Attach native virtual controller.");
                DisplayServer.ProcessEvents();
                Device = Input.GetConnectedJoypads().Except(prior).Single();
                _joystick = SDL.OpenJoystick(_instance);
                Check(_joystick != 0, "Open native virtual controller.");
            }
            catch { Dispose(); throw; }
        }

        internal void Sample(Vector3 acceleration, Vector3 gyro)
        {
            _timestamp += 8_333_333;
            Span<float> values = stackalloc float[3];
            values[0] = acceleration.X; values[1] = acceleration.Y; values[2] = acceleration.Z;
            if (_acceleration) Check(SDL.SendJoystickVirtualSensorData(_joystick, SDL.SensorType.Accel, _timestamp,
                values, 3), "Submit native acceleration sample.");
            values[0] = gyro.X; values[1] = gyro.Y; values[2] = gyro.Z;
            if (_gyro) Check(SDL.SendJoystickVirtualSensorData(_joystick, SDL.SensorType.Gyro, _timestamp,
                values, 3), "Submit native angular sample.");
            SDL.UpdateJoysticks(); DisplayServer.ProcessEvents();
        }

        internal void Remap()
        {
            var mapping = SDL.GetGamepadMapping(SDL.GetGamepadFromID(_instance))!;
            _originalMapping ??= mapping;
            var first = mapping.IndexOf(','); var second = mapping.IndexOf(',', first + 1);
            Check(SDL.SetGamepadMapping(_instance, mapping[..(first + 1)] + "Remapped Motion Controller" + mapping[second..]), "Update native controller mapping.");
            DisplayServer.ProcessEvents();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                if (_instance == 0) return;
                if (_originalMapping is not null) SDL.SetGamepadMapping(_instance, _originalMapping);
                if (_joystick != 0) SDL.CloseJoystick(_joystick);
                _joystick = 0;
                Check(SDL.DetachVirtualJoystick(_instance), "Detach native virtual controller."); _instance = 0;
                DisplayServer.ProcessEvents();
            }
            finally
            {
                Marshal.FreeHGlobal(_sensors); Marshal.FreeCoTaskMem(_name);
                GC.KeepAlive(_descriptor);
            }
        }
    }
}
