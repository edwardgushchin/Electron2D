# VisualShaderNodeVectorCompose

Last updated: 2026-10-21

Source: [VisualShaderNodeVectorCompose.cs](../../src/Scene/Resources/VisualShaderNodeVectorCompose.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVectorCompose
```

Inherits: [VisualShaderNodeVectorBase](VisualShaderNodeVectorBase.md).

## Description

Assembles two, three or four scalar graph inputs into a numeric vector.

Default slots zero through three remain available while inactive, preserving authored components across narrowing, duplication and archives. A width change recreates missing active defaults as zero. Only the selected components accept graph connections.

## Ports and example

Inputs 0/1/2/3 are scalar X/Y/Z/W; only the selected width accepts connections. Default slots 0 through 3 remain valid while inactive. Output 0 is the selected numeric vector.

```csharp
using var node = new VisualShaderNodeVectorCompose { OpType = VisualShaderNodeVectorBase.OpTypeEnum.Vector4 };
node.SetInputPortDefaultValue(0, .25f);
node.SetInputPortDefaultValue(3, .5f);
```

Add and connect the definition through [the shared graph author/import path](../components/visual-shaders.md).

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVectorCompose()`](#member-479aaf7c8e7f) | Creates a three-component definition with three explicit zero scalar defaults. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-775523e58961) | Uses the inherited Resource construction or stored-property contract. |

<a id="member-479aaf7c8e7f"></a>
### `.ctor`

```csharp
public VisualShaderNodeVectorCompose()
```

Creates a three-component definition with three explicit zero scalar defaults.

<a id="member-775523e58961"></a>
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
