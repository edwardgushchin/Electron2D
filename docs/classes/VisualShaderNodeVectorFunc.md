# VisualShaderNodeVectorFunc

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorFunc.cs](../../src/Scene/Resources/VisualShaderNodeVectorFunc.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVectorFunc
```

Inherits: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

Applies one unary vector function in an imported canvas shader graph.

Input constants/defaults must be finite; computed results retain native shader domain, NaN/infinity and rounding rules. Graph authoring is cold and requires reimport before drawing changes.

## Example

```csharp
using var node = new VisualShaderNodeVectorFunc { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2, Function = VisualShaderNodeVectorFunc.FunctionEnum.Normalize };
node.SetInputPortDefaultValue(0, new Vector2(.25f, .75f));
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/function-source.e2dres", SaverFlags.BundleResources);
```

Import this source resource through [the existing graph pipeline](../components/visual-shaders.md) before assigning the compiled result to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVectorFunc()`](#member-c94bc61a5c0a) | Creates a normalize function with a zero vector default. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-9fa9366be565) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-b9d45702f7ff) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Function { get; set; }`](#member-d2f0214683f7) | Gets or selects the unary function. |

<a id="member-c94bc61a5c0a"></a>
### `.ctor`

```csharp
public VisualShaderNodeVectorFunc()
```

Creates a normalize function with a zero vector default.

<a id="member-9fa9366be565"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-b9d45702f7ff"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-d2f0214683f7"></a>
### `Function`

```csharp
public Electron2D.VisualShaderNodeVectorFunc.FunctionEnum Function { get; set; }
```

Gets or selects the unary function.

An unchanged selection emits no Changed event.

Normalize initially; Max is nonselectable.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The function is undefined or the Max sentinel.
- `T:System.ObjectDisposedException`: This definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graph additions borrow them; ordinary disposal guards apply. Defaults, selectors and width survive duplication and typed archives. Editing requires reimport before rendering changes.

The public VisualShader.Consumer covers typed requests, copy/archives, every scalar/vector function and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
