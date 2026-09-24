# CanvasItemMaterial

Last updated: 2026-09-23

- Declaration: `public sealed class CanvasItemMaterial : Material`
- Source: [CanvasItemMaterial.cs](../../src/Scene/Resources/CanvasItemMaterial.cs)
- Inherits: [Material](Material.md), [Resource](Resource.md)
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Description

Selects fixed blending for all geometry and textures drawn by a CanvasItem. Multiple nodes may borrow one material. Setting its mode affects the next frame without rerecording drawing commands. Disposal belongs to the resource owner; disposing a node does not dispose the material. A disposed assigned material fails rendering explicitly.

The resource does not contain a shader or native handle. GPU pipelines are cached by shader code and blend mode. The compatibility backend uses SDL blend operations for each geometry or texture batch. The SDL software driver correctly executes only Mix; other modes throw `NotSupportedException` before drawing. This backend limit does not change the stored mode.

## Example

```csharp
using var material = new CanvasItemMaterial
{
    BlendMode = BlendMode.Add
};
sprite.Material = material;
```

The caller keeps `material` alive while the sprite uses it.

## API summary

| Declaration | Contract |
| --- | --- |
| `CanvasItemMaterial()` | Creates a material in Mix mode. |
| [`BlendMode`](BlendMode.md) | Five numeric blend identities. |
| `BlendMode BlendMode { get; set; }` | Reads or changes the mode used by later frames. |

## Property descriptions

### BlendMode

Defaults to Mix. Every successful assignment commits under the material gate and synchronously emits Resource.Changed outside it, including an assignment of the same value. Values outside 0–4 throw `ArgumentOutOfRangeException` without changing state or emitting Changed. Disposal throws `ObjectDisposedException`. Material state is read during frame capture on the scene owner thread; resource mutation itself has no scene thread affinity.

The stored mode participates in `Duplicate`, `DuplicateDeep`, `CopyFromResource`, typed property discovery and scene-local copying. Duplicates hold independent mode values. The inherited Material state has no public shader priority or next-pass chain.

## Verification and limits

[CanvasMaterialRenderingTests](../../tests/Electron2D.Tests/CanvasMaterialRenderingTests.cs) checks defaults, invalid values, change delivery, independent copies, live mode changes and readback of untextured and textured pixels. All five modes passed on Linux Wayland GPU/Vulkan and compatibility hardware. Dummy/software passed Mix and rejected Add before frame completion; the other non-Mix values use the same preflight. 2D light and particle animation properties await their actual renderer/simulation integrations, as recorded in [coverage](../coverage/classes/CanvasItemMaterial.md). Other platforms have no native verification for this slice.
