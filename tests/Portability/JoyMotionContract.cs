using Electron2D;

internal static class JoyMotionContract
{
    internal static void Run()
    {
        const int absent = int.MaxValue;
        Check(!Input.HasJoyMotionSensors(absent) && !Input.IsJoyMotionSensorsEnabled(absent) &&
            Input.GetJoyMotionSensorsRate(absent) == 0 && Input.GetJoyAccelerometer(absent) == default &&
            Input.GetJoyGravity(absent) == default && Input.GetJoyGyroscope(absent) == default &&
            !Input.IsJoyMotionSensorsCalibrated(absent) && !Input.IsJoyMotionSensorsCalibrating(absent) &&
            Input.GetJoyMotionSensorsCalibration(absent) is null, "Absent controller motion state.");
        Input.SetJoyMotionSensorsEnabled(absent, true);
        Input.StartJoyMotionSensorsCalibration(absent);
        Input.StopJoyMotionSensorsCalibration(absent);
        Input.ClearJoyMotionSensorsCalibration(absent);
        Input.SetJoyMotionSensorsCalibration(absent, new(new(.1f, -.2f, .3f)));
        Check(!Input.HasJoyMotionSensors(absent) && !DisplayServer.IsAvailable, "Absent operations do not create a display or controller.");
        foreach (var call in new Action[]
        {
            () => Input.HasJoyMotionSensors(-1), () => Input.GetJoyMotionSensorsRate(-1),
            () => Input.IsJoyMotionSensorsEnabled(-1), () => Input.SetJoyMotionSensorsEnabled(-1, true),
            () => Input.GetJoyAccelerometer(-1), () => Input.GetJoyGravity(-1), () => Input.GetJoyGyroscope(-1),
            () => Input.StartJoyMotionSensorsCalibration(-1), () => Input.StopJoyMotionSensorsCalibration(-1),
            () => Input.ClearJoyMotionSensorsCalibration(-1), () => Input.GetJoyMotionSensorsCalibration(-1),
            () => Input.SetJoyMotionSensorsCalibration(-1, default), () => Input.IsJoyMotionSensorsCalibrated(-1),
            () => Input.IsJoyMotionSensorsCalibrating(-1)
        }) Expect<ArgumentOutOfRangeException>(call);
        Expect<ArgumentOutOfRangeException>(() => Input.SetJoyMotionSensorsCalibration(absent, new(new(float.NaN, 0, 0))));
        Expect<ArgumentOutOfRangeException>(() => Input.SetJoyMotionSensorsCalibration(absent, new(new(0, float.PositiveInfinity, 0))));
        Expect<ArgumentOutOfRangeException>(() => Input.SetJoyMotionSensorsCalibration(absent, new(new(float.MaxValue, 0, 0))));
        var copy = new JoyMotionCalibration(new(.1f, .2f, .3f));
        var value = copy.GyroscopeOffset; value.X = 9;
        Check(copy.GyroscopeOffset.X == .1f, "Calibration values are independent copies.");
        for (var i = 0; i < 128; i++) ReadAbsent(absent);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 1024; i++) ReadAbsent(absent);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0, $"Controller motion reads allocated {ownerBytes}/{allBytes} owner/all-thread bytes.");
        Console.WriteLine("Public controller motion absent/error/value contract passed; prepared reads 0/0 B.");
    }

    private static void ReadAbsent(int device)
    {
        _ = Input.HasJoyMotionSensors(device); _ = Input.GetJoyMotionSensorsRate(device);
        _ = Input.IsJoyMotionSensorsEnabled(device); _ = Input.GetJoyAccelerometer(device);
        _ = Input.GetJoyGravity(device); _ = Input.GetJoyGyroscope(device);
        _ = Input.IsJoyMotionSensorsCalibrated(device); _ = Input.IsJoyMotionSensorsCalibrating(device);
        _ = Input.GetJoyMotionSensorsCalibration(device);
    }

    internal static void Expect<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
