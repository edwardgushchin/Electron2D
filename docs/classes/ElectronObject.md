# ElectronObject

Last updated: 2026-09-21

## Declaration

- Source: [`ElectronObject.cs`](../../src/Core/Object/ElectronObject.cs)
- Namespace: `Electron2D`
- Declaration: `public abstract class ElectronObject : IDisposable`
- Domain: [Core](../domains/core.md)
- Components: [Object lifecycle](../components/object-lifecycle.md), [typed editor properties](../components/editor-properties.md), and [translation](../components/localization.md)

## Responsibility

`ElectronObject` is the common base for engine-owned objects. It provides process-local identity, runtime diagnostics, numeric notification dispatch, deterministic cleanup, disposed-state protection, typed property exposure, per-object translation settings, and lifecycle events. It does not emulate Godot's Variant-based dynamic API.

## Constants

| Constant | Value | Meaning |
| --- | --- | --- |
| `NotificationPostInitialize` | `0` | Reserved post-initialization notification; callers must dispatch it explicitly because constructors cannot safely invoke virtual members |
| `NotificationPreDelete` | `1` | Sent automatically immediately before `Dispose(bool)` |

## Public API

| Member | Current behavior |
| --- | --- |
| `ulong InstanceId { get; }` | Non-zero identifier allocated with `Interlocked.Increment`; meaningful only within the current process |
| `string ClassName { get; }` | Runtime type name from `GetType().Name` |
| `bool IsDisposed { get; }` | `true` from the moment disposal starts |
| `bool CanTranslateMessages { get; set; }` | Enables or disables translation for this instance; defaults to `true` and is stored by packed node scenes |
| `string TranslationDomain { get; set; }` | Domain passed to `TranslationServer`; defaults to the empty domain and is stored by packed node scenes |
| `event Action<ElectronObject>? Disposed` | Raised once after successful notification and resource cleanup |
| `event Action<ElectronObject>? PropertyListChanged` | Raised by derived types when their exposed property set changes |
| `event Action<ElectronObject>? ScriptChanged` | Raised by a derived or future scripting component when its script attachment changes; no script attachment exists yet |
| `void Notify(int what)` | Delivers a numeric notification to `OnNotification`; rejected after disposal starts |
| `IReadOnlyList<PropertyDescriptor> GetPropertyList()` | Builds, validates, checks for duplicate names, and returns the current tooling property list |
| `bool PropertyCanRevert(PropertyDescriptor property)` | Reports whether the current value differs from a descriptor's revert value |
| `void RevertProperty(PropertyDescriptor property)` | Restores a descriptor's typed revert value |
| `string Tr(string message, string? context = null)` | Resolves a singular message through `TranslationServer` or returns the source message |
| `string TrN(string singular, string plural, long count, string? context = null)` | Resolves a plural message or uses the source singular/plural fallback |
| `void Dispose()` | Non-virtual, idempotent, thread-safe entry point for deterministic cleanup |
| `string ToString()` | Returns `<ClassName>#<InstanceId>` |

## Protected API

| Member | Current behavior |
| --- | --- |
| `OnNotification(int what)` | Override point for numeric engine notifications; derived overrides call the base implementation |
| `ValidateMutation()` | Checks mutable base state before translation setters commit; derived types may add atomic-operation barriers |
| `GetPropertyDescriptors()` | Returns the unvalidated descriptor sequence; the base exposes identity, state, and translation settings |
| `ValidateProperty(PropertyDescriptor property)` | Returns the original descriptor, a replacement, or `null` to hide it |
| `CanRevertProperty(PropertyDescriptor property)` | Override point for custom revert policy |
| `NotifyPropertyListChanged()` | Raises the typed `PropertyListChanged` event |
| `NotifyScriptChanged()` | Raises the typed `ScriptChanged` event after checking that the object is alive |
| `ValidateDisposal()` | Rejects caller-specific disposal preconditions before the atomic state transition; the base accepts every caller |
| `Dispose(bool disposing)` | Override point for derived resource owners; public disposal passes `true` |
| `CompleteFailedConstruction()` | Assembly-only derived helper that publishes terminal disposal without notifications, derived cleanup, or `Disposed`; the failed constructor still releases its partial resources explicitly |
| `ThrowIfDisposed()` | Throws once disposal starts, except on the one thread currently running this object's pre-delete/cleanup callbacks |

## Lifecycle contract

`Dispose()` first returns when disposal has already started, then calls `ValidateDisposal()` before that caller attempts the atomic transition from alive to disposing. The winning caller records its thread, sends notification `1`, calls `Dispose(bool)`, publishes the final disposed state, clears the `Disposed`, `PropertyListChanged`, and `ScriptChanged` subscriber lists, and suppresses finalization. Concurrent calls that reach the transition after another caller return without repeating work.

`IsDisposed` is already `true` during notification and cleanup. `ThrowIfDisposed()` nevertheless permits the recorded disposing thread to inspect guarded state so pre-delete and derived teardown callbacks can identify and detach the object. Other threads are rejected as soon as disposal starts; after final publication every thread, including the former disposing thread, is rejected.

Notification and cleanup exceptions are both preserved: one exception is rethrown with its stack, while simultaneous failures become `AggregateException`. `Disposed` is raised only when both stages complete successfully.

[`EventConnection`](EventConnection.md) can own subscriptions to the object's typed events, consume one emission, or route delivery through an explicit scheduler such as `SceneTree.Defer`. The events themselves continue to pass the publishing object as their argument.

Derived resource owners dispose their resources and call the base override:

```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
        _ownedResource.Dispose();

    base.Dispose(disposing);
}
```

## Invariants and threading

- Instance IDs do not change; parallel allocation is safe.
- Notification delivery is synchronous on the caller's thread.
- Disposal entry and disposed-state publication are thread-safe and cleanup runs at most once.
- The disposing-thread exception is scoped to `ThrowIfDisposed`; it is not a general synchronization guarantee and does not make re-entrant mutation during teardown safe.
- `ValidateDisposal()` may run concurrently in more than one caller and may race with another caller beginning disposal; overrides must be side-effect-free and tolerate that race. A validation failure prevents its own caller from starting disposal but cannot prevent another valid caller.
- Translation configuration and property events have their documented member-level synchronization only; derived mutable state is not made thread-safe. `Node` overrides mutation validation so inherited translation setters cannot change a hierarchy during packed capture.
- Reference equality remains standard .NET reference equality.

## Native resources

The class has no finalizer. Derived SDL resource types must put native handles in `SafeHandle` wrappers and dispose them from `Dispose(bool)`.

## Deliberate omissions

- No `Variant`, `dynamic`, string-based `Get`, `Set`, or `Call`.
- No metadata bag, script attachment, script runtime, or generic signal registry. `ScriptChanged` is the typed notification contract reserved for the confirmed future scripting component; nothing raises it automatically yet.
- No persistent event connections; in-memory packed scenes intentionally omit subscribers because a typed stable endpoint schema does not yet exist.
- No global registry or lookup by `InstanceId`.
- No queued deletion; that behavior belongs to [`Node`](Node.md) and [`SceneTree`](SceneTree.md).

## Verification

[`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) verifies identity, diagnostics, notification dispatch, pre-delete state access on the disposing thread, concurrent idempotent disposal, property-list and script-change event delivery, duplicate subscription/removal, typed event connections, typed property behavior, translation delegation, and invalid access after disposal.
