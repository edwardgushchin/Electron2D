# PhysicsSnapshot

Last updated: 2026-10-10

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
