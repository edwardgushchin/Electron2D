# WorldRuntime

Last updated: 2026-10-13

- Visibility: internal
- Source: [WorldRuntime.cs](../../src/Scene/Resources/WorldRuntime.cs)
- Component: [Canvas and physics worlds](../components/worlds.md)

## Description

Retains the requested physics backend and startup-fallback policy until lazy space
creation. Public wrappers expose the requested choice separately from the created
space's actual backend and diagnostic; duplicates share the same selection and runtime.

Retains complete world identity independently of public resource wrappers: canvas, lazy registered physics space, resource references, scene driver lease and default-scene expiration policy. Lifetime changes share a cold gate; Alive uses a volatile read from the canvas registry to avoid inverse lock acquisition. It borrows no native backend handle into the public API. Physical access and world-binding changes keep the existing PhysicsSpace thread/callback guards.

Last-resource release uses the physics release guard so a failed GPU interval
does not prevent disposal. Owner-thread, active-solver and live body callback
restrictions remain enforced. Binding a failed world still rejects.

## Verification

[WorldTests](../../tests/Electron2D.Tests/WorldTests.cs) checks sharing, transfer, wrapper replacement, query identity, notifications, once-per-tick simulation and real GPU/compatibility canvas lifetime. [The world contract](../components/worlds.md) records current limits.

## Navigation map integration

World.NavigationMap now lazily owns an active borrowed map in the same runtime lifetime as canvas/physics. Scene NavigationRegion nodes and server-owned regions use that same map storage; the physics lane commits staged topology. NavigationServer is available through Engine named-service lookup. [The navigation contract](../components/navigation-maps.md) records implemented behavior and remaining dependencies.

## Registered initialization

Explicit and lazy scene worlds now use PhysicsServer.CreateRegisteredSpace. It allocates and registers an immutable space RID before invoking the selected implementation factory outside the registry lock. Metadata/settings exist first; ordinary query access rejects until an implementation is retained. The initialization phase can then construct guarded direct-space extensions and query the real selected world. Activation, stepping, attachment changes, recursive initialization and release reject until it completes. Explicit spaces publish inactive; scene spaces become active after successful initialization.

Failure retires owned/direct-state/space registrations and releases any implementation retained by that space. A foreign implementation is rejected without disposing its original world. Initialization and cleanup errors are aggregated after cleanup attempts. [Ownership tests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.Initialization.cs) exercise live RID lookup, registry-lock independence, all six query hooks, transfer rejection, real motion after publication and CPU/GPU native retirement. This is the internal construction transaction; public registered factories execute through the adapter described below, while complete numerical-extension acceptance remains open.

## Retained named selection

Existing CPU/GPU creation resolves built-in entries through the retained internal [PhysicsBackendRegistry](PhysicsBackendRegistry.md). A captured selection can retain its requested token and actual registration across lazy creation and shared World aliases. The common initialization transaction invokes the selected factory outside registry locks; internal name diagnostics distinguish a custom registration or built-in fallback. Public named registration and its typed operation bridge execute; complete numerical-extension acceptance remains open. See [selection behavior and checks](../components/physics-backends.md#named-implementation-selection).

[Registered numerical adapter](../components/physics-registered-adapter.md) now connects a public value/RID-only implementation to real scene/raw motion, fields/forces, joints, queries, contact/Area publication, private/portable history and terminal failure. Its independent circle/spring test solver owns its own state and does not wrap a built-in. Public registered factories and the typed family now execute in a separate consumer; arbitrary custom geometry and unexercised operation acceptance remain open.

## Project physics capture

`CreateSceneDefault()` resolves active-feature project engine/fallback settings before allocating canvas identity. It captures the retained registry selection, including DEFAULT priority, for lazy physics creation; later settings do not change it. Explicit constructors retain their own choices.

See [the selection checks](../components/physics-backends.md#project-physics-selection).
