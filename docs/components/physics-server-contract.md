# Physics server declared contract

Last updated: 2026-10-27

The pinned reference at `ed1daf0bf001b61586d9930840f2f1394092c079` declares
214 PhysicsServer2D members (119 methods, 16 enums and 79 enum values) and
140 PhysicsServer2DExtension methods. None is deprecated. The server's eight
selector enums and their 47 values remain Excluded because the accepted typed
operations replace those selector bags. All 259 methods have applicable paths;
no operation is excluded by this audit.

[ADR 0063](../decisions/physics.md#adr-0063),
[ADR 0095](../decisions/singleton-services.md#adr-0095) and
[ADR 0103](../decisions/physics-extensions.md#adr-0103) define the adaptation:
static service operations own logical resources, configuration and observers;
each selected implementation owns numerical state through typed world,
collider, joint, query, direct-state and history hooks. Common metadata getters
do not require duplicate backend storage. The extension inherits the common
object role rather than replacing the process service. A shared hook may serve
several reference operations, and typed setters/getters split selector operations.

The reverse audit covers the compiled server, backend/extension, collider and
joint boundaries and their immutable input/result families. Factories, guarded
resource operations and direct-state projections reach the selected owner.
Raw native pointers, Variant containers, global singleton replacement and public
Instance accessors are absent by the accepted adaptations, not deferred stubs.

## Executed operation families

The public-only `--server-contract` consumer composes existing behavior checks.
It accepts `--cpu`, `--gpu` or the independent registered provider, with optional
`--threaded`. Each check retains its existing numerical bounds and allocation
assertions. A signature match alone does not satisfy a family.

| Family | Shared behavior checks | Selected path |
| --- | --- | --- |
| Shape resources | ShapeResources, ShapeGeometry, ShapePolicy, ShapePairs | Eight factories; pending data; exact type/copy/slot identity; shared edits, correction bias, contact pairs and release |
| Body configuration | BodyStates, BodyParameters, MassReset, Construction | All scene siblings and raw bodies; four roles; transform, velocities, sleeping/CanSleep, mass, inertia, center, materials, damping, CCD and reentry |
| Body input | AxisVelocity, Forces, Exceptions | Current selected motion, transient/persistent forces and impulses, omission, explicit/joint exceptions and guarded detached inputs |
| Areas | AreaParameters, AreaMonitors, Picking | Common metadata, object/canvas identity, masks, field policies, raw/scene overlap callbacks, shape slots, pickability and lifecycle |
| Joints | JointParameters, JointLifecycle | Pin, groove and spring construction, typed parameters/flags, collision policy, identity-preserving replacement, detach and release |
| World configuration | WorldParameters, SceneSpaceChecks, SpaceCreation | All nine policies, exact retained values, input rejection, inactive startup, selected initialization, scene/raw membership and stale identities |
| Queries and motion | LogicalQueries, MotionExclusions, MotionResultLifetime | All six direct-space query families, logical indices/order, filters, exclusions, nested scopes, borrowed motion output and retirement |
| Publication and diagnostics | Statistics, NotificationPhase, ContactThreshold | Actual completed counts, shared Performance projections, force/integration/state/contact/Area/sleep delivery and error cleanup |

The registered profile additionally runs SelectionChecks, FactoryViews,
DebugContacts and Lifecycle. These exercise strict default priorities/ties,
lazy aliases, factory rejection, real factory-selected views, debug point/count
validation and complete initialize/active/begin/solve/sync/statistics/flush/end/
finish dispatch with failures. The provider supplies independent numerical and
query results; it does not forward to CPU or GPU.

The broader numerical checks remain in their owning contracts:
[registered geometry and preparation](physics-registered-adapter.md),
[custom geometry](physics-custom-geometry.md),
[rigid bodies](physics-rigid-body-contract.md),
[body motion](physics-motion-operation.md),
[contact numerics](physics-registered-adapter.md#registered-contact-numerics),
[joints](physics-joints.md) and [sleep](physics-sleep.md).
These include noncircular, compound, directed and custom geometry, continuous
motion, actual impulses, private/portable restoration and failure recovery.
The server audit does not replace their physical invariants with hook counts.

## Completed samples and notification ownership

Statistics now has an additional scene-world plus owned-server-world regression.
A new world contributes zero until its first completed interval. Local inactivity
excludes only that world's retained sample; global suspension preserves both
samples. Retirement removes only the retired world's contribution. All three
selectors remain consistent with their Performance projections. The existing
128 warmed/128 measured moving frames still require exactly zero owner/all-thread
managed bytes.

Notification status is an owner-thread lifecycle read, including terminal-error
cleanup. Solve on the separate numerical thread cannot read either the backend
helper or static world status. The combined registered test checks that rejection
once before its measured interval, then checks owner-thread callbacks, nested
world delivery, errors and clearing with the existing zero-byte assertions.
It does not grant the solver general lifecycle or cross-world access.

The first combined run exposed two test-composition assumptions: priority tests
must precede tests that deliberately install the maximum default priority, and
the older notification fixture assumed solving on the owner thread. The fixtures
now preserve the accepted runtime policy. No production API or solver behavior
changes are needed for these corrections.

## Qualification boundary

The Linux cohort passes all twelve combinations of CLR/strict full-trim NativeAOT,
CPU/NVIDIA GPU/independent registered implementation and owner/separate solving.
The root CI selector adds a thirteenth registered separate-thread run. CPU and
registered runs use an invalid Vulkan ICD path and the dummy video driver; GPU
runs require NVIDIA device mappings and reject software-device mappings. The
existing strict owner/all-thread allocation assertions remain zero. No new Xid
is observed. Concurrent functional execution supplies no performance comparison.

Evidence is retained in `bin/physics-server-contract/2026-10-27/validation.json`
(SHA-256 `66155d50480b31cd98a2f91fa4d21d2bdc20713357407493f08e2610a628a1c6`).
The declaration audit contains all 354 pinned members and 350 compiled declarations
from the five server/backend/collider/joint boundary types. PDB/source proofs
qualify the actual CLR and native compiler inputs. A final XML wording correction
to PhysicsServerExtension is separately archived and verified to leave executable
C# unchanged; the final Release runtime and wiki use the corrected XML.

The consumer command is `dotnet run --project
tests/PhysicsBackendExtension.Consumer/PhysicsBackendExtension.Consumer.csproj
-c Release -- --server-contract --cpu --threaded` on one line. Select `--gpu`
or omit the backend switch for the other implementations. The root test runner
accepts `ELECTRON2D_TEST_SERVER_CONTRACT=1` for its registered separate-thread
profile. Ordinary consumer/root suites also include the expanded statistics check.

The class rows describe the audited declared operation families. Inherited
ElectronObject/script and other cross-domain gaps remain on their declaring
owners in the [unique physics ledger](../coverage/physics-status.md).
Complete numerical/network scenarios, broader NativeAOT/platform qualification, native
allocator accounting, real-window performance and a measured full GPU advantage
remain separate open requirements. Closing these two declaration rows cannot
close those requirements.
