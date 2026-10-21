# VisualShaderNodeMatrixCompose

Last updated: 2026-10-22

Source: [VisualShaderNodeMatrixCompose.cs](../../src/Scene/Resources/VisualShaderNodeMatrixCompose.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeMatrixCompose
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Composes three numeric columns and one final column into an affine four-by-four matrix.

Inputs and results are numeric shader data. Editing requires reimport before drawing changes.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeMatrixCompose()`](#member-3058b9c89f74) | Creates the definition with four explicit zero vector defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-ce8edca69019) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-3058b9c89f74"></a>
### `.ctor`

```csharp
public VisualShaderNodeMatrixCompose()
```

Creates the definition with four explicit zero vector defaults.

<a id="member-ce8edca69019"></a>
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
