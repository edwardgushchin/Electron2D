# PhysicsDebugDrawing

Last updated: 2026-10-09

- **Declaration:** internal static helper.
- **Source:** [PhysicsDebugDrawing.cs](../../src/Scene/2D/PhysicsDebugDrawing.cs).
- **Component:** [Physics canvas diagnostics](../components/physics-debug.md).

Records reusable diagnostic arrows, disabled tints and authored joint markers into
the owning node's current canvas. Before retained recording it reconciles borrowed
shape revision/disposal for CollisionShape and ShapeCast. It owns no resources,
physics space, device or scene nodes and exposes no game-facing API. Node lifecycle,
thread validation and canvas command capacity remain with their existing owners.
PhysicsDebugTests and PhysicsDebugNativeTests verify its common CPU/GPU consumers.
