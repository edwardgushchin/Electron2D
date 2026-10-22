# VisualShaderNodeParticleMultiplyByAxisAngle

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleMultiplyByAxisAngle.cs](../../src/Scene/Resources/VisualShaderNodeParticleMultiplyByAxisAngle.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParticleMultiplyByAxisAngle
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Multiplies three numeric components by an axis-angle rotation in a particle graph.

This is numeric shader arithmetic. It does not add a spatial scene transform or particle state.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeParticleMultiplyByAxisAngle`](#member-2e11532a1427) | Multiplies three numeric components by an axis-angle rotation in a particle graph. |
| [`public VisualShaderNodeParticleMultiplyByAxisAngle()`](#member-e93c6ab52e98) | Creates a degree-based operation with axis (1,0,0) and angle zero. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-fcd2ce9cd7d4) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-fa8d7da4e619) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Boolean DegreesMode { get; set; }`](#member-fa63a9021e98) | Gets or sets whether the angle input uses degrees. |

<a id="member-2e11532a1427"></a>
### `VisualShaderNodeParticleMultiplyByAxisAngle`

```csharp
public sealed class Electron2D.VisualShaderNodeParticleMultiplyByAxisAngle
```

Multiplies three numeric components by an axis-angle rotation in a particle graph.

This is numeric shader arithmetic. It does not add a spatial scene transform or particle state.

<a id="member-e93c6ab52e98"></a>
### `.ctor`

```csharp
public VisualShaderNodeParticleMultiplyByAxisAngle()
```

Creates a degree-based operation with axis (1,0,0) and angle zero.

<a id="member-fcd2ce9cd7d4"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-fa8d7da4e619"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-fa63a9021e98"></a>
### `DegreesMode`

```csharp
public System.Boolean DegreesMode { get; set; }
```

Gets or sets whether the angle input uses degrees.

True initially; false selects radians.

Exceptions:

- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
