# Performance.Monitor

[Object lifetime monitors](../components/object-lifetime-monitors.md) add live object, cached-resource, attached-node and orphan-node populations without owning targets, forcing collection or opening a native device.

Last updated: 2026-10-27

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
| AudioOutputLatency | 23 | Last valid active AudioServer output latency, in seconds |
| NavigationActiveMaps | 24 | Committed NavigationServer.ProcessInfo.ActiveMaps |
| NavigationRegionCount | 25 | Committed NavigationServer.ProcessInfo.RegionCount |
| NavigationAgentCount | 26 | Committed NavigationServer.ProcessInfo.AgentCount |
| NavigationLinkCount | 27 | Committed NavigationServer.ProcessInfo.LinkCount |
| NavigationPolygonCount | 28 | Committed NavigationServer.ProcessInfo.PolygonCount |
| NavigationEdgeCount | 29 | Committed NavigationServer.ProcessInfo.EdgeCount |
| NavigationEdgeMergeCount | 30 | Committed NavigationServer.ProcessInfo.EdgeMergeCount |
| NavigationEdgeConnectionCount | 31 | Committed NavigationServer.ProcessInfo.EdgeConnectionCount |
| NavigationEdgeFreeCount | 32 | Committed NavigationServer.ProcessInfo.EdgeFreeCount |
| NavigationObstacleCount | 33 | Committed NavigationServer.ProcessInfo.ObstacleCount |
| Navigation2DActiveMaps | 39 | Committed NavigationServer.ProcessInfo.ActiveMaps |
| Navigation2DRegionCount | 40 | Committed NavigationServer.ProcessInfo.RegionCount |
| Navigation2DAgentCount | 41 | Committed NavigationServer.ProcessInfo.AgentCount |
| Navigation2DLinkCount | 42 | Committed NavigationServer.ProcessInfo.LinkCount |
| Navigation2DPolygonCount | 43 | Committed NavigationServer.ProcessInfo.PolygonCount |
| Navigation2DEdgeCount | 44 | Committed NavigationServer.ProcessInfo.EdgeCount |
| Navigation2DEdgeMergeCount | 45 | Committed NavigationServer.ProcessInfo.EdgeMergeCount |
| Navigation2DEdgeConnectionCount | 46 | Committed NavigationServer.ProcessInfo.EdgeConnectionCount |
| Navigation2DEdgeFreeCount | 47 | Committed NavigationServer.ProcessInfo.EdgeFreeCount |
| Navigation2DObstacleCount | 48 | Committed NavigationServer.ProcessInfo.ObstacleCount |

Use [Performance.GetMonitor](Performance.md). Values retain their declared numeric
identities; the gaps do not imply that other selectors work. Sample lifecycle and
backend-specific workload semantics are defined in [Physics statistics](../components/physics-statistics.md).

The three durations use seconds and publish maxima once per completed one-second Engine sampling window. Startup, headless/direct-host, error, rendering and thread boundaries are defined in [Frame timing monitors](../components/frame-monitors.md).

The three canvas counts follow [completed canvas counters](../components/canvas-counters.md). They publish on completed native frames and return zero without an active renderer; they do not use the one-second timing window.

Navigation aggregates and planar selectors share one committed snapshot. Audio diagnostics use the active output's managed sample without initializing audio or invoking native code. See [server monitor snapshots](../components/runtime-diagnostics.md#server-monitor-snapshots).
