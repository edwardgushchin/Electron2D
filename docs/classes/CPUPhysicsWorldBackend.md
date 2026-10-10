# CPUPhysicsWorldBackend

Last updated: 2026-10-10

**Declaration:** `internal sealed partial class CPUPhysicsWorldBackend : PhysicsWorldBackend` · **Source:** [CPUPhysicsWorldBackend.cs](../../src/Servers/Physics/CPUPhysicsWorldBackend.cs) · **Component:** [Physics backends](../components/physics-backends.md)

## Description and internal flow

Owns one native CPU world, its retained PhysicsTaskScheduler and any diagnostic GPUPhysicsWorld stages. Creation applies the existing world definition, sleep/contact/iteration settings and pre-solve callback. Partial construction releases already-created native resources; the implementation does not initialize a renderer.

WorldID exposes a borrowed internal native identity; Tasks and StageGPU expose owned internal helpers. EnsureAccess rejects disposed or solver-owned state. Step uses the common space boundary and CPU-owned phases; every CPU discrete and CCD interval reaches StepNative. EnableGPUIntegration and EnableGPUSolver retain the existing CPU-hosted diagnostic paths, which keep CPU identity. Dispose joins workers, removes native stage callbacks, releases diagnostic GPU resources and destroys the native world; cleanup errors are aggregated after attempting each owned resource. Repeated disposal is harmless.

The query partials own native ray/point scans, standalone query proxies, candidate collection, intersection/sweep refinement, manifold contact pairs and deepest rest selection. Retained lists serve all array/span overloads without output allocation for span calls. The CPU motion partial owns body candidate collection, weighted recovery, directed-ray/one-way/full-contour geometry and sweep refinement using the same Overlaps, Cast and GetManifold helpers. Dispose clears retained direct-query and motion candidates. Source: [CPU body motion](../../src/Servers/Physics/CPUPhysicsWorldBackend.Motion.cs).

CreateCollider constructs a fresh CPUPhysicsColliderImplementation for each body/Area attachment. The retained collider identity facade delegates all engine-unit body and shape operations to that concrete owner; reattachment never retargets its immutable world.

CreateJoint creates a fresh CPUPhysicsJointImplementation for each constraint attachment. Shared settings and physical RID stay in PhysicsJointRuntime. GetJointWorldBody lazily creates and reuses the CPU world's shape-free static anchor with ordinary lifetime/solver guards. Generic policy/setters/portable-frame/release operations dispatch to that attachment.

Source partials: [policy](../../src/Servers/Physics/CPUPhysicsWorldBackend.Policy.cs), [capacity](../../src/Servers/Physics/CPUPhysicsWorldBackend.Capacity.cs), [statistics](../../src/Servers/Physics/CPUPhysicsWorldBackend.Statistics.cs).

## World policy, capacity and statistics

The selected owner now applies sleep/contact thresholds, solver iterations and world joint defaults. PhysicsSpace validates and retains authored values, then dispatches implementation work. CPU owns native threshold updates, waking, worker selection, sensor scratch, solver/arena capacity and native sensor/process-statistics traversal. GPU applies resident settings and epoch invalidation, prepares existing GPU publication/report scratch and reads completed store statistics; it creates no CPU motion-history mirror.

CPU capacity preparation keeps a conservative flag once any dynamic attachment can sleep. Worlds whose dynamic bodies have never enabled sleep skip repeated complete-body scans during attachment. Enabling sleep or changing a sleep-enabled static/kinematic body to a dynamic role prepares dormant capacity immediately. The flag never resets, so private checkpoint restores cannot introduce an unobserved sleeping role. Existing dormant budgets and physics semantics are retained.

[PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks typed policy, invalid values, independent worlds, CPU native settings, selected statistics, no GPU CPU-motion storage, live role changes and disposed-owner guards. Common sleep/contact/iteration/statistics, replay and network suites remain the behavioral acceptance boundary.

## Selected step phases

CPU step phases resolve world fields and body callbacks, select workers, execute native discrete/CCD/kinematic intervals, complete poses, collect contacts in joined ranges and scan Area observations. ResultsReady becomes true only after solve advances the tick. EndStep prunes one-way history on both success and failure. The [common phase/lifetime boundary](PhysicsWorldBackend.md#selected-step-phases) owns observer publication and ordered callbacks. Source: [CPU phases](../../src/Servers/Physics/CPUPhysicsWorldBackend.Step.cs).

Body-motion tests now dispatch through this selected owner using engine-unit inputs and RID exclusions. The [common contract and verification](PhysicsWorldBackend.md#body-motion-and-platform-lookup) records geometry, identity, lookup and allocation boundaries.

## Ownership and verification

The space owner thread controls lifecycle and stepping. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks fresh implementations, distinct physical stores, unchanged requested/actual diagnostics, real published motion, independent ticks, warmed full-step allocation, native lifetime, injected worker-cleanup failure and a no-device child process. It also checks all six direct-space operations, access/lifetime/failure guards and 64 warmed hit/miss query cycles at zero owner/all-thread allocation. Existing common physics, checkpoint and network checks remain separate acceptance evidence.

## Limits and decisions

Native IDs and worker callbacks stay internal. Common body/shape/joint adapters still contain CPU/GPU branches; their complete extension boundary remains open. No new public backend capability is exported. Native allocator and foreign-platform execution remain separate gates; recorded workload-specific GPU/window measurements do not establish general superiority. See [ADR 0054](../decisions/physics-backends.md#adr-0054) and [ADR 0103](../decisions/physics-extensions.md#adr-0103).

## Area implementation ownership

[The Area partial](../../src/Servers/Physics/CPUPhysicsWorldBackend.Areas.cs) owns retained stable priority scratch, exact native overlaps, scene/raw observer scans and indexed audio containment. [Body preparation](../../src/Servers/Physics/CPUPhysicsWorldBackend.BodyState.cs) resolves fields and damping before motion and solver integration. Native boundary/directed-ray overlap rules are preserved; Dispose clears field references. Shared event histories remain in PhysicsSpace. [Contract and checks](../components/physics-backends.md#area-implementation-ownership) cover both selected backends and no-device CPU.
