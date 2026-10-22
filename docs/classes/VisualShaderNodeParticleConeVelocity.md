# VisualShaderNodeParticleConeVelocity

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleConeVelocity.cs](../../src/Scene/Resources/VisualShaderNodeParticleConeVelocity.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParticleConeVelocity
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Generates a seeded three-component numeric direction with independent angular spread.

The direction is numeric data; physical particle outputs retain only their planar components.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeParticleConeVelocity`](#member-d7e80fa8c1c5) | Generates a seeded three-component numeric direction with independent angular spread. |
| [`public VisualShaderNodeParticleConeVelocity()`](#member-277308478e73) | Creates direction (1,0,0) and a 45-degree spread. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-cce5ba283879) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-d7e80fa8c1c5"></a>
### `VisualShaderNodeParticleConeVelocity`

```csharp
public sealed class Electron2D.VisualShaderNodeParticleConeVelocity
```

Generates a seeded three-component numeric direction with independent angular spread.

The direction is numeric data; physical particle outputs retain only their planar components.

<a id="member-277308478e73"></a>
### `.ctor`

```csharp
public VisualShaderNodeParticleConeVelocity()
```

Creates direction (1,0,0) and a 45-degree spread.

<a id="member-cce5ba283879"></a>
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
