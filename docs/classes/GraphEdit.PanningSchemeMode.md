# GraphEdit.PanningSchemeMode

Last updated: 2026-10-07

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.GraphEdit.PanningSchemeMode`. **Source:** [source](../../src/Scene/GUI/GraphEdit.cs). **Component:** [Graph authoring](../components/graph-authoring.md).

## Description

Owner-specific ScrollZooms/ScrollPans domain for wheel navigation. The Mode suffix retains the PanningScheme property under ADR 0051.

## Members

| Declaration | Contract |
| --- | --- |
| [`public const Electron2D.GraphEdit.PanningSchemeMode ScrollPans = 1`](#member-b3dbe960288b) | Wheel pans; modified wheel zooms. |
| [`public const Electron2D.GraphEdit.PanningSchemeMode ScrollZooms = 0`](#member-4713e4b26fbb) | Wheel zooms; modified wheel pans. |

## Example

```csharp
using var graph = new GraphEdit { PanningScheme = GraphEdit.PanningSchemeMode.ScrollPans };
```

## Lifetime and verification

Attached reads and mutations use the scene owner thread. Own mutations reject active port/connection drawing; borrowed resources remain caller-owned. GraphNode and GraphFrame titlebars and title labels, and GraphEdit menu/layers cannot be disposed independently. GraphElement/GraphEdit disposal releases owned children and event subscriptions. See the component for executable checks, preparation boundaries and remaining dependencies.

## Member details

<a id="member-b3dbe960288b"></a>
### `ScrollPans`

```csharp
public const Electron2D.GraphEdit.PanningSchemeMode ScrollPans = 1
```

Wheel pans; modified wheel zooms.

<a id="member-4713e4b26fbb"></a>
### `ScrollZooms`

```csharp
public const Electron2D.GraphEdit.PanningSchemeMode ScrollZooms = 0
```

Wheel zooms; modified wheel pans.
