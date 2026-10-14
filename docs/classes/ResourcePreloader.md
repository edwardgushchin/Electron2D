# ResourcePreloader

Last updated: 2026-10-14

## Overview

`ResourcePreloader : Node` retains named resources for compiled scene callbacks. It exposes direct lookup, membership, snapshots and editing; it does not initiate loading or processing automatically. Resource objects are borrowed. PackedScene/file reconstruction uses existing resource aliases, local-scene copies and graph leases. See [named scene resources](../components/resource-preloading.md) and [ADR 0013](../decisions/resources.md#adr-0013).

## Syntax

```csharp
var assets = new ResourcePreloader();
assets.AddResource("character", texture);
Texture drawTexture = (Texture)assets.GetResource("character");
```

## Member reference

| Member | Behavior |
| --- | --- |
| `ResourcePreloader()` | Empty named table. |
| `AddResource(string name, Resource resource)` | Borrows a live resource. Occupied names choose the first free `name 2`, `name 3`, ... . |
| `GetResource(string name)` | Exact case-sensitive identity; absent name throws KeyNotFoundException. Logical resource disposal remains controlled by its owner. |
| `GetResourceList()` | Copies names in insertion order. Capture/file reconstruction may change that order to canonical Unicode scalar sorting. |
| `HasResource(string name)` | Name membership, including entries whose borrowed resource was subsequently disposed by its owner. |
| `RemoveResource(string name)` | Removes membership without disposing the resource; absent name throws. |
| `RenameResource(string name, string newname)` | Chooses a destination after considering the source name free; applies the same numbered rule, preserving identity and appending to insertion order. Renaming to itself also appends. |
| `GetPropertyDescriptors()` / `CreateSceneInstanceFactory()` | Typed stored Resource slots and a static exact-type reconstruction factory. Derived classes provide their own exact factory under Node's existing rules. |
| `Dispose()` | Clears the table after ordinary Node cleanup; borrowed resources remain caller-owned. |

## Ownership and boundaries

Null names/resources reject. Empty names, separators, embedded NUL and valid Unicode names work; unpaired UTF16 surrogates reject before mutation. Invalid additions/renames preserve the table. Reads and mutations retain ordinary Node/scene-thread/capture guards; capture cannot edit the source table.

The private stored family is `PreloadedResource/` plus canonical uppercase UTF8 hex, with the exact Resource schema. Names are labels, never CLR member dispatch. Typed recovery binds only this node role and family. Stored null removes/omits a slot. Snapshot containers are independent; actual resource aliases pass through normal PackedScene copy/local-scene/decoded graph ownership. Repeated prepared Get/Has work allocates no managed owner bytes; lists, edits, descriptors, packing, file/native loading and user callbacks are outside that interval.
