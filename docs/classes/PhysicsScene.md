# PhysicsScene

Last updated: 2026-10-06

**Namespace:** `Electron2D.Examples.PhysicsSandbox`. **Declaration:** `internal sealed partial class PhysicsScene : Entity`. **Inherits:** [Entity](Entity.md). **Source:** [interaction/drawing](../../examples/PhysicsSandbox/PhysicsScene.cs), [stories](../../examples/PhysicsSandbox/PhysicsScene.Stories.cs), [games](../../examples/PhysicsSandbox/PhysicsScene.Games.cs). **Component:** [Physics sandbox](../components/physics-sandbox.md).

An executable consumer owning one complete physics story. The factory index selects warehouse, marble delivery, clockwork, gravity garden, courier, radar, tug, live shape atelier, active stress particles, suspended motorcycle or slingshot towers. These internal types are not compiled into the runtime assembly or the compiled runtime API snapshot.

## Construction and ownership

```csharp
var scene = new PhysicsScene(0, font);
viewport.AddChild(scene);
```

`font` is borrowed. Shapes, materials and query parameter objects are retained by the story. Dynamic/static/character/animatable bodies use ordinary CollisionShape resources and shape-owner geometry; there is no backend import. The atelier owns additional explicit server body/Area/space RIDs. Disposal frees those before children detach and then releases the resources; repeated dropdown/reset cycles use fresh identities.

## Internal consumer surface

| Member | Role |
| --- | --- |
| `PhysicsScene(int index, Font font)` | Constructs one detached story; invalid index rejects |
| `bool Running` | Local scene and independent-space activity policy |
| `bool DebugEnabled` | Shows the optional public-data observation layer |
| `void Act(int action)` | Executes one of three story actions; rejects invalid action index |
| `void StepOnce()` | Requests one physics interval even while paused |
| `void SetPointer(Vector2 position)` / `void ReleaseGrab()` | Root input cooperation for movement and release over GUI |
| `int BodyCount`, `int ContactEvents`, `int Score`, `int PhysicsSteps` | Real simulation observations used by the interface |
| `string[] Actions`, `string Help`, `string Observation` | Visible controls and per-story status |

The fixed callback gates the real space, applies force/impulse/character controls and explicitly steps only the independent atelier world. Mouse dragging preserves a local anchor and uses current point velocity; forces are capped, custom integration receives impulses, and frozen kinematic bodies use target poses. The selected-body commands execute real sleep, rotation lock, persistent force and disable-mode behavior.

Debug outlines query current logical shape indices and effective local poses, including raw server replacements. Contact points/normals, velocities and centres come from public direct-body state. Normal and debug drawing reuse the same shape-family rendering. Stress dots use eight visual vertices; contact/velocity lines are batched through retained buffers. The static stage is recorded once; a separate story canvas updates changing paths/queries. Stable shape-owner identities are cached for the lifetime of each factory-built body; selected-body readouts update at 10 Hz and observation text records only when changed; individual bodies retain geometry between shape/contact appearance edits. The [capability map](../components/physics-sandbox.md) states coverage and limits. Ordinary stories cap body creation at 160; the stress scene supports 64–1,024 always-awake particles. World gravity/damping use the public default-Area setters on the world space RID; selected-object properties and the story-specific parameter are exposed to the window sliders. The simulation CanvasLayer applies a 0.76 visual scale; physics transforms keep unit scale and pointer coordinates are transformed back into world space. Prepared owner-thread frames have a zero-managed-allocation gate, including debug/UI drawing, periodic impulses and cursor query movement. Scene construction, body additions and configuration edits allocate explicitly. Native/GPU allocations remain unmeasured. Numeric status uses stack formatting and glyph draws, while query and slide results fill reusable buffers.
