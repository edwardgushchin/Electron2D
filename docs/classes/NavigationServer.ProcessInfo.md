# NavigationServer.ProcessInfo

Last updated: 2026-10-10

**Declaration:** `public enum NavigationServer.ProcessInfo`. **Component:** [Authored navigation maps](../components/navigation-maps.md). **Source:** [NavigationServer.Topology.cs](../../src/Servers/Navigation/NavigationServer.Topology.cs).

## Overview

Selects one counter from the latest successfully synchronized active-map snapshot.

## Syntax

```csharp
public enum Electron2D.NavigationServer.ProcessInfo
```

## Members

| Member | Kind | Summary |
| --- | --- | --- |
| [`public const Electron2D.NavigationServer.ProcessInfo ActiveMaps = 0`](#member-2b7bd6349ece) | enumValue | Number of active maps. |
| [`public const Electron2D.NavigationServer.ProcessInfo AgentCount = 2`](#member-cc4683f95a73) | enumValue | Number of agents belonging to active maps. |
| [`public const Electron2D.NavigationServer.ProcessInfo EdgeConnectionCount = 7`](#member-eee2aad4151b) | enumValue | Interregion raster pairs plus directed margin connections. |
| [`public const Electron2D.NavigationServer.ProcessInfo EdgeCount = 5`](#member-acd30ec9c658) | enumValue | Sum of distinct raster edge keys within each region. |
| [`public const Electron2D.NavigationServer.ProcessInfo EdgeFreeCount = 8`](#member-1f0bbd3a3fcf) | enumValue | Unpaired external edges eligible for margin connections, before margin matching. |
| [`public const Electron2D.NavigationServer.ProcessInfo EdgeMergeCount = 6`](#member-c57ff428ed7d) | enumValue | Number of paired raster edges within regions. |
| [`public const Electron2D.NavigationServer.ProcessInfo LinkCount = 3`](#member-79dc634d941e) | enumValue | Number of links belonging to active maps, including unattached or disabled links. |
| [`public const Electron2D.NavigationServer.ProcessInfo ObstacleCount = 9`](#member-b4ba038204c6) | enumValue | Number of obstacles belonging to active maps. |
| [`public const Electron2D.NavigationServer.ProcessInfo PolygonCount = 4`](#member-b437db61893f) | enumValue | Number of authored region polygons; synthetic link polygons are excluded. |
| [`public const Electron2D.NavigationServer.ProcessInfo RegionCount = 1`](#member-9376a215691e) | enumValue | Number of regions belonging to active maps, including disabled regions. |

## Member Details

<a id="member-2b7bd6349ece"></a>
### `ActiveMaps`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo ActiveMaps = 0
```

#### Summary

Number of active maps.

<a id="member-cc4683f95a73"></a>
### `AgentCount`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo AgentCount = 2
```

#### Summary

Number of agents belonging to active maps.

<a id="member-eee2aad4151b"></a>
### `EdgeConnectionCount`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo EdgeConnectionCount = 7
```

#### Summary

Interregion raster pairs plus directed margin connections.

<a id="member-acd30ec9c658"></a>
### `EdgeCount`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo EdgeCount = 5
```

#### Summary

Sum of distinct raster edge keys within each region.

<a id="member-1f0bbd3a3fcf"></a>
### `EdgeFreeCount`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo EdgeFreeCount = 8
```

#### Summary

Unpaired external edges eligible for margin connections, before margin matching.

<a id="member-c57ff428ed7d"></a>
### `EdgeMergeCount`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo EdgeMergeCount = 6
```

#### Summary

Number of paired raster edges within regions.

<a id="member-79dc634d941e"></a>
### `LinkCount`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo LinkCount = 3
```

#### Summary

Number of links belonging to active maps, including unattached or disabled links.

<a id="member-b4ba038204c6"></a>
### `ObstacleCount`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo ObstacleCount = 9
```

#### Summary

Number of obstacles belonging to active maps.

<a id="member-b437db61893f"></a>
### `PolygonCount`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo PolygonCount = 4
```

#### Summary

Number of authored region polygons; synthetic link polygons are excluded.

<a id="member-9376a215691e"></a>
### `RegionCount`

Kind: `enumValue`

```csharp
public const Electron2D.NavigationServer.ProcessInfo RegionCount = 1
```

#### Summary

Number of regions belonging to active maps, including disabled regions.
