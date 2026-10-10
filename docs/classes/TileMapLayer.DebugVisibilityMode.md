# TileMapLayer.DebugVisibilityMode

Last updated: 2026-10-10

**Namespace:** `Electron2D`. **Source:** [TileMapLayer.cs](../../src/Scene/2D/TileMapLayer.cs).

## Overview

Selects collision overlay visibility independently of ordinary tile drawing.

## Syntax

```csharp
public enum Electron2D.TileMapLayer.DebugVisibilityMode
```

## Lifecycle, units and limits

See [the tile component](../components/tiles.md) for ownership, batching, coordinate transforms, storage format, error boundaries and remaining capabilities. Attached layer edits require its scene owner thread; resource authors coordinate edits with consuming trees. Missing indices and invalid input throw typed exceptions before ordinary authored mutations. Polygon/coordinate arrays are copied. Compilation and storage allocate; unchanged warmed frames reuse prepared state.

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public const Electron2D.TileMapLayer.DebugVisibilityMode Default = 0`](#member-0b4031385b42) | enumValue | Follows the scene tree's collision hint. |
| [`public const Electron2D.TileMapLayer.DebugVisibilityMode ForceHide = 2`](#member-02ed405f7dd1) | enumValue | Hides collision geometry even when the scene hint is enabled. |
| [`public const Electron2D.TileMapLayer.DebugVisibilityMode ForceShow = 1`](#member-ebf1d0a81cc3) | enumValue | Draws collision geometry whenever this layer is visible. |

## Member Details

<a id="member-0b4031385b42"></a>
### `Default`

Kind: `enumValue`

```csharp
public const Electron2D.TileMapLayer.DebugVisibilityMode Default = 0
```

#### Summary

Follows the scene tree's collision hint.

<a id="member-02ed405f7dd1"></a>
### `ForceHide`

Kind: `enumValue`

```csharp
public const Electron2D.TileMapLayer.DebugVisibilityMode ForceHide = 2
```

#### Summary

Hides collision geometry even when the scene hint is enabled.

<a id="member-ebf1d0a81cc3"></a>
### `ForceShow`

Kind: `enumValue`

```csharp
public const Electron2D.TileMapLayer.DebugVisibilityMode ForceShow = 1
```

#### Summary

Draws collision geometry whenever this layer is visible.

## Verification

TileMapLayerTests exercises this type through explicit CPU/GPU worlds, typed resources, fresh-process scenes and the real window workflow. [Recorded measurements and remaining gates](../components/tiles.md#verification) keep native, rendering, throughput and platform claims separate. Owning decision: [ADR 0101](../decisions/tiles.md#adr-0101).
