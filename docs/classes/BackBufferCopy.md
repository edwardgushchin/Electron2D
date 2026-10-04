# BackBufferCopy

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class BackBufferCopy : Entity`.

**Source:** [BackBufferCopy.cs](../../src/Scene/2D/BackBufferCopy.cs).

**Inherits:** [Entity](Entity.md).

## Description

Copies the existing canvas image at its normal Z/order position for later SCREEN_TEXTURE materials. CopyMode defaults to Rect and Rect to -100,-100,200,200. Stored signed local dimensions remain unchanged; rendering transforms bounds and clips them to viewport/ancestor rectangles. The zero-valued Rect is the full-copy sentinel. Viewport copies the full canvas; Disabled/hidden/masked nodes leave the snapshot unchanged, and in-group copies are ignored. Samples outside a copied region are unspecified. Every valid CopyMode assignment commits then notifies the property list, including equality; every Rect write commits and emits ItemRectChanged, including equality. Callback failure does not roll back state. The first ordinary screen-reading draw automatically snapshots if no explicit copy supplied data; later draws reuse the current snapshot. The node supplies Entity placement rather than Control anchors. Scene factories/descriptors store both policies; native storage belongs to the viewport target, not this node. GPU HLSL/GLSL consume the screen binding; compatibility performs copies but rejects arbitrary shaders. Editor inspector Rect hiding and alpha-mask clipping have separate prerequisites.

Attached reads and mutations use the scene owner; mutation also rejects scene capture. Disposed access rejects. In-memory PackedScene reconstruction has executable checks; file/editor authoring and owner visual acceptance are separate. [The composition component](../components/canvas-rendering.md#group-composition-and-screen-snapshots) records native lifetime, backend and allocation limits.

## Example

Requires the indicated authored drawable/material objects and a live Window. CanvasCompositionTests compiles and executes this public workflow.

```csharp
var copy = new BackBufferCopy
{
    CopyMode = BackBufferCopyMode.Rect,
    Rect = new Rect2(0, 0, 128, 128),
};
window.AddChild(background);
window.AddChild(copy);
window.AddChild(screenReadingDrawable);
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public BackBufferCopy()` | Creates a rectangular copy of the local -100,-100,200,200 region. |

## Constructor Descriptions

<a id="member-a441c883bc49"></a>
### .ctor

`public BackBufferCopy()`

Creates a rectangular copy of the local -100,-100,200,200 region.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.BackBufferCopyMode CopyMode { get; set; }` | Gets or sets the active buffering policy. |
| `public Electron2D.Rect2 Rect { get; set; }` | Gets or sets the finite local rectangle used by Rect mode. |

## Property Descriptions

<a id="member-62d2efb534e5"></a>
### CopyMode

`public Electron2D.BackBufferCopyMode CopyMode { get; set; }`

Gets or sets the active buffering policy.

Value: Rect initially.

Remarks: Every valid assignment commits before notifying the property list, including equal assignments.

System.ArgumentOutOfRangeException: The enumeration is undefined.

System.InvalidOperationException: Mutation is off-owner or during scene capture.

System.ObjectDisposedException: This node is disposed.

System.Exception: A property-list subscriber fails after commitment.

<a id="member-b8c9f1fe209c"></a>
### Rect

`public Electron2D.Rect2 Rect { get; set; }`

Gets or sets the finite local rectangle used by Rect mode.

Value: -100,-100,200,200 initially; signed dimensions are retained.

Remarks: Every assignment commits and raises ItemRectChanged. The zero-valued rectangle uses a complete viewport copy, matching the full-copy sentinel. Samples outside a nonempty copied region are unspecified.

System.ArgumentException: The rectangle is nonfinite.

System.InvalidOperationException: Mutation is off-owner or during scene capture.

System.ObjectDisposedException: This node is disposed.

System.Exception: An ItemRectChanged subscriber fails after commitment.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()` | Creates a reusable factory for packed-scene instances of this exact runtime node type. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |

## Method Descriptions

<a id="member-cb29bc998f22"></a>
### CreateSceneInstanceFactory

`protected override System.Func<Electron2D.Node> CreateSceneInstanceFactory()`

Creates a reusable factory for packed-scene instances of this exact runtime node type.

Returns: A non-null factory that creates a fresh node of the exact same runtime type.

Remarks: The base implementation supports only an exact Electron2D.Node. Derived node types that can be packed must return a static, non-capturing factory that remains valid after the source node is disposed and creates a live, detached, parentless, childless, unowned, and non-queued instance. Stored writable property descriptors restore the instance state.

System.NotSupportedException: A derived node has not explicitly supplied an instancing factory.

<a id="member-f23c6c0e7042"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.


## Verification and limits

[CanvasCompositionTests](../../tests/Electron2D.Tests/CanvasCompositionTests.cs) checks authoring, callbacks, typed packing/owner boundaries and actual native pixels. Linux Wayland GPU and hardware compatibility execute the baseline; GPU HLSL/GLSL additionally check custom screen reading and generated LOD. Stable-size warm active/idle intervals measure managed allocation; native allocator totals, other platforms, large scenes, editor inspector integration and human acceptance remain separate.
