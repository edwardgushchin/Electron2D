# VisualShaderNodeIf

Last updated: 2026-10-22

Source: [VisualShaderNodeIf.cs](../../src/Scene/Resources/VisualShaderNodeIf.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeIf
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

Selects an equal, greater or lesser three-component value from two floating graph inputs.

Inputs are a, b, tolerance, equal result, greater result and lesser result. Strict abs(a-b) < tolerance selects equal; otherwise a < b selects lesser, and the remaining cases select greater.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeIf()`](#member-56ab3e9e441d) | Creates zero operands/results and an explicit 0.00001 tolerance. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-116c3824bb2c) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-56ab3e9e441d"></a>
### `.ctor`

```csharp
public VisualShaderNodeIf()
```

Creates zero operands/results and an explicit 0.00001 tolerance.

<a id="member-116c3824bb2c"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```

## Lifecycle and verification

Definitions are cold, single-threaded Resource authoring objects. Graphs borrow them. Selectors, typed defaults and default absence survive copies and archives. Changes require reimport before rendering.

The public VisualShader.Consumer covers typed requests, dormant tolerance, condition presence, transactional operand conversion, copy/archives and real software-rendered pixels. See [the component contract](../components/visual-shaders.md#comparisons-and-conditional-results) for exact profiles, arithmetic rules and outstanding hardware/platform/native-heap gates.

## Decisions

- [Typed graph import](../decisions/rendering.md#adr-0028)
- [Enum identities](../decisions/enum-identities.md#adr-0051)
- [Typed resources](../decisions/resources.md#adr-0013)
