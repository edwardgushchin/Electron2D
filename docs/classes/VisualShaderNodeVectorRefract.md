# VisualShaderNodeVectorRefract

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorRefract.cs](../../src/Scene/Resources/VisualShaderNodeVectorRefract.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVectorRefract
```

Inherits: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

Refracts an incident numeric vector using a normal and scalar index ratio.

The normal and incident vectors follow native shader normalization/domain rules. Input two is a scalar ratio and retains its value or absence across width changes.

## Ports and example

Input 0 is incident, input 1 is normal, and input 2 is scalar eta. Output 0 is the selected vector. Width changes preserve eta as scalar, including explicit absence. Callers supply normalized incident and normal vectors; total internal reflection returns zero under the native refract operation.

```csharp
using var node = new VisualShaderNodeVectorRefract { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
node.SetInputPortDefaultValue(0, new Vector2(.6f, -.8f));
node.SetInputPortDefaultValue(1, new Vector2(0, 1));
node.SetInputPortDefaultValue(2, .5f);
```

Add and connect the definition through [the shared graph author/import path](../components/visual-shaders.md).

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVectorRefract()`](#member-28c58c6a30ef) | Creates the definition with zero numeric input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-592ebe01aaed) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-28c58c6a30ef"></a>
### `.ctor`

```csharp
public VisualShaderNodeVectorRefract()
```

Creates the definition with zero numeric input defaults.

<a id="member-592ebe01aaed"></a>
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
