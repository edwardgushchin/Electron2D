# 0003: Use deterministic IDisposable lifetime for engine objects

Last updated: 2026-09-20

- Status: Accepted; refined by [0009](0009-disposal-callback-access.md) and clarified by [0014](0014-managed-resource-lifetime.md)
- Scope: `ElectronObject` and future resource-owning derived types

## Context

Godot objects use engine-managed validity and explicit `free()`. Electron2D runs on .NET, while future SDL windows, renderers, textures, audio devices, and other resources will own native handles that cannot wait for ordinary garbage collection.

## Decision

`ElectronObject` implements the standard extensible .NET dispose pattern:

- public non-virtual idempotent `Dispose()`;
- protected virtual `Dispose(bool disposing)` for derived cleanup;
- protected precondition validation before disposal begins, used by owner-thread-bound derived types;
- immediate disposed-state publication when disposal begins;
- a typed `Disposed` event after successful cleanup;
- `ThrowIfDisposed()` for live-object preconditions.

The base uses atomic state transitions so concurrent disposal runs cleanup at most once. It has no finalizer. Native handles must be owned by `SafeHandle`-derived wrappers, whose finalization protects against missed disposal without putting every engine object on the finalizer queue.

## Consequences

- Deterministic logical/native-resource ownership is explicit and compatible with `using`; managed object memory remains owned by the runtime.
- Repeated or concurrent `Dispose()` calls are safe.
- Operations can fail deterministically with `ObjectDisposedException`.
- Invalid disposal callers can be rejected without poisoning the object into a partially disposed state.
- Derived classes are responsible for releasing only resources they own and for calling the base override.
- If derived cleanup throws, cleanup is not retried and `Disposed` is not raised.

## Rejected alternatives

- Depend only on garbage collection: rejected because GC does not deterministically release native SDL resources.
- Add a finalizer to `ElectronObject`: rejected because the base owns no unmanaged handle and finalization overhead would apply to every engine object.
- Mirror Godot `free()` and invalid non-null references: rejected in favor of the standard C# lifetime contract.
