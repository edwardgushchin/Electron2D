# VisualShaderNodeParticleOutput

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleOutput.cs](../../src/Scene/Resources/VisualShaderNodeParticleOutput.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParticleOutput
```

Inherits: [VisualShaderNodeOutput](VisualShaderNodeOutput.md).

## Description

The reserved planar particle output operation of a visual shader lifecycle stage.

Physical position and velocity use two components. Color/custom channels retain numeric components; matrix inputs project their XY basis and origin. Start and Process angle inputs use radians about the planar normal.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeParticleOutput`](#member-e1a98559a3cf) | The reserved planar particle output operation of a visual shader lifecycle stage. |
| [`public VisualShaderNodeParticleOutput()`](#member-df813b9c7fd3) | Creates an unbound particle-start output definition. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-1056a8093ecb) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-e1a98559a3cf"></a>
### `VisualShaderNodeParticleOutput`

```csharp
public sealed class Electron2D.VisualShaderNodeParticleOutput
```

The reserved planar particle output operation of a visual shader lifecycle stage.

Physical position and velocity use two components. Color/custom channels retain numeric components; matrix inputs project their XY basis and origin. Start and Process angle inputs use radians about the planar normal.

<a id="member-df813b9c7fd3"></a>
### `.ctor`

```csharp
public VisualShaderNodeParticleOutput()
```

Creates an unbound particle-start output definition.

<a id="member-1056a8093ecb"></a>
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
