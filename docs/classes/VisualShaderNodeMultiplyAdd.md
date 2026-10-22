# VisualShaderNodeMultiplyAdd

Last updated: 2026-10-22

Source: [VisualShaderNodeMultiplyAdd.cs](../../src/Scene/Resources/VisualShaderNodeMultiplyAdd.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeMultiplyAdd
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Multiplies two numeric inputs and adds a third using fused shader arithmetic.

Inputs are a, b and c. Arithmetic executes in the imported shader; rounding follows the native fused operation.

## Example

This authoring snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var graph = new VisualShader();
using var node = new VisualShaderNodeMultiplyAdd { OpType = VisualShaderFloatOpType.Vector3 };
node.SetInputPortDefaultValue(0, new Vector3(.125f, .25f, .375f));
node.SetInputPortDefaultValue(1, new Vector3(2, 2, 2));
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
ResourceSaver.Save(graph, "res://Shaders/operation-source.e2dres", SaverFlags.BundleResources);
```

Import the saved source through [the canvas workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial before drawing.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeMultiplyAdd()`](#member-15f9899df2df) | Creates a scalar definition with its initial input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-056161b2db08) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-17935be6c3b9) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderFloatOpType OpType { get; set; }`](#member-e57339cca7d8) | Gets or selects the numeric input layout, converting existing defaults atomically. |

<a id="member-15f9899df2df"></a>
### `.ctor`

```csharp
public VisualShaderNodeMultiplyAdd()
```

Creates a scalar definition with its initial input defaults.

The explicit scalar defaults are zero, one and zero.

<a id="member-056161b2db08"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-17935be6c3b9"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-e57339cca7d8"></a>
### `OpType`

```csharp
public Electron2D.VisualShaderFloatOpType OpType { get; set; }
```

Gets or selects the numeric input layout, converting existing defaults atomically.

An unchanged selection emits no event. The complete new layout and defaults commit before Changed observers run.

Scalar initially. A changed selection recreates absent defaults; vector expansion repeats the last component.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selection is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers derivative layouts/functions/precisions, converted defaults, shared fused arithmetic and portable archives. The executable fixtures also observe real software-rendered pixels, affine rates and fractional antialiasing edges. See [the component contract](../components/visual-shaders.md#screen-space-derivatives) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
