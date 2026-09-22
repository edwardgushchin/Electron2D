# Object lifecycle component

Last updated: 2026-09-23

## Scope

This Core component defines identity, diagnostics, notifications, translation access, validity, and deterministic cleanup shared by engine-owned objects.

## Owned types

| Type | Source | Role |
| --- | --- | --- |
| [`ElectronObject`](../classes/ElectronObject.md) | [`ElectronObject.cs`](../../src/Core/Object/ElectronObject.cs) | Abstract base for engine objects |

## Current implementation status

Implemented and covered by the executable checks. The component includes notification dispatch, typed property-list/script-change hooks, and disposal precondition validation in addition to identity and lifetime.

## Dependencies

The component uses .NET threading and exception-dispatch primitives. Its `Tr`/`TrN` entry points call the [Localization component](localization.md); it has no SDL3-CS dependency.

## Runtime behavior

Each instance receives a non-zero process-local `ulong InstanceID` from a static counter using `Interlocked.Increment`. `ClassName` exposes `GetType().Name`; `ToString()` returns `<ClassName>#<InstanceID>`. `Notify(int)` dispatches to `OnNotification(int)`. IDs 0 and 1 are reserved for post-initialization and pre-delete notifications; disposal sends pre-delete before resource cleanup.

Lifetime follows this state transition:

```text
Alive --Dispose()--> Disposing --Dispose(bool) succeeds--> Disposed --Disposed event--> complete
```

Before attempting the atomic transition, each caller may run `ValidateDisposal()` so a derived type can reject that caller. `SceneNode`, `MainLoop`, and `SceneTree` use it to enforce owner-thread or active-callback disposal rules. Concurrent callers can validate at the same time or race with another caller beginning disposal, so overrides must be side-effect-free and race-tolerant.

The first valid caller atomically changes the state from `Alive` to `Disposing` and records its thread. Concurrent and later `Dispose()` calls return without repeating cleanup. `IsDisposed` becomes `true` when disposal starts. Guarded state remains readable through `ThrowIfDisposed()` only on the recorded thread while it runs pre-delete and derived cleanup callbacks; other threads are rejected. The base always reaches `Disposed`, clears exactly its own `Disposed`, `PropertyListChanged`, and `ScriptChanged` subscriber lists, and suppresses finalization. `Disposed` is raised once only when both pre-delete notification and `Dispose(bool)` succeed.

`PropertyListChanged` and `ScriptChanged` are synchronous typed C# events raised through protected methods. The script-change hook is implemented for the confirmed future scripting component, but no current production type attaches scripts or raises it automatically.

The separate [Typed event connections](event-connections.md) component can own, defer, or consume subscriptions to these events without changing their declarations.

A failure in notification or cleanup is rethrown after state publication; simultaneous failures become `AggregateException`. A derived type controls when its own event lists are cleared and may leave derived cleanup incomplete if its override throws.

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
