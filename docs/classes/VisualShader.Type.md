# VisualShader.Type

Last updated: 2026-10-25

Source: [VisualShader.cs](../../src/Scene/Resources/VisualShader.cs).

```csharp
public enum Electron2D.VisualShader.Type
```

## Description

The source stage containing a graph.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public enum Electron2D.VisualShader.Type`](#member-48cc808d870d) | The source stage containing a graph. |
| [`public const Electron2D.VisualShader.Type Collide = 5`](#member-a06c39b432d0) | Processes an active particle touching the canvas distance field. |
| [`public const Electron2D.VisualShader.Type Fragment = 1`](#member-c5d87a95f843) | Computes canvas fragment color. |
| [`public const Electron2D.VisualShader.Type Light = 2`](#light) | Computes one actual canvas scene-light contribution. |
| [`public const Electron2D.VisualShader.Type Max = 11`](#member-17d66c334528) | The nonselectable upper bound of all shader graph stage identities. |
| [`public const Electron2D.VisualShader.Type Process = 4`](#member-a47bbf11a53a) | Processes an active particle outside a contact. |
| [`public const Electron2D.VisualShader.Type ProcessCustom = 7`](#member-9a11cad2d555) | Applies custom channels after ordinary particle processing. |
| [`public const Electron2D.VisualShader.Type Start = 3`](#member-02994a7ccd5d) | Initializes a newly born particle. |
| [`public const Electron2D.VisualShader.Type StartCustom = 6`](#member-4d6ca256be51) | Applies custom channels after particle initialization. |
| [`public const Electron2D.VisualShader.Type TextureBlit = 10`](#member-1e63bbed14bf) | Computes up to four writable-texture color outputs. |
| [`public const Electron2D.VisualShader.Type Vertex = 0`](#member-5c6c0a685db8) | Transforms supplied canvas vertices before rasterization. |

<a id="member-48cc808d870d"></a>
### `Type`

```csharp
public enum Electron2D.VisualShader.Type
```

The source stage containing a graph.

<a id="member-a06c39b432d0"></a>
### `Collide`

```csharp
public const Electron2D.VisualShader.Type Collide = 5
```

Processes an active particle touching the canvas distance field.

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

<a id="member-a47bbf11a53a"></a>
### `Process`

```csharp
public const Electron2D.VisualShader.Type Process = 4
```

Processes an active particle outside a contact.

<a id="member-9a11cad2d555"></a>
### `ProcessCustom`

```csharp
public const Electron2D.VisualShader.Type ProcessCustom = 7
```

Applies custom channels after ordinary particle processing.

<a id="member-02994a7ccd5d"></a>
### `Start`

```csharp
public const Electron2D.VisualShader.Type Start = 3
```

Initializes a newly born particle.

<a id="member-4d6ca256be51"></a>
### `StartCustom`

```csharp
public const Electron2D.VisualShader.Type StartCustom = 6
```

Applies custom channels after particle initialization.

<a id="member-1e63bbed14bf"></a>
### `TextureBlit`

```csharp
public const Electron2D.VisualShader.Type TextureBlit = 10
```

Computes up to four writable-texture color outputs.

<a id="member-5c6c0a685db8"></a>
### `Vertex`

```csharp
public const Electron2D.VisualShader.Type Vertex = 0
```

Transforms supplied canvas vertices before rasterization.

<a id="light"></a>
### `Light`

`public const Electron2D.VisualShader.Type Light = 2` selects the authored canvas per-light stage. Node zero accepts RGB, alpha and RGB shadow modulation. Fragment/color, both varying roles, images and shared parameters follow [the light graph contract](../components/canvas-light-graphs.md).

## Lifecycle and verification

This owner-specific selector has value lifetime. Undefined selections or flag bits are rejected by the owning API. Max, where present, is nonselectable.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
