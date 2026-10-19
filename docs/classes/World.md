# World

Last updated: 2026-10-14

- Declaration: `public sealed class World : Resource`
- Source: [World.cs](../../src/Scene/Resources/World.cs)
- Inherits: [Resource](Resource.md)
- Component: [Canvas and physics worlds](../components/worlds.md)

## Description

Lazy aliases preserve the selected registration and separate numerical thread policy captured by their shared runtime. Disposing the original resource does not retire an alias-owned space; the final resource release retires the space and joins its retained numerical worker. See [creation verification](../components/physics-registered-adapter.md#space-creation-operation-audit).

Combines a stable rendering canvas and lazily created physics space. Viewport.World selects its live runtime and CanvasItem.GetWorld resolves that selection. Independent viewports isolate canvas and physics; explicitly shared worlds expose the same content, body identities and direct queries. A scene without viewports uses its SceneTree fallback world.

## API summary

| Member | Contract |
| --- | --- |
| `World()` | Caller-owned CPU runtime with logical canvas and lazy physics, independent of project selection. |
| `World(PhysicsServer.Backend backend, bool allowCPUFallback = false)` | Explicit lazy physics selection; invalid enum values reject immediately. |
| `PhysicsServer.Backend RequestedPhysicsBackend { get; }` | Original choice without starting physics. |
| `PhysicsServer.Backend PhysicsBackend { get; }` | Actual selection; initializes physics if needed. |
| `string? PhysicsFallbackReason { get; }` | Startup failure diagnostic after allowed CPU fallback, otherwise null; initializes physics if needed. |
| `RID Canvas { get; }` | Stable borrowed canvas identity before native startup. |
| `RID Space { get; }` | Stable borrowed registered physics identity, allocated on first physics use. |
| `PhysicsDirectSpaceState DirectSpaceState { get; }` | Cached live query view; disposed views are recreated. |
| `CreateDuplicateInstance()` / `CopyCustomStateTo(...)` | Borrow the complete runtime without cloning solver state; bound target replacement rejects. |
| `ValidateDisposal()` / `Dispose(bool)` | Validate unbound final native ownership and release the wrapper's runtime reference, including after a failed GPU interval. Owner/solver/live-body-callback guards remain enforced. |

## Lifecycle and limits

[Physics backend selection](../components/physics-backends.md) defines CPU/GPU creation,
startup fallback and failure semantics. Caller parameterless creation remains CPU. Duplicates
share the selected runtime, and Viewport.World replacement retains normal identity and
attachment behavior. Selection does not change with the renderer.

[The world contract](../components/worlds.md) defines scene/default versus explicit resource lifetime, wrapper recreation, shared once-per-tick simulation, world replacement and failure recovery. Scene-default identities expire with the tree; caller worlds can remain alive across hosts. RIDs remain borrowed by their owning servers. One runtime has one scene driver and one physics thread. NavigationMap supplies a real lazy map; further navigation topology/baking/avoidance features retain their own exact prerequisites.

[WorldTests](../../tests/Electron2D.Tests/WorldTests.cs) covers headless behavior and actual native shared/independent pixels; [PhysicsQueryTests](../../tests/Electron2D.Tests/PhysicsQueryTests.cs) covers established direct queries and fallback-world teardown. See [ADR 0063](../decisions/physics.md#adr-0063) and [ADR 0028](../decisions/rendering.md#adr-0028).

## Navigation map integration

World.NavigationMap now lazily owns an active borrowed map in the same runtime lifetime as canvas/physics. Scene NavigationRegion nodes and server-owned regions use that same map storage; the physics lane commits staged topology. NavigationServer is available through Engine named-service lookup. [The navigation contract](../components/navigation-maps.md) records implemented behavior and remaining dependencies.


[Shared public CPU/GPU scene checks](../components/physics-backends.md#public-scene-motion-conformance)
now run WorldTests with explicit backend selection. The linked record
separates verified motion/lifetime/allocation cases from remaining physics coverage.

Public [registered implementations](../components/physics-registered-adapter.md) now execute through PhysicsServerManager, World(string) and PhysicsServer.SpaceCreate(string). Requested/actual names retain the captured registration and startup fallback identity. The scoped PhysicsServerBackend/Extension family and immutable inputs/results have a public-only circle/spring consumer; custom geometry and full extension/performance/platform acceptance remain open.

The complete PhysicsQueryTests now verifies explicit CPU/GPU resource ownership: duplicate/recreated World wrappers retain one space and view, SceneTree disposal removes scene colliders while caller-owned aliases still allow empty queries, and releasing the final aliases invalidates retained direct views. The implicit scene-owned world has its separate SceneTree retirement policy.

WorldTests retains default CPU world lifetime and mixed default/explicit transfers, then separately verifies two explicitly selected CPU/GPU viewport worlds: actual backend, isolated scene point queries, preserved body RID and expired old attachment view on sharing, one complete public tick and 60-unit/s body travel of 1 +/- .02 scene units. This tolerance covers default damping and floating-point integration. Ninety-six warmup and 64 measured shared-world steps/lookups require 0/0 owner/all-thread managed bytes. The complete suite now runs on both backends in the ordinary full test entry and ELECTRON2D_TEST_SHARED_PHYSICS=cpu|gpu. Native allocations, wider inherited/extension, foreign platforms and final performance acceptance remain open.

## Project physics capture

`new World()` remains caller-owned CPU regardless of project defaults; enum/name overloads retain their explicit choice. Implicit scene worlds capture `Physics2DEngine` and `Physics2DAllowCPUFallback` separately before lazy startup. All aliases retain that original runtime, selection and requested/actual diagnostics.

See [the selection checks](../components/physics-backends.md#project-physics-selection).

New world/space creation captures ProjectSettings.Physics2DRunOnSeparateThread with feature overrides, retaining it across aliases and later project edits. True runs complete selected Solve on a retained numerical lane and joins before owner publication/callbacks; unsupported hosts reject startup. [Thread conformance](../components/physics-thread.md) records CPU/provider checks and the open GPU/native gate.

## World resource-copy preflight (2026-10-14)

Replacing a runtime through CopyFromResource checks the bound target, last-owner
release policy and source runtime lifetime before Resource reset, name/locality,
file ownership or Changed batching. An early refusal preserves target metadata,
canvas, physics space and navigation map. Same-runtime aliases still copy base
state even while bound; self-copy and standalone World.ResetState keep their
existing harmless behavior. Allowed unbound replacement retains the source
runtime before publishing it to the target and retiring the old final-owner
space/map. If old-backend cleanup fails, the original error escapes while the
new shared runtime stays committed and its retained ownership remains balanced.
A later Changed failure likewise keeps accepted values under Resource's policy.

The public-only --world-copies fixture runs CPU/GPU/registered serial/separate
profiles. It checks bound and expired-source rejection, off-owner last release,
real scene callback rejection, registered status-hook borrowing, aliases and
accepted replacement/notification failure and registered retirement failure.
Explicit disposal of both new-runtime wrappers verifies balanced retention.
Physics spaces and navigation maps are checked through public identities and
stale-RID queries. After 128 warmups,
128 complete scene frames with current point queries require zero owner/all-thread
managed bytes. The prescribed 60-unit/s body advances one unit per 1/60-s frame,
within .02 units; metadata, identity, event counts and query membership are exact.
Structural copies are outside that bracket. Native/platform/network, hardware
performance and full numerical-extension acceptance remain separate.
