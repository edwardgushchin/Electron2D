# VisualShaderNodeRemap

Last updated: 2026-10-21

Source: [VisualShaderNodeRemap.cs](../../src/Scene/Resources/VisualShaderNodeRemap.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeRemap
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Maps a numeric value from one interval to another without clamping.

Inputs are value, input minimum, input maximum, output minimum and output maximum. Scalar-control layouts broadcast all four bounds. Reversed intervals remain valid; zero input width retains native division behavior.

## Example

```csharp
using var node = new VisualShaderNodeRemap { OpType = VisualShaderNumericOpType.Vector3 };
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
| [`public VisualShaderNodeRemap()`](#member-94aed6c7e310) | Creates a scalar definition with its initial input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-e314f9e41318) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-745e2366bee1) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNumericOpType OpType { get; set; }`](#member-c5c0a6c9bbb5) | Gets or selects the numeric input layout, converting existing defaults atomically. |

<a id="member-94aed6c7e310"></a>
### `.ctor`

```csharp
public VisualShaderNodeRemap()
```

Creates a scalar definition with its initial input defaults.

The value default is absent. Input and output bounds each have explicit zero and one float defaults.

<a id="member-e314f9e41318"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-745e2366bee1"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-c5c0a6c9bbb5"></a>
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
