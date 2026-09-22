# PropertyDescriptor

Last updated: 2026-09-23

**Inherits:** —

**Inherited By:** [PropertyDescriptor<TOwner, TValue>](PropertyDescriptor.Generic.md)

- **Source:** [`src/Core/Object/PropertyDescriptor.cs`](../../src/Core/Object/PropertyDescriptor.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract class PropertyDescriptor`

> Describes a typed property exposed to Electron2D tooling.

## Description

Describes a typed property exposed to Electron2D tooling.

`PropertyDescriptor` is the non-generic identity and compatibility surface for one property exposed to tooling and optional packed-scene storage. It carries the property name, required owner type, value type, read-only state, and explicit storage state while delegating typed access to [`PropertyDescriptor<TOwner, TValue>`](PropertyDescriptor.Generic.md).

This is the non-generic discovery surface used in property lists. It contains immutable metadata and revert
operations but deliberately exposes no Variant-like untyped getter or setter.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
PropertyDescriptor descriptor = new PropertyDescriptor<Node, string>(
    nameof(Node.Name), node => node.Name, (node, value) => node.Name = value, _ => "Node");
```

## Constructors

| Member | Description |
| --- | --- |
| [`protected PropertyDescriptor(string name, Type ownerType, Type valueType, bool isReadOnly, bool isStored = false)`](#m-electron2d-propertydescriptor-ctor-system-string-system-type-system-type-system-boolean-system-boolean) | Initializes immutable metadata for a tooling property. |

## Properties

| Member | Description |
| --- | --- |
| [`public string Name { get; }`](#p-electron2d-propertydescriptor-name) | Gets the property name. |
| [`public Type OwnerType { get; }`](#p-electron2d-propertydescriptor-ownertype) | Gets the runtime owner type accepted by this descriptor. |
| [`public Type ValueType { get; }`](#p-electron2d-propertydescriptor-valuetype) | Gets the property's exact value type. |
| [`public bool IsReadOnly { get; }`](#p-electron2d-propertydescriptor-isreadonly) | Gets whether the property has no setter. |
| [`public bool IsStored { get; }`](#p-electron2d-propertydescriptor-isstored) | Gets whether packed scenes should store this property when its owner is a node. |

## Methods

| Member | Description |
| --- | --- |
| [`public abstract bool CanRevert(ElectronObject owner)`](#m-electron2d-propertydescriptor-canrevert-electron2d-electronobject) | Determines whether a compatible live owner's value currently differs from its revert value. |
| [`public abstract void Revert(ElectronObject owner)`](#m-electron2d-propertydescriptor-revert-electron2d-electronobject) | Restores a compatible live owner's property to its current revert value. |

## Constructor Descriptions

<a id="m-electron2d-propertydescriptor-ctor-system-string-system-type-system-type-system-boolean-system-boolean"></a>
### `protected PropertyDescriptor(string name, Type ownerType, Type valueType, bool isReadOnly, bool isStored = false)`

Initializes immutable metadata for a tooling property.

**Parameters**

- `name`: The nonblank property name.
- `ownerType`: The runtime owner type accepted by this descriptor. The base constructor checks only for null; custom derived
descriptors are responsible for supplying an [`ElectronObject`](ElectronObject.md)-compatible type.
- `valueType`: The exact type of the property value.
- `isReadOnly`: Whether the descriptor has no setter.
- `isStored`: Whether packed scenes should store this property when it belongs to a node.

**Exceptions**

- `ArgumentException`: `name` is empty or consists only of whitespace.
- `ArgumentNullException`: `name`, `ownerType`, or `valueType` is `null`.

## Property Descriptions

<a id="p-electron2d-propertydescriptor-name"></a>
### `public string Name { get; }`

Gets the property name.

**Value:** A nonblank name that is immutable for the descriptor's lifetime.

<a id="p-electron2d-propertydescriptor-ownertype"></a>
### `public Type OwnerType { get; }`

Gets the runtime owner type accepted by this descriptor.

**Value:** The required owner type. [`PropertyDescriptor`2`](PropertyDescriptor.Generic.md) always supplies an
[`ElectronObject`](ElectronObject.md) subtype; the protected base constructor does not enforce that constraint.

<a id="p-electron2d-propertydescriptor-valuetype"></a>
### `public Type ValueType { get; }`

Gets the property's exact value type.

**Value:** The value type supplied when the descriptor was constructed.

<a id="p-electron2d-propertydescriptor-isreadonly"></a>
### `public bool IsReadOnly { get; }`

Gets whether the property has no setter.

**Value:** `true` for a read-only descriptor; otherwise `false`.

<a id="p-electron2d-propertydescriptor-isstored"></a>
### `public bool IsStored { get; }`

Gets whether packed scenes should store this property when its owner is a node.

**Value:** `true` only for an explicitly storage-enabled writable descriptor.

## Method Descriptions

<a id="m-electron2d-propertydescriptor-canrevert-electron2d-electronobject"></a>
### `public abstract bool CanRevert(ElectronObject owner)`

Determines whether a compatible live owner's value currently differs from its revert value.

**Parameters**

- `owner`: The owner whose property is inspected.

**Returns:** `true` when the property can currently be reverted; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `owner` is `null`.
- `ArgumentException`: `owner` is not assignable to [`PropertyDescriptor.OwnerType`](PropertyDescriptor.md#p-electron2d-propertydescriptor-ownertype).
- `ObjectDisposedException`: Disposal of `owner` has started.
- `Exception`: A configured getter or revert-value delegate throws.

<a id="m-electron2d-propertydescriptor-revert-electron2d-electronobject"></a>
### `public abstract void Revert(ElectronObject owner)`

Restores a compatible live owner's property to its current revert value.

**Parameters**

- `owner`: The owner whose property is restored.

**Exceptions**

- `ArgumentNullException`: `owner` is `null`.
- `ArgumentException`: `owner` is not assignable to [`PropertyDescriptor.OwnerType`](PropertyDescriptor.md#p-electron2d-propertydescriptor-ownertype).
- `InvalidOperationException`: The descriptor has no writable revert value.
- `ObjectDisposedException`: Disposal of `owner` has started.
- `Exception`: A configured getter, revert-value, validator, or setter delegate throws.

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
