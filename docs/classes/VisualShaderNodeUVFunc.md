# VisualShaderNodeUVFunc

Last updated: 2026-10-22

Source: [VisualShaderNodeUVFunc.cs](../../src/Scene/Resources/VisualShaderNodeUVFunc.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeUVFunc
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Pans or scales numeric UV coordinates in an imported canvas graph.

An unconnected UV input uses the current stage UV, including when a numeric default is stored. Inputs one and two are scale and offset/pivot. Edits require reimport before drawing changes.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeUVFunc { Function = VisualShaderNodeUVFunc.FunctionEnum.Scaling };
node.SetInputPortDefaultValue(1, new Vector2(2, 2));
node.SetInputPortDefaultValue(2, new Vector2(.5f, .5f));
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/effect-source.e2dres", SaverFlags.BundleResources);
```

Import through [the graph workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeUVFunc`](#member-35ec288575ab) | Pans or scales numeric UV coordinates in an imported canvas graph. |
| [`public VisualShaderNodeUVFunc()`](#member-4b4530b94161) | Creates a panning operation with unit scale, zero offset and implicit UV. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-6fce862bb359) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-e7a1a85301fb) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeUVFunc.FunctionEnum Function { get; set; }`](#member-fae423219fdc) | Gets or selects panning or scaling. |

<a id="member-35ec288575ab"></a>
### `VisualShaderNodeUVFunc`

```csharp
public sealed class Electron2D.VisualShaderNodeUVFunc
```

Pans or scales numeric UV coordinates in an imported canvas graph.

An unconnected UV input uses the current stage UV, including when a numeric default is stored. Inputs one and two are scale and offset/pivot. Edits require reimport before drawing changes.

<a id="member-4b4530b94161"></a>
### `.ctor`

```csharp
public VisualShaderNodeUVFunc()
```

Creates a panning operation with unit scale, zero offset and implicit UV.

<a id="member-6fce862bb359"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-e7a1a85301fb"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-fae423219fdc"></a>
### `Function`

```csharp
public Electron2D.VisualShaderNodeUVFunc.FunctionEnum Function { get; set; }
```

Gets or selects panning or scaling.

An unchanged selection is quiet. A changed selection commits its defaults and function before Changed is emitted.

Panning initially. Existing offset/pivot values survive a change; an absent value becomes zero for Panning or (0.5,0.5) for Scaling.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selector is undefined or Max.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer exercises both canvas stages, selector transitions, errors, source/compiled archives and composed image-effect materials. See [the operation contract](../components/visual-shaders.md#uv-and-color-image-effects) for formulas, native arithmetic and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
