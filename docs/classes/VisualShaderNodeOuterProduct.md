# VisualShaderNodeOuterProduct

Last updated: 2026-10-22

Source: [VisualShaderNodeOuterProduct.cs](../../src/Scene/Resources/VisualShaderNodeOuterProduct.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeOuterProduct
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Computes a four-by-four outer product of two (x,y,z,0) numeric vectors.

Inputs and results are numeric shader data. Editing requires reimport before drawing changes.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeOuterProduct()`](#member-a9a8c31a5315) | Creates the definition with two explicit zero vector defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-d46510062f0d) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-a9a8c31a5315"></a>
### `.ctor`

```csharp
public VisualShaderNodeOuterProduct()
```

Creates the definition with two explicit zero vector defaults.

<a id="member-d46510062f0d"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed requests, all sixteen matrix components, copy/archive state, row/column layout and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
