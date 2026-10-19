# PropertyDescriptor\<TOwner, TValue\>

Last updated: 2026-10-19

## Stored physical state

Packed-scene capture can use an internal typed authored-value reader while ordinary
tooling reads retain the current observable value. RigidBody uses this existing
storage path to retain its dynamic sleep request separately from static inactivity.
This adds no public descriptor option and preserves owner access checks, supported
value types, snapshot copies and normal restoration validation.


**Inherits:** [PropertyDescriptor](PropertyDescriptor.md)

**Inherited By:** —

- **Source:** [`src/Core/Object/PropertyDescriptor.cs`](../../src/Core/Object/PropertyDescriptor.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class PropertyDescriptor<TOwner, TValue> : PropertyDescriptor`

> Provides strongly typed access, validation, and revert behavior for a tooling property.

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

## Description

Provides strongly typed access, validation, and revert behavior for a tooling property.

The generic descriptor provides compile-time typed property reads, optional writes, optional validation, optional revert behavior, and explicit packed-scene storage without `Variant`, `object` values, reflection, or string-addressed mutation.

Delegate execution is synchronous on the caller's thread. The descriptor is immutable, but access to an owner
follows that owner's threading rules.


Typed property inspection uses the exact teardown-thread exception from ADR 0009: GetValue, TryGetRevertValue and CanRevert remain usable in pre-delete/cleanup on that thread; foreign-thread and terminal access fail. SetValue and Revert reject once disposal starts. TryGetRevertValue reports factory presence even for a null or unchanged default, while CanRevert and PropertyCanRevert retain the current value-difference policy. Factories run on each request; their results follow delegate ownership with no implicit copy. Validators reject before the setter, and an exception after a setter commits does not undo that write. Ordinary owner setters and getters retain thread, capture and numerical-phase rules.

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
- `ObjectDisposedException`: The owner is disposing on another thread or has finished disposing.
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
- `ObjectDisposedException`: The owner is disposing on another thread or has finished disposing.
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
- `ObjectDisposedException`: The owner is disposing on another thread or has finished disposing.
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

- Null owners throw `ArgumentNullException`; terminal or foreign-thread disposal access throws `ObjectDisposedException`. Inspection on the exact teardown thread is permitted; writes reject every disposing owner.
- An owner of the wrong runtime type passed through the base API throws `ArgumentException`.
- Writes to a read-only descriptor and reverts without a revert factory throw `InvalidOperationException`.
- A validator returning `false` causes `ArgumentOutOfRangeException` before the setter runs.
- A stored property whose declared value is neither a string, a `Resource` subtype, a reference-free value type, a typed Node reference, nor an explicitly supported array profile throws `NotSupportedException` during packed capture.
- Resource-valued storage is remapped through the current scene's alias-preserving duplication scope before the setter runs.
- Exceptions from user-supplied delegates propagate unchanged.

## Threading

Descriptor metadata is immutable. Delegates execute synchronously on the caller's thread and receive no locking; their owner type defines legal access.

## Verification and limitations

The executable test covers typed get/set, validation rejection, revert-value retrieval, revert availability, restoration, storage metadata, packed capture/restore including reference-free [`Color`](Color.md), [`Vector2`](Vector2.md), [`Vector2i`](Vector2i.md), [`Vector4`](Vector4.md), [`Vector4i`](Vector4i.md), [`Rect2`](Rect2.md), [`Rect2i`](Rect2i.md), [`Transform`](Transform.md), and [`ProcessPhase`](ProcessPhase.md) values plus Timer configuration, resource remapping, and unsupported stored-shape rejection. Property-value change events, undo/redo, attributes, node-reference remapping, arbitrary collection storage, and automatic reflection discovery are not implemented.

## Text array storage

The explicit packed-scene array profile includes `float[]` tab increments and `string[]` structured-text options. Capture, state reads and restoration clone these arrays; structural element equality drives revert checks. Label setters still enforce finite tab increments with a positive repeated cycle and nonnull parser options when restoring stored values. This extends the existing vector/color/polygon-index array profiles without accepting arbitrary managed reference graphs. [LabelTests](../../tests/Electron2D.Tests/LabelTests.cs) exercises real scene packing and independent array snapshots.

Stored typed Node references on node owners use [StoredNodeReferenceValue](StoredNodeReferenceValue.md): capture records a relative path and reconstruction resolves it after the complete hierarchy exists. This does not allow arbitrary object graphs. Resource-array revert comparisons use reference identity element by element; storage of arbitrary resource arrays remains outside the supported node-property profiles. Shortcut owns and duplicates its event array through its Resource hook instead.

## Integer array storage

SplitContainer.SplitOffsets adds the exact `int[]` packed profile under ADR 0023. Capture, state reads and restoration clone the array; element equality drives revert instead of array reference equality. SplitContainerTests verifies captured-source changes and independently mutable public snapshots. Other array/reference graphs remain rejected.

## Typed archive storage

File values use the exact TValue schema with registered codecs, graph-aware resource/array references and relative node paths. Capture snapshots explicit value arrays/dictionaries; registered reference-shaped custom values snapshot through their codec. ResourceArchiveTests exercises endian/file reconstruction and typed resource ownership; see [resource files](../components/resource-files.md) for bounds and failures.

## Terrain graph restoration

The internal stored-restore delegate can stage terrain indices before graph association, without exposing a second public setter. Ordinary typed writes keep their configured setter and validation; portable codecs still validate owner/value types and copied values. Terrain file/scene tests verify this distinction, including inactive peering values.

[ObjectPropertyTests](../../tests/Electron2D.Tests/ObjectPropertyTests.cs) executes discovery snapshots, compatible replacement/filtering and invalid lists, owner policy, live/default/null/array values, readonly/validation/delegate errors and teardown inspection. Explicit CPU/GPU serial and separate-thread profiles use real RigidBody properties, permitted post-solver mass edits, owner/capture rejection and callback-error recovery. Each retains 128 warm/128 measured full frames with typed inspection, velocity writes and reversion at strict zero owner/all-thread managed bytes. GPU runs use CPU-only Vulkan; this does not establish hardware performance, editor automation or native/platform acceptance.
