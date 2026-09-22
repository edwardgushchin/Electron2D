# Typed editor properties component

Last updated: 2026-09-23

## Scope

This Core component exposes engine properties to future tooling and typed packed-scene storage without `Variant`, string-based mutation, or dictionary-shaped metadata.

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
6. A writable descriptor explicitly constructed with `stored: true` may capture/restore a node property for `PackedScene`; storage accepts strings, resources, and reference-free value types such as [`Color`](../classes/Color.md), [`Vector2`](../classes/Vector2.md), [`Vector2I`](../classes/Vector2I.md), [`Vector4`](../classes/Vector4.md), [`Vector4I`](../classes/Vector4I.md), [`Rect`](../classes/Rect.md), [`RectI`](../classes/RectI.md), and [`Transform`](../classes/Transform.md).

The base object exposes `InstanceID`, `ClassName`, `IsDisposed`, `CanTranslateMessages`, and `TranslationDomain`. The two translation properties are stored. `Node` adds stored name, process/input enablement and priority properties. `CanvasItem` adds stored visibility, Z order, top-level state, modulation and borrowed material properties. Spatial `Entity` adds stored Position, RotationDegrees, Scale and Skew. Neutral nodes have no canvas or spatial descriptors. Identity/lifetime entries are not stored. [`Timer`](../classes/Timer.md) adds stored process lane, wait, one-shot, autostart, and ignore-time-scale configuration plus runtime-only local pause and read-only remaining time. Global/derived state and notification switches are deliberately runtime API rather than tooling properties.

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

The executable test verifies discovery, storage metadata, typed reads and writes, validation, typed revert-value retrieval, object-level revert, property-list change notification, packed capture/restore including `Color`, all four vector values, `Rect`, `RectI`, `Transform`, and Timer configuration, resource remapping, and unsupported stored-shape rejection.
