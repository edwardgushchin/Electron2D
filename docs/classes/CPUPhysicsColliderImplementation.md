# CPUPhysicsColliderImplementation

Last updated: 2026-10-11

**Declaration:** `internal sealed partial class CPUPhysicsColliderImplementation : PhysicsColliderImplementation` · **Source:** [CPUPhysicsColliderImplementation.cs](../../src/Servers/Physics/CPUPhysicsColliderImplementation.cs) · **Component:** [Physics backends](../components/physics-backends.md#runtime-flow-and-ownership)

## Responsibility and flow

The one RebuildShapes span consumes neutral PhysicsColliderShape values projected
from all scene/server bodies and Areas. Disabled/disposed slots retain their
logical indices without producing fixtures. Complete source preflight occurs
before mutation; this owner retains concrete geometry validation and storage.
See [shared inputs](../components/physics-backends.md#shared-collider-shape-inputs).

The CPU creator retains this concrete attachment in its private collider table.
Native body/shape IDs are absent from common scene/server identities. Selected
wrappers can delegate operations while the creator resolves native storage for
queries, fields, CCD, contacts and joints. Matching detach retires the table entry;
late detach of a previous attachment preserves its replacement.

Owns one native body/world reference, logical fixture pieces, exact saved transform, reusable mass geometry and shape policy epoch. Implements CPU shape validation/creation, filters/materials/tags, state/force/mass/surface operations and current/frame contact projection in scene units. Native simulation access, field application, sleep timers and checkpoint piece lookup stay internal. Detach retires cached handles even when native destruction fails; final failed-world cleanup belongs to the selected world.

Common resource RID, object/canvas association, collision priority and monotonically increasing AttachmentVersion stay in [PhysicsColliderBackend](PhysicsColliderBackend.md). Library guards retain owner-thread, solver and failed-world access rules. Factory selection uses the actual implementation, including CPU startup fallback; native handles never become public resource identity.

Joint local-frame sampling now uses this same selected body attachment. CPU keeps native local-point/rotation math without a decoded-angle round trip; GPU samples its current typed pose and validates the existing anchor extent.

## Lifecycle and verification

A failed attachment attempts cleanup and leaves the common facade detached. Retiring a concrete implementation clears its handles; a disposed view cannot retarget to a new attachment. [PhysicsBackendOwnershipTests](../../tests/Electron2D.Tests/PhysicsBackendOwnershipTests.cs) checks invalid creation followed by reuse, four body/Area world transfers, native retirement, stable physical/object IDs, current motion/mass/force and query geometry. Existing shared physics, GPU publication, private/portable checkpoint and network checks establish separate behavior limits.

## Limits and decisions

Only internal CPU/GPU implementations are currently constructed. Public backend registration, server/direct-state extension contexts and custom geometry remain open under [ADR 0103](../decisions/physics-extensions.md#adr-0103); this type is not a public plugin API. Native allocator, foreign-platform and real-window FPS acceptance remain separate gates.

CPU dynamic attachment creation, sleep enabling and live mode changes now notify the owning CPU world before sleeping capacity can be needed. The conservative world flag persists across replay and retired attachments, avoiding repeated no-sleep population scans without reducing dormant capacity.

World-point platform velocity now dispatches through the current selected attachment. Common lookup uses the shared scene/raw RID registry and checks current world ownership, including transfer/detach/free. CPU keeps direct world-point native conversion; GPU keeps resident center-aware surface motion. See [motion ownership and checks](PhysicsWorldBackend.md#body-motion-and-platform-lookup).

## Selected integration preparation

[Integration preparation](../../src/Servers/Physics/CPUPhysicsColliderImplementation.Integration.cs) owns pending/constant force and torque budgets, omission clearing and native gravity setup. ApplyResolvedFields combines scene/raw policy, validates all fields and candidate motion before mutation, and publishes totals to PhysicsBodyRuntime. Native transient/field helpers are private. [Contract and verification](../components/physics-backends.md#selected-body-integration-preparation).

## Procedural resource geometry

Runtime geometry decisions now use ShapeGeometry's represented primitive rather than concrete resource classes. Caller-defined Shape resources retain the same full-contour, infinite-boundary, directed-ray and zero-geometric-mass policies. Compound CPU contour metadata borrows any Shape with a large convex payload. Scene/raw geometry construction is preflighted before retiring existing CPU/GPU fixtures, so a failing protected hook leaves preparation retryable. See [procedural shape verification](../components/physics-shapes.md#caller-defined-shape-resources).
