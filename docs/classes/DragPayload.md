# DragPayload

Last updated: 2026-09-30

**Inherits:** — · **Inherited By:** [DragPayload&lt;T&gt;](DragPayload.Generic.md) and caller-defined payloads

**Declaration:** `public abstract class DragPayload` · **Source:** [DragPayload.cs](../../src/Scene/GUI/DragPayload.cs) · **Component:** [Input runtime](../components/input-runtime.md)

## Description

DragPayload is the polymorphic boundary for a single GUI drag. A producer returns a subclass from `Control.OnGetDragData` or supplies it to `Control.ForceDrag`. A target can inspect its concrete type from `Control.OnCanDropData` and `Control.OnDropData`. The scene tree borrows the payload until drop or cancellation; it does not copy, serialize, dispose, or place the value into a general dynamic property store. The root Viewport exposes the active borrowed identity through `GetGUIDragData`.

```csharp
DragPayload payload = new DragPayload<string>("item-id");
if (payload is DragPayload<string> text) Console.WriteLine(text.Value);
```

## API summary

| Signature | Contract |
| --- | --- |
| `protected DragPayload()` | Allows a concrete typed payload subclass. |

The constructor has no side effects. Payload contents belong to their creator. A drag ending through an accepted target, cancellation, source removal, or scene teardown stops borrowing the payload. [GUIDragTests](../../tests/Electron2D.Tests/GUIDragTests.cs) verifies identity and lifetime; [coverage](../coverage/electron2d-unmapped.md) records this drag-specific typed C# projection.
