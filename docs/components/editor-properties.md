# Typed editor properties component

Last updated: 2026-10-13

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

## Scope

This Core component exposes engine properties to future tooling and typed packed-scene storage without `Variant`, string-based mutation, or dictionary-shaped metadata.

The C# integration selected by [ADR 0091](../decisions/scripting.md#adr-0091) reuses these explicit typed descriptors for user-node properties and future Script metadata; ordinary public C# properties do not become stored/editor properties automatically. A project type catalog, source/build association, file reconstruction and automated registration remain separate absent capabilities. This component introduces neither a Script resource nor a runtime reflection scanner.

## Owned types

| Type | Role |
| --- | --- |
| [`PropertyDescriptor`](../classes/PropertyDescriptor.md) | Non-generic property identity and compatibility contract |
| [`PropertyDescriptor<TOwner, TValue>`](../classes/PropertyDescriptor.Generic.md) | Typed getter, optional setter, validation, and revert behavior |

`ElectronObject` owns property-list discovery, validation/filtering, duplicate-name checks, revert dispatch, and the `PropertyListChanged` event.

## Current implementation status

Implemented and covered by executable checks. [`PackedScene`](../classes/PackedScene.md) consumes the explicit storage flag at runtime; no editor application currently consumes the tooling surface.

## Dependencies

The component depends on Core's `ElectronObject` and `Resource` plus .NET delegates, collections, runtime type metadata, and equality comparers. Its internal storage hooks are consumed by Scene's packed-scene component, but it has no dependency on `PackedScene`, SDL3-CS, reflection scanning, or an editor framework.

## Runtime flow

1. `ElectronObject.GetPropertyDescriptors()` yields base and derived descriptors.
2. `ValidateProperty()` may retain, replace, or hide each descriptor.
3. `GetPropertyList()` verifies owner compatibility and unique ordinal names.
4. Tooling uses the generic descriptor for typed reads and writes.
5. Revert behavior is supplied by a typed factory and invoked through either the descriptor or `ElectronObject.RevertProperty()`.
6. A writable descriptor explicitly constructed with `stored: true` may capture/restore a node property for `PackedScene`; storage accepts strings, resources, and reference-free value types such as [`Color`](../classes/Color.md), [`Vector2`](../classes/Vector2.md), [`Vector2i`](../classes/Vector2i.md), [`Vector4`](../classes/Vector4.md), [`Vector4i`](../classes/Vector4i.md), [`Rect2`](../classes/Rect2.md), [`Rect2i`](../classes/Rect2i.md), and [`Transform`](../classes/Transform.md).

The base object exposes `InstanceID`, `ClassName`, `IsDisposed`, `CanTranslateMessages`, and `TranslationDomain`. The two translation properties are stored. `Node` adds stored name, process/input enablement and priority properties. `CanvasItem` adds stored visibility, Z order, top-level state, modulation and borrowed material properties. Spatial `Entity` adds stored Position, RotationDegrees, Scale and Skew. Neutral nodes have no canvas or spatial descriptors. Identity/lifetime entries are not stored. [`Timer`](../classes/Timer.md) adds stored process lane, wait, one-shot, autostart, and ignore-time-scale configuration plus runtime-only local pause and read-only remaining time. Global/derived state and notification switches are deliberately runtime API rather than tooling properties.

Each `CanvasItem.Material` assignment, including an equal one, now raises the typed `PropertyListChanged` event after storing the borrowed reference. A throwing subscriber observes committed state. This runtime signal is available to future editor tooling; no editor inspector behavior is claimed yet.

## Invariants and errors

- Names are non-empty and unique within a resolved property list.
- Owner and value types are fixed by generic arguments.
- Read-only descriptors reject writes and cannot define revert values.
- Stored descriptors must be writable. Unsupported reference-shaped stored values fail capture instead of retaining arbitrary object graphs.
- Validators reject values with `ArgumentOutOfRangeException`.
- Access after owner disposal throws `ObjectDisposedException`.
- Tooling property access is synchronous and does not make the owner thread-safe.

## Exclusions

No editor UI, reflection scanner, attributes, file-serialization schema, categories, ranges, localization hints, undo stack, node-reference remapping, or automatic value-change notification exists yet.

## Verification

The executable test verifies discovery, storage metadata, typed reads and writes, validation, typed revert-value retrieval, object-level revert, property-list change notification, packed capture/restore including `Color`, all six vector values, `Rect2`, `Rect2i`, `Transform`, and Timer configuration, resource remapping, and unsupported stored-shape rejection.

Node.GetNodeAndResource reads resource references through the same typed descriptors, including read-only/derived schemas, without reflection-driven property invocation or an untyped public value accessor. Scalar/null properties delimit the returned remainder; getter failures retain their normal error behavior.

The internal indexed-property correspondence was inspected against pinned [object indexed reads and back-propagated writes](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/object/object.cpp). Compiled descriptors preserve typed access under ADRs 0001/0004/0005; dynamic property-name dispatch remains excluded.
