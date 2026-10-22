# VisualShaderNodeParticleEmit

Last updated: 2026-10-22

Source: [VisualShaderNodeParticleEmit.cs](../../src/Scene/Resources/VisualShaderNodeParticleEmit.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParticleEmit
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Emits a subparticle through the retained bounded GPU queue when its condition is true.

This side-effect operation executes even without a connection to the reserved output. Missing field inputs use the current particle channels; queue saturation drops excess records.

## Example

See the executable authoring and runtime workflow in [particle and writable-texture graphs](../components/visual-shaders.md#particle-and-writable-texture-graph-domains). It includes canonical stages, port order, units, defaults, mode transitions and ownership.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeParticleEmit`](#member-f62599d96274) | Emits a subparticle through the retained bounded GPU queue when its condition is true. |
| [`public VisualShaderNodeParticleEmit()`](#member-d5e1191ba6e6) | Creates an emission operation supplying all five fields. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-a1842c011bf1) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-1e602d2e86b4) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.ParticlesEmitFlags Flags { get; set; }`](#member-f52179f5a786) | Gets or replaces the supplied-field mask. |

<a id="member-f62599d96274"></a>
### `VisualShaderNodeParticleEmit`

```csharp
public sealed class Electron2D.VisualShaderNodeParticleEmit
```

Emits a subparticle through the retained bounded GPU queue when its condition is true.

This side-effect operation executes even without a connection to the reserved output. Missing field inputs use the current particle channels; queue saturation drops excess records.

<a id="member-d5e1191ba6e6"></a>
### `.ctor`

```csharp
public VisualShaderNodeParticleEmit()
```

Creates an emission operation supplying all five fields.

<a id="member-a1842c011bf1"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-1e602d2e86b4"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-f52179f5a786"></a>
### `Flags`

```csharp
public Electron2D.ParticlesEmitFlags Flags { get; set; }
```

Gets or replaces the supplied-field mask.

All five flags initially; zero supplies no fields.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The mask contains undefined bits.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer and native particle integration corpus exercise typed defaults, copies, factories, source/compiled archives and executable compute/blit graphs. See [the domain contract](../components/visual-shaders.md#particle-and-writable-texture-graph-domains) for ports, units, scope, control flow, source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
