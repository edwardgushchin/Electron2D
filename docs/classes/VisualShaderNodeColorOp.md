# VisualShaderNodeColorOp

Last updated: 2026-10-22

Source: [VisualShaderNodeColorOp.cs](../../src/Scene/Resources/VisualShaderNodeColorOp.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeColorOp
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Blends two numeric RGB triples in an imported canvas shader graph.

Outputs retain signed/HDR values. Overlay, SoftLight and HardLight select each component's branch from the first input at 0.5. Dodge and Burn keep native division-by-zero semantics.

## Example

This snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var node = new VisualShaderNodeColorOp { Operator = VisualShaderNodeColorOp.OperatorEnum.Screen };
node.SetInputPortDefaultValue(0, new Vector3(.2f, .5f, .8f));
node.SetInputPortDefaultValue(1, new Vector3(.1f, .2f, .3f));
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/effect-source.e2dres", SaverFlags.BundleResources);
```

Import through [the graph workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public sealed class Electron2D.VisualShaderNodeColorOp`](#member-459398da414e) | Blends two numeric RGB triples in an imported canvas shader graph. |
| [`public VisualShaderNodeColorOp()`](#member-99a3c7ad882b) | Creates Screen with two zero Vector3 defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-c757a8b5a29f) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-bd4b2fd23ee8) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeColorOp.OperatorEnum Operator { get; set; }`](#member-a2b528835d85) | Gets or selects the color operation. |

<a id="member-459398da414e"></a>
### `VisualShaderNodeColorOp`

```csharp
public sealed class Electron2D.VisualShaderNodeColorOp
```

Blends two numeric RGB triples in an imported canvas shader graph.

Outputs retain signed/HDR values. Overlay, SoftLight and HardLight select each component's branch from the first input at 0.5. Dodge and Burn keep native division-by-zero semantics.

<a id="member-99a3c7ad882b"></a>
### `.ctor`

```csharp
public VisualShaderNodeColorOp()
```

Creates Screen with two zero Vector3 defaults.

<a id="member-c757a8b5a29f"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-bd4b2fd23ee8"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-a2b528835d85"></a>
### `Operator`

```csharp
public Electron2D.VisualShaderNodeColorOp.OperatorEnum Operator { get; set; }
```

Gets or selects the color operation.

An unchanged selection is quiet; edits require reimport before drawing changes.

Screen initially. Input values and default absence survive selection changes.

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
