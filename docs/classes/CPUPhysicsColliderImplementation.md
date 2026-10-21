# CPUPhysicsColliderImplementation

Last updated: 2026-10-21

**Declaration:** `internal sealed partial class CPUPhysicsColliderImplementation : PhysicsColliderImplementation` · **Source:** [CPUPhysicsColliderImplementation.cs](../../src/Servers/Physics/CPUPhysicsColliderImplementation.cs) · **Component:** [Physics backends](../components/physics-backends.md#runtime-flow-and-ownership)

## Responsibility and flow

The one RebuildShapes span consumes neutral PhysicsColliderShape values projected
from all scene/server bodies and Areas. Disabled/disposed slots retain their
logical indices without producing fixtures. Complete source preflight occurs
before mutation; this owner retains concrete geometry validation and storage.
See [shared inputs](../components/physics-backends.md#shared-collider-shape-inputs).

Sleep-capable non-static attachment, motion-mode changes and CanSleep writes
prepare dormant capacity before numerical use, including kinematic bodies.
See [the first-sleep and wake checks](../components/physics-sleep.md#kinematic-dormant-capacity).

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

Built-in CPU/GPU implementations remain internal. Public registered implementations and typed direct-state/custom-geometry paths execute under [ADR 0103](../decisions/physics-extensions.md#adr-0103), with bounded numerical evidence and complete extension acceptance still open; this concrete type is not a public plugin API. Native allocator, foreign-platform and real-window FPS acceptance remain separate gates.

CPU dynamic attachment creation, sleep enabling and live mode changes now notify the owning CPU world before sleeping capacity can be needed. The conservative world flag persists across replay and retired attachments, avoiding repeated no-sleep population scans without reducing dormant capacity.

World-point platform velocity now dispatches through the current selected attachment. Common lookup uses the shared scene/raw RID registry and checks current world ownership, including transfer/detach/free. CPU keeps direct world-point native conversion; GPU keeps resident center-aware surface motion. See [motion ownership and checks](PhysicsWorldBackend.md#body-motion-and-platform-lookup).

## Selected integration preparation

[Integration preparation](../../src/Servers/Physics/CPUPhysicsColliderImplementation.Integration.cs) owns pending/constant force and torque budgets, omission clearing and native gravity setup. ApplyResolvedFields combines scene/raw policy, validates all fields and candidate motion before mutation, and publishes totals to PhysicsBodyRuntime. Native transient/field helpers are private. [Contract and verification](../components/physics-backends.md#selected-body-integration-preparation).

## Procedural resource geometry

Runtime geometry decisions now use ShapeGeometry's represented primitive rather than concrete resource classes. Caller-defined Shape resources retain the same full-contour, infinite-boundary, directed-ray and zero-geometric-mass policies. Compound CPU contour metadata borrows any Shape with a large convex payload. Scene/raw geometry construction is preflighted before retiring existing CPU/GPU fixtures, so a failing protected hook leaves preparation retryable. See [procedural shape verification](../components/physics-shapes.md#caller-defined-shape-resources).

## Selected fields, exceptions and portable observation

Resolved fields and exception invalidation dispatch through the selected collider/world; common server/replay/map code retains no GPU handle/state aliases. Portable preparation and bound-body observation use the selected owner and borrowed common inputs. GPU pair/batch scratch stays private; CPU preserves existing field/fixture preparation. See [the executing contract and limits](../components/physics-backends.md#selected-observation-and-collision-policy).

## Value-only integration preparation

[Force and policy inputs](../components/physics-backends.md#value-only-body-integration-inputs) now separate engine-owned authoring/command state from numerical preparation. Default forces use PhysicsBodyForceConfiguration; CPU fields and GPU parameters use PhysicsBodyIntegrationConfiguration. Pending command retirement and GPU dirty flags stay in the binding layer; numerical methods receive finite authored values and return complete field/preparation results. CPU/GPU/no-device, replay, parameter and full operation checks remain the acceptance boundaries; complete registered numerical acceptance stays open.

[Custom collision geometry](../components/physics-custom-geometry.md) now provides copied canonical data, exact mass/bounds, diagnostic segments and standalone pair callbacks to registered implementations. Built-in compilation rejects unsupported custom geometry before mutation. Portable schema covers the custom authoring payload; detached force preparation resolves exact custom mass while automatic getters retain cached-resolution semantics.


Material and density updates retain native fixture identity when every logical slot,
geometry value identity, transform, bias, filter and sensor value matches.
A retained ShapeInput array compares authored metadata; actual geometry changes
use the complete creation path. Existing material fields are preserved while
friction/restitution/flags and density update, and touching bodies wake. Density
changes defer native mass recalculation to the owner's validated profile. Role
changes therefore avoid recreating unchanged fixtures and their tags. Retired cache suffixes
and detach clear references. [Contact verification](../components/physics-registered-adapter.md#independent-contact-response)
checks changed geometry, signed material response, replay and 128 changed-material
full-frame cycles at 0/0 managed bytes on CPU; native totals remain unmeasured.


Authored body, target, query/motion and joint-frame angles now use
PhysicsShapeBackend.RotationToBackend (ordinary sine/cosine) at the CPU boundary.
An approximate .37-rad conversion displaced a far analytic plane by about 7.3
scene units; the shared public consumer now verifies the rotated plane at tangent
distance 6,000 within .005 units. Numerical integration is unchanged. See
[boundary verification](../components/physics-registered-adapter.md#registered-boundary-numerics)
for the zero-allocation, lifecycle, impulse and remaining acceptance limits.

## Reusable shape compilation metadata

Actual geometry changes retire every old fixture before reusing private fixture
tags. Each tag refreshes logical identity, weak geometry source, one-way policy,
compound contour pose/piece and directed-ray values. Sensor visitor capacity is
reused by the owning CPU world and released on world destruction. Neither cache
owns application Shape resources. [Shared edit verification](../components/physics-queries.md#failed-raw-shape-edits-and-warmed-reuse)
keeps compound/ray queries and sensor history alongside strict warm allocation checks.


[Special source motion](../components/physics-registered-adapter.md#special-source-motion)
retains represented family across physical pieces and live shape replacement.
Initial recovery/residual stopping still executes; ignored special displacement
cannot manufacture a new sweep, and two concave resources cannot contact through
private edge pieces. Shared public CPU/software-GPU/registered assertions cover
logical slots, analytical boundaries, fractions and zero warmed full-frame bytes.
