# TextureRect

Last updated: 2026-10-01

**Declaration:** `public class TextureRect : Control` · **Source:** [TextureRect.cs](../../src/Scene/GUI/TextureRect.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md#texture-rectangles)

**Inherits:** [Control](Control.md), [CanvasItem](CanvasItem.md), [Node](Node.md), [ElectronObject](ElectronObject.md). **Inherited By:** no shipped subclass.

## Description

Displays one borrowed Texture in the ordinary retained canvas, with six intrinsic-minimum policies and seven placement modes. Transform, modulation, clipping, material, sampling, visibility and pointer routing follow Control/CanvasItem. MouseFilter defaults Pass. Reflection changes pixels inside the occupied rectangle, without moving its origin or changing minimum size. This control needs no font, theme asset, or additional backend.

All attached queries and mutations require the scene owner thread. Mutations reject scene capture and disposal. Texture resources remain caller-owned. Assignment commits before redraw/minimum/warning requests; equal references and equal enum/flip writes are silent. Direct disposal of the assigned texture clears the binding on the owner thread. A disposed underlying atlas source remains an invalid borrowed dependency and fails when consumed. The control releases subscriptions on replacement/disposal and never disposes a texture.

Resource changes request redraw, minimum-size recomputation and configuration-warning refresh. Owner-thread deferred work and internal frame polling also inspect the full nested-atlas revision/disposal chain, recovering changes when an earlier observer throws or forwarding is missed. Detached minimum/draw queries read current data. Entry creates a membership-specific deferred callback; exit invalidates old queued work. Minimum updates use the existing Control coalescing. Internal processing does not enable the public Process event.

Texture size callbacks may change the control. Queries/drawing retry against the new generation, texture and control size, with a 64-attempt bound; nonsettling callbacks fail with InvalidOperationException. Nonfinite/negative texture dimensions and overflowing pixel fits fail explicitly. Empty texture dimensions produce no drawing and proportional minima avoid division by zero.

Fit modes use the current rectangle and can feed back into container layout. In a FlowContainer with multiple wraps, they retain the current child size before ordinary Container fitting; positions still follow alignment, RTL and reverse fill. Single-wrap layout resumes ordinary sizing. This preserves the bounded stabilization rule, rather than promising an optimal solution for cyclic size constraints.

## Example

Partial snippet; the host owns `texture` and attaches `picture` to its scene:

```csharp
var picture = new TextureRect
{
    Texture = texture,
    ExpandMode = TextureRectExpandMode.IgnoreSize,
    StretchMode = TextureRectStretchMode.KeepAspectCentered,
    Size = new Vector2(160, 100)
};
```

## API summary

| Signature | Default/contract |
| --- | --- |
| `public TextureRect()` | Null texture, KeepSize, Scale, false flips, MouseFilter.Pass. |
| [`public Texture? Texture { get; set; }`](#texture) | Borrowed image, initially null. |
| [`public TextureRectExpandMode ExpandMode { get; set; }`](#expandmode) | KeepSize initially. |
| [`public TextureRectStretchMode StretchMode { get; set; }`](#stretchmode) | Scale initially. |
| [`public bool FlipH { get; set; }`](#fliph) | False initially. |
| [`public bool FlipV { get; set; }`](#flipv) | False initially. |
| [`public override string[] GetConfigurationWarnings()`](#getconfigurationwarnings) | Adds one warning for Tile with a nonzero margin in any atlas ancestor. |
| `protected override Vector2 OnGetMinimumSize()` | Current texture/control-derived minimum. |
| `protected override void OnNotification(int what)` | Entry/exit, internal polling, resize invalidation and retained drawing. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stored image, modes, flips and inherited Pass pointer default. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Exact TextureRect identity; custom subclasses retain the base factory rules. |
| `protected override void Dispose(bool disposing)` | Unsubscribes borrowed resources and clears tracking. |

## Property descriptions

### Texture

`public Texture? Texture { get; set; }`

Null means no geometry and zero intrinsic minimum. Assigning a disposed resource throws ObjectDisposedException before changing the binding. Invalid attached/capture access throws InvalidOperationException. Texture, ImageTexture, AtlasTexture and custom Texture drawing overrides use the same borrowed contract. Resource and warning callbacks can throw after state commits. PackedScene stores the reference through ordinary resource-graph policy: external resources stay shared and scene-local resources duplicate normally.

### ExpandMode

`public TextureRectExpandMode ExpandMode { get; set; }`

See [TextureRectExpandMode](TextureRectExpandMode.md). KeepSize uses natural logical dimensions; IgnoreSize contributes zero. FitWidth derives minimum width from current height; FitHeight transposes this. Proportional variants multiply by the natural aspect ratio. Control still combines custom minimums, zero and effective maximums. Undefined values throw ArgumentOutOfRangeException before mutation. Changes request redraw and minimum refresh.

### StretchMode

`public TextureRectStretchMode StretchMode { get; set; }`

See [TextureRectStretchMode](TextureRectStretchMode.md). Scale fills the rectangle. Tile repeats natural logical pixels; nested AtlasTexture tiling reuses retained zero-border nine-patch geometry. Atlas margins are unsupported for Tile and generate a warning; drawing follows the existing atlas clipping path. Keep modes use natural size, potentially extending beyond the control unless inherited clipping applies. Aspect fit truncates width/height to integer pixels before optional centering; offsets may remain fractional. Covered mode uses a centered logical source crop. Undefined values reject before mutation; changes redraw and refresh warnings.

### FlipH

`public bool FlipH { get; set; }`

Reflects horizontally inside the selected destination, including aspect crop and tiled atlas output. Requests redraw, preserving size, source ownership and minimum. False initially.

### FlipV

`public bool FlipV { get; set; }`

Vertical equivalent, independently combinable with FlipH. False initially.

## Method descriptions

### GetConfigurationWarnings

`public override string[] GetConfigurationWarnings()`

Returns inherited warnings plus one message when Tile is selected and any nested atlas has a nonzero Margin. The caller owns the array. It requires a live control and owner-thread access. It does not imply an editor UI; Node.UpdateConfigurationWarnings uses the existing selected-scene notification service.

### Extension hooks

OnGetMinimumSize supplies intrinsic geometry. OnNotification always attempts base and owned work, collecting callback failures; resize requests minimum recomputation. The descriptor hook replaces only the inherited MouseFilter default with Pass. The factory hook reconstructs this concrete type. Dispose clears borrowed tracking before inherited teardown. These protected overrides retain their inherited call requirements.

## Verification and limits

[TextureRectTests](../../tests/Electron2D.Tests/TextureRectTests.cs) covers defaults, enum rollback, all six minimum policies, empty/nonfinite/negative geometry, owner/disposal guards, worker changes/disposal, missed nested-atlas notifications, callback reentry/nonsettlement, exact PackedScene storage, all four fit modes across 32 orientation/RTL/reverse flow profiles, seven recorded modes and warmed reflected recording/polling with zero managed bytes.

[TextureRectRenderingTests](../../tests/Electron2D.Tests/TextureRectRenderingTests.cs) checks 13 pixel states: seven placements, independent/joint reflections, nested atlas tiling and null binding. After 20 warmup frames, 64 active resize/flip frames and 64 idle frames measure zero managed bytes from ProcessFrameStarted to FramePostDraw on Linux Wayland GPU/compatibility and dummy/software. Software truncates half-texel crop UVs before interpolation under the existing [fallback precision limit](CompatibilityCanvasBackend.md#verification-and-limits); the cropped 4×2 fixture therefore samples a different color distribution, asserted separately from hardware. Native allocations, other platforms, dense atlas tile performance and owner acceptance are unverified. CPU nine-patch tiling retains its existing geometry limits. Applicable own declarations are implemented; inherited gaps retain their declaring coverage rows under [ADR 0028](../decisions/rendering.md#adr-0028) and [ADR 0081](../decisions/rendering.md#adr-0081).

## Internal state

Private SourceState holds a borrowed Texture, revision and disposal snapshot. The reusable list stores the current atlas chain only. Generation/pending/membership values guard callbacks and retries; none transfers resource ownership.
