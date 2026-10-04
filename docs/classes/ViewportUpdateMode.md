# ViewportUpdateMode

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.ViewportUpdateMode` · **Source:** [SubViewport.cs](../../src/Scene/Main/SubViewport.cs).

## Description

Selects when an offscreen canvas submits a new image.

Disabled=0 retains completed data; Once=1 submits once then becomes Disabled; WhenVisible=2 follows actual submitted canvas/material sampling; WhenParentVisible=3 follows the nearest submitted parent viewport; Always=4 submits every enabled rendering frame while attached. Once transitions happen after successful submission. Undefined values reject. Native target/resource lifetimes are independent of these policies.

Native targets belong to RenderingServer, which reuses the active device. Completed/write image pairs avoid writable-attachment feedback; producers update before consumers and cycles observe available completed data. Scene input is isolated by viewport, with explicit PushInput removing that viewport final transform. Rendering/mutation/query uses the attached owner thread. No new clock, vendor patch or backend dependency is introduced. [The offscreen component](../components/canvas-rendering.md#offscreen-canvas-targets) records actual behavior and exact gaps.

## Example

This snippet requires the indicated live window/view and an authored child hierarchy; [SubViewportTests](../../tests/Electron2D.Tests/SubViewportTests.cs) exercises this public workflow.

```csharp
view.RenderTargetUpdateMode = ViewportUpdateMode.Always;
```

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.ViewportUpdateMode Always = 4` | Update every enabled rendering frame while attached. |
| `public const Electron2D.ViewportUpdateMode Disabled = 0` | Retain the completed image without rendering. |
| `public const Electron2D.ViewportUpdateMode Once = 1` | Render the next frame, then select Disabled. |
| `public const Electron2D.ViewportUpdateMode WhenParentVisible = 3` | Update when its nearest parent viewport updates. |
| `public const Electron2D.ViewportUpdateMode WhenVisible = 2` | Update when sampled by submitted canvas geometry or a material. |

## Enumeration Descriptions

<a id="member-4f95b0d021c1"></a>
### Always

`public const Electron2D.ViewportUpdateMode Always = 4`

Update every enabled rendering frame while attached.

<a id="member-f9d1f425aa7b"></a>
### Disabled

`public const Electron2D.ViewportUpdateMode Disabled = 0`

Retain the completed image without rendering.

<a id="member-707b0e829bfd"></a>
### Once

`public const Electron2D.ViewportUpdateMode Once = 1`

Render the next frame, then select Disabled.

<a id="member-194daf05c30f"></a>
### WhenParentVisible

`public const Electron2D.ViewportUpdateMode WhenParentVisible = 3`

Update when its nearest parent viewport updates.

<a id="member-884f382958ec"></a>
### WhenVisible

`public const Electron2D.ViewportUpdateMode WhenVisible = 2`

Update when sampled by submitted canvas geometry or a material.


## Verification and dependencies

SubViewportTests verifies guards/defaults/clamps, stable RID/texture identity, observer failures, scene-local copies, input/transform isolation, owner checks and nonunit AA recording invalidation. Two Linux Wayland GPU/two compatibility Engine.Run cycles check ten pixel phases; additional hosts check dependencies, external layers, feedback, stretch, hidden root and detached cleanup. Warm 64 active and 64 idle frames measure zero owner managed bytes separately from explicit readback. GPU checks uniform-only visibility/live sampling and server RID readback. Native/driver allocation totals, other platforms/browser, human acceptance, file/editor/container and multiview/layered storage remain unverified or absent. [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0008](../decisions/scene.md#adr-0008) define rendering and node roles.
