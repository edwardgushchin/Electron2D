using System.Globalization;
using Electron2D;

internal static class JoyMotionTests
{
    internal static void Run()
    {
        var state = new JoyMotionState(120);
        var count = 0;
        foreach (var line in File.ReadLines(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "JoyMotion.csv")))
        {
            var v = line.Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
            var acceleration = new Vector3(v[1], v[2], v[3]);
            var gyro = new Vector3(v[4], v[5], v[6]);
            switch ((int)v[0])
            {
                case 1: state.ResetCalibration(); state.Calibrating = true; break;
                case 2: state.Calibrating = false; break;
                case 3: state.ResetCalibration(); break;
                case 4: state.SetOffset(gyro); break;
                case 5: state = new JoyMotionState(120); break;
                default: state.Process(acceleration, gyro, v[7]); break;
            }
            Near(state.Gyroscope, new(v[8], v[9], v[10]), count, "gyroscope");
            Near(state.Gravity, new(v[11], v[12], v[13]), count, "gravity");
            Near(state.Offset, new(v[14], v[15], v[16]), count, "calibration");
            count++;
        }
        Check(count >= 500, "The numerical trace must include complete stationary, rotation, acceleration and calibration intervals.");
        var sample = new Vector3(.1f, -.2f, .3f);
        for (var i = 0; i < 128; i++) state.Process(new(0, -9.80665f, 0), sample, 1f / 120);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 1024; i++) state.Process(new(0, -9.80665f, 0), sample, 1f / 120);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0, $"Prepared motion filtering allocated {ownerBytes}/{allBytes} owner/all-thread bytes.");
        Console.WriteLine($"Controller motion numerical trace passed: {count} samples; 1024 prepared filter intervals at 0/0 B.");
    }

    private static void Near(Vector3 actual, Vector3 expected, int sample, string lane) =>
        Check(actual.IsFinite() && actual.DistanceTo(expected) < .0001f,
            $"Motion oracle {lane} sample {sample}: expected {expected}, observed {actual}.");

    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
