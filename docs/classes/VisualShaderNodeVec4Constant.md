# VisualShaderNodeVec4Constant

Last updated: 2026-10-21

Source: [`VisualShaderNodeVec4Constant.cs`](../../src/Scene/Resources/VisualShaderNodeVec4Constant.cs).

```csharp
public sealed class Electron2D.VisualShaderNodeVec4Constant
```

Inherits: [VisualShaderNodeConstant](VisualShaderNodeConstant.md).

## Description

Produces a copied Vector4 constant in an imported visual shader.

## Example

```csharp
using var node = new VisualShaderNodeVec4Constant { Constant = new Vector4(.25f, .5f, .75f, 1) };
graph.AddNode(VisualShader.Type.Fragment, node, Vector2.Zero, 2);
```

The surrounding graph is supplied by the caller; save and import it before drawing. Approximately equal floating constants and equal integer/Boolean constants preserve the stored value and emit no Changed event. Invalid/nonfinite requests fail before publication.

## Member summary

| Member | Contract |
| --- | --- |
| [`public VisualShaderNodeVec4Constant()`](#member-31d8ff4012ed) | Creates the node with its initial constant. |
| [`protected override Electron2D.Resource CreateDuplicateInstance()`](#member-7a3c5bca38aa) | Inherited typed Resource lifecycle implementation. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-eb1cb2f48d43) | Inherited typed Resource lifecycle implementation. |
| [`public Electron2D.Vector4 Constant { get; set; }`](#member-3565742160c4) | Gets or replaces the authored constant. |

<a id="member-31d8ff4012ed"></a>
### `.ctor`

```csharp
public VisualShaderNodeVec4Constant()
```

Creates the node with its initial constant.


<a id="member-7a3c5bca38aa"></a>
### `CreateDuplicateInstance`

```csharp
protected override Electron2D.Resource CreateDuplicateInstance()
```


<a id="member-eb1cb2f48d43"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```


<a id="member-3565742160c4"></a>
### `Constant`

```csharp
public Electron2D.Vector4 Constant { get; set; }
```

Gets or replaces the authored constant.

Initially (0, 0, 0, 1); numeric floating components must be finite.

- `System.ArgumentException`: A numeric component is nonfinite.
- `System.ObjectDisposedException`: The node is disposed.

## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
