# 0009: Permit teardown inspection on the disposing thread

Last updated: 2026-09-20

- Status: Accepted
- Scope: `ElectronObject.ThrowIfDisposed()` during deterministic teardown

## Context

ADR 0003 publishes `IsDisposed` as soon as disposal starts so concurrent callers cannot continue using an object. The same guard originally rejected the thread that had just won disposal. That made pre-delete notifications and derived teardown callbacks unable to read ordinary guarded state; an attached `Node` exit callback could fail merely by reading `Name` while direct disposal detached it.

## Decision

- `IsDisposed` remains `true` from the start of disposal.
- The atomic winner records its managed thread before invoking pre-delete and `Dispose(bool)`.
- `ThrowIfDisposed()` permits that exact thread while the state is `Disposing`.
- Every other thread is rejected during disposal, and every thread is rejected after the final `Disposed` state is published.
- The exception exists for teardown inspection and cleanup. It is not a promise that arbitrary re-entrant mutation during disposal is safe.

## Consequences

- Pre-delete, exit-tree, and derived cleanup callbacks can read the object's identity and guarded state needed to detach or release resources.
- Concurrent callers still observe disposal immediately through `IsDisposed` and guarded operations.
- Cleanup remains at-most-once and uses no new public API or dependency.

## Rejected alternatives

- Publish `IsDisposed` only after cleanup: rejected because concurrent callers could continue mutating an object being torn down.
- Make all getters bypass lifetime checks: rejected because it would weaken post-disposal failure behavior globally.
- Suppress node exit callbacks during direct disposal: rejected because direct and queued deletion should retain lifecycle delivery.
