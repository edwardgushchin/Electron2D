# PhysicsScene

Last updated: 2026-10-07

**Namespace:** `Electron2D.Examples.PhysicsSandbox`. **Declaration:** `internal sealed partial class PhysicsScene : Entity`. **Inherits:** [Entity](Entity.md). **Source:** [interaction/drawing](../../examples/PhysicsSandbox/PhysicsScene.cs), [stories](../../examples/PhysicsSandbox/PhysicsScene.Stories.cs), [games](../../examples/PhysicsSandbox/PhysicsScene.Games.cs). **Component:** [Physics sandbox](../components/physics-sandbox.md).

An executable consumer owning one complete physics story. The factory index selects warehouse, marble delivery, clockwork, gravity garden, courier, radar, tug, live shape atelier, active stress particles, suspended motorcycle, slingshot towers or a Smash impact wall. These internal types are not compiled into the runtime assembly or the compiled runtime API snapshot.

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
| `void Act(int action)` | Executes one of three story actions; rejects invalid action index |
| `void StepOnce()` | Requests one physics interval even while paused |
| `void SetPointer(Vector2 position)` / `void ReleaseGrab()` | Root input cooperation for movement and release over GUI |
| `int BodyCount`, `int ContactEvents`, `int Score`, `int PhysicsSteps` | Real simulation observations used by the interface |
| `string[] Actions`, `string Help`, `string Observation` | Action captions, shared controls and story diagnostics |

The fixed callback gates the real space, applies force/impulse/character controls and explicitly steps only the independent atelier world. Mouse dragging preserves a local anchor and uses current point velocity; forces are capped, custom integration receives impulses, and frozen kinematic bodies use target poses. Factory construction and population growth do not select a body. A point pick first clears the previous selection; a hit selects the actual physics body, while empty space leaves it clear. Removing a selected stress particle clears selection and the grab. The selected-body commands execute real sleep, rotation lock, persistent force and disable-mode behavior.

Stress dots use eight visual vertices. The static stage is recorded once; a separate story canvas updates changing paths/queries. Stable shape-owner identities are cached for the lifetime of each factory-built body; inspector values update at 10 Hz; the selected-body outline and named label follow its live pose; individual bodies retain geometry between shape/contact appearance edits. The [capability map](../components/physics-sandbox.md) states coverage and limits. Ordinary stories cap body creation at 160; the stress scene supports 64–1,024 always-awake particles. Smash supports 64–65,536 square fragments (9,600 by default) and one 12 kg block, with zero default gravity and sleep/wake contact propagation. Launch reuses body identities, shockwave applies real impulses, and rebuilding clears motion/score. Population edits rebuild the layout, release removed bodies and clear a removed selection; fragment mass and other user material settings persist for retained bodies. Impact speed and population are exposed to the scene inspector. World gravity/damping use the public default-Area setters on the world space RID; selected-object properties and the story-specific parameter are exposed to the window sliders. The simulation CanvasLayer applies per-story presentation framing; physics transforms keep unit scale and pointer coordinates convert back into world space. Story and selection drawing share the public clipping Control. Invisible enclosure colliders and a thin visible floor replace the grid and heavy walls. Prepared owner-thread frames have a zero-managed-allocation gate, including scene/UI drawing, periodic impulses and cursor query movement. Scene construction, body additions and configuration edits allocate explicitly. Native/GPU allocations remain unmeasured. Numeric status uses stack formatting and glyph draws, while queries fill reusable buffers.

Smash fragments use ordinary invisible RigidBody scene nodes with real geometry and collision response. A retained public DrawMultilineColors command draws square silhouettes interpolated directly from unit-scale positions and angles using equal segment length/width; no simulation is replaced by the visual batch. Only the block enables contact snapshots/notifications. The moved-fragment counter observes each body pose, and point queries still pick those bodies for object edits. The retained vertex/color/previous-pose buffers cover the population ceiling and allocate during construction.

Smash tint follows live public sleep, freeze and velocity state. Sleeping bodies are muted; awake slow bodies are pink; fast bodies are apricot when linear-plus-angular travel per fixed interval exceeds one quarter of their square side. The block uses its own side length. A stopped render uses the current pose rather than cycling the interpolation fraction. These are scene colors; no diagnostic collider layer is enabled.

Smash uses 20× world geometry with a 0.05 presentation zoom; physics transforms retain unit scale. Initial body creation uses the intended wall position and sleeping state. Speed controls report actual world units (4,000–20,000 u/s, default 12,000), while the screen composition stays fixed. This prevents tiny screen geometry from producing disproportionate speculative contacts.

Fragment parents, shape resources and surface materials are grouped in blocks of 128. This bounds sibling-name insertion and shared resource event fan-out/teardown copying. Removed bodies detach and dispose their geometry while the small resource/group cache stays owned by the scene until disposal.
