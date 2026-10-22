# VisualShaderNodeParticleRandomness

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleRandomness.cs](../../src/Scene/Resources/VisualShaderNodeParticleRandomness.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParticleRandomness
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Generates a seeded scalar or component-wise numeric vector range in a particle graph.

An unconnected seed uses the lifecycle's sequential stream. A connected seed is a writable generated value whose state advances for each component. Equal and reversed bounds retain arithmetic range semantics.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeParticleRandomness`](#member-6e7b033008e2) | Generates a seeded scalar or component-wise numeric vector range in a particle graph. |
| [`public VisualShaderNodeParticleRandomness()`](#member-96e1f5893ba3) | Creates a scalar range with explicit bounds minus one and one. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-bcf21ea47285) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-f77a5de17c90) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderFloatOpType OpType { get; set; }`](#member-899cca8f44fd) | Gets or changes the range width, converting both bounds to the selected numeric role. |

<a id="member-6e7b033008e2"></a>
### `VisualShaderNodeParticleRandomness`

```csharp
public sealed class Electron2D.VisualShaderNodeParticleRandomness
```

Generates a seeded scalar or component-wise numeric vector range in a particle graph.

An unconnected seed uses the lifecycle's sequential stream. A connected seed is a writable generated value whose state advances for each component. Equal and reversed bounds retain arithmetic range semantics.

<a id="member-96e1f5893ba3"></a>
### `.ctor`

```csharp
public VisualShaderNodeParticleRandomness()
```

Creates a scalar range with explicit bounds minus one and one.

<a id="member-bcf21ea47285"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-f77a5de17c90"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-899cca8f44fd"></a>
### `OpType`

```csharp
public Electron2D.VisualShaderFloatOpType OpType { get; set; }
```

Gets or changes the range width, converting both bounds to the selected numeric role.

Scalar initially. Missing bounds are recreated as zero when the width changes.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The width is undefined or Max.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
