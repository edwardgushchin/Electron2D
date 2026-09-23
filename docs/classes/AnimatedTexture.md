# AnimatedTexture

Last updated: 2026-09-23

- Declaration: `public sealed class AnimatedTexture : Texture`
- Source: [AnimatedTexture.cs](../../src/Scene/Resources/AnimatedTexture.cs)
- Inherits: [Texture](Texture.md), [Resource](Resource.md)
- Component: [Canvas rendering](../components/canvas-rendering.md)

## Description

AnimatedTexture selects one of up to 256 borrowed Texture resources for any ordinary texture draw, including Sprite and a material texture binding. It needs no node. The renderer advances every live animated texture on each submitted frame using unscaled monotonic time; Pause, zero SpeedScale and disabled rendering hold the frame. Manual CurrentFrame selection restarts that frame's duration. OneShot holds the last frame when moving forward or first when moving backward. This clock is independent of AnimatedSprite's tree processing and the scaled canvas shader clock.

The resource does not dispose its frame textures. Keep them live while assigned. Frame slots, durations and the selected frame are copied by Resource.Duplicate; deep duplication follows the inherited resource graph policy. PackedScene duplicates scene-local resources through the same hooks. Source Changed notifications forward to this resource, and a frame transition emits Changed before FramePreDraw on the renderer owner thread. Edits emit Changed on the editing thread. Subscribers run after state is committed; a failing subscriber propagates its exception, and Engine.Run cleans up a failed frame. Cross-resource edits require caller coordination.

Only a current frame with readable pixels can be drawn or bound. With no active texture, logical size is 1×1. An empty selected slot has no image and the base L8 sentinel format; drawing it fails under the existing uninitialized texture rule. AtlasTexture frames and dependency cycles are rejected. The active textures establish the smallest width and height independently; larger frames are cropped from the top left to those dimensions. Source size changes recompute the minimum and invalidate cached pixels. Sprite redraws when the resource changes. No editor/import authoring is supplied by this resource.

## Example

```csharp
using var first = ImageTexture.CreateFromImage(firstImage);
using var second = ImageTexture.CreateFromImage(secondImage);
using var animation = new AnimatedTexture { Frames = 2 };
animation.SetFrameTexture(0, first);
animation.SetFrameTexture(1, second);
animation.SetFrameDuration(0, 0.1f);
animation.SetFrameDuration(1, 0.1f);
sprite.Texture = animation;
```

This is a partial snippet: `firstImage`, `secondImage` and `sprite` are caller-owned live objects, and playback needs a running renderer.

## API summary

| Declaration | Contract |
| --- | --- |
| `AnimatedTexture()` | One slot, one-second duration, frame zero, forward speed one. |
| `const int MaxFrames = 256` | Maximum slot count. |
| `int Frames { get; set; }` | Active slots, 1..256. |
| `int CurrentFrame { get; set; }` | Selected active slot. |
| `bool OneShot { get; set; }` | Hold at direction endpoint. |
| `bool Pause { get; set; }` | Hold current frame and progress. |
| `float SpeedScale { get; set; }` | Playback speed and direction. |
| `Texture? GetFrameTexture(int frame)` | Borrowed slot texture. |
| `void SetFrameTexture(int frame, Texture? texture)` | Assign borrowed slot texture. |
| `float GetFrameDuration(int frame)` | Slot duration in seconds. |
| `void SetFrameDuration(int frame, float duration)` | Set nonnegative slot duration. |
| `override int GetWidth()` / `override int GetHeight()` | Smallest active texture dimensions, or one. |
| `override Image? GetImage()` | Cropped copy of current image, or null. |
| `override bool IsPixelOpaque(int x, int y)` | Current frame opacity query, or true. |
| `override bool HasAlpha`, `override Image.Format PixelFormat`, `override bool HasMipmaps`, `override int MipmapCount` | Current frame metadata or empty defaults. |

Inherited ResourceLocalToScene, Changed, duplication, property metadata and disposal follow [Resource](Resource.md); inherited Texture drawing and GetSize use these current-frame queries. The protected Resource hooks `CreateDuplicateInstance()`, `CopyCustomStateTo(...)`, `GetPropertyDescriptors()` and `Dispose(bool)` override the base contract.

## Property descriptions

### Frames

One by default; values outside 1..MaxFrames throw ArgumentOutOfRangeException. Shrinking below CurrentFrame selects the last remaining slot and resets progress. Inactive slot contents remain available and are retained on later growth. Changing the active set recomputes the smallest frame dimensions. A changed count emits Changed.

### CurrentFrame

Zero by default. Assignment requires an active index, resets accumulated playback time and emits Changed even if the index is unchanged. Invalid indices throw ArgumentOutOfRangeException.

### OneShot

False by default. True prevents wrapping past the endpoint for the current direction; changing direction or manually selecting a frame permits playback again. Assignment emits Changed.

### Pause

False by default. True stops accumulation without discarding progress. Assignment emits Changed.

### SpeedScale

One by default. Positive values move forward, negative backward, zero freezes; frame duration is divided by the speed magnitude, including time already accumulated in the current frame when speed changes. Finite values in [-1000, 1000) are accepted. Invalid values throw ArgumentOutOfRangeException. Assignment emits Changed.

## Method descriptions

### GetFrameTexture and SetFrameTexture

Both accept any slot index in 0..255, including inactive slots; invalid indices throw ArgumentOutOfRangeException. Get returns a borrowed reference or null. Set accepts a live Texture or null, forwards its Changed notification once even if shared by slots, and emits Changed after a real identity change. Active non-null textures set the independent minimum width and height. It rejects disposed sources, AtlasTexture views and direct or transitive animated-texture cycles. Replacing or disposing the animation disconnects old source notifications without disposing borrowed textures.

### GetFrameDuration and SetFrameDuration

Both accept any slot index in 0..255. The default is one second. Set accepts finite nonnegative seconds; invalid values throw ArgumentOutOfRangeException. A zero-duration frame is skipped when playback advances; an all-zero loop stays bounded and holds its current frame. Assignment emits Changed. Existing elapsed progress is retained.

### GetWidth, GetHeight, GetImage and IsPixelOpaque

Width and height report the smallest dimensions among non-null active frames, or one when none is assigned. GetImage returns a caller-owned copy of the selected frame cropped from the top left, or null when that frame is empty. IsPixelOpaque samples the cropped image and falls back to true. HasAlpha falls back to false, PixelFormat to L8, HasMipmaps to false, and MipmapCount to zero; mipmaps are regenerated for a cropped image, so its level count reflects the cropped size. Disposed resources reject queries.

## Lifecycle and verification

Playback begins on the first renderer frame, which establishes the clock origin; later renders consume elapsed monotonic time, including time spent while the host runs without a submitted frame. Per-frame advancement is bounded by active frame count and skips complete loop cycles. Resource Changed callbacks run without holding the animation registry lock. Resource edits are serialized, while coherent multi-resource transactions remain a caller responsibility.

[AnimatedTextureTests](../../tests/Electron2D.Tests/AnimatedTextureTests.cs) verifies defaults, frame data, playback policies, mixed-size crop and source resizing, invalid inputs, graph copies, scene ownership and disposal. [AnimatedTextureRenderingTests](../../tests/Electron2D.Tests/AnimatedTextureRenderingTests.cs) verifies timed and cropped pixels, pause, reverse playback, callback reentry/failure and host cleanup on Linux Wayland GPU/compatibility and dummy/software. Other platforms, visual owner acceptance and import/editor workflows are unverified. Shared Texture/Resource gaps remain on their own coverage pages. See [ADRs 0013](../decisions/resources.md#adr-0013) and [0028](../decisions/rendering.md#adr-0028).
