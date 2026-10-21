# VisualShaderNodeVectorLen

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorLen.cs](../../src/Scene/Resources/VisualShaderNodeVectorLen.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVectorLen
```

Inherits: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

Computes the length of a numeric graph vector at the selected width.

Arithmetic executes in the imported shader; source edits require reimport. Numeric components do not introduce spatial scene types.

## Ports and example

Input 0 is a vector of the selected width. Output 0 is its scalar length. All selected components contribute, including W at width four.

```csharp
using var node = new VisualShaderNodeVectorLen { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 };
node.SetInputPortDefaultValue(0, new Vector4(.25f, .5f, .75f, 1));
```

Add and connect the definition through [the shared graph author/import path](../components/visual-shaders.md).

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVectorLen()`](#member-35a94affa29e) | Creates the definition with zero numeric input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-10c32eddb52e) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-35a94affa29e"></a>
### `.ctor`

```csharp
public VisualShaderNodeVectorLen()
```

Creates the definition with zero numeric input defaults.

<a id="member-10c32eddb52e"></a>
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
