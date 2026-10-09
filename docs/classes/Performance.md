# Performance

Last updated: 2026-10-09

**Declaration:** `public sealed class Performance : ElectronObject` ·
**Source:** [Performance.cs](../../src/Core/Config/Performance.cs) ·
**Component:** [Physics statistics](../components/physics-statistics.md).

A retained process-wide diagnostics service with static public access. Engine's
named singleton lookup returns the same borrowed object; consumer disposal or
unregistration throws without changing its identity. Construction is private.

## API

| Signature | Contract |
| --- | --- |
| `public static double GetMonitor(Performance.Monitor monitor)` | Read the selected latest completed physical count as a double. |
| `protected override void ValidateDisposal()` | Reject consumer disposal of the permanent service. |

The [Monitor](Performance.Monitor.md) enum exposes the three functioning physics
producers. Reads may run on any thread, do not advance simulation, synchronize a
device or allocate managed memory, and use the same snapshots as
[PhysicsServer.GetProcessInfo](PhysicsServer.md#statistics). Undefined monitor values
throw ArgumentOutOfRangeException. Aggregate integer overflow throws OverflowException.

```csharp
double active = Performance.GetMonitor(Performance.Monitor.PhysicsActiveObjects);
double pairs = Performance.GetMonitor(Performance.Monitor.PhysicsCollisionPairs);
double islands = Performance.GetMonitor(Performance.Monitor.PhysicsIslandCount);
```

Other built-in producers and custom monitor registration/lifecycle are not yet
implemented. No unsupported selector or constant-return placeholder is exposed.
See the component for sample lifetime, verification and platform limits.
