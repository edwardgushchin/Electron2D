# VisualShaderNodeVectorBase

Last updated: 2026-10-21

Source: [`VisualShaderNodeVectorBase.cs`](../../src/Scene/Resources/VisualShaderNodeVectorBase.cs).

```csharp
public abstract class Electron2D.VisualShaderNodeVectorBase
```

Inherits: [VisualShaderNode](VisualShaderNode.md).

Inherited by: [VisualShaderNodeVectorOp](VisualShaderNodeVectorOp.md).

## Description

A numeric vector graph operation with a selectable component count.

These are numeric shader components, independent of the engine's planar scene domain.

## Member summary

| Member | Contract |
| --- | --- |
| [`protected VisualShaderNodeVectorBase()`](#member-d570ea6991a5) | Initializes a three-component numeric operation. |
| [`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`](#member-1aa11052a63e) | Inherited typed Resource lifecycle implementation. |
| [`public Electron2D.VisualShaderNodeVectorBase.OpType OperationType { get; set; }`](#member-09159a03eb03) | Gets or selects the component count, converting existing input defaults. |

<a id="member-d570ea6991a5"></a>
### `.ctor`

```csharp
protected VisualShaderNodeVectorBase()
```

Initializes a three-component numeric operation.


<a id="member-1aa11052a63e"></a>
### `GetPropertyDescriptors`

```csharp
protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()
```


<a id="member-09159a03eb03"></a>
### `OperationType`

```csharp
public Electron2D.VisualShaderNodeVectorBase.OpType OperationType { get; set; }
```

Gets or selects the component count, converting existing input defaults.

An unchanged selection emits no event. Changing width recreates removed defaults with zero vectors, or (0,0,0,1) for four components. Cross product requires three components.

Vector3 initially; expanding a default repeats its last previous component.

- `System.ArgumentOutOfRangeException`: The component-count selection is undefined.
- `System.NotSupportedException`: The selected cross-product width is not three.
- `System.ObjectDisposedException`: The operation is disposed.

## Lifecycle and verification

Graph authoring, snapshots, import and file I/O are allocating cold operations. Definition resources are single-threaded authoring objects. Access after disposal rejects ordinary reads/writes; graph disposal detaches borrowed definitions and retires its owned outputs/replacements. Immutable connection/default entries have value lifetime and are not disposable.

`tests/VisualShader.Consumer` exercises ownership, invalid requests, snapshots, shallow/deep copying, fresh archives and native graph execution. Fifty-seven programs cover constants, scalar/vector arithmetic, width conversion, UVs and authored vertex movement on Linux software Wayland/X11/HDR/MSAA. Prepared active/idle replay is checked separately from cold generation. See [the component contract](../components/visual-shaders.md) for exact coverage and limits.

## Decisions

- [Typed shader authoring and import](../decisions/rendering.md#adr-0028)
- [Typed resource lifetime and archives](../decisions/resources.md#adr-0013)
