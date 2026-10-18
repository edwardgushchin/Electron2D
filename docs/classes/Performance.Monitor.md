# Performance.Monitor

Last updated: 2026-10-18

**Declaration:** `public enum Performance.Monitor` ·
**Source:** [Performance.cs](../../src/Core/Config/Performance.cs).

| Value | Number | Producer |
| --- | ---: | --- |
| TimeFPS | 0 | Existing Engine frames per unscaled host second |
| TimeProcess | 1 | Largest completed process-frame duration; owned Window presentation included |
| TimePhysicsProcess | 2 | Largest individual fixed-step duration |
| TimeNavigationProcess | 3 | Largest synchronization/avoidance duration within a fixed step |
| PhysicsActiveObjects | 17 | PhysicsServer.ProcessInfo.ActiveObjects |
| PhysicsCollisionPairs | 18 | PhysicsServer.ProcessInfo.CollisionPairs |
| PhysicsIslandCount | 19 | PhysicsServer.ProcessInfo.IslandCount |

Use [Performance.GetMonitor](Performance.md). Values retain their declared numeric
identities; the gaps do not imply that other selectors work. Sample lifecycle and
backend-specific workload semantics are defined in [Physics statistics](../components/physics-statistics.md).

The three durations use seconds and publish maxima once per completed one-second Engine sampling window. Startup, headless/direct-host, error, rendering and thread boundaries are defined in [Frame timing monitors](../components/frame-monitors.md).
