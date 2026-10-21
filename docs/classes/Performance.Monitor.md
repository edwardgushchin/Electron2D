# Performance.Monitor

[Object lifetime monitors](../components/object-lifetime-monitors.md) add live object, cached-resource, attached-node and orphan-node populations without owning targets, forcing collection or opening a native device.

Last updated: 2026-10-21

**Declaration:** `public enum Performance.Monitor` ·
**Source:** [Performance.cs](../../src/Core/Config/Performance.cs).

| Value | Number | Producer |
| --- | ---: | --- |
| TimeFPS | 0 | Existing Engine frames per unscaled host second |
| TimeProcess | 1 | Largest completed process-frame duration; owned Window presentation included |
| TimePhysicsProcess | 2 | Largest individual fixed-step duration |
| TimeNavigationProcess | 3 | Largest synchronization/avoidance duration within a fixed step |
| ObjectCount | 7 | Reachable engine objects before committed disposal |
| ObjectResourceCount | 8 | Live owners registered in the resource path cache |
| ObjectNodeCount | 9 | Live nodes inside the attached Engine SceneTree |
| ObjectOrphanNodeCount | 10 | Live nodes outside that attached tree, in both build configurations |
| RenderTotalObjectsInFrame | 11 | Completed renderer-frame logical canvas drawing records |
| RenderTotalPrimitivesInFrame | 12 | Submitted topology/instance budgets |
| RenderTotalDrawCallsInFrame | 13 | Native canvas geometry calls |
| PhysicsActiveObjects | 17 | PhysicsServer.ProcessInfo.ActiveObjects |
| PhysicsCollisionPairs | 18 | PhysicsServer.ProcessInfo.CollisionPairs |
| PhysicsIslandCount | 19 | PhysicsServer.ProcessInfo.IslandCount |

Use [Performance.GetMonitor](Performance.md). Values retain their declared numeric
identities; the gaps do not imply that other selectors work. Sample lifecycle and
backend-specific workload semantics are defined in [Physics statistics](../components/physics-statistics.md).

The three durations use seconds and publish maxima once per completed one-second Engine sampling window. Startup, headless/direct-host, error, rendering and thread boundaries are defined in [Frame timing monitors](../components/frame-monitors.md).

The three canvas counts follow [completed canvas counters](../components/canvas-counters.md). They publish on completed native frames and return zero without an active renderer; they do not use the one-second timing window.
