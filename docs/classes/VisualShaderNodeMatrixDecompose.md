# VisualShaderNodeMatrixDecompose

Last updated: 2026-10-22

Source: [VisualShaderNodeMatrixDecompose.cs](../../src/Scene/Resources/VisualShaderNodeMatrixDecompose.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeMatrixDecompose
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Returns xyz from each of the four numeric matrix columns.

Inputs and results are numeric shader data. Editing requires reimport before drawing changes.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeMatrixDecompose()`](#member-81311548c9a3) | Creates the definition with an explicit identity matrix default. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-7df987c77fc7) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-81311548c9a3"></a>
### `.ctor`

```csharp
public VisualShaderNodeMatrixDecompose()
```

Creates the definition with an explicit identity matrix default.

<a id="member-7df987c77fc7"></a>
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
