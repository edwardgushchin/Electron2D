# VisualShaderNodeOutput

Last updated: 2026-10-25

Source: [VisualShaderNodeOutput.cs](../../src/Scene/Resources/VisualShaderNodeOutput.cs).

```csharp
public class Electron2D.VisualShaderNodeOutput
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

The reserved canvas or writable-texture output operation owned by a visual shader stage.

Retrieve it as node zero. Its input indices follow the containing stage. Canvas vertex and fragment roots retain their renderer roles. Light accepts RGB, alpha and RGB shadow modulation at ports zero through two; TextureBlit accepts four numeric RGBA outputs. Particle roots use VisualShaderNodeParticleOutput.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public class Electron2D.VisualShaderNodeOutput`](#member-7d81b91565a0) | The reserved canvas or writable-texture output operation owned by a visual shader stage. |
| [`public VisualShaderNodeOutput()`](#member-4df3df9d230f) | Creates an unbound fragment output definition. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-004a7f904f56) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-ca715114a207) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-7d81b91565a0"></a>
### `VisualShaderNodeOutput`

```csharp
public class Electron2D.VisualShaderNodeOutput
```

The reserved canvas or writable-texture output operation owned by a visual shader stage.

Retrieve it as node zero. Its input indices follow the containing stage. Canvas vertex and fragment roots retain their renderer roles. Light accepts RGB, alpha and RGB shadow modulation at ports zero through two; TextureBlit accepts four numeric RGBA outputs. Particle roots use VisualShaderNodeParticleOutput.

<a id="member-4df3df9d230f"></a>
### `.ctor`

```csharp
public VisualShaderNodeOutput()
```

Creates an unbound fragment output definition.

<a id="member-004a7f904f56"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-ca715114a207"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)

## Authored canvas lights

See [canvas light graphs](../components/canvas-light-graphs.md) for the executing stage, typed inputs/outputs, fragment/vertex varying producers, shared native resource slots, custom provider activation, portable program validation and measured limits.
