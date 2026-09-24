# WeakRef\<T\>

Last updated: 2026-09-24

**Inherits:** [`ElectronObject`](ElectronObject.md)

**Inherited By:** —

- **Source:** [`src/Core/Object/WeakRef.cs`](../../src/Core/Object/WeakRef.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class WeakRef<T> : ElectronObject where T : ElectronObject`

> Holds a typed, non-owning reference to an engine object.

## Description

`WeakRef<T>` observes an engine object without keeping it alive or owning its disposal. `GetRef()` returns the same object while it remains alive. It returns `null` when the target has begun disposal or has been collected, including when a disposed object is still strongly held by caller code. A null or already disposed target creates an empty reference. The wrapper itself has the ordinary [`ElectronObject`](ElectronObject.md) disposal lifetime.

## Example

```csharp
using var texture = new ImageTexture();
using var reference = new WeakRef<ImageTexture>(texture);
ImageTexture? live = reference.GetRef();
```

## Constructor

| Member | Description |
| --- | --- |
| `public WeakRef(T? target)` | Observes a live target, or creates an empty reference for null or a disposed target. |

## Method

| Member | Description |
| --- | --- |
| `public T? GetRef()` | Returns the live target, or null after disposal or collection. Throws `ObjectDisposedException` after this wrapper is disposed. |

The returned managed reference keeps the target reachable while held by the caller, but does not prevent another thread from disposing the target after `GetRef()` checks it. Callers must still respect the target's own lifetime and threading rules.

## Verification

`WeakRefTests` checks live identity, null and disposed construction, target disposal, wrapper disposal, and collection of an otherwise unrooted target in the managed test host. Platform-host lifetime behavior is not separately verified. The typed contract is defined by [ADR 0050](../decisions/core-object-runtime.md#adr-0050); the [coverage page](../coverage/classes/WeakRef.md) tracks the reference API pairing.
