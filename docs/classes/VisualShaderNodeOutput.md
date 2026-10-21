# VisualShaderNodeOutput

Last updated: 2026-10-21

Source: [`VisualShaderNodeOutput.cs`](../../src/Scene/Resources/VisualShaderNodeOutput.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeOutput
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

## Description

The reserved canvas output operation owned by a visual shader stage.

Retrieve it as node zero. Its input indices follow the canvas vertex or fragment port table. The initial graph importer executes vertex position/UV/color/alpha and fragment color/alpha.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeOutput()`](#member-4df3df9d230f) | Creates an unbound fragment output definition. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-004a7f904f56) | Inherited typed Resource lifecycle implementation. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-ca715114a207) | Inherited typed Resource lifecycle implementation. |

<a id="member-4df3df9d230f"></a>
### `.ctor`

```csharp
public VisualShaderNodeOutput()
```

Creates an unbound fragment output definition.


<a id="member-004a7f904f56"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```


<a id="member-ca715114a207"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```


## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
