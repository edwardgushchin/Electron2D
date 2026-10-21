# VisualShaderNodeDotProduct

Last updated: 2026-10-21

Source: [VisualShaderNodeDotProduct.cs](../../src/Scene/Resources/VisualShaderNodeDotProduct.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeDotProduct
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Computes the dot product of two three-component numeric graph inputs.

Arithmetic executes in the imported shader; source edits require reimport. Numeric components do not introduce spatial scene types.

## Ports and example

Inputs 0 and 1 are fixed Vector3 numeric values; output 0 is their scalar dot product. This class has no OpType property. Planar graph connections widen with a zero third component through the existing numeric conversion.

```csharp
using var node = new VisualShaderNodeDotProduct();
node.SetInputPortDefaultValue(0, new Vector3(.25f, .5f, .75f));
node.SetInputPortDefaultValue(1, new Vector3(.5f, .25f, .125f));
```

Add and connect the definition through [the shared graph author/import path](../components/visual-shaders.md).

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeDotProduct()`](#member-aeb394ff7429) | Creates the definition with zero numeric input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-060488300391) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-aeb394ff7429"></a>
### `.ctor`

```csharp
public VisualShaderNodeDotProduct()
```

Creates the definition with zero numeric input defaults.

<a id="member-060488300391"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graph additions borrow them; ordinary disposal guards apply. Defaults, selectors and width survive duplication and typed archives. Editing requires reimport before rendering changes.

The public VisualShader.Consumer covers typed requests, mixed/dormant defaults, copy/archives, multi-output graphs and real software-rendered pixels. See [the component contract](../components/visual-shaders.md) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
