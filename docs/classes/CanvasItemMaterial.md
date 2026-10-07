# CanvasItemMaterial

Last updated: 2026-10-07

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

[CanvasMaterialRenderingTests](../../tests/Electron2D.Tests/CanvasMaterialRenderingTests.cs) checks defaults, invalid values, change delivery, independent copies, live mode changes and readback of untextured and textured pixels. All five modes passed on Linux Wayland GPU/Vulkan and compatibility hardware. Dummy/software passed Mix and rejected Add before frame completion; the other non-Mix values use the same preflight. 2D light properties await their actual light-pass integrations, as recorded in [coverage](../coverage/classes/CanvasItemMaterial.md). Other platforms have no native verification for this slice.

## Particle sprite-sheet sampling

| Signature | Contract |
| --- | --- |
| `bool ParticlesAnimation { get; set; }` | Enable particle-specific sheet sampling; false initially. |
| `int ParticlesAnimHFrames { get; set; }` | Horizontal count, one initially, 1..1024. |
| `int ParticlesAnimVFrames { get; set; }` | Vertical count, one initially, 1..1024. |
| `bool ParticlesAnimLoop { get; set; }` | Wrap normalized phase rather than clamp, false initially. |

### ParticlesAnimation

Enables sheet UV selection only for recorded CPU particle quads. The full texture size still determines geometry. Replay reads the coherent settings under the material lock, so setting changes reach retained output without simulation or OnDraw. Ordinary texture/mesh commands are unaffected. Equal assignments are silent; actual changes emit Changed after commitment.

### ParticlesAnimHFrames

Positive horizontal frame count bounded to 1024. Invalid counts throw ArgumentOutOfRangeException before mutation. Together with VFrames, row-major indexing selects floor(phase * frameCount), capped to the last frame. One by default. Actual assignments commit before Changed; equal values are silent.

### ParticlesAnimVFrames

Positive vertical frame count with the same 1..1024 bounds, notification and disposal rules as HFrames. One by default. The renderer subdivides the recorded source region, including ordinary atlas handling.

### ParticlesAnimLoop

False clamps normalized phase to [0,1]; true applies positive fractional wrapping, including negative offsets. Actual writes notify after commitment; equal values are silent. Disposed resource access throws ObjectDisposedException for all four settings. Material mutation remains serialized resource work without scene thread affinity.

The five stored properties participate together in exact independent duplication and registered .e2dres/.e2dscene graphs. [CPUParticles](CPUParticles.md) consumes them on the existing GPU/compatibility canvas; its tests cover real UVs, paused live settings, fresh process and prepared rendering. LightMode remains dependent on actual light passes.
