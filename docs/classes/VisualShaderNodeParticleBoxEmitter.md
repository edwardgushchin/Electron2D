# VisualShaderNodeParticleBoxEmitter

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleBoxEmitter.cs](../../src/Scene/Resources/VisualShaderNodeParticleBoxEmitter.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParticleBoxEmitter
```

Inherits: [VisualShaderNodeParticleEmitter](VisualShaderNodeParticleEmitter.md).

## Description

Samples a centered rectangle whose input is the full width and height.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeParticleBoxEmitter`](#member-0f5d0e69286b) | Samples a centered rectangle whose input is the full width and height. |
| [`public VisualShaderNodeParticleBoxEmitter()`](#member-ed2271949a44) | Creates the initial emission shape and its explicit numeric defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-51a3b40c1a7b) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-0f5d0e69286b"></a>
### `VisualShaderNodeParticleBoxEmitter`

```csharp
public sealed class Electron2D.VisualShaderNodeParticleBoxEmitter
```

Samples a centered rectangle whose input is the full width and height.

<a id="member-ed2271949a44"></a>
### `.ctor`

```csharp
public VisualShaderNodeParticleBoxEmitter()
```

Creates the initial emission shape and its explicit numeric defaults.

<a id="member-51a3b40c1a7b"></a>
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
