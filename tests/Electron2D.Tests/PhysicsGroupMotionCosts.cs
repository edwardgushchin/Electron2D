using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using Electron2D;

internal static class PhysicsGroupMotionCosts
{
    internal static void Run(string backendName)
    {
        if (backendName is not ("cpu" or "gpu")) throw new ArgumentException("Select cpu or gpu.", nameof(backendName));
        var backend = backendName == "gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
        foreach (var worker in new[] { false, true })
        {
            long upload = 0, readback = 0, submissions = 0, timestamp = 0, workers = 0;
            double wait = 0;
            BackendChecks.RunGroupMotion(backend, false, (world, before) =>
            {
                var gpu = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore;
                if (before)
                {
                    upload = gpu?.UploadBytes ?? 0; readback = gpu?.ReadbackBytes ?? 0;
                    submissions = gpu?.SubmissionCount ?? 0; wait = gpu?.WaitMS ?? 0;
                    workers = WorkerBytes(); timestamp = Stopwatch.GetTimestamp();
                }
                else
                {
                    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"Group motion cost: backend={backend}; worker={worker}; frames=64; elapsedMS={Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds}; uploadBytes={(gpu?.UploadBytes ?? 0) - upload}; readbackBytes={(gpu?.ReadbackBytes ?? 0) - readback}; submissions={(gpu?.SubmissionCount ?? 0) - submissions}; waitMS={(gpu?.WaitMS ?? 0) - wait}; workerManagedBytes={WorkerBytes() - workers}"));
                }
            }, worker);
        }
    }

    private static long WorkerBytes()
    {
        if (!WorkerThreadPool.Service.HasRuntime) return 0;
        var field = typeof(WorkerThreadPool).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return ((Lazy<WorkerPoolRuntime>)field.GetValue(WorkerThreadPool.Service)!).Value.WorkerAllocatedBytes;
    }
}
