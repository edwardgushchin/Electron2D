# VisualShaderNodeParticleAccelerator.ModeEnum

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleAccelerator.cs](../../src/Scene/Resources/VisualShaderNodeParticleAccelerator.cs).

```csharp
public enum Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum
```

## Description

The acceleration direction policy.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public enum Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum`](#member-1103e5b47245) | The acceleration direction policy. |
| [`public const Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Linear = 0`](#member-498dbd602d2c) | Normalize the current velocity. |
| [`public const Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Max = 3`](#member-e624aeddb580) | The nonselectable mode bound. |
| [`public const Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Radial = 1`](#member-af53fc1ce4ef) | Normalize displacement from the emission origin. |
| [`public const Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Tangential = 2`](#member-c914405b0f3f) | Normalize the cross product of radial displacement and the supplied numeric axis. |

<a id="member-1103e5b47245"></a>
### `ModeEnum`

```csharp
public enum Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum
```

The acceleration direction policy.

<a id="member-498dbd602d2c"></a>
### `Linear`

```csharp
public const Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Linear = 0
```

Normalize the current velocity.

<a id="member-e624aeddb580"></a>
### `Max`

```csharp
public const Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Max = 3
```

The nonselectable mode bound.

<a id="member-af53fc1ce4ef"></a>
### `Radial`

```csharp
public const Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Radial = 1
```

Normalize displacement from the emission origin.

<a id="member-c914405b0f3f"></a>
### `Tangential`

```csharp
public const Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Tangential = 2
```

Normalize the cross product of radial displacement and the supplied numeric axis.

## Lifecycle and verification

This owner-specific selector has value lifetime. Undefined selections or flag bits are rejected by the owning API. Max, where present, is nonselectable.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
