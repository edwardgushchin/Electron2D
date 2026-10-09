# Performance.Monitor

Last updated: 2026-10-09

**Declaration:** `public enum Performance.Monitor` ·
**Source:** [Performance.cs](../../src/Core/Config/Performance.cs).

| Value | Number | Producer |
| --- | ---: | --- |
| PhysicsActiveObjects | 17 | PhysicsServer.ProcessInfo.ActiveObjects |
| PhysicsCollisionPairs | 18 | PhysicsServer.ProcessInfo.CollisionPairs |
| PhysicsIslandCount | 19 | PhysicsServer.ProcessInfo.IslandCount |

Use [Performance.GetMonitor](Performance.md). Values retain their declared numeric
identities; the gaps do not imply that other selectors work. Sample lifecycle and
backend-specific workload semantics are defined in [Physics statistics](../components/physics-statistics.md).
