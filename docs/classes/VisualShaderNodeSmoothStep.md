# VisualShaderNodeSmoothStep

Last updated: 2026-10-21

Source: [VisualShaderNodeSmoothStep.cs](../../src/Scene/Resources/VisualShaderNodeSmoothStep.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeSmoothStep
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Interpolates a smooth threshold mask between two edges.

Inputs are lower edge, upper edge and value. Scalar-control layouts broadcast both edges. Values outside ordered edges clamp to zero or one; equal or reversed edges retain native shader domain behavior.

## Example

```csharp
using var node = new VisualShaderNodeSmoothStep { OpType = VisualShaderNumericOpType.Vector3 };
node.SetInputPortDefaultValue(0, new Vector3(.125f, .25f, .5f));
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/range-source.e2dres", SaverFlags.BundleResources);
```

Import this resource through [the graph pipeline](../components/visual-shaders.md#numeric-ranges-and-masks) before assigning the compiled material.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeSmoothStep()`](#member-ae1cd7fea387) | Creates a scalar definition with its initial input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-76cfd195407e) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-6e2fcd54daa8) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNumericOpType OpType { get; set; }`](#member-87d57086dc32) | Gets or selects the numeric input layout, converting existing defaults atomically. |

<a id="member-ae1cd7fea387"></a>
### `.ctor`

```csharp
public VisualShaderNodeSmoothStep()
```

Creates a scalar definition with its initial input defaults.

The lower and upper edges are zero and one, with value one half.

<a id="member-76cfd195407e"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-6e2fcd54daa8"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-87d57086dc32"></a>
### `OpType`

```csharp
public Electron2D.VisualShaderNumericOpType OpType { get; set; }
```

Gets or selects the numeric input layout, converting existing defaults atomically.

An unchanged selection emits no event. The complete new layout and defaults commit before Changed observers run.

Scalar initially. A changed selection recreates absent defaults; vector expansion repeats the last component.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selection is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graph additions borrow them; ordinary disposal guards apply. Defaults, selectors and width survive duplication and typed archives. Editing requires reimport before rendering changes.

The public VisualShader.Consumer covers typed requests, mixed/dormant defaults, copy/archives, multi-output graphs and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
