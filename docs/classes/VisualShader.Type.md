# VisualShader.Type

Last updated: 2026-10-21

Source: [`VisualShader.cs`](../../src/Scene/Resources/VisualShader.cs).

```csharp
public enum Electron2D.VisualShader.Type
```

## Description

The canvas stage containing a graph.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShader.Type Fragment = 1`](#member-c5d87a95f843) | Computes canvas fragment color. |
| [`public const Electron2D.VisualShader.Type Max = 11`](#member-17d66c334528) | The nonselectable upper bound of all shader graph stage identities. |
| [`public const Electron2D.VisualShader.Type Vertex = 0`](#member-5c6c0a685db8) | Transforms supplied canvas vertices before rasterization. |

<a id="member-c5d87a95f843"></a>
### `Fragment`

```csharp
public const Electron2D.VisualShader.Type Fragment = 1
```

Computes canvas fragment color.


<a id="member-17d66c334528"></a>
### `Max`

```csharp
public const Electron2D.VisualShader.Type Max = 11
```

The nonselectable upper bound of all shader graph stage identities.


<a id="member-5c6c0a685db8"></a>
### `Vertex`

```csharp
public const Electron2D.VisualShader.Type Vertex = 0
```

Transforms supplied canvas vertices before rasterization.


## Lifecycle and verification

This enum or immutable value carrier has ordinary value lifetime and is not disposable. Enum sentinels are not selectable roles; typed default entries validate their numeric value at construction. Authoring and snapshots are allocating cold operations.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
