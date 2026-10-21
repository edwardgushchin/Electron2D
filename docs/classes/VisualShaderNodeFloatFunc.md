# VisualShaderNodeFloatFunc

Last updated: 2026-10-21

Source: [VisualShaderNodeFloatFunc.cs](../../src/Scene/Resources/VisualShaderNodeFloatFunc.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeFloatFunc
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Applies one unary scalar function in an imported canvas shader graph.

Input constants/defaults must be finite; computed results retain native shader domain, NaN/infinity and rounding rules. Graph authoring is cold and requires reimport before drawing changes.

## Example

```csharp
using var node = new VisualShaderNodeFloatFunc { Function = VisualShaderNodeFloatFunc.FunctionEnum.Sin };
node.SetInputPortDefaultValue(0, .5f);
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/function-source.e2dres", SaverFlags.BundleResources);
```

Import this source resource through [the existing graph pipeline](../components/visual-shaders.md) before assigning the compiled result to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeFloatFunc()`](#member-7a84fb0cf4ec) | Creates a sin function with a zero scalar default. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-a7b095064cd5) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-d32741a16984) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Function { get; set; }`](#member-7be5c2cf1a7e) | Gets or selects the unary function. |

<a id="member-7a84fb0cf4ec"></a>
### `.ctor`

```csharp
public VisualShaderNodeFloatFunc()
```

Creates a sin function with a zero scalar default.

<a id="member-a7b095064cd5"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-d32741a16984"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-7be5c2cf1a7e"></a>
### `Function`

```csharp
public Electron2D.VisualShaderNodeFloatFunc.FunctionEnum Function { get; set; }
```

Gets or selects the unary function.

An unchanged selection emits no Changed event.

Sin initially; Max is nonselectable.

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
