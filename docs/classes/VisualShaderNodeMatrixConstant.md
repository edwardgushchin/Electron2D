# VisualShaderNodeMatrixConstant

Last updated: 2026-10-22

Source: [VisualShaderNodeMatrixConstant.cs](../../src/Scene/Resources/VisualShaderNodeMatrixConstant.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeMatrixConstant
```

Inherits: [VisualShaderNodeConstant](VisualShaderNodeConstant.md).

## Description

Produces a copied four-by-four numeric matrix constant in an imported visual shader.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeMatrixConstant()`](#member-1fb21381d241) | Creates the node with its initial constant. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-176e123dcd12) | Uses the inherited Resource construction or stored-property contract. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-a73dc83b0ffa) | Uses the inherited Resource construction or stored-property contract. |
| [`public System.Numerics.Matrix4x4 Constant { get; set; }`](#member-c29d82caeb5b) | Gets or replaces the authored constant. |

<a id="member-1fb21381d241"></a>
### `.ctor`

```csharp
public VisualShaderNodeMatrixConstant()
```

Creates the node with its initial constant.

<a id="member-176e123dcd12"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

<a id="member-a73dc83b0ffa"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```

<a id="member-c29d82caeb5b"></a>
### `Constant`

```csharp
public System.Numerics.Matrix4x4 Constant { get; set; }
```

Gets or replaces the authored constant.

Initially identity; all sixteen numeric components must be finite.

Exceptions:

- `T:System.ArgumentException`: A numeric component is nonfinite.
- `T:System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed requests, all sixteen matrix components, copy/archive state, row/column layout and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
