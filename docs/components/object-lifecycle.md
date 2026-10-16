# Object lifecycle component

Last updated: 2026-10-16

## Scope

This Core component defines identity, diagnostics, notifications, translation access, validity, and deterministic cleanup shared by engine-owned objects.

## Owned types

| Type | Source | Role |
| --- | --- | --- |
| [`ElectronObject`](../classes/ElectronObject.md) | [`ElectronObject.cs`](../../src/Core/Object/ElectronObject.cs) | Abstract base for engine objects |
| [`WeakRef<T>`](../classes/WeakRef.Generic.md) | [`WeakRef.cs`](../../src/Core/Object/WeakRef.cs) | Typed non-owning reference to a live engine object |

## Current implementation status

Implemented and covered by the executable checks. The component includes notification dispatch, typed property-list/script-change hooks, and disposal precondition validation in addition to identity and lifetime.

## Dependencies

The component uses .NET threading and exception-dispatch primitives. Its `Tr`/`TrN` entry points call the [Localization component](localization.md); it has no SDL3-CS dependency.

## Runtime behavior

Each instance receives a non-zero process-local `ulong InstanceID` from a static counter using `Interlocked.Increment`. `ClassName` exposes `GetType().Name`; `ToString()` returns `<ClassName>#<InstanceID>`. `Notify(int)` dispatches to `OnNotification(int)`. IDs 0 and 1 are reserved for post-initialization and pre-delete notifications; disposal sends pre-delete before resource cleanup.

Lifetime follows this state transition:

```text
Alive --Dispose()--> PreDelete --accepted--> Disposing --Dispose(bool) succeeds--> Disposed
                         |                                      |
                  CancelDisposal                                Disposed event
                         |
                       Alive
```

Before attempting the atomic transition, each caller may run `ValidateDisposal()` so a derived type can reject that caller. `Node`, `MainLoop`, and `SceneTree` use it to enforce owner-thread or active-callback disposal rules. Concurrent callers can validate at the same time or race with another caller beginning disposal, so overrides must be side-effect-free and race-tolerant.

The first valid caller atomically changes the state from `Alive` to automatic pre-delete and records its thread. Concurrent and reentrant `Dispose()` calls return during an active attempt. Terminal calls return without repeating cleanup; a vetoed attempt may be retried later. `IsDisposed` becomes `true` when disposal starts. Guarded state remains readable through `ThrowIfDisposed()` only on the recorded thread while it runs pre-delete and derived cleanup callbacks; other threads are rejected. Without a veto, the base reaches `Disposed`, clears exactly its own `Disposed`, `PropertyListChanged`, and `ScriptChanged` subscriber lists, and suppresses finalization. `Disposed` is raised once only when both pre-delete notification and `Dispose(bool)` succeed.

`PropertyListChanged` and `ScriptChanged` are synchronous typed C# events raised through protected methods. The script-change hook is implemented for the confirmed future scripting component, but no current production type attaches scripts or raises it automatically.

The separate [Typed event connections](event-connections.md) component can own, defer, or consume subscriptions to these events without changing their declarations.

`WeakRef<T>` uses a managed weak reference and returns the original target only while it is reachable and is neither in an active disposal attempt nor terminally disposed. Constructing it with null or a disposed target leaves it empty. Disposing the wrapper leaves the target untouched; disposing the target invalidates `GetRef()` immediately, even if the target is still strongly held elsewhere. The [typed weak-reference decision](../decisions/core-object-runtime.md#adr-0050) defines this lifetime boundary.

A failure in notification or cleanup is rethrown after state publication; simultaneous failures become `AggregateException`. A derived type controls when its own event lists are cleared and may leave derived cleanup incomplete if its override throws.

`ElectronObject.IsQueuedForDeletion` is a nonvirtual atomic observation inherited by every object role. SceneTree counts independent explicit requests until execution or discard; Node contributes its cancellable generation through claimed execution. Cancellation affects only Node work, and queued parents do not mark children. The observation stays true during pre-delete and cleanup, then reads false after terminal disposal. Direct disposal without a queue stays false. Readonly nonstored descriptor discovery adds no serialized state or revert value, scheduling API, numerical mirror or backend wait. The base lifetime state machine and its validation remain unchanged.

`CancelDisposal()` is a nonvirtual helper for the exact automatic pre-delete thread. It marks that attempt canceled without publishing live state during notification. On return, a canceled attempt restores Alive and its weak identity, retains subscribers/resources and skips all derived cleanup. Notification errors still propagate after restoration. Manual/outside/foreign calls do not arm cancellation, and committed cleanup cannot be canceled. Ordinary non-vetoed attempts keep the prior disposal order and errors. The state fits the existing lifetime word and adds no object field, backend read, native fence or managed allocation.

Weak identity stores distinguish temporary pre-delete closure from committed cleanup. Read-side unavailability never retires a veto-capable target. Sweep/registration cleanup retains physical/rendering RIDs, ResourcePath ownership, owner reverse records and unique namespaces, group membership and writer registrations until commit or collection. Name/path reservations stay occupied during notification; ordinary live lookup still hides the closed target. Registry probes cover in-notification lookups, 256-registration sweeps, restoration and terminal release.

## Threading

- Instance ID allocation is safe under concurrent construction.
- Entry into disposal is safe under concurrent `Dispose()` calls and cleanup runs at most once.
- `IsDisposed` uses volatile reads.
- The disposing-thread allowance supports teardown inspection only; arbitrary re-entrant mutation is not promised safe.
- The component does not make members introduced by derived classes thread-safe.
- Concurrent event subscription and disposal have ordinary C# event semantics; no stronger ordering guarantee is provided.

## Ownership rules

- Derived types release owned managed resources from `Dispose(bool)` when `disposing` is `true` and then call the base implementation.
- Types owning native SDL resources must wrap native handles in `SafeHandle`-derived types.
- `ElectronObject` has no finalizer because it directly owns no unmanaged handle.

## Exclusions

The component does not provide object lookup, public manual reference counting, weak-event infrastructure, a service container, metadata, script attachment/runtime, dynamic method calls, or string-addressed signals. A future Resources-domain asset manager may count internal disposable leases to control shared native payload retention without changing `ElectronObject` lifetime. Deferred execution belongs to `SceneTree`.

## Verification

The executable check in `tests/Electron2D.Tests/Program.cs` covers:

- 10,000 parallel constructions with unique non-zero IDs;
- runtime class name and diagnostic string;
- 1,000 concurrent calls to `Dispose()`;
- one derived cleanup and one `Disposed` event;
- guarded state access during pre-delete on the disposing thread;
- `ObjectDisposedException` after cleanup through `ThrowIfDisposed()`.
- arbitrary and pre-delete notification delivery.
- property-list and script-change notifier delivery.
- per-object translation flags and domains through the Localization domain.
- disposal precondition rejection before the disposed state begins, through Scene tests.

Release build and executable checks pass on .NET SDK 10.0.101 targeting .NET 8.

Typed property inspection uses the exact teardown-thread exception from ADR 0009: GetValue, TryGetRevertValue and CanRevert remain usable in pre-delete/cleanup on that thread; foreign-thread and terminal access fail. SetValue and Revert reject once disposal starts. TryGetRevertValue reports factory presence even for a null or unchanged default, while CanRevert and PropertyCanRevert retain the current value-difference policy. Factories run on each request; their results follow delegate ownership with no implicit copy. Validators reject before the setter, and an exception after a setter commits does not undo that write. Ordinary owner setters and getters retain thread, capture and numerical-phase rules.

[ObjectPropertyTests](../../tests/Electron2D.Tests/ObjectPropertyTests.cs) executes discovery snapshots, compatible replacement/filtering and invalid lists, owner policy, live/default/null/array values, readonly/validation/delegate errors and teardown inspection. Explicit CPU/GPU serial and separate-thread profiles use real RigidBody properties, permitted post-solver mass edits, owner/capture rejection and callback-error recovery. Each retains 128 warm/128 measured full frames with typed inspection, velocity writes and reversion at strict zero owner/all-thread managed bytes. GPU runs use CPU-only Vulkan; this does not establish hardware performance, editor automation or native/platform acceptance.

## Per-object ordinary event blocking

The inherited [ElectronObject delivery policy](../classes/ElectronObject.md#ordinary-event-delivery) applies at publication, including direct C# subscribers and EventConnection tokens. A blocked publisher drops new ordinary emissions without removing listeners, consuming one-shot tokens or submitting deferred calls. Existing accepted deferred work retains its usual cancellation rules. Each emission selects once; changing the flag inside a handler affects the next emission. Unblocking does not replay suppressed transitions.

Numeric notifications, virtual callbacks, operation callbacks, committed state and resource revisions remain active. Deterministic Disposed cleanup remains independent. Physical handler buffers select an empty snapshot while blocked, preserving existing payloads, per-handler eligibility, exception policy and subscription capacity. Resource geometry/material polling still observes committed edits. Static facades use their retained service object's policy.
