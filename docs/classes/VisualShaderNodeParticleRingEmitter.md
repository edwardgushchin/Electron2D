# VisualShaderNodeParticleRingEmitter

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleRingEmitter.cs](../../src/Scene/Resources/VisualShaderNodeParticleRingEmitter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParticleRingEmitter
```

Inherits: [VisualShaderNodeParticleEmitter](VisualShaderNodeParticleEmitter.md).

## Description

Samples a planar ring using an angular distribution and a linear radius range.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeParticleRingEmitter`](#member-b21048368196) | Samples a planar ring using an angular distribution and a linear radius range. |
| [`public VisualShaderNodeParticleRingEmitter()`](#member-6cd76d53dfba) | Creates the initial emission shape and its explicit numeric defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-e4deac262450) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-b21048368196"></a>
### `VisualShaderNodeParticleRingEmitter`

```csharp
public sealed class Electron2D.VisualShaderNodeParticleRingEmitter
```

Samples a planar ring using an angular distribution and a linear radius range.

<a id="member-6cd76d53dfba"></a>
### `.ctor`

```csharp
public VisualShaderNodeParticleRingEmitter()
```

Creates the initial emission shape and its explicit numeric defaults.

<a id="member-e4deac262450"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
