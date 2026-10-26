# Rigid-body declared contract

Last updated: 2026-10-27

The pinned RigidBody2D family at `ed1daf0bf001b61586d9930840f2f1394092c079`
contains 54 members: four enums, nine enum values, thirteen methods, twenty-three
properties and five signals. None is deprecated. The reverse audit uses the
compiled RigidBody surface, its supporting enums and its eight source files.
The constructor, property descriptors and scene factory project existing
construction, storage and inherited lifecycle roles. Inherited declarations stay
on PhysicsBody, CollisionObject, Entity, CanvasItem, Node and ElectronObject.

The public-only `--rigid-contract` consumer composes the existing numerical and
lifecycle checks rather than replacing them with signature checks.

The accepted adaptations remain explicit: ADR 0057 owns scene-unit force and
world-axis offset rules; ADR 0070 runs the scene integration hook after solving
with immutable completed contacts and synchronized scene state; ADR 0073 resolves
mass profiles immediately. Contact-point reporting can remain enabled while
object monitoring is disabled. The class audit preserves these existing decisions.

| Family | Exercised contract |
| --- | --- |
| Defaults and storage | All 23 detached defaults; combined nondefault packed configuration, independent body RID, borrowed material and retained dynamic sleep request |
| Forces and axis velocity | Central/positioned/rotational transient and persistent input, world-axis offsets, current selected motion, finite-input rejection, clearing and detached/reentry boundaries |
| Mass and fields | Automatic/custom center and inertia, changed geometry, rotation lock, gravity and both damping policies, signed materials, shared-resource updates and failed publication recovery |
| Motion and activity | Dynamic/static/kinematic/linear-only roles, explicit and automatic sleep, CanSleep, disable/freeze/reentry and configuration retained across role changes |
| Continuous motion | All three configured CCD modes, actual impacts, moving frozen kinematic paths, reporting-only intermediate contacts and directional masks |
| Contacts and events | Bounded points, deduplicated bodies, logical shape identities, object-before-shape delivery, five typed events, sleep transitions, callback mutation/failure and departure without replay |
| Integration | Ordinary/custom integration, omission with retained impulse response, force/scene/sync callback order, typed userdata, lifetime and numerical-borrow guards |

## Authored sleep permission on the solver thread

BodyGetCanSleep reads retained configuration through the same guarded server
parameter boundary for scene and raw bodies. A selected implementation can read
it during Solve on the world's borrowed numerical thread. The scene property
continues to require its owner or current processing group; the server read does
not grant that thread general scene access or perform a solver query.

The complete composed separate-thread run exposed the former scene-property
forwarding failure. Both CLR and NativeAOT controls fail before the correction.
The runtime now reads the retained scene permission directly after the common
RID, lifetime, owner/borrow and failed-world preflight. The regression checks
configured reads, rejected numerical writes, unrelated-thread reads, wrong-kind
and freed RIDs, and terminal-world rejection. A separate assertion keeps direct
scene CanSleep access rejected on the solver thread. The same runtime getter also
serves portable body capture; it does not introduce a second stored value.

## Independent registered kinematic paths

The external consumer's numerical implementation formerly selected continuous
intervals only for dynamic CCD bodies. Its full frozen-body check exposed a
missed fast kinematic impact. Moving kinematic bodies now use its existing full
shape-sweep scheduler independently of their configured dynamic CCD mode. This
is a correction to the external test provider, not a replacement of either
built-in solver.

Reporting-only pairs also retain intermediate encounters when either direction
selects them. The shared public regression crosses a static peer, verifies zero
impulse and the complete target pose, then checks one departure on the idle frame
and no encounter when both masks reject. Both bodies retain CCD Disabled.
Existing fast-impact checks retain one outer integration callback and the full
duration of a pending force; warmed moving frozen frames keep their zero-byte gate.

## Verification boundaries

Identity, enum/default/configuration values, event counts and rejection behavior
are exact. The new path endpoint permits 0.001 scene units of integration error.
The composed suites retain their existing analytic force/mass/field tolerances,
CCD geometric bounds and contact/impulse invariants; none is widened for a
provider. Numerical checks do not require bitwise equality between backends.
Existing measured active, idle, changed-configuration and replay intervals retain
zero owner/all-thread managed-byte limits after their original warmups.

## Qualification

All twelve complete profiles pass: CLR and strict full-trim NativeAOT, each on
CPU with unavailable graphics ICDs, NVIDIA GPU without fallback, and an independent
registered provider, in ordinary and separate numerical modes. Their original
active/idle/change/replay brackets retain exact 0/0 owner/all-thread managed bytes.
Process maps identify NVIDIA for GPU profiles, reject software drivers, and reject
CoreCLR for native profiles. Paired functional GPU processes overlap; their cold
process durations are not throughput measurements.

The matrix uses source base `e290b158a684e0fd7216b6b32fdc19841ab9041a` plus this
slice. Actual DLL/PDB and native compiler inputs bind 2672 runtime and 289 consumer
documents. The independent editor-mode commit
`665cc28f68bb210b9c07896159289185a2bf8099` entered main during qualification.
Its nine changed consumed runtime documents resolve to the earlier Git revision;
the frozen matrix is not relabelled as that later source. Fresh integrated root
proofs match all 2672 runtime, 1001 root-test and 289 consumer documents on the
combined source. The complete registered separate-thread contract and CPU/GPU
64-body, 64-sample zero-byte physical controls pass there.
The final XML remark placement is checked by the Release/API/wiki gates; its
non-XML C# source is identical to the retained executable cohort.

Eight additional CLR/native CPU/registered ordinary/separate portable-role
profiles pass correction, continuation, configuration validation and warmed
capture/apply cycles at 0/0 managed bytes. These are in-process portable checks;
separate-process network acceptance retains its own gate.

The old external kinematic implementation fails the fast-path assertion. Both
CLR and NativeAOT controls reproduce the scene CanSleep forwarding failure on the
separate solver thread before the runtime correction. An initial external build
also exposed two internal mode probes and an unused internal service reference in
shared tests; the mode checks now use BodyGetMode and the unused reference is
removed. Exploratory/stale payload runs are excluded from the final matrix.

The root suite now runs the registered separate-thread contract by default. Its
focused entry is ELECTRON2D_TEST_RIGID_CONTRACT=1; the external consumer also exposes
--rigid-contract with the existing backend/thread selectors.

The RigidBody class row becomes Implemented; its 54 member states were already
Implemented. Physics coverage advances from 1351/1369 (98.69%) to **1352/1369
(98.76%)**, with **17 open** (9 Partial, 5 Blocked, 3 Unimplemented) and 92 Excluded.
This completes the declared-family audit and corrects two demonstrated paths.
Full numerical-extension, network, native allocation, foreign-platform,
real-window performance and complete-goal acceptance remain separate.

Receipts, pinned sources, actual compiler inputs, source/PDB proofs and failed
controls are retained under `bin/physics-rigid-contract/2026-10-27/`.
