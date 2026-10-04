# BackBufferCopyMode

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum BackBufferCopyMode`.

**Source:** [BackBufferCopy.cs](../../src/Scene/2D/BackBufferCopy.cs).

## Description

Exact numeric copy policy: Disabled=0 leaves the current snapshot, Rect=1 copies transformed/clipped local bounds, and Viewport=2 copies the entire canvas. Only the zero-valued rectangle is a full-copy sentinel in Rect mode. Invalid enumeration values reject before mutation.

The values are stored through the copy node's typed descriptor; invalid node policy writes reject before mutation.

## Example

Requires a BackBufferCopy variable named copy. CanvasCompositionTests checks the policy in detached configuration and live native hosts.

```csharp
copy.CopyMode = BackBufferCopyMode.Viewport;
```

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.BackBufferCopyMode Disabled = 0` | Leave the current screen snapshot unchanged. |
| `public const Electron2D.BackBufferCopyMode Rect = 1` | Copy the transformed local rectangle, clipped to the viewport. |
| `public const Electron2D.BackBufferCopyMode Viewport = 2` | Copy the complete viewport image. |

## Enumeration Descriptions

<a id="member-226504e3a426"></a>
### Disabled

`public const Electron2D.BackBufferCopyMode Disabled = 0`

Leave the current screen snapshot unchanged.

<a id="member-1b78d9b0faf0"></a>
### Rect

`public const Electron2D.BackBufferCopyMode Rect = 1`

Copy the transformed local rectangle, clipped to the viewport.

<a id="member-0d36d5e7bd4c"></a>
### Viewport

`public const Electron2D.BackBufferCopyMode Viewport = 2`

Copy the complete viewport image.


## Verification and limits

[CanvasCompositionTests](../../tests/Electron2D.Tests/CanvasCompositionTests.cs) checks authoring, callbacks, typed packing/owner boundaries and actual native pixels. Linux Wayland GPU and hardware compatibility execute the baseline; GPU HLSL/GLSL additionally check custom screen reading and generated LOD. Stable-size warm active/idle intervals measure managed allocation; native allocator totals, other platforms, large scenes, editor inspector integration and human acceptance remain separate.
