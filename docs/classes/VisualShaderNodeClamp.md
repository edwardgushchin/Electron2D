# VisualShaderNodeClamp

Last updated: 2026-10-21

Source: [VisualShaderNodeClamp.cs](../../src/Scene/Resources/VisualShaderNodeClamp.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeClamp
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Clamps each input component to its inclusive lower and upper bounds.

Inputs are value, minimum and maximum. Bounds must be ordered; reversed bounds retain native shader domain behavior.

## Example

```csharp
using var node = new VisualShaderNodeClamp { OpType = VisualShaderNodeClamp.OpTypeEnum.Vector3 };
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
| [`public VisualShaderNodeClamp()`](#member-e051c1b601ae) | Creates a scalar definition with its initial input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-781b014b686c) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-b287749b9000) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeClamp.OpTypeEnum OpType { get; set; }`](#member-7966852717fe) | Gets or selects the numeric input layout, converting existing defaults atomically. |

<a id="member-e051c1b601ae"></a>
### `.ctor`

```csharp
public VisualShaderNodeClamp()
```

Creates a scalar definition with its initial input defaults.

Value and lower bound are explicit zero floats; the upper bound is one.

<a id="member-781b014b686c"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-b287749b9000"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-7966852717fe"></a>
### `OpType`

```csharp
public Electron2D.VisualShaderNodeClamp.OpTypeEnum OpType { get; set; }
```

Gets or selects the numeric input layout, converting existing defaults atomically.

An unchanged selection emits no event. The complete new layout and defaults commit before Changed observers run.

Float initially. A changed selection recreates absent defaults; vector expansion repeats the last component.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selection is undefined or the count sentinel.
- `T:System.OverflowException`: An existing default cannot be represented by the selected integer type; no state changes.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graph additions borrow them; ordinary disposal guards apply. Defaults, selectors and width survive duplication and typed archives. Editing requires reimport before rendering changes.

The public VisualShader.Consumer covers typed requests, mixed/dormant defaults, copy/archives, multi-output graphs and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
