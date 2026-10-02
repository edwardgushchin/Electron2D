# TextureButton

Last updated: 2026-10-02

**Inherits:** [BaseButton](BaseButton.md) · **Source:** [TextureButton.cs](../../src/Scene/GUI/TextureButton.cs) · **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

A button whose appearance comes from borrowed state textures. BaseButton supplies input, toggling, groups and shortcuts. TextureButton supplies placement, a focus overlay and an optional [BitMap](BitMap.md) hit mask. It starts empty, fully focusable, with `StretchMode=Keep`, both flips false and texture sizes included in its minimum.

The minimum uses the first assigned resource in this order: normal, pressed, hover, click mask. Disabled and focused textures never contribute. The result is componentwise absolute; `IgnoreTextureSize=true` supplies zero intrinsic minimum while inherited custom minimum/maximum constraints still apply.

## API

| Member | Contract |
| --- | --- |
| `TextureButton()` | Creates the defaults above and inherits BaseButton input policy. |
| `Texture? TextureNormal` | Normal image, initially null. |
| `Texture? TexturePressed` | Pressed and hover-pressed image; falls back to hover, then normal. |
| `Texture? TextureHover` | Hover image; absent uses pressed while the button is pressed, otherwise normal. |
| `Texture? TextureDisabled` | Disabled image; falls back to normal. |
| `Texture? TextureFocused` | Full-image overlay while focus is held, including hidden focus. It uses the selected image's destination. If no state texture is selected, the focused image determines that rectangle and draws once. |
| `BitMap? TextureClickMask` | Optional bitmap for hit testing; initially null. |
| `bool IgnoreTextureSize` | Excludes texture/mask dimensions from intrinsic minimum. False initially. |
| `TextureStretchMode StretchMode` | One of the seven [placement modes](TextureStretchMode.md); Keep initially. Raw undefined identities are stored and use Keep placement. |
| `bool FlipH`, `bool FlipV` | Negate the corresponding drawing dimension without moving its origin. False initially; do not reflect the mask. |
| `OnGetMinimumSize()` | Protected override supplying the priority-based intrinsic minimum. |
| `HasPoint(Vector2)` | Protected override performing mask or inherited rectangle hit testing. |
| `OnNotification(int)` | Protected override integrating built-in drawing, focus/size invalidation and resource polling. |
| `GetPropertyDescriptors()` | Protected override adding all ten typed stored properties. |
| `CreateSceneInstanceFactory()` | Protected override preserving exact TextureButton identity in PackedScene. Unhandled derived runtime types retain the base factory rejection. |
| `Dispose(bool)` | Protected override detaching resource listeners without disposing borrowed resources. |

## Drawing and input geometry

Built-in drawing happens during NotificationDraw, before user Draw handlers and OnDraw. Scale fills the control; Tile repeats natural pixels; Keep preserves natural size; centered variants offset their destination; aspect-fit variants preserve proportions; covered mode selects a centered source crop. The focus overlay uses its entire image over the same destination, including visual flips, even when the base image uses a crop. Existing virtual Texture drawing, atlas mapping, canvas transforms, filtering, modulation and materials remain in effect.

Hit testing uses the most recently recorded image rectangle. Before the first recording, or when that rectangle has no positive area, a mask uses natural mask coordinates. Tile repeats mask coordinates inside the recorded image bounds; other modes map back through the recorded destination and covered source crop. This preserves the source timing and mask policy, including masks unaffected by FlipH/FlipV. Without a mask, the inherited Control rectangle applies. An empty mask is never clickable. Out-of-range coordinates after a mismatched crop or concurrent mask resize return false rather than indexing outside its storage. Zero-size source images produce no aspect geometry instead of nonfinite coordinates.

## Resources, failures and storage

Equal assignments are silent. Changed assignments commit their borrowed references, maintain one subscription per distinct aliased resource, then invalidate drawing and minimum size. Resource events from another thread defer scene work with membership-generation checks. Active polling compares exact resource identities, change revisions and disposal state, including nested atlas sources; an earlier throwing observer cannot permanently leave equal-size source regions or custom drawing stale. Bitmap changes participate in the same invalidation.

While attached, the button acquires one renderer-residency lease for each distinct live state texture and atlas-chain dependency. Switching states therefore preserves previously uploaded images across unused frames. Replacement, exit and disposal release obsolete leases; detached configuration acquires none, aliases share one lease per button, and other controls keep their own independent leases. The residency counter adds no reference back to the control.

An externally disposed resource stays readable through its property and fails when that resource is consumed. A replacement must be live. Disposing the node detaches listeners and releases its cached resource snapshots without taking resource ownership. Attached access follows the scene owner thread and mutation is forbidden during scene capture.

Virtual texture size callbacks may replace resources or change the control. Minimum and drawing queries retry changed state up to 64 times, then fail explicitly if callbacks never settle. A resource or BaseButton state change during virtual drawing keeps the node dirty for the next recording and prevents a stale focus overlay. State changes during virtual size lookup retry before committing geometry. Drawing failures use the ordinary canvas rollback/retry lifecycle. Failed inherited notifications still allow required membership, polling and invalidation work before errors are reported.

PackedScene stores all ten properties through typed descriptors. It preserves aliases and ordinary external, built-in and scene-local resource graph policy; transient draw rectangles and subscriptions are not stored.

## Verification

[TextureButtonTests](../../tests/Electron2D.Tests/TextureButtonTests.cs) covers defaults, minimum priorities, raw stretch identities, exact placement, all flips, mask mapping, state fallback/focus ordering, missed direct/nested-atlas callbacks, worker changes, disposed references, reentry, typed packing, alias/peer/exit/failed-activation residency balance and warmed polling/recording/replay. [TextureButtonRenderingTests](../../tests/Electron2D.Tests/TextureButtonRenderingTests.cs) contains seven pixel phases for both backends and an active-frame allocation check. Focused managed checks pass, including 64 warmed mutation/polling/recording/replay cycles with zero managed bytes. The seven native pixel phases pass on Linux Wayland GPU and compatibility, and each backend passes 64 measured active frames alternating distinct normal/pressed textures after 64 warm-up frames with zero managed bytes from ProcessFrameStarted through FramePostDraw. Saved first/focus-frame readbacks were visually inspected on both backends. Native allocator counts, other platforms, broad-scene performance and owner acceptance remain unverified.
