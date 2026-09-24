# PropertyDescriptor\<TOwner, TValue\>

Last updated: 2026-09-23

**Inherits:** [PropertyDescriptor](PropertyDescriptor.md)

**Inherited By:** —

- **Source:** [`src/Core/Object/PropertyDescriptor.cs`](../../src/Core/Object/PropertyDescriptor.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class PropertyDescriptor<TOwner, TValue> : PropertyDescriptor`

> Provides strongly typed access, validation, and revert behavior for a tooling property.

## Description

Provides strongly typed access, validation, and revert behavior for a tooling property.

The generic descriptor provides compile-time typed property reads, optional writes, optional validation, optional revert behavior, and explicit packed-scene storage without `Variant`, `object` values, reflection, or string-addressed mutation.

Delegate execution is synchronous on the caller's thread. The descriptor is immutable, but access to an owner
follows that owner's threading rules.

## Examples

The following focused snippet uses the current public API. Names not declared in the snippet are supplied by the surrounding application or callback context.

```csharp
var descriptor = new PropertyDescriptor<Node, string>(
    nameof(Node.Name),
    node => node.Name,
    (node, value) => node.Name = value,
    _ => "Node");
```

## Constructors

| Member | Description |
| --- | --- |
| [`public PropertyDescriptor<TOwner, TValue>(string name, Func<TOwner, TValue> getter, Action<TOwner, TValue> setter = null, Func<TOwner, TValue> revertValue = null, Func<TOwner, TValue, bool> validator = null, bool stored = false)`](#m-electron2d-propertydescriptor-2-ctor-system-string-system-func-0-1-system-action-0-1-system-func-0-1-system-func-0-1-system-boolean-system-boolean) | Initializes a strongly typed tooling property descriptor. |

## Methods

| Member | Description |
| --- | --- |
| [`public TValue GetValue(TOwner owner)`](#m-electron2d-propertydescriptor-2-getvalue-0) | Reads the property's current value from a compatible live owner. |
| [`public void SetValue(TOwner owner, TValue value)`](#m-electron2d-propertydescriptor-2-setvalue-0-1) | Validates and writes a property value to a compatible live owner. |
| [`public bool TryGetRevertValue(TOwner owner, out TValue value)`](#m-electron2d-propertydescriptor-2-trygetrevertvalue-0-1-byref) | Attempts to compute the property's current revert value. |
| [`public override bool CanRevert(ElectronObject owner)`](#m-electron2d-propertydescriptor-2-canrevert-electron2d-electronobject) | Determines whether a compatible live owner's value currently differs from its revert value. |
| [`public override void Revert(ElectronObject owner)`](#m-electron2d-propertydescriptor-2-revert-electron2d-electronobject) | Restores a compatible live owner's property to its current revert value. |

## Constructor Descriptions

<a id="m-electron2d-propertydescriptor-2-ctor-system-string-system-func-0-1-system-action-0-1-system-func-0-1-system-func-0-1-system-boolean-system-boolean"></a>
### `public PropertyDescriptor<TOwner, TValue>(string name, Func<TOwner, TValue> getter, Action<TOwner, TValue> setter = null, Func<TOwner, TValue> revertValue = null, Func<TOwner, TValue, bool> validator = null, bool stored = false)`

Initializes a strongly typed tooling property descriptor.

**Parameters**

- `name`: The nonblank property name.
- `getter`: The required value reader.
- `setter`: An optional value writer. Omit it to create a read-only descriptor.
- `revertValue`: An optional factory for the current revert value.
- `validator`: An optional predicate evaluated before each write.
- `stored`: Whether packed scenes should capture this property from node owners.

**Exceptions**

- `ArgumentException`: `name` is blank, or `revertValue` is supplied without a
`setter`, or `stored` is true without a setter.
- `ArgumentNullException`: `name` or `getter` is `null`.

## Method Descriptions

<a id="m-electron2d-propertydescriptor-2-getvalue-0"></a>
### `public TValue GetValue(TOwner owner)`

Reads the property's current value from a compatible live owner.

**Parameters**

- `owner`: The owner passed to the configured getter.

**Returns:** The current property value.

**Exceptions**

- `ArgumentNullException`: `owner` is `null`.
- `ObjectDisposedException`: Disposal of `owner` has started.
- `Exception`: The configured getter throws.

<a id="m-electron2d-propertydescriptor-2-setvalue-0-1"></a>
### `public void SetValue(TOwner owner, TValue value)`

Validates and writes a property value to a compatible live owner.

**Parameters**

- `owner`: The owner passed to the configured validator and setter.
- `value`: The value to validate and write.

**Exceptions**

- `ArgumentNullException`: `owner` is `null`.
- `ArgumentOutOfRangeException`: The configured validator rejects `value`.
- `InvalidOperationException`: The descriptor is read-only.
- `ObjectDisposedException`: Disposal of `owner` has started.
- `Exception`: The configured validator or setter throws.

<a id="m-electron2d-propertydescriptor-2-trygetrevertvalue-0-1-byref"></a>
### `public bool TryGetRevertValue(TOwner owner, out TValue value)`

Attempts to compute the property's current revert value.

**Parameters**

- `owner`: The owner passed to the configured revert-value factory.
- `value`: Receives the revert value, or `default` when no factory exists.

**Returns:** `true` when a revert-value factory exists; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `owner` is `null`.
- `ObjectDisposedException`: Disposal of `owner` has started.
- `Exception`: The configured revert-value factory throws.

<a id="m-electron2d-propertydescriptor-2-canrevert-electron2d-electronobject"></a>
### `public override bool CanRevert(ElectronObject owner)`

Determines whether a compatible live owner's value currently differs from its revert value.

**Parameters**

- `owner`: The owner whose property is inspected.

**Returns:** `true` when the property can currently be reverted; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `owner` is `null`.
- `ArgumentException`: `owner` is not assignable to [`PropertyDescriptor.OwnerType`](PropertyDescriptor.md#p-electron2d-propertydescriptor-ownertype).
- `ObjectDisposedException`: Disposal of `owner` has started.
- `Exception`: A configured getter or revert-value delegate throws.

<a id="m-electron2d-propertydescriptor-2-revert-electron2d-electronobject"></a>
### `public override void Revert(ElectronObject owner)`

Restores a compatible live owner's property to its current revert value.

**Parameters**

- `owner`: The owner whose property is restored.

**Exceptions**

- `ArgumentNullException`: `owner` is `null`.
- `ArgumentException`: `owner` is not assignable to [`PropertyDescriptor.OwnerType`](PropertyDescriptor.md#p-electron2d-propertydescriptor-ownertype).
- `InvalidOperationException`: The descriptor has no writable revert value.
- `ObjectDisposedException`: Disposal of `owner` has started.
- `Exception`: A configured getter, revert-value, validator, or setter delegate throws.

## Inherited API

Public and protected members inherited from [PropertyDescriptor](PropertyDescriptor.md). Their lifecycle and error contracts remain applicable unless this page states an override.

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

The executable test covers typed get/set, validation rejection, revert-value retrieval, revert availability, restoration, storage metadata, packed capture/restore including reference-free [`Color`](Color.md), [`Vector2`](Vector2.md), [`Vector2i`](Vector2i.md), [`Vector4`](Vector4.md), [`Vector4i`](Vector4i.md), [`Rect`](Rect.md), [`Rect2i`](Rect2i.md), [`Transform`](Transform.md), and [`TimerProcessCallback`](TimerProcessCallback.md) values plus Timer configuration, resource remapping, and unsupported stored-shape rejection. Property-value change events, undo/redo, attributes, node-reference remapping, arbitrary collection storage, and automatic reflection discovery are not implemented.
