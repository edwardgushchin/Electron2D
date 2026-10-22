# VisualShaderNodeResizableBase

Last updated: 2026-10-22

Source: [VisualShaderNodeResizableBase.cs](../../src/Scene/Resources/VisualShaderNodeResizableBase.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeResizableBase
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

A graph definition with portable canvas-layout size metadata.

Size belongs to source authoring and has no effect on rendered geometry or compiled code.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeExpression();
node.AddInputPort(0, VisualShaderNode.PortType.Scalar, "amount");
node.AddOutputPort(0, VisualShaderNode.PortType.Vector3, "color");
node.SetInputPortDefaultValue(0, .5f);
node.Size = new Vector2(240, 160);
node.Expression = "color = vec3(amount);";
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/expression-source.e2dres", SaverFlags.BundleResources);
```

Import through [the graph workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public abstract class Electron2D.VisualShaderNodeResizableBase`](#member-79d648e36bc4) | A graph definition with portable canvas-layout size metadata. |
| [`protected VisualShaderNodeResizableBase()`](#member-38d098bb1b27) | Initializes zero layout size. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-0cddce091053) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.Vector2 Size { get; set; }`](#member-b0cfc8768be7) | Gets or sets the finite graph-layout size without publishing Changed. |

<a id="member-79d648e36bc4"></a>
### `VisualShaderNodeResizableBase`

```csharp
public abstract class Electron2D.VisualShaderNodeResizableBase
```

A graph definition with portable canvas-layout size metadata.

Size belongs to source authoring and has no effect on rendered geometry or compiled code.

<a id="member-38d098bb1b27"></a>
### `.ctor`

```csharp
protected VisualShaderNodeResizableBase()
```

Initializes zero layout size.

<a id="member-0cddce091053"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-b0cfc8768be7"></a>
### `Size`

```csharp
public Electron2D.Vector2 Size { get; set; }
```

Gets or sets the finite graph-layout size without publishing Changed.

Zero initially. Signed components are preserved as source metadata.

Exceptions:

- `T:System.ArgumentException`: A component is nonfinite.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer exercises typed defaults, copies, factories, typed source/port limits, source/compiled archives and executable GLSL procedures. See [the operation contract](../components/visual-shaders.md#glsl-expression-blocks) for ports, pixel units, port mutation, scope, control flow and source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
