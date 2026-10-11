# PhysicsShapePairBuffers

Last updated: 2026-10-11

**Declaration:** `internal sealed class PhysicsShapePairBuffers` · **Source:** [PhysicsShapePairBuffers.cs](../../src/Servers/Physics/PhysicsShapePairBuffers.cs) · **Component:** [Selected shape-pair queries](../components/physics-shape-pairs.md)

## Description and internal members

Retains vector arrays per synchronous nesting depth. A PhysicsSpace owns one
instance; detached body queries use a thread-local instance. Frames own no shape,
body, resource, solver or object references. Larger capacity/deeper nesting can
allocate cold; equal warmed operations reuse arrays. World disposal clears storage.

| Internal signature | Contract |
| --- | --- |
| `static void ValidateCapacity(int length)` | Reject odd point capacity with ArgumentException; each pair consists of two points. |
| `Span<Vector2> Begin(int length)` | Validate capacity, grow the current retained frame if needed, advance depth and borrow exactly length points. |
| `void End()` | Retire one completed frame in the enclosing finally. |
| `void Clear()` | Release retained per-world arrays outside borrowed query phases. |
| `static void Commit(bool hit, int count, ReadOnlySpan<Vector2> scratch, Span<Vector2> contacts)` | Validate nonnegative bounded pair count, zero count on a miss and finite points; copy only the used prefix after all checks. Invalid results throw InvalidOperationException without changing caller output. |

## Internal example

Partial runtime snippet; it assumes a same-length caller span and an executing
selected query with an independently borrowed world context:

```csharp
var scratch = buffers.Begin(contacts.Length);
try
{
    bool hit = Query(scratch, out int count);
    PhysicsShapePairBuffers.Commit(hit, count, scratch, contacts);
}
finally { buffers.End(); }
```

Nested calls cannot overwrite their parent's staged output. The selected pair
fixture exercises failures, nesting, count bounds, caller-tail preservation and
zero warmed managed allocations on CPU/GPU. See the component for exact scope and
remaining public/native/platform acceptance.
