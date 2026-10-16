using System.Diagnostics;

namespace Electron2D.Examples.PhysicsNetwork;

internal static partial class ProcessCheck
{
    static partial void ConfigureChildForTesting(ProcessStartInfo info, string role)
    {
        var preload = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_PHYSICS_HEAPTRACK_PRELOAD");
        if (string.IsNullOrEmpty(preload)) return;
        var folder = Environment.GetEnvironmentVariable("ELECTRON2D_TEST_PHYSICS_HEAPTRACK_OUTPUT");
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) throw new IOException("The allocation trace directory is absent.");
        var trace = System.IO.Path.Combine(folder, role + ".heaptrack.raw");
        if (File.Exists(trace)) throw new IOException("The allocation trace already exists.");
        info.Environment["LD_PRELOAD"] = preload;
        info.Environment["DUMP_HEAPTRACK_OUTPUT"] = trace;
    }
}
