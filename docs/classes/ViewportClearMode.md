# ViewportClearMode

Last updated: 2026-10-04

**Namespace:** `Electron2D` · **Declaration:** `public enum Electron2D.ViewportClearMode` · **Source:** [SubViewport.cs](../../src/Scene/Main/SubViewport.cs).

## Description

Selects how an offscreen canvas retains its previous pixels.

Always=0 clears every update; Never=1 preserves completed pixels before drawing; Once=2 clears the next successful update then becomes Never. Both backends initialize first storage deterministically. Undefined values reject. This is one semantic enum across scene/server consumers.

Native targets belong to RenderingServer, which reuses the active device. Completed/write image pairs avoid writable-attachment feedback; producers update before consumers and cycles observe available completed data. Scene input is isolated by viewport, with explicit PushInput removing that viewport final transform. Rendering/mutation/query uses the attached owner thread. No new clock, vendor patch or backend dependency is introduced. [The offscreen component](../components/canvas-rendering.md#offscreen-canvas-targets) records actual behavior and exact gaps.

## Example

This snippet requires the indicated live window/view and an authored child hierarchy; [SubViewportTests](../../tests/Electron2D.Tests/SubViewportTests.cs) exercises this public workflow.

```csharp
view.RenderTargetClearMode = ViewportClearMode.Once;
```

## Enumeration summary

| Complete C# signature | Contract |
| --- | --- |
| `public const Electron2D.ViewportClearMode Always = 0` | Clear before every update. |
| `public const Electron2D.ViewportClearMode Never = 1` | Retain completed pixels before drawing the next update. |
| `public const Electron2D.ViewportClearMode Once = 2` | Clear the next successful update, then select Never. |

## Enumeration Descriptions

<a id="member-e9e2efde3698"></a>
### Always

`public const Electron2D.ViewportClearMode Always = 0`

Clear before every update.

<a id="member-41381d5523c6"></a>
### Never

`public const Electron2D.ViewportClearMode Never = 1`

Retain completed pixels before drawing the next update.

<a id="member-18d7fdcb0e1b"></a>
### Once

`public const Electron2D.ViewportClearMode Once = 2`

Clear the next successful update, then select Never.


## Verification and dependencies

SubViewportTests verifies guards/defaults/clamps, stable RID/texture identity, observer failures, scene-local copies, input/transform isolation, owner checks and nonunit AA recording invalidation. Two Linux Wayland GPU/two compatibility Engine.Run cycles check ten pixel phases; additional hosts check dependencies, external layers, feedback, stretch, hidden root and detached cleanup. Warm 64 active and 64 idle frames measure zero owner managed bytes separately from explicit readback. GPU checks uniform-only visibility/live sampling and server RID readback. Native/driver allocation totals, other platforms/browser, human acceptance, file/editor/container and multiview/layered storage remain unverified or absent. [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0008](../decisions/scene.md#adr-0008) define rendering and node roles.
