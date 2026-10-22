# PropertyDescriptor

Last updated: 2026-10-22

**Inherits:** —

**Inherited By:** [PropertyDescriptor<TOwner, TValue>](PropertyDescriptor.Generic.md)

- **Source:** [`src/Core/Object/PropertyDescriptor.cs`](../../src/Core/Object/PropertyDescriptor.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public abstract partial class PropertyDescriptor`

> Describes a typed property exposed to Electron2D tooling.

## Compiled indexed properties

`ElectronObject.GetIndexed` and `SetIndexed` accept a typed `PropertyDescriptor` for the root owner and result value. The supplied descriptor may describe a direct property or a composition built with `PropertyDescriptor.Through`, `Member`, `Element` or `Key`. Names are metadata labels; there is no runtime string setter, reflection scanner, new path wrapper or universal value container. This follows [ADR 0005](../decisions/core-object-runtime.md#adr-0005).

Through resolves the current referenced ElectronObject for each operation. Its setter invokes the child property then the parent property; validation, notifications and committed changes remain visible when an outer setter fails. Both levels must be writable. Member combines supplied typed struct readers/updaters and writes the complete updated value through the parent. Element checks a fixed nonnegative array index on each access; Key uses a typed dictionary key, permits additions on write and retains the dictionary comparer. Collection writes copy the original container before calling the parent setter, so a precommit validation failure cannot modify the original container. Reads and object values remain borrowed. Wrong owners, null/disposed links, missing keys, bounds and callback failures are explicit. Attached Node access follows owner affinity; writes retain scene capture/mutation guards.

Synthetic compositions are non-stored and provide no invented revert default. Save the original owning properties and resource graph, and use their ordinary revert operations. Their compiled accessors also drive the existing typed TweenProperty API. Descriptor construction and collection copies are cold operations; prepared scalar/object reads and writes are checked separately for zero managed allocation.

```csharp
var position = new PropertyDescriptor<Sprite, Vector2>(
    nameof(Sprite.Position), sprite => sprite.Position,
    (sprite, value) => sprite.Position = value);
var horizontal = PropertyDescriptor.Member(position, "X",
    point => point.X, (point, x) => point with { X = x });
sprite.SetIndexed(horizontal, 32f);
float x = sprite.GetIndexed(horizontal);
```

IndexedPropertyTests verifies object rebinding, components, copied arrays/maps/comparers, validation/read-only/lifetime/errors, owner/capture guards, saved scenes and existing Tween integration. Linux Wayland GPU/hardware compatibility hosts check actual atlas-region and Sprite-position pixels. Other platforms, full editor automation, native allocations and human acceptance remain unverified.

## Scene-owned skeletal copies

An internal AlwaysDuplicateResource policy on the Skeleton stack descriptor selects force duplication through the existing scene resource session. It is schema metadata, with no public dynamic selector, and ensures independent executable modifications for each instantiated rig.

## Description

Describes a typed property exposed to Electron2D tooling.

`PropertyDescriptor` is the non-generic identity and compatibility surface for one property exposed to tooling and optional packed-scene storage. It carries the property name, required owner type, value type, read-only state, and explicit storage state while delegating typed access to [`PropertyDescriptor<TOwner, TValue>`](PropertyDescriptor.Generic.md).

This is the non-generic discovery surface used in property lists. It contains immutable metadata and revert
operations but deliberately exposes no Variant-like untyped getter or setter.


Typed property inspection uses the exact teardown-thread exception from ADR 0009: GetValue, TryGetRevertValue and CanRevert remain usable in pre-delete/cleanup on that thread; foreign-thread and terminal access fail. SetValue and Revert reject once disposal starts. TryGetRevertValue reports factory presence even for a null or unchanged default, while CanRevert and PropertyCanRevert retain the current value-difference policy. Factories run on each request; their results follow delegate ownership with no implicit copy. Validators reject before the setter, and an exception after a setter commits does not undo that write. Ordinary owner setters and getters retain thread, capture and numerical-phase rules.

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
- `ObjectDisposedException`: The owner is disposing on another thread or has finished disposing.
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
- Packed capture accepts stored strings, `Resource` subtypes, value types containing no managed references, and copied `Vector2[]`, `Color[]`, and nested `int[][]` contour arrays. These arrays use value equality for revert checks. Other reference-shaped values fail explicitly.

## Threading

The descriptor's metadata is immutable and safe to read concurrently. Operations on an owner inherit that owner's threading rules; this type adds no synchronization.

## Dependencies and interactions

`ElectronObject.GetPropertyList()` consumes this base type so heterogeneous typed descriptors can share one discovery list. [`PackedScene`](PackedScene.md) consumes only `IsStored` descriptors from nodes and invokes internal typed capture/restore hooks; actual public access remains generic and compile-time typed.

## Verification and limitations

[`tests/Electron2D.Tests/Program.cs`](../../tests/Electron2D.Tests/Program.cs) verifies compatible discovery, duplicate-name rejection, storage metadata, typed access through the generic subtype, revert dispatch through `ElectronObject`, packed capture/restore, and rejection of unsupported stored values.

There are no categories, ranges, hints, file-format flags, reflection-based access, arbitrary object storage, node-reference remapping, or editor widgets.

## Typed file integration

See [resource-file contracts](../components/resource-files.md) for registered typed schemas, cache/UID resolution, file-root and scene-instance ownership, public extension hooks and exercised verification. File operations allocate outside frame processing. UID paths resolve through the permanent catalog before directory-backed path resolution; unknown UIDs fail explicitly. The archive profile does not add an editor, arbitrary import/remap rules or every resource schema.

[ObjectPropertyTests](../../tests/Electron2D.Tests/ObjectPropertyTests.cs) executes discovery snapshots, compatible replacement/filtering and invalid lists, owner policy, live/default/null/array values, readonly/validation/delegate errors and teardown inspection. Explicit CPU/GPU serial and separate-thread profiles use real RigidBody properties, permitted post-solver mass edits, owner/capture rejection and callback-error recovery. Each retains 128 warm/128 measured full frames with typed inspection, velocity writes and reversion at strict zero owner/all-thread managed bytes. GPU runs use CPU-only Vulkan; this does not establish hardware performance, editor automation or native/platform acceptance.

## Advisory hints

PropertyDescriptor exposes immutable init-only Hint and HintString metadata.
PropertyHint retains applicable nondeprecated identities for typed tooling providers;
Max and undefined values reject initialization. HintString rejects null. The text
and hint do not change setter validation, readonly/stored state or runtime values.
Through retains its leaf metadata; synthetic component/collection projections have
no inferred hint. The public [graph parameter consumer](../components/visual-shaders.md#parameters-and-graph-local-references)
exercises provider metadata and compiled numeric range/enum/Color consumers.
No editor controls or visual editor acceptance are implied by descriptor discovery.
