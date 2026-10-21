using System.Diagnostics;
using Electron2D;

namespace Electron2D.Examples.PhysicsNetwork;

internal sealed partial class NetworkWorld
{
    internal double PreparationMS;
    internal long PreparationManagedBytes;
    internal int PreparationFrames;
    internal void PrepareInitialState()
    {
        if (Tick != 0 || Created != 0) throw new InvalidOperationException("Preparation requires a new example world.");
        var started = Stopwatch.GetTimestamp(); var allocated = GC.GetTotalAllocatedBytes(true);
        Reconcile(InitialObjects);
        var snapshot = new PhysicsSnapshot(24000); Map.Capture(snapshot);
        var before = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(before);
        using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(World.Space))
        {
            // This example has no scene scripts/timers; only its physical state is rewound.
            Phase = SimulationPhase.Correction;
            try { Tree.PhysicsFrame(FixedStep); PreparationFrames++; checkpoint.Restore(); }
            finally { Phase = SimulationPhase.Authority; }
        }
        Map.Capture(snapshot);
        var after = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(after);
        if (Tick != 0 || EventSequence != 0 || !before.AsSpan().SequenceEqual(after))
            throw new InvalidOperationException("Preparation did not restore the exact initial physical state.");
        PreparationManagedBytes = GC.GetTotalAllocatedBytes(true) - allocated;
        PreparationMS = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        if (PreparationMS > NetworkSession.PreparationTimeoutSeconds * 1000)
            throw new TimeoutException("Initial physics preparation exceeded its separate startup budget.");
    }
}
