using System.Runtime.CompilerServices;
using System.Text.Json;
using Electron2D;

if (args is ["--process-memory-calibration"]) { Console.WriteLine(JsonSerializer.Serialize(ProcessMemory.Calibrate())); return; }
if (args.Length > 0 && args[0] == "--retire") { Retirement.Run(args[1..]); return; }
if (args.Length != 3 || args[0] is not ("cpu" or "gpu")) throw new ArgumentException("Expected cpu|gpu, body count, checkpoint count.");
var count = int.Parse(args[1]); var historyCount = int.Parse(args[2]);
if (count < 4 || count > 65536 || count % 4 != 0 || historyCount is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(args));
var backend = args[0] == "cpu" ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
var space = default(RID);
var bodies = new RID[count]; var joint = default(RID);
using var shape = new CircleShape { Radius = 2 };
var roots = new MemoryRoots();
Ready("baseline", 0);
try
{
    space = PhysicsServer.SpaceCreate(backend);
    if (PhysicsServer.SpaceGetBackend(space) != backend || PhysicsServer.SpaceGetBackendFallbackReason(space) is not null)
        throw new InvalidOperationException("The memory fixture requires its selected backend without fallback.");
    PhysicsServer.SpaceSetActive(space, true);
    for (var i = 0; i < count; i++)
    {
        var body = bodies[i] = PhysicsServer.BodyCreate();
        PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
        PhysicsServer.BodySetTransform(body, new(0, new(i / 2 % 32 * 12, i / 64 * 12 + i % 2 * 3.5f)));
        PhysicsServer.BodySetSpace(body, space);
    }
    joint = PhysicsServer.JointCreate(); PhysicsServer.JointMakePin(joint, Vector2.Zero, bodies[0]);
    Warm(); Ready("world", count);
    roots.Map = new(space); roots.Snapshot = new();
    for (var i = 0; i < count; i++) roots.Map.Bind((ulong)i + 1, 1, bodies[i]);
    roots.Map.Bind(1000000, 1, joint);
    roots.Checkpoints = new PhysicsCheckpoint[historyCount];
    for (var i = 0; i < historyCount; i++) roots.Checkpoints[i] = PhysicsServer.SpaceCreateCheckpoint(space);
    Warm(); Ready("history", count);
    roots.Map.Unbind(1000000); PhysicsServer.FreeRID(joint); joint = default;
    for (var i = count / 4; i < count; i++)
    {
        roots.Map.Unbind((ulong)i + 1); PhysicsServer.FreeRID(bodies[i]); bodies[i] = default;
    }
    Warm(); Ready("shrink", count / 4);
    foreach (var point in roots.Checkpoints) point.Dispose();
    roots.Map.Dispose(); Ready("released-history", count / 4);
    foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
    Array.Clear(bodies); shape.Dispose(); PhysicsServer.FreeRID(space); space = default;
    Ready("released-world", 0);
}
finally
{
    foreach (var point in roots.Checkpoints) point.Dispose(); roots.Map?.Dispose();
    if (joint.IsValid()) PhysicsServer.FreeRID(joint);
    if (space.IsValid())
    {
        foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
        PhysicsServer.FreeRID(space);
    }
    GC.KeepAlive(roots);
}

void Warm()
{
    // Warm the same full solver, private rewind and portable publication paths before collection.
    for (var n = 0; n < 8; n++)
    {
        PhysicsServer.SpaceStep(space, 1d / 120);
        foreach (var point in roots.Checkpoints) { point.Capture(); point.Restore(); }
        if (roots.Map is not null) { roots.Map.Capture(roots.Snapshot!); roots.Map.Apply(roots.Snapshot!); }
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
void Ready(string phase, int liveBodies)
{
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        Phase = phase,
        ProcessMemory = ProcessMemory.Capture(),
        Bodies = liveBodies,
        Checkpoints = roots.Checkpoints.Length,
        EncodedBytes = roots.Snapshot?.GetEncodedSize() ?? 0,
        Active = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects),
        Pairs = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs),
        Islands = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.IslandCount)
    }));
    if (Console.ReadLine() != "next") throw new IOException("Memory probe disconnected.");
    GC.KeepAlive(roots);
}

// Explicit profiler anchors; these caller-owned wrappers deliberately survive disposal.
sealed class MemoryRoots
{
    public PhysicsSnapshotMap? Map;
    public PhysicsSnapshot? Snapshot;
    public PhysicsCheckpoint[] Checkpoints = [];
    public Calibration Calibration = new();
    public byte[] WeakTarget = new byte[131072];
    public MemoryRoots() => Calibration.Weak = new(WeakTarget);
}
