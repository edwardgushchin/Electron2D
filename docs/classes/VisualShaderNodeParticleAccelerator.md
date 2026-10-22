# VisualShaderNodeParticleAccelerator

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleAccelerator.cs](../../src/Scene/Resources/VisualShaderNodeParticleAccelerator.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParticleAccelerator
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Calculates seeded linear, radial or tangential particle acceleration components.

Three-component arithmetic lifts physical planar velocity/position with a zero third component.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeParticleAccelerator`](#member-eb860cbae8d3) | Calculates seeded linear, radial or tangential particle acceleration components. |
| [`public VisualShaderNodeParticleAccelerator()`](#member-2aee3501707a) | Creates a linear operation with unit component amounts, zero randomness and axis (0,-9.8,0). |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-2cb3ec3d07ec) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-39aed16ba78f) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Mode { get; set; }`](#member-cefd3ae070fe) | Gets or changes the acceleration direction policy. |

<a id="member-eb860cbae8d3"></a>
### `VisualShaderNodeParticleAccelerator`

```csharp
public sealed class Electron2D.VisualShaderNodeParticleAccelerator
```

Calculates seeded linear, radial or tangential particle acceleration components.

Three-component arithmetic lifts physical planar velocity/position with a zero third component.

<a id="member-2aee3501707a"></a>
### `.ctor`

```csharp
public VisualShaderNodeParticleAccelerator()
```

Creates a linear operation with unit component amounts, zero randomness and axis (0,-9.8,0).

<a id="member-2cb3ec3d07ec"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-39aed16ba78f"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-cefd3ae070fe"></a>
### `Mode`

```csharp
public Electron2D.VisualShaderNodeParticleAccelerator.ModeEnum Mode { get; set; }
```

Gets or changes the acceleration direction policy.

Linear initially.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The mode is undefined or Max.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
