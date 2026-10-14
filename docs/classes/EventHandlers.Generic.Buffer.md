# EventHandlers.Generic.Buffer

Last updated: 2026-10-14

- **Source:** [EventHandlers.cs](../../src/Core/Object/EventHandlers.cs)
- **Declaration:** `internal sealed class EventHandlers<T>.Buffer`
- **Visibility:** Internal implementation only; no game-facing API.

## Description

Retained handler array, logical count and active reader count.

Each emission leases an ordered immutable handler snapshot. Duplicate handlers and last-matching multicast subsequence removal retain ordinary delegate semantics. Reentrant/concurrent changes affect later emissions; user callbacks run outside the storage lock. Subscription prepares a spare buffer for one-shot removal. Capacity or overlapping-snapshot growth can allocate; arbitrary consumer event accessors and user callbacks keep their own allocation policy. Disposal clears these publisher-owned lists after teardown. Pointer events collect per-handler errors and retain eligibility checks; overlap/contact/sleep handlers stop the current event on failure.

## Members

| Member | Contract |
| --- | --- |
| `Items` | Typed delegate entries; unused/released entries clear references. |
| `Count` | Current logical length. |
| `Readers` | Outstanding snapshot leases protected by the owning storage gate. |

## Ownership, lifecycle and errors

Snapshots are internal scoped leases used with deterministic disposal. The owner retains buffers up to observed capacity and concurrent/reentrant depth, and clears references when retired leases end. Handler array storage initializes on first non-null subscription rather than on every collider construction. No event-name registry or untyped argument dispatch is introduced.

## Verification and limits

EventHandlerTests compares 1,024 mixed delegate add/remove operations with native delegate lists, checks nested snapshot changes/errors, 256 concurrent producers and weak-reference release from disposed publishers. All 18 physical accessors each deliver 128 prepared first one-shot callbacks at strict zero owner/all-thread managed bytes. EventConnectionPhysicsTests adds a real full selected CPU/GPU Area transition with 128 one-shot listeners and a permanent listener. Serial/separate numerical profiles use CPU-only Vulkan for GPU kernels; native allocations, hardware performance, real-window FPS and foreign acceptance remain open.

See [ADR 0010](../decisions/core-object-runtime.md#adr-0010) and [event connections](../components/event-connections.md).
