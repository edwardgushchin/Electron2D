# VisualShaderNodeDerivativeFunc

Last updated: 2026-10-22

Source: [VisualShaderNodeDerivativeFunc.cs](../../src/Scene/Resources/VisualShaderNodeDerivativeFunc.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeDerivativeFunc
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Computes screen-space rates of change in an imported fragment graph.

Derivatives use neighboring fragment invocations. Their native precision and behavior at discontinuities are backend-defined. Authored input defaults are finite; graph edits require reimport.

## Example

This authoring snippet assumes an initialized project resource root.

```csharp
using Electron2D;
using var graph = new VisualShader();
using var uv = new VisualShaderNodeInput { InputName = "uv" };
using var node = new VisualShaderNodeDerivativeFunc
{
    OpType = VisualShaderFloatOpType.Vector2,
    Function = VisualShaderNodeDerivativeFunc.FunctionEnum.Sum,
    Precision = VisualShaderNodeDerivativeFunc.PrecisionEnum.Fine
};
graph.AddNode(VisualShader.Type.Fragment, uv, Vector2.Zero, 3);
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 3, 0, 2, 0);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, 0, 0);
ResourceSaver.Save(graph, "res://Shaders/operation-source.e2dres", SaverFlags.BundleResources);
```

Import the saved source through [the canvas workflow](../components/visual-shaders.md#author-import-and-execute), then load the compiled resource and assign it to ShaderMaterial before drawing.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeDerivativeFunc()`](#member-b536dc5dec77) | Creates a scalar Sum operation with default precision and an explicit zero input. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-6f30c8df9dc2) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-217dff09c941) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum Function { get; set; }`](#member-fcbb72d44f78) | Gets or selects the derivative operation. |
| [`public Electron2D.VisualShaderFloatOpType OpType { get; set; }`](#member-9f1dbc4343f5) | Gets or selects the floating component layout, converting the authored input atomically. |
| [`public Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum Precision { get; set; }`](#member-d920f391bb1d) | Gets or selects the native derivative precision. |

<a id="member-b536dc5dec77"></a>
### `.ctor`

```csharp
public VisualShaderNodeDerivativeFunc()
```

Creates a scalar Sum operation with default precision and an explicit zero input.

<a id="member-6f30c8df9dc2"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-217dff09c941"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-fcbb72d44f78"></a>
### `Function`

```csharp
public Electron2D.VisualShaderNodeDerivativeFunc.FunctionEnum Function { get; set; }
```

Gets or selects the derivative operation.

Sum initially. Repeated selection is quiet; input defaults are retained.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The function is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-9f1dbc4343f5"></a>
### `OpType`

```csharp
public Electron2D.VisualShaderFloatOpType OpType { get; set; }
```

Gets or selects the floating component layout, converting the authored input atomically.

Repeated selection is quiet. The layout and default commit before observers run.

Scalar initially. Vector expansion repeats the previous last component. A changed layout recreates a missing default as zero, or (0,0,0,1) for four components.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The layout is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

<a id="member-d920f391bb1d"></a>
### `Precision`

```csharp
public Electron2D.VisualShaderNodeDerivativeFunc.PrecisionEnum Precision { get; set; }
```

Gets or selects the native derivative precision.

None initially. Repeated selection is quiet; input defaults are retained.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The precision is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers derivative layouts/functions/precisions, converted defaults, shared fused arithmetic and portable archives. The executable fixtures also observe real software-rendered pixels, affine rates and fractional antialiasing edges. See [the component contract](../components/visual-shaders.md#screen-space-derivatives) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
