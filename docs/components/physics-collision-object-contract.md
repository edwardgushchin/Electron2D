# CollisionObject declared contract

Last updated: 2026-10-26

The abstract [CollisionObject](../classes/CollisionObject.md) base now has a
reconciled declared contract. Its class row and 45 members are applicable in the
[pinned declaration table](../coverage/classes/CollisionObject2D.md), with no
deprecated declarations or local exclusions. This qualifies the existing API;
it adds no runtime member or behavioral adaptation. Inherited open rows remain
requirements on Entity, CanvasItem, Node and ElectronObject.

| Declared family | Rows | Contract and evidence |
| --- | ---: | --- |
| Abstract class | 1 | Common body/Area identity and geometry ownership, across all six current concrete sibling roles. |
| Shape owners | 21 | Weak arbitrary identity, sorted/reusable IDs, copied enumeration, borrowed resources, local pose and one-way settings; append-order logical indices across groups and repeated uses of a resource. |
| RID | 1 | Stable detached/attached identity, rebuild/reentry retention and invalidation on disposal. |
| Layer/mask properties and bit access | 6 | Unsigned 32-bit storage, one-based bit access and selected query/response filtering, including bit 32. |
| Collision priority | 1 | Finite positive recovery weight, live changes, retained sleep and independent rigid impulse behavior. |
| Disable property and enum | 5 | Remove/MakeStatic/KeepActive, inherited disabled processing, retained ownership, temporary body role and restoration. |
| Picking property, virtual callbacks and events | 11 | Body/Area defaults, layer/visibility/canvas eligibility, input/hover ordering and logical shape indices. |

[ADR 0071](../decisions/physics.md#adr-0071) owns owner/slot semantics;
[ADR 0072](../decisions/physics.md#adr-0072) owns disabled participation.
[ADR 0063](../decisions/physics.md#adr-0063),
[ADR 0098](../decisions/physics-contacts.md#adr-0098) and
[ADR 0099](../decisions/physics-picking.md#adr-0099) retain the identity, priority
and pointer boundaries. No architectural decision changes here.

## Composed owner and disable behavior

The public consumer's
[collision-object cohort](../../tests/PhysicsBackendExtension.Consumer/BackendChecks.CollisionObjects.cs)
reuses the complete ShapeOwnerTests, CollisionDisableModeTests, owner-read,
picking and priority cohorts. Disable tests now observe attachment through public
BodyGetSpace/AreaGetSpace instead of private backend flags.

A composed scene adds StaticBody, AnimatableBody, CharacterBody, RigidBody,
an active Skeleton-bound PhysicalBone and Area. Each uses the same resource in
interleaved owner groups with three distinct global slots. Public server slot
overrides, sensor events and point queries verify physical indices. Removing an
early shape reindexes both groups and the selected geometry. Owner disabling
removes only that group's query response. Whole-object Remove retains RID,
owner identity and resources; edits while removed reach later reattachment.
KeepActive and MakeStatic retain group policy, while enabling restores the
requested sibling role and sensor membership.

The new 128-frame allocation interval retains actual rigid motion with explicit
zero replacement damping and performs live indexed queries for all six siblings.
Existing numerical tolerances and allocation assertions are unchanged. Initial
fixture failures exposed missing prerequisites: an unbound PhysicalBone is
intentionally noncolliding, and default damping invalidated the new uniform-motion
witness. The fixture now supplies the rig and explicit damping policy; these were
not runtime defects or production fixes.

## Verification, 2026-10-26

All eight CPU/GPU × CLR/strict full-trim NativeAOT × owner/separate physical-thread
profiles pass. Each retains ten measured intervals totaling 1152 complete frames;
all **9216 measured frames in 80 intervals** allocate exactly **0/0 owner/all-thread
managed bytes**. CPU profiles run with unavailable Vulkan ICDs and map no NVIDIA
driver. GPU profiles use only the NVIDIA ICD; no software GPU is observed.
Native process maps contain no CoreCLR. Kernel intervals contain no new Xid.
These are functional checks, with no renderer window or performance comparison.

Source/PDB proofs bind the same 2626 authored runtime and 284 consumer documents
in both builds. Native executable SHA-256 is
`2c6fce487d1d378655f3bc8a4fc89b7cda271a6ddb6cf0e4b545b03591b41687`.
Evidence is local ignored `bin/physics-collision-object/2026-10-26/`: `validation.json`,
`pinned-declared-audit.json`, source/build/ILC proofs, per-profile immutable
manifests, maps, environment, telemetry and complete logs. The rejected fixture
binaries remain reconstructible through `red-payloads.json` and retained inputs.

The class row becomes Implemented; its 45 member states are unchanged. Physics
declaration coverage is **1346/1369 (98.32%)**, with **23 open** (15 Partial,
5 Blocked, 3 Unimplemented) and 92 Excluded. Full solver/extension conformance,
inherited gaps, native heap, foreign platforms, real-window/mass-load performance
and human acceptance remain open.
