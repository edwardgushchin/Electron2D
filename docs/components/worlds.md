# Canvas and physics worlds

Last updated: 2026-10-07

A World combines one stable logical rendering canvas with one lazily created registered physics space. Viewport.World selects that runtime; CanvasItem.GetWorld resolves the nearest viewport's selection, or a SceneTree fallback in a viewport-free scene. FindWorld returns the live local world. Each ordinary viewport starts independently. Assigning another viewport's World shares scene canvas content and physics queries/simulation; null assignment creates a fresh independent default, matching the pinned viewport setter. CanvasLayer canvases keep their explicit viewport scope.

## Runtime and ownership

World() creates caller-owned identities. Duplicating or copying a World borrows the same runtime and never clones the solver. CopyFromResource cannot replace a bound target's identity; assign Viewport.World. Runtime identity survives disposal/recreation of a bound wrapper. SceneTree retains each distinct registered runtime, steps it once per physics tick and attempts cleanup for every runtime. A switched-out registered world stays retained and continues its active simulation until tree teardown, including server-only colliders. Default viewport and fallback runtimes expire at owning tree teardown, including externally retained duplicate wrappers. Failed activation releases its scene driver and default runtime; a still-live default viewport can create a fresh runtime when activation is retried. Explicit caller worlds survive tree teardown while a resource reference remains; callers use deterministic Dispose to release final unbound physics storage. Viewports dispose internally created wrappers and borrow explicitly assigned resources.

Canvas is available before native startup; physics storage is allocated only for Space/direct-query/body use. The owning servers reject FreeRID for these borrowed identities. A runtime has one scene driver: sharing between viewports in one SceneTree works; assigning it to a second simultaneous tree rejects before publication. Existing physics storage must belong to the scene thread. Creating a space on a loading thread therefore prevents binding it on another thread. Direct queries retain the existing owner/solver boundaries. World runtime liveness is read without taking its lifetime gate from the canvas registry, avoiding inverse lock acquisition during cold sweeping/disposal.

## World replacement

Assignments validate scene/native/capture and world-membership guards before publication. Moving a World during live body-state callbacks is rejected even though direct queries may still be permitted there. The old canvas attachment and the new canvas's stale native attachment override are cleared for the viewport. Other viewports keep their own transforms and memberships. CanvasTransform republishes the current world attachment's native transform field; GlobalCanvasTransform continues to compose outside it.

SceneTree snapshots the changed viewport's canvas descendants, stopping at independent nested viewports. Joints detach first, colliders move, then joints register against final membership. Collision and joint RIDs, shape-owner indices, authored transforms and retained drawing commands are preserved. Cross-world joints become unconfigured through their existing contract. Removal callbacks cannot stop the subsequent attach attempt. Failures aggregate after committed assignment; failed collider transfers are retried before later physics steps so correcting invalid geometry can recover membership. NotificationWorldChanged (36) follows committed association in parent-first order and continues after observer failures. Independent nested viewports receive no notification. Source canvas getters and explicit low-level render-parent overrides retain their existing separate state contract.

Spatial audio area routing selects the emitter's current physics world. RayCast, ShapeCast and direct scene queries use the same selected space. No parallel renderer or second scene-only physics solver is added.

Failed GPU intervals reject subsequent query/step/binding access, while final
resource disposal still releases the world. Release preserves the same
owner-thread, active-solver and live body callback restrictions. GPU development
checks verify failure cleanup and expiration of the borrowed space identity.

## Public use

```csharp
// Inside an attached scene callback; both viewports share one scene tree.
secondary.World = primary.World;
World world = secondary.FindWorld()!;
RID canvas = world.Canvas;
PhysicsDirectSpaceState queries = world.DirectSpaceState;
secondary.World = null; // fresh independent canvas and physics world
```

World association is live, discoverable typed state and is not stored in PackedScene. Existing scene resource schemas do not serialize native runtime identities. World.NavigationMap now registers a real active map in the same runtime lifetime. [Authored map/region topology](navigation-maps.md) and World replacement execute; further navigation bake/avoidance/query capabilities retain exact prerequisites.

## Verification

[WorldTests](../../tests/Electron2D.Tests/WorldTests.cs), selected with ELECTRON2D_TEST_WORLD=1, checks default/shared/explicit identity, resource duplicates, physics query isolation, body/Area transfer, once-per-tick stepping, joints, observer failure completion, live-body callback guards, stale/disposed/cross-tree/off-owner rejection and teardown, invalid-transfer recovery and failed-activation retry. Two thousand prepared GetWorld/FindWorld/canvas lookups allocate zero managed bytes; replacement is a cold snapshot operation and does allocate.

ELECTRON2D_TEST_WORLD_HOST=1 exercises actual Window/SubViewport targets with fallback disabled and the requested backend asserted. Five state phases after four warm frames verify independent/shared canvas pixels, per-view transforms, null replacement, source transform republishing, transition notification scope and native teardown. Optional ELECTRON2D_WORLD_SNAPSHOT writes final real rendered pixels. Current native evidence is Linux Wayland GPU and hardware compatibility; no foreign-platform or human acceptance is implied. Full-suite checks also cover existing physics, hierarchy, viewport and audio consumers under ADR 0021.

The GPU and hardware compatibility final captures were inspected and are byte-identical: SHA-256 `a9f4652bfae6dc386a3f3542f4e9c5dccc188c0f15bd0c6e865c5af3aa013a0e`. They show both source viewport contributions in the restored shared world. Captures remain local ignored verification artifacts.
