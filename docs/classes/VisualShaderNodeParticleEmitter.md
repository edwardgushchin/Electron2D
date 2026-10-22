# VisualShaderNodeParticleEmitter

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleEmitter.cs](../../src/Scene/Resources/VisualShaderNodeParticleEmitter.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeParticleEmitter
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

The shared planar position role of particle emission-shape graph operations.

Emitter positions use two components. Dimensional mode selection has no separate planar behavior.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public abstract class Electron2D.VisualShaderNodeParticleEmitter`](#member-b1d6b5e5952c) | The shared planar position role of particle emission-shape graph operations. |
| [`protected VisualShaderNodeParticleEmitter()`](#member-2890dfde6b16) | Initializes a planar emission-shape operation. |

<a id="member-b1d6b5e5952c"></a>
### `VisualShaderNodeParticleEmitter`

```csharp
public abstract class Electron2D.VisualShaderNodeParticleEmitter
```

The shared planar position role of particle emission-shape graph operations.

Emitter positions use two components. Dimensional mode selection has no separate planar behavior.

<a id="member-2890dfde6b16"></a>
### `.ctor`

```csharp
protected VisualShaderNodeParticleEmitter()
```

Initializes a planar emission-shape operation.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
