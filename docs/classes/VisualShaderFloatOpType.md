# VisualShaderFloatOpType

Last updated: 2026-10-22

Source: [VisualShaderFloatOpType.cs](../../src/Scene/Resources/VisualShaderFloatOpType.cs).

```csharp
public enum Electron2D.VisualShaderFloatOpType
```

## Description

The floating component layout shared by fused arithmetic, derivative and particle range graph operations.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public enum Electron2D.VisualShaderFloatOpType`](#member-7d4c33e3ab7c) | The floating component layout shared by fused arithmetic, derivative and particle range graph operations. |
| [`public const Electron2D.VisualShaderFloatOpType Max = 4`](#member-1209278c1fa7) | The nonselectable layout-count sentinel. |
| [`public const Electron2D.VisualShaderFloatOpType Scalar = 0`](#member-71df5058f0d0) | A floating scalar. |
| [`public const Electron2D.VisualShaderFloatOpType Vector2 = 1`](#member-2cea61eddfdb) | Two floating components. |
| [`public const Electron2D.VisualShaderFloatOpType Vector3 = 2`](#member-ca291e911e2a) | Three floating components. |
| [`public const Electron2D.VisualShaderFloatOpType Vector4 = 3`](#member-8c887892b9fb) | Four floating components. |

<a id="member-7d4c33e3ab7c"></a>
### `VisualShaderFloatOpType`

```csharp
public enum Electron2D.VisualShaderFloatOpType
```

The floating component layout shared by fused arithmetic, derivative and particle range graph operations.

<a id="member-1209278c1fa7"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderFloatOpType Max = 4
```

The nonselectable layout-count sentinel.

<a id="member-71df5058f0d0"></a>
### `Scalar`

```csharp
public const Electron2D.VisualShaderFloatOpType Scalar = 0
```

A floating scalar.

<a id="member-2cea61eddfdb"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderFloatOpType Vector2 = 1
```

Two floating components.

<a id="member-ca291e911e2a"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderFloatOpType Vector3 = 2
```

Three floating components.

<a id="member-8c887892b9fb"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderFloatOpType Vector4 = 3
```

Four floating components.

## Lifecycle and verification

This owner-specific selector has value lifetime. Undefined selections or flag bits are rejected by the owning API. Max, where present, is nonselectable.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
