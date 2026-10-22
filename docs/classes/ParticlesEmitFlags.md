# ParticlesEmitFlags

Last updated: 2026-10-22

Source: [ParticlesEmitFlags.cs](../../src/Scene/Resources/ParticlesEmitFlags.cs).

```csharp
public enum Electron2D.ParticlesEmitFlags
```

## Description

Selects supplied fields for manual particle emission and graph subemission.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public enum Electron2D.ParticlesEmitFlags`](#member-038325a263de) | Selects supplied fields for manual particle emission and graph subemission. |
| [`public const Electron2D.ParticlesEmitFlags Color = 8`](#member-acba52e35df2) | Copies the supplied color. |
| [`public const Electron2D.ParticlesEmitFlags Custom = 16`](#member-8e786b4fafc5) | Copies the supplied four numeric custom channels. |
| [`public const Electron2D.ParticlesEmitFlags Position = 1`](#member-214534d8378d) | Copies the supplied position. |
| [`public const Electron2D.ParticlesEmitFlags RotationScale = 2`](#member-a4b4084e6906) | Copies the supplied rotation and scale. |
| [`public const Electron2D.ParticlesEmitFlags Velocity = 4`](#member-ddfb8c07ed9b) | Copies the supplied velocity. |

<a id="member-038325a263de"></a>
### `ParticlesEmitFlags`

```csharp
public enum Electron2D.ParticlesEmitFlags
```

Selects supplied fields for manual particle emission and graph subemission.

<a id="member-acba52e35df2"></a>
### `Color`

```csharp
public const Electron2D.ParticlesEmitFlags Color = 8
```

Copies the supplied color.

<a id="member-8e786b4fafc5"></a>
### `Custom`

```csharp
public const Electron2D.ParticlesEmitFlags Custom = 16
```

Copies the supplied four numeric custom channels.

<a id="member-214534d8378d"></a>
### `Position`

```csharp
public const Electron2D.ParticlesEmitFlags Position = 1
```

Copies the supplied position.

<a id="member-a4b4084e6906"></a>
### `RotationScale`

```csharp
public const Electron2D.ParticlesEmitFlags RotationScale = 2
```

Copies the supplied rotation and scale.

<a id="member-ddfb8c07ed9b"></a>
### `Velocity`

```csharp
public const Electron2D.ParticlesEmitFlags Velocity = 4
```

Copies the supplied velocity.

## Lifecycle and verification

This owner-specific selector has value lifetime. Undefined selections or flag bits are rejected by the owning API. Max, where present, is nonselectable.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
