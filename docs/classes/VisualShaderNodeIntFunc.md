# VisualShaderNodeIntFunc

Last updated: 2026-10-21

Source: [VisualShaderNodeIntFunc.cs](../../src/Scene/Resources/VisualShaderNodeIntFunc.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeIntFunc
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Applies a native signed 32-bit integer function to one graph input.

Inputs and outputs retain integer precision until connected to another numeric port role. Arithmetic executes in the imported shader; invalid native arithmetic domains retain their backend behavior.

## Example

```csharp
using var node = new VisualShaderNodeIntFunc { Function = VisualShaderNodeIntFunc.FunctionEnum.BitwiseNot };
node.SetInputPortDefaultValue(0, 0x12345678);
using var graph = new VisualShader();
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
graph.ConnectNodes(VisualShader.Type.Fragment, 2, 0, VisualShader.NodeIDOutput, 0);
ResourceSaver.Save(graph, "res://Shaders/integer-source.e2dres", SaverFlags.BundleResources);
```

Integer computation retains all bits. A numeric connection to the RGB output converts the final value to floating point; [the byte-encoding example](../components/visual-shaders.md#integer-arithmetic-and-bit-patterns) preserves individual bits in observable channels. Import the saved resource before assigning it to ShaderMaterial.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeIntFunc()`](#member-09380a5f5bfe) | Creates a signed definition with explicit zero input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-5d775a46a904) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-7f11e4337505) | Uses the inherited Resource construction or stored-property contract. |
| [`public Electron2D.VisualShaderNodeIntFunc.FunctionEnum Function { get; set; }`](#member-a298a998a3ab) | Gets or selects the signed integer function. |

<a id="member-09380a5f5bfe"></a>
### `.ctor`

```csharp
public VisualShaderNodeIntFunc()
```

Creates a signed definition with explicit zero input defaults.

<a id="member-5d775a46a904"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-7f11e4337505"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-a298a998a3ab"></a>
### `Function`

```csharp
public Electron2D.VisualShaderNodeIntFunc.FunctionEnum Function { get; set; }
```

Gets or selects the signed integer function.

An unchanged selection emits no event. The new selection commits before Changed observers execute.

Sign initially. The count sentinel is not selectable.

Exceptions:

- `T:System.ArgumentOutOfRangeException`: The selection is undefined or the count sentinel.
- `T:System.ObjectDisposedException`: The definition is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graph additions borrow them; ordinary disposal guards apply. Defaults, selectors and width survive duplication and typed archives. Editing requires reimport before rendering changes.

The public VisualShader.Consumer covers every signed/unsigned selector, exact default bits and absence, copy/archives, byte-encoded software pixels in both stages, and UV-derived integer computation. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
