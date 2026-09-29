# DragPayload&lt;T&gt;

Last updated: 2026-09-30

**Inherits:** [DragPayload](DragPayload.md) · **Inherited By:** —

**Declaration:** `public sealed class DragPayload<T> : DragPayload` · **Source:** [DragPayload.cs](../../src/Scene/GUI/DragPayload.cs) · **Component:** [Input runtime](../components/input-runtime.md)

## Description and example

This convenience payload holds one nonnull value whose compile-time type is `T`. `Value` returns the original reference or value, without copying it. The caller retains ownership, including responsibility for disposing a Resource carried as the value.

```csharp
var payload = new DragPayload<int>(42);
int value = payload.Value;
```

## API summary

| Signature | Contract |
| --- | --- |
| `public DragPayload(T value)` | Captures one nonnull value; null throws `ArgumentNullException`. |
| `public T Value { get; }` | Returns the caller's original typed value. |

The payload is runtime-only; PackedScene does not persist an active drag. [GUIDragTests](../../tests/Electron2D.Tests/GUIDragTests.cs) verifies null rejection and typed target delivery. See [DragPayload](DragPayload.md) and [coverage](../coverage/electron2d-unmapped.md).
