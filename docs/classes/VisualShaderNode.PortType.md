# VisualShaderNode.PortType

Last updated: 2026-10-22

Source: [VisualShaderNode.cs](../../src/Scene/Resources/VisualShaderNode.cs).

```csharp
public enum Electron2D.VisualShaderNode.PortType
```

## Description

The value role carried by a graph port.

## Member summary

| Member | Contract |
| --- | --- |
| [`public const Electron2D.VisualShaderNode.PortType Boolean = 6`](#member-bc51c1d68f30) | A Boolean scalar. |
| [`public const Electron2D.VisualShaderNode.PortType Matrix = 7`](#member-f5aa6431158b) | A four-by-four numeric matrix carried by System.Numerics.Matrix4x4. |
| [`public const Electron2D.VisualShaderNode.PortType Max = 9`](#member-042e6a654b2d) | The nonselectable upper bound of the complete logical port type table. |
| [`public const Electron2D.VisualShaderNode.PortType Sampler = 8`](#member-9c81fa6cf67d) | An opaque sampled-image reference; it accepts no numeric default or conversion. |
| [`public const Electron2D.VisualShaderNode.PortType Scalar = 0`](#member-bed930ad0a4f) | A single-precision scalar. |
| [`public const Electron2D.VisualShaderNode.PortType ScalarInt = 1`](#member-a5ab7a2bf8c0) | A signed 32-bit scalar. |
| [`public const Electron2D.VisualShaderNode.PortType ScalarUInt = 2`](#member-dc29b158f555) | An unsigned 32-bit scalar. |
| [`public const Electron2D.VisualShaderNode.PortType Vector2 = 3`](#member-ab6e384ab888) | Two numeric floating components. |
| [`public const Electron2D.VisualShaderNode.PortType Vector3 = 4`](#member-93033b68b5b5) | Three numeric floating components. |
| [`public const Electron2D.VisualShaderNode.PortType Vector4 = 5`](#member-6926103f2c95) | Four numeric floating components. |

<a id="member-bc51c1d68f30"></a>
### `Boolean`

```csharp
public const Electron2D.VisualShaderNode.PortType Boolean = 6
```

A Boolean scalar.

<a id="member-f5aa6431158b"></a>
### `Matrix`

```csharp
public const Electron2D.VisualShaderNode.PortType Matrix = 7
```

A four-by-four numeric matrix carried by System.Numerics.Matrix4x4.

<a id="member-042e6a654b2d"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNode.PortType Max = 9
```

The nonselectable upper bound of the complete logical port type table.

<a id="member-9c81fa6cf67d"></a>
### `Sampler`

```csharp
public const Electron2D.VisualShaderNode.PortType Sampler = 8
```

An opaque sampled-image reference; it accepts no numeric default or conversion.

<a id="member-bed930ad0a4f"></a>
### `Scalar`

```csharp
public const Electron2D.VisualShaderNode.PortType Scalar = 0
```

A single-precision scalar.

<a id="member-a5ab7a2bf8c0"></a>
### `ScalarInt`

```csharp
public const Electron2D.VisualShaderNode.PortType ScalarInt = 1
```

A signed 32-bit scalar.

<a id="member-dc29b158f555"></a>
### `ScalarUInt`

```csharp
public const Electron2D.VisualShaderNode.PortType ScalarUInt = 2
```

An unsigned 32-bit scalar.

<a id="member-ab6e384ab888"></a>
### `Vector2`

```csharp
public const Electron2D.VisualShaderNode.PortType Vector2 = 3
```

Two numeric floating components.

<a id="member-93033b68b5b5"></a>
### `Vector3`

```csharp
public const Electron2D.VisualShaderNode.PortType Vector3 = 4
```

Three numeric floating components.

<a id="member-6926103f2c95"></a>
### `Vector4`

```csharp
public const Electron2D.VisualShaderNode.PortType Vector4 = 5
```

Four numeric floating components.

## Lifecycle and verification

This selector has value lifetime and is not disposable. Max is a nonselectable logical bound; undefined values do not become selectable through numeric casts.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
