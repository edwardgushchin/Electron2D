# PropertyDescriptor<TOwner, TValue>

Last updated: 2026-09-21

## Declaration

- Source: [`PropertyDescriptor.cs`](../../src/Core/Object/PropertyDescriptor.cs)
- Namespace: `Electron2D`
- Declaration: `public sealed class PropertyDescriptor<TOwner, TValue> : PropertyDescriptor where TOwner : ElectronObject`
- Domain: [Core](../domains/core.md)
- Component: [Typed editor properties](../components/editor-properties.md)

## Responsibility

The generic descriptor provides compile-time typed property reads, optional writes, optional validation, optional revert behavior, and explicit packed-scene storage without `Variant`, `object` values, reflection, or string-addressed mutation.

## Constructor

```csharp
PropertyDescriptor(
    string name,
    Func<TOwner, TValue> getter,
    Action<TOwner, TValue>? setter = null,
    Func<TOwner, TValue>? revertValue = null,
    Func<TOwner, TValue, bool>? validator = null,
    bool stored = false)
```

`getter` is required. A descriptor is read-only when `setter` is null. Supplying a revert factory or `stored: true` without a setter is rejected because the value could not be restored. A validator is evaluated only on writes. Storage is opt-in and defaults to `false`.

## Public API

| Member | Current behavior |
| --- | --- |
| `TValue GetValue(TOwner owner)` | Invokes the typed getter for a non-null live owner |
| `void SetValue(TOwner owner, TValue value)` | Validates and writes the value; rejects read-only descriptors |
| `bool TryGetRevertValue(TOwner owner, out TValue value)` | Returns the current typed revert value when a factory exists |
| `bool CanRevert(ElectronObject owner)` | Requires a compatible live owner and compares current/revert values with `EqualityComparer<TValue>.Default` |
| `void Revert(ElectronObject owner)` | Reads the current revert value, validates it, and sends it to the typed setter |

The inherited `Name`, `OwnerType`, `ValueType`, `IsReadOnly`, and `IsStored` metadata describe the generic arguments, setter availability, and packed-scene intent.

## Lifecycle and state

The descriptor is immutable after construction. Revert values are not snapshots: the factory runs for every query or revert, so a derived type may compute the current default from owner state. Packed-scene values are snapshots captured through the getter and later restored through the same validator/setter path on a fresh compatible owner.

## Invariants and errors

- Null owners throw `ArgumentNullException`; disposed owners throw `ObjectDisposedException`.
- An owner of the wrong runtime type passed through the base API throws `ArgumentException`.
- Writes to a read-only descriptor and reverts without a revert factory throw `InvalidOperationException`.
- A validator returning `false` causes `ArgumentOutOfRangeException` before the setter runs.
- A stored property whose declared value is neither a string, a `Resource` subtype, nor a reference-free value type throws `NotSupportedException` during packed capture.
- Resource-valued storage is remapped through the current scene's alias-preserving duplication scope before the setter runs.
- Exceptions from user-supplied delegates propagate unchanged.

## Threading

Descriptor metadata is immutable. Delegates execute synchronously on the caller's thread and receive no locking; their owner type defines legal access.

## Verification and limitations

The executable test covers typed get/set, validation rejection, revert-value retrieval, revert availability, restoration, storage metadata, packed capture/restore including reference-free [`Color`](Color.md), [`Vector2`](Vector2.md), [`Vector2I`](Vector2I.md), [`Vector4`](Vector4.md), [`Vector4I`](Vector4I.md), [`Rect`](Rect.md), [`RectI`](RectI.md), and [`Transform`](Transform.md) values, resource remapping, and unsupported stored-shape rejection. Property-value change events, undo/redo, attributes, node-reference remapping, arbitrary collection storage, and automatic reflection discovery are not implemented.
