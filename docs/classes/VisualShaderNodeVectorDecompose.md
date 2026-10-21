# VisualShaderNodeVectorDecompose

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorDecompose.cs](../../src/Scene/Resources/VisualShaderNodeVectorDecompose.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVectorDecompose
```

Inherits: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

Splits a numeric vector into scalar X, Y, Z and W outputs at the selected width.

Output indices follow component order. Width changes retain stored connections; import rejects connections to outputs no longer present until the caller repairs them.

## Ports and example

Input 0 is the selected numeric vector. Outputs 0/1/2/3 are scalar X/Y/Z/W; only the selected component count exists. A stored connection to a removed output rejects import until repaired.

```csharp
using var node = new VisualShaderNodeVectorDecompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 };
node.SetInputPortDefaultValue(0, new Vector4(.25f, .5f, .75f, 1));
```

Add and connect the definition through [the shared graph author/import path](../components/visual-shaders.md).

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVectorDecompose()`](#member-76b8af1b31d4) | Creates a three-component split with one zero vector input default. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-55d62accae8a) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-76b8af1b31d4"></a>
### `.ctor`

```csharp
public VisualShaderNodeVectorDecompose()
```

Creates a three-component split with one zero vector input default.

<a id="member-55d62accae8a"></a>
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
