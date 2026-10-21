# VisualShaderNodeVectorBase.OpType

Last updated: 2026-10-21

Source: [`VisualShaderNodeVectorBase.cs`](../../src/Scene/Resources/VisualShaderNodeVectorBase.cs).

```csharp
public enum Electron2D.VisualShaderNodeVectorBase.OpType
```

## Description

The number of numeric components processed by a vector operation.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNodeVectorBase.OpType Max = 3`](#member-fd85d406e1a8) | The nonselectable component-count sentinel. |
| [`public const Electron2D.VisualShaderNodeVectorBase.OpType Vector2 = 0`](#member-8d31d1e571ab) | Two numeric components. |
| [`public const Electron2D.VisualShaderNodeVectorBase.OpType Vector3 = 1`](#member-211f434190fe) | Three numeric components. |
| [`public const Electron2D.VisualShaderNodeVectorBase.OpType Vector4 = 2`](#member-a5e1ea0fa37b) | Four numeric components. |

<a id="member-fd85d406e1a8"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeVectorBase.OpType Max = 3
```

The nonselectable component-count sentinel.


<a id="member-8d31d1e571ab"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderNodeVectorBase.OpType Vector2 = 0
```

Two numeric components.


<a id="member-211f434190fe"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderNodeVectorBase.OpType Vector3 = 1
```

Three numeric components.


<a id="member-a5e1ea0fa37b"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderNodeVectorBase.OpType Vector4 = 2
```

Four numeric components.


## Lifecycle and verification

This enum or immutable value carrier has ordinary value lifetime and is not disposable. Enum sentinels are not selectable roles; typed default entries validate their numeric value at construction. Authoring and snapshots are allocating cold operations.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
