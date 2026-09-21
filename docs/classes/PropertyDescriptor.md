# PropertyDescriptor

Last updated: 2026-09-21

## Declaration

- Source: [`PropertyDescriptor.cs`](../../src/Core/Object/PropertyDescriptor.cs)
- Namespace: `Electron2D`
- Declaration: `public abstract class PropertyDescriptor`
- Domain: [Core](../domains/core.md)
- Component: [Typed editor properties](../components/editor-properties.md)

## Responsibility

`PropertyDescriptor` is the non-generic identity and compatibility surface for one property exposed to tooling and optional packed-scene storage. It carries the property name, required owner type, value type, read-only state, and explicit storage state while delegating typed access to [`PropertyDescriptor<TOwner, TValue>`](PropertyDescriptor.Generic.md).

## Public API

| Member | Current behavior |
| --- | --- |
| `string Name { get; }` | Non-empty property name; comparisons in a resolved property list are ordinal |
| `Type OwnerType { get; }` | The runtime type accepted by the descriptor; the generic implementation guarantees an `ElectronObject` subtype, while the protected base constructor only rejects null |
| `Type ValueType { get; }` | The exact property value type |
| `bool IsReadOnly { get; }` | `true` when the typed descriptor has no setter |
| `bool IsStored { get; }` | `true` only when a writable descriptor explicitly opts into packed-scene node storage |
| `bool CanRevert(ElectronObject owner)` | Reports whether the compatible live owner's current value differs from the typed revert value |
| `void Revert(ElectronObject owner)` | Restores the typed revert value or throws when no revert behavior exists |

The protected constructor also accepts `isStored`, which defaults to `false`. It validates that `name` is not null, empty, or whitespace and that both type arguments are non-null. It does not enforce that `ownerType` derives from `ElectronObject`; custom derived descriptors own that invariant. The shipped generic descriptor enforces it through its `TOwner : ElectronObject` constraint and rejects a stored descriptor without a setter.

## Lifecycle and ownership

Descriptors do not own their target objects and are not disposable. Engine types currently keep immutable descriptors in static fields or static read-only lists, so one descriptor can serve every compatible live instance.

## Invariants and errors

- `Name`, `OwnerType`, `ValueType`, `IsReadOnly`, and `IsStored` do not change after construction.
- An incompatible owner causes `ArgumentException`.
- A null owner causes `ArgumentNullException`.
- Disposed-owner checks and the exact revert error are supplied by the generic implementation.
- The base type deliberately exposes no untyped value getter or setter.
- Packed capture accepts stored strings, `Resource` subtypes, and value types containing no managed references. Other reference-shaped values fail explicitly.

## Threading

The descriptor's metadata is immutable and safe to read concurrently. Operations on an owner inherit that owner's threading rules; this type adds no synchronization.

## Dependencies and interactions

`ElectronObject.GetPropertyList()` consumes this base type so heterogeneous typed descriptors can share one discovery list. [`PackedScene`](PackedScene.md) consumes only `IsStored` descriptors from nodes and invokes internal typed capture/restore hooks; actual public access remains generic and compile-time typed.

## Verification and limitations

[`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) verifies compatible discovery, duplicate-name rejection, storage metadata, typed access through the generic subtype, revert dispatch through `ElectronObject`, packed capture/restore, and rejection of unsupported stored values.

There are no categories, ranges, hints, file-format flags, reflection-based access, arbitrary object storage, node-reference remapping, or editor widgets.
