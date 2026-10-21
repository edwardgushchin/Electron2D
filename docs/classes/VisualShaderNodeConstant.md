# VisualShaderNodeConstant

Last updated: 2026-10-21

Source: [`VisualShaderNodeConstant.cs`](../../src/Scene/Resources/VisualShaderNodeConstant.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeConstant
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

Inherited by: [VisualShaderNodeBooleanConstant](VisualShaderNodeBooleanConstant.md), [VisualShaderNodeColorConstant](VisualShaderNodeColorConstant.md), [VisualShaderNodeFloatConstant](VisualShaderNodeFloatConstant.md), [VisualShaderNodeIntConstant](VisualShaderNodeIntConstant.md), [VisualShaderNodeUIntConstant](VisualShaderNodeUIntConstant.md), [VisualShaderNodeVec2Constant](VisualShaderNodeVec2Constant.md), [VisualShaderNodeVec3Constant](VisualShaderNodeVec3Constant.md), [VisualShaderNodeVec4Constant](VisualShaderNodeVec4Constant.md).

## Description

A visual shader node producing immutable values for an imported program.

Changing a constant edits graph source. Reimport the graph to replace its compiled shader.

## Member summary

| Member | Contract |
| --- | --- |
| [`protected VisualShaderNodeConstant()`](#member-1daba8a5ab59) | Initializes a constant graph operation. |

<a id="member-1daba8a5ab59"></a>
### `.ctor`

```csharp
protected VisualShaderNodeConstant()
```

Initializes a constant graph operation.


## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
