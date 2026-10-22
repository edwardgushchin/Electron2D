# VisualShaderNodeColorFunc

Last updated: 2026-10-22

Source: [VisualShaderNodeColorFunc.cs](../../src/Scene/Resources/VisualShaderNodeColorFunc.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeColorFunc
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Transforms three numeric color components in an imported canvas shader graph.

RGB and HSV use Vector3 ports. Conversions do not clamp signed or HDR output. Grayscale replicates the largest input component. Edits require reimport.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeColorFunc { Function = VisualShaderNodeColorFunc.FunctionEnum.Sepia };
node.SetInputPortDefaultValue(0, new Vector3(.2f, .5f, .8f));
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/effect-source.e2dres", SaverFlags.BundleResources);
```

Import through [the graph workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeColorFunc`](#member-63392368b3ae) | Transforms three numeric color components in an imported canvas shader graph. |
| [`public VisualShaderNodeColorFunc()`](#member-f82ea8967850) | Creates a grayscale transform with a zero Vector3 default. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-dea69eee531d) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-c228680cbd34) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeColorFunc.FunctionEnum Function { get; set; }`](#member-89045cafe041) | Gets or selects the color transform. |

<a id="member-63392368b3ae"></a>
### `VisualShaderNodeColorFunc`

```csharp
public sealed class Electron2D.VisualShaderNodeColorFunc
```

Transforms three numeric color components in an imported canvas shader graph.

RGB and HSV use Vector3 ports. Conversions do not clamp signed or HDR output. Grayscale replicates the largest input component. Edits require reimport.

<a id="member-f82ea8967850"></a>
### `.ctor`

```csharp
public VisualShaderNodeColorFunc()
```

Creates a grayscale transform with a zero Vector3 default.

<a id="member-dea69eee531d"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-c228680cbd34"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-89045cafe041"></a>
### `Function`

```csharp
public Electron2D.VisualShaderNodeColorFunc.FunctionEnum Function { get; set; }
```

Gets or selects the color transform.

An unchanged selection is quiet.

Grayscale initially. Existing input values and default absence survive selection changes.

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
