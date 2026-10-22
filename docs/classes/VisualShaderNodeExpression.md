# VisualShaderNodeExpression

Last updated: 2026-10-22

Source: [VisualShaderNodeExpression.cs](../../src/Scene/Resources/VisualShaderNodeExpression.cs).

```csharp
public class Electron2D.VisualShaderNodeExpression
```

Inherits: [VisualShaderNodeGroupBase](VisualShaderNodeGroupBase.md).

## Description

Executes a GLSL statement block with typed, named graph ports during offline canvas import.

Reachable blocks run after their inputs, in a local scope. Numeric outputs begin at zero, except matrices which begin at identity. Input sampler names refer to connected image symbols; samplers cannot be local output variables. Global definitions use VisualShaderNodeGlobalExpression. Statements can contain loops, branches and fragment discard. The shader compiler validates stage legality. Graph edits never compile or replace the last imported runtime program.

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
| [`public class Electron2D.VisualShaderNodeExpression`](#member-8b9440978c82) | Executes a GLSL statement block with typed, named graph ports during offline canvas import. |
| [`public VisualShaderNodeExpression()`](#member-6582e1042fe7) | Creates empty source with no ports and zero layout size. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-733b640c66b6) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-f1a44b1f24fa) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.String Expression { get; set; }`](#member-ba1c0b3d3f51) | Gets or sets GLSL source consumed by the offline graph importer. |

<a id="member-8b9440978c82"></a>
### `VisualShaderNodeExpression`

```csharp
public class Electron2D.VisualShaderNodeExpression
```

Executes a GLSL statement block with typed, named graph ports during offline canvas import.

Reachable blocks run after their inputs, in a local scope. Numeric outputs begin at zero, except matrices which begin at identity. Input sampler names refer to connected image symbols; samplers cannot be local output variables. Global definitions use VisualShaderNodeGlobalExpression. Statements can contain loops, branches and fragment discard. The shader compiler validates stage legality. Graph edits never compile or replace the last imported runtime program.

<a id="member-6582e1042fe7"></a>
### `.ctor`

```csharp
public VisualShaderNodeExpression()
```

Creates empty source with no ports and zero layout size.

<a id="member-733b640c66b6"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-f1a44b1f24fa"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-ba1c0b3d3f51"></a>
### `Expression`

```csharp
public System.String Expression { get; set; }
```

Gets or sets GLSL source consumed by the offline graph importer.

Every successful assignment publishes Changed, including equal text. Port identifier references are substituted without rewriting comments, member selectors or longer identifiers.

Empty initially; strict UTF-8 source is bounded to eight MiB.

Exceptions:

- `T:System.ArgumentNullException`: The source is null.
- `T:System.ArgumentException`: The text has invalid UTF-16 or exceeds its UTF-8 byte budget.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Typed values, explicit default absence and shared definition aliases survive copies and portable archives. Changes require reimport before execution.

The public VisualShader.Consumer exercises typed defaults, copies, factories, typed source/port limits, source/compiled archives and executable GLSL procedures. See [the operation contract](../components/visual-shaders.md#glsl-expression-blocks) for ports, pixel units, port mutation, scope, control flow and source validation and current verification limits.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Typed resources](../decisions/resources.md#adr-0013)
