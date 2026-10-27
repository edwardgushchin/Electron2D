# Navigation simulation controls and diagnostics

Last updated: 2026-10-27

[ADR 0097](../decisions/navigation.md#global-simulation-and-scene-diagnostics) owns the lifecycle and drawing contract. [NavigationServer](../classes/NavigationServer.md) adds process-wide SetActive, GetDebugEnabled/SetDebugEnabled, coalesced NavigationDebugChanged/AvoidanceDebugChanged and copied MapGetObstacles. Existing [navigation maps](navigation-maps.md), [agent paths](../classes/NavigationAgent.md#debugenabled), scene nodes, SceneTree and ProjectSettings supply the connected consumers.

Suspending avoidance retains maps, velocities and queries. Scene/batch synchronization still publishes staged geometry. Debug requests are independent of simulation: a global flag, tree-local DebugNavigationHint or editor authoring requests scene geometry. Events defer to the next navigation/scene process boundary, attempt all subscribers of both events and aggregate failures. Equal assignments do not notify; observer edits defer another notification and recursive synchronization rejects.

Constructor-owned internal [NavigationDebugCanvas](../classes/NavigationDebugCanvas.md) helpers read immutable [NavigationDebugData](../classes/NavigationDebugData.md) and retain commands across unchanged frames. Recording borrows stable arrays; it never queries/steps navigation or starts a renderer. World-space coordinates use the current canvas/camera/layer and ordinary source visibility. No matrix inverse is required, including a zero source scale. Absolute overlay Z is MaximumZIndex minus two; explicit agent path Z remains one level above.

Regions draw convex faces/edge lines, disabled colors, stable per-polygon shades, committed margin pathways and baking bounds. Links draw attachment circles from the actual selected map, directional arrows and disabled colors. Avoidance-enabled scene agents draw world radii. Obstacles draw maximum-axis discs, oriented static faces/edges and transformed local normals; winding selects push-in/out colors. Two-point walls retain one segment and its normal. Scene storage reconstructs helpers from constructors; they are absent from authored child lists and files.

## Settings

The global producer samples these twenty keys, with feature overrides, once on first use. [NavigationGeometryDebugSettings](../classes/NavigationGeometryDebugSettings.md) owns that immutable snapshot. Registry metadata requires restart to resample. Explicit agent paths keep their independent three keys.

| Key suffix | Default | Consumer |
| --- | --- | --- |
| `debug/shapes/navigation/2d/edge_connection_color` | `new(1, 0, 1, 1)` | EdgeConnectionColor |
| `debug/shapes/navigation/2d/geometry_edge_color` | `new(.5f, 1, 1, 1)` | GeometryEdgeColor |
| `debug/shapes/navigation/2d/geometry_face_color` | `new(.5f, 1, 1, .4f)` | GeometryFaceColor |
| `debug/shapes/navigation/2d/geometry_edge_disabled_color` | `new(.5f, .5f, .5f, 1)` | GeometryEdgeDisabledColor |
| `debug/shapes/navigation/2d/geometry_face_disabled_color` | `new(.5f, .5f, .5f, .4f)` | GeometryFaceDisabledColor |
| `debug/shapes/navigation/2d/link_connection_color` | `new(1, .5f, 1, 1)` | LinkConnectionColor |
| `debug/shapes/navigation/2d/link_connection_disabled_color` | `new(.5f, .5f, .5f, 1)` | LinkConnectionDisabledColor |
| `debug/shapes/navigation/2d/enable_edge_connections` | `true` | EdgeConnections |
| `debug/shapes/navigation/2d/enable_edge_lines` | `true` | EdgeLines |
| `debug/shapes/navigation/2d/enable_geometry_face_random_color` | `true` | GeometryFaceRandomColor |
| `debug/shapes/navigation/2d/enable_link_connections` | `true` | LinkConnections |
| `debug/shapes/avoidance/2d/agents_radius_color` | `new(1, 1, 0, .25f)` | AgentsRadiusColor |
| `debug/shapes/avoidance/2d/obstacles_radius_color` | `new(1, .5f, 0, .25f)` | ObstaclesRadiusColor |
| `debug/shapes/avoidance/2d/obstacles_static_face_pushin_color` | `new(1, 0, 0, 0)` | StaticFacePushInColor |
| `debug/shapes/avoidance/2d/obstacles_static_edge_pushin_color` | `new(1, 0, 0, 1)` | StaticEdgePushInColor |
| `debug/shapes/avoidance/2d/obstacles_static_face_pushout_color` | `new(1, 1, 0, .5f)` | StaticFacePushOutColor |
| `debug/shapes/avoidance/2d/obstacles_static_edge_pushout_color` | `new(1, 1, 0, 1)` | StaticEdgePushOutColor |
| `debug/shapes/avoidance/2d/enable_agents_radius` | `true` | AgentsRadius |
| `debug/shapes/avoidance/2d/enable_obstacles_radius` | `true` | ObstaclesRadius |
| `debug/shapes/avoidance/2d/enable_obstacles_static` | `true` | ObstaclesStatic |

## Verification

[NavigationGlobalDebugTests](../../tests/Electron2D.Tests/NavigationGlobalDebugTests.cs) checks pathways, arrow count, winding, reflected/nonuniform/zero scales, visibility, independent tree/editor requests, immutable configuration, helper storage/lifetime and changed/unchanged prepared recording. A fresh process disables all seven producer switches and checks their actual commands. [NavigationDiagnosticsChecks](../../tests/NavigationDiagnostics.Consumer/NavigationDiagnosticsChecks.cs) is public-only: it checks events, worker/reentry/failure handling, copied memberships and suspend/resume, then reads real region/link/agent/obstacle pixels on Compatibility and GPU. The native sequence measures 64 prepared frames. CLR, strict NativeAOT, regression and allocation results require their recorded receipts; foreign targets, native allocator and large-scene performance remain separate acceptance.
