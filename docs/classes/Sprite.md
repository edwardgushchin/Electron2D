# Sprite

Last updated: 2026-09-23

- Declaration: `public class Sprite : Entity`
- Source: [Sprite.cs](../../src/Scene/2D/Sprite.cs)
- Inherits: [Entity](Entity.md)
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Description

Displays a borrowed [Texture](Texture.md), a selected sheet frame or a rectangular texture region. It inherits hierarchy/lifecycle from Node, drawing/visibility/Z/modulation/materials from CanvasItem, and spatial transforms from Entity. Engine.Run renders its retained commands through the active canvas backend. Sprite does not own a timer: change Frame directly, through a Tween or from scene processing.

The node owns its subscription to Texture.Changed and releases that subscription on replacement/disposal. It never disposes an ordinary borrowed texture. PackedScene separately owns any resource it duplicates for an instance through ResourceLocalToScene. Texture content changes request redraw without emitting TextureChanged; replacing the reference emits TextureChanged once. A resource notification from a worker thread only atomically requests redraw; drawing stays on the scene owner thread.

All properties and queries reject a disposed Sprite. Setters also reject scene capture and mutation off the attached tree's owner thread. Detached nodes require caller coordination. Synchronous event handlers observe committed state; a throwing handler stops later subscribers and propagates, leaving the new value and pending redraw intact. Resource.Changed retains its own documented delivery behavior, including failures in earlier subscribers.

## Example

This fragment uses a caller-owned texture and runs inside ordinary scene construction:

```csharp
var sprite = new Sprite
{
    Texture = texture,
    HFrames = 4,
    VFrames = 2,
    FrameCoords = new Vector2I(1, 0),
    Position = new Vector2(120, 80),
};
window.AddChild(sprite);
```

The texture must outlive its use by the scene. `Engine.Instance.Run(window)` owns the supplied window hierarchy; it does not own this ordinary borrowed texture.

## Constructor

| Declaration | Contract |
| --- | --- |
| `public Sprite()` | Detached, centered, no texture, one column/row, frame zero. |

## Properties

| Declaration | Default | Description |
| --- | --- | --- |
| `public Texture? Texture { get; set; }` | null | [Texture](#texture) |
| `public bool Centered { get; set; }` | true | [Centered](#centered) |
| `public Vector2 Offset { get; set; }` | Zero | [Offset](#offset) |
| `public bool FlipH { get; set; }` | false | [Flips](#fliph-and-flipv) |
| `public bool FlipV { get; set; }` | false | [Flips](#fliph-and-flipv) |
| `public bool RegionEnabled { get; set; }` | false | [Region](#regionenabled-and-regionrect) |
| `public Rect RegionRect { get; set; }` | zero rectangle | [Region](#regionenabled-and-regionrect) |
| `public bool RegionFilterClipEnabled { get; set; }` | false | [Clipping](#regionfilterclipenabled) |
| `public int HFrames { get; set; }` | 1 | [Grid](#hframes-and-vframes) |
| `public int VFrames { get; set; }` | 1 | [Grid](#hframes-and-vframes) |
| `public int Frame { get; set; }` | 0 | [Frame](#frame) |
| `public Vector2I FrameCoords { get; set; }` | Zero | [Frame coordinates](#framecoords) |

## Methods and events

| Declaration | Description |
| --- | --- |
| `public Rect GetRect()` | [Inspection bounds](#getrect) |
| `public bool IsPixelOpaque(Vector2 position)` | [Source opacity](#ispixelopaque) |
| `public event Action? FrameChanged` | [Frame event](#framechanged) |
| `public event Action? TextureChanged` | [Texture event](#texturechanged) |

## Property descriptions

### Texture

Assigns a live texture or null. Assigning the same instance is a no-op. A disposed replacement throws ObjectDisposedException before changing state. The old subscription is removed before publishing the new reference. A content change requests redraw, so changes in logical size also rebuild geometry. Null clears the retained image on the next visible frame. Disposal of a still-borrowed texture is an error when rendering or reading its data.

### Centered

Subtracts half the selected frame size from Offset when true. Changing it requests redraw. Fractional centers are retained; no implicit pixel snapping is performed.

### Offset

Finite local coordinates, independent of Entity.Position. Positive Y is down. Invalid values throw ArgumentException without mutation.

### FlipH and FlipV

Reverse image sampling on the selected axes while retaining the destination origin. These do not change GetRect. Negative source region sizes also participate in the texture drawing flip convention.

### RegionEnabled and RegionRect

When disabled, the sheet area is the complete texture's logical size. When enabled, it is RegionRect. Frame grid division applies within this area. RegionRect must be finite; invalid values throw ArgumentException. Regions may extend beyond the texture, with edge clamping. A zero-area region draws nothing. Signed region sizes follow Texture.DrawRectRegion; they are not automatically normalized. A disabled region may be configured without redrawing; enabling it requests redraw.

### RegionFilterClipEnabled

When RegionEnabled is true, limits UV sampling to texel centers in the selected source frame. Otherwise it has no effect. This uses the existing half-texel clipping implementation and is independent of custom material behavior.

### HFrames and VFrames

Positive columns/rows. Their product must fit Int32.MaxValue, avoiding arithmetic overflow in frame addressing. Invalid changes throw ArgumentOutOfRangeException before mutation. Grid changes preserve the selected column and row when they still exist; otherwise Frame becomes zero. Such implicit adjustments do not emit FrameChanged. A changed grid requests redraw and raises inherited PropertyListChanged after committing its state.

### Frame

Zero-based row-major index in `[0, HFrames * VFrames)`. Invalid values throw ArgumentOutOfRangeException. Reassignment to the current index is a no-op. A changed explicit assignment requests redraw and emits FrameChanged.

### FrameCoords

Alias for Frame, with X as the column and Y as the row. Both coordinates must be within the grid. Assignment routes through Frame and shares its no-op/event behavior. It is exposed to typed tooling but is not stored separately from Frame.

## Method descriptions

### GetRect

Returns local inspection bounds independently of flip, visibility, modulation and materials. Without a texture it returns `(0, 0, 1, 1)`. Otherwise texture/region dimensions are truncated toward zero, divided by the frame grid and truncated again. Offset and Centered use that resulting size. If both dimensions are zero, the returned size becomes one by one after the origin is calculated. A single zero dimension stays zero; signed region dimensions remain signed.

Rendering uses fractional frame dimensions, so GetRect may omit a fractional drawing edge. This preserves the existing integer inspection contract. Invalid custom texture dimensions or overflowed bounds throw InvalidOperationException. No viewport pixel-snapping policy is implemented yet.

### IsPixelOpaque

Accepts a finite point in local drawing coordinates; nonfinite input throws ArgumentException. Returns false without a nonempty texture or outside the half-open fractional drawing rectangle. Maps through the selected frame, region and flips, clamps to the full texture's logical edges and delegates to Texture.IsPixelOpaque. It measures source alpha, not final material/modulated/visible output. ImageTexture treats alpha above 0.1 as opaque.

Negative region sizes use the same combined source/destination flipping as actual drawing. Shared repeat/mirrored-repeat and viewport snapping policies remain dependencies of the wider canvas API; this path currently samples with clamp behavior. Custom Texture opacity overrides remain authoritative.

## Event descriptions

### FrameChanged

Emitted synchronously after an explicit Frame or FrameCoords assignment changes the index. The observer sees the new index/coordinates. Changing the frame grid can adjust Frame silently as documented above.

### TextureChanged

Emitted synchronously after changing the reference, including clearing it. Content changes within the same texture do not emit this event. Neither event is serialized by PackedScene; disposal clears subscribers.

## Protected integration

| Declaration | Contract |
| --- | --- |
| `protected override void OnDraw()` | Records the frame through Texture.DrawRectRegion. Derived overrides call base.OnDraw to retain the image. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Adds typed properties; dimensions precede Frame in storage order. |
| `protected override Func<Node> CreateSceneInstanceFactory()` | Static factory for exact Sprite instances. Derived types retain Entity's explicit factory contract. |
| `protected override void Dispose(bool disposing)` | Disconnects the borrowed texture and clears events, then releases inherited Entity state. |

## Verification and limits

[SpriteTests](../../tests/Electron2D.Tests/SpriteTests.cs) covers defaults, event/no-op ordering, grid resizing, invalid-state rollback, opacity, fractional/zero/signed bounds, resource subscriptions, event failures, owner-thread enforcement and PackedScene reconstruction with shared and scene-local textures. It checks a worker notification during OnDraw is retained for the next frame and zero allocations for unchanged preparation. A custom Texture verifies virtual region drawing, invalid dimensions and disposal, and derived-coordinate overflow is rejected.

[SpriteRenderingTests](../../tests/Electron2D.Tests/SpriteRenderingTests.cs) checks twelve successive frames: ordinary output, both flips, sheet coordinates, region selection/clipping, worker size/pixel changes, replacement, hidden redraw and clearing. It runs with GPU and compatibility, plus a custom GLSL material on GPU. Exact current native evidence and limits are recorded in the [canvas component](../components/canvas-rendering.md#verification).

AtlasTexture, SpriteFrames and an animated sprite node are separate unfinished resources/capabilities. Inherited repeat/filter policies, viewport pixel snapping, GUI/editor/accessibility integration and other target platforms remain incomplete. [Coverage](../coverage/classes/Sprite2D.md) separates verified members from those inherited dependencies.
