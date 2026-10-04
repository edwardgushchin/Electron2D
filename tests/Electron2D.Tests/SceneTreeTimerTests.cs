using Electron2D;

internal static class SceneTreeTimerTests
{
    public static void Run()
    {
        var engine = Engine.Service;
        var previousScale = Engine.TimeScale;
        using var tree = new SceneTree(new Entity());
        var scaledProcess = tree.CreateTimer(0.02d);
        var originalProcess = tree.CreateTimer(0.02d, ignoreTimeScale: true);
        var scaledPhysics = tree.CreateTimer(0.02d, processInPhysics: true);
        var originalPhysics = tree.CreateTimer(0.02d, processInPhysics: true, ignoreTimeScale: true);
        var order = new List<string>();
        originalProcess.Timeout += timer =>
        {
            Check(timer.TimeLeft == 0d && !timer.IsDisposed, "Process timeout sees a live timer at zero.");
            order.Add("process");
        };
        originalPhysics.Timeout += _ => order.Add("physics");

        try
        {
            Engine.TimeScale = 0d;
            Engine.Start(tree);
            Engine.AdvanceFrame(1d);
            Check(order.SequenceEqual(["physics", "process"]), "Original frame deltas expire both lanes before deferred work.");
            Check(originalProcess.IsDisposed && originalPhysics.IsDisposed, "Expired timers are disposed after timeout.");
            Check(scaledProcess.TimeLeft == 0.02d && scaledPhysics.TimeLeft == 0.02d,
                "Scaled timers stay frozen when Engine time scale is zero.");
        }
        finally
        {
            if (ReferenceEquals(Engine.MainLoop, tree))
                Engine.Stop();
            Engine.TimeScale = previousScale;
        }

        using var directTree = new SceneTree(new Entity());
        var directTimer = directTree.CreateTimer(0.02d, ignoreTimeScale: true);
        directTree.ProcessFrame(0.01d);
        Check(directTimer.TimeLeft == 0.01d, "Direct callers supply the same delta for both timer modes.");
        Check(Throws<ArgumentOutOfRangeException>(() => directTimer.TimeLeft = double.NaN) &&
              directTimer.TimeLeft == 0.01d, "Invalid remaining time does not mutate the timer.");
        directTimer.Dispose();
        Check(Throws<ObjectDisposedException>(() => _ = directTimer.TimeLeft), "A disposed timer rejects reads.");

        directTree.CreateTimer(100d, ignoreTimeScale: true);
        directTree.CreateTimer(100d, processInPhysics: true, ignoreTimeScale: true);
        for (var index = 0; index < 16; index++)
        {
            directTree.ProcessFrame(0d);
            directTree.PhysicsFrame(0d);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 128; index++)
        {
            directTree.ProcessFrame(0d);
            directTree.PhysicsFrame(0d);
        }
        Check(GC.GetAllocatedBytesForCurrentThread() == before,
            "Warm active original-delta timer lanes allocate no managed memory.");
    }

    private static bool Throws<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
