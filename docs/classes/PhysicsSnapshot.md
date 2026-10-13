# PhysicsSnapshot

Last updated: 2026-10-14

[Character motion validation](../components/physics-snapshots.md#character-motion-validation)
rejects impossible slide encodings and self/Area/unbound platform references before
packet or world mutation. CPU/CPU, CPU/GPU and CPU/registered serial/separate cases preserve previous
state/events and verify real movement after valid platform correction.

The supported encoding is now version 3, including a strict four-byte character
query-pose flag. Older versions reject without replacing existing payloads.
[Contact/history validation](../components/physics-snapshots.md#portable-contact-and-motion-history)
also checks destination logical shape ranges before application.

**Declaration:** `public sealed class PhysicsSnapshot`

**Source:** [PhysicsSnapshot.cs](../../src/Servers/Physics/PhysicsSnapshot.cs)

**Component:** [Portable physics snapshots](../components/physics-snapshots.md)

Owner-thread reusable bytes for one portable authoritative physical state. Storage
outlives its source world and has no native ownership or disposal requirement.

| Member | Contract |
| --- | --- |
| `PhysicsSnapshot(int maxBytes = 67108864)` | Empty storage; a budget from 48 bytes to 64 MiB. |
| `MaxBytes` | Immutable encoded byte budget. |
| `Tick`, `ObjectCount` | Metadata of the last complete captured/decoded payload; empty storage rejects. |
| `GetEncodedSize()` | Exact encoded payload length. |
| `WriteTo(Span<byte>)` | Copy into caller-owned storage; an undersized destination stays unchanged. |
| `ReadFrom(ReadOnlySpan<byte>)` | Validate exact complete encoding and copy; invalid input preserves the previous payload. |

Use PhysicsSnapshotMap for world capture and apply. Packet syntax validation does
not establish sender authority, tick ordering or compatibility with a receiving
world. See the component for supported state, versioning, budgets and boundaries.

The [rendered PhysicsNetwork client](../components/physics-network-example.md#rendered-client-window)
now draws remote interpolation and local correction smoothing in a real window
while CPU authority/late client stay headless. Public snapshot and pixel checks
verify visible separation without physical-state mutation; renderer choice and
window FPS/allocation reporting remain separate from physics/replay metrics.

[Mass roles](../components/physics-registered-adapter.md#mass-profiles-across-roles)
and [portable role correction](../components/physics-snapshots.md#portable-body-roles)
now execute explicit CPU/GPU/registered serial and separate numerical profiles.
Scene/raw automatic/custom centers, inverse dynamics, point velocity and exact
slot queries survive role changes; CPU/GPU round trips preserve pending force/
torque consumption and atomic authoring rejection. Original warm brackets retain
0/0 managed bytes. GPU uses llvmpipe; hardware and broader acceptance stay open.

[Portable contact and motion history](../components/physics-snapshots.md#portable-contact-and-motion-history)
now passes CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Imported points/impulses and character slides retain destination peer/slot identity;
raw Area/contact history stays silent during correction and avoids repeated enters.
Malformed indices/platforms preserve state and events, and real movement retains
platform carry. Warm contact cycles require 0/0 bytes; cold motion validation has
no allocation claim. Hardware and wider geometry/joint/network gates remain open.

[Capture topology](../components/physics-snapshots.md#capture-after-topology-edits)
and [same-count authoring validation](../components/physics-snapshots.md#same-count-authoring-validation)
now pass CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Each capture profile retains 24 removal/reindex/readdition cases; each geometry
profile retains 14 incompatible/restored cases across both directions. Rejection
preserves complete packets, sampled values and events; real refresh/continuation
recovers. These cold structural checks have no allocation claim. Hardware and
broader geometry/joint/network acceptance remain open.

[Portable joint continuation](../components/physics-snapshots.md#portable-joint-continuation)
now passes CPU/CPU, CPU/GPU and CPU/registered serial/separate public-only profiles.
Scene/raw pin/groove/spring frames, atomic policy rejection, private replay and
continued constraints retain their bounds; all six warm cases require 0/0 bytes.
[Off-center spring checks](../components/physics-registered-adapter.md#off-center-spring-anchors-and-portable-joints)
also retain force/moment/cap and stable damping bounds. GPU executes on llvmpipe;
hardware, broader geometry/graphs and network acceptance remain open.
