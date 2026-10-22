# VisualShaderNodeParameterRef

Last updated: 2026-10-22

Source: [VisualShaderNodeParameterRef.cs](../../src/Scene/Resources/VisualShaderNodeParameterRef.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeParameterRef
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Reads a named parameter declared in either canvas graph stage.

Resolution is local to the containing graph, including when definitions are borrowed by several graphs. Color references expose RGB and alpha separately; image references carry the containing declaration's sampler shape. The initial [None] selection produces scalar zero. Renaming or removing a declaration may invalidate stored connections; import rejects them before replacing output.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeParameterRef()`](#member-2ee4e8519257) | Creates an unselected parameter reference. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-5fec759f3c68) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-f812ca3bf91a) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.String ParameterName { get; set; }`](#member-90a178ffb019) | Gets or assigns the referenced case-sensitive parameter name. |

<a id="member-2ee4e8519257"></a>
### `.ctor`

```csharp
public VisualShaderNodeParameterRef()
```

Creates an unselected parameter reference.

<a id="member-5fec759f3c68"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-f812ca3bf91a"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-90a178ffb019"></a>
### `ParameterName`

```csharp
public System.String ParameterName { get; set; }
```

Gets or assigns the referenced case-sensitive parameter name.

[None] initially. Each assignment emits Changed after commitment.

Exceptions:

- `T:System.ArgumentNullException`: The name is null.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed numeric and opaque ports, sampler policies, resource aliases and portable archives. Its image fixtures check real software-rendered pixels, color transfer, independent layers/mips and refreshed native storage. See [the component contract](../components/visual-shaders.md#sampled-images-and-canvas-channels) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
