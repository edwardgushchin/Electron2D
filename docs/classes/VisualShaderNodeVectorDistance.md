# VisualShaderNodeVectorDistance

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorDistance.cs](../../src/Scene/Resources/VisualShaderNodeVectorDistance.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVectorDistance
```

Inherits: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

Computes distance between two numeric graph vectors at the selected width.

Arithmetic executes in the imported shader; source edits require reimport. Numeric components do not introduce spatial scene types.

## Ports and example

Inputs 0 and 1 are vectors of the selected width. Output 0 is scalar distance. All selected components contribute, including W at width four.

```csharp
using var node = new VisualShaderNodeVectorDistance { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector2 };
node.SetInputPortDefaultValue(1, new Vector2(.5f, .5f));
```

Add and connect the definition through [the shared graph author/import path](../components/visual-shaders.md).

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVectorDistance()`](#member-61de00e06dbf) | Creates the definition with zero numeric input defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-f77324f4333a) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-61de00e06dbf"></a>
### `.ctor`

```csharp
public VisualShaderNodeVectorDistance()
```

Creates the definition with zero numeric input defaults.

<a id="member-f77324f4333a"></a>
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
