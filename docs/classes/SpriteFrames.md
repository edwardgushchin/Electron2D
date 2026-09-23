# SpriteFrames

Last updated: 2026-09-23

**Inherits:** [Resource](Resource.md)

- **Declaration:** `public sealed class SpriteFrames : Resource`
- **Source:** [SpriteFrames.cs](../../src/Scene/Resources/SpriteFrames.cs)
- **Component:** [Canvas rendering](../components/canvas-rendering.md), Resources domain

## Description

A library of named animations consumed by [AnimatedSprite](AnimatedSprite.md). Each animation stores a nonnegative double FPS, a LoopMode, and ordered `(Texture?, float duration)` frames. Names are case-sensitive ordinal strings; empty names are valid. GetAnimationNames returns an independent sorted array. Selection fallback inside AnimatedSprite instead uses insertion order; renaming moves an animation to the end.

The library borrows textures, including AtlasTexture views and null blank frames. It never disposes those resources. Each state operation is serialized under the library lock; inherited Changed handlers execute synchronously after the mutation and outside that lock. Callback exceptions propagate with state committed. Contained textures do not forward Changed. Resource duplication still requires the base caller-coordination contract.

Only AddFrame, applied SetFrame, successful RemoveFrame and Clear emit Changed. Name, speed, loop and ClearAll edits are silent, including when they affect the selected animation. EmitChanged can explicitly reconcile attached consumers after such edits. Pixel changes of an existing ImageTexture are visible through retained renderer commands; geometry changes inside contained textures require explicit redraw. There is no separate public dictionary/Variant serialization surface.

## Example

This constructs a detached animation using an atlas view. Attach the sprite through the normal Node/Window API to advance it.

```csharp
using var image = Image.CreateEmpty(32, 16, false, Image.Format.Rgba8);
image.Fill(Colors.White);
using var sheet = ImageTexture.CreateFromImage(image);
using var left = new AtlasTexture { Atlas = sheet, Region = new(0, 0, 16, 16) };
using var right = new AtlasTexture { Atlas = sheet, Region = new(16, 0, 16, 16) };
using var frames = new SpriteFrames();
frames.AddFrame("default", left);
frames.AddFrame("default", right, duration: 2);
frames.SetAnimationSpeed("default", 8);
using var sprite = new AnimatedSprite { SpriteFrames = frames };
sprite.Play();
```

## Enumerations

[LoopMode](SpriteFrames.LoopMode.md): None = 0, Linear = 1, PingPong = 2. The obsolete bool-loop methods remain executable: true selects Linear, false selects None, and GetAnimationLoop returns false for PingPong.

## Constructors

| Declaration | Contract |
| --- | --- |
| [`public SpriteFrames()`](#api-spriteframes) | Creates the empty default animation. |

## Methods

| Declaration | Contract |
| --- | --- |
| [`public void AddAnimation(string animation)`](#api-addanimation) | Adds an empty animation at five frames per second with linear looping, without emitting Changed. |
| [`public bool HasAnimation(string animation)`](#api-hasanimation) | Tests whether an exact animation name exists. |
| [`public string[] GetAnimationNames()`](#api-getanimationnames) | Returns a new array of animation names sorted using ordinal order. |
| [`public void DuplicateAnimation(string from, string to)`](#api-duplicateanimation) | Copies an animation to a new name, sharing its textures but not its frame collection. |
| [`public void RemoveAnimation(string animation)`](#api-removeanimation) | Removes an animation if present, without emitting Changed. |
| [`public void RenameAnimation(string animation, string newName)`](#api-renameanimation) | Renames an animation, placing it last in insertion order, without emitting Changed. |
| [`public void SetAnimationSpeed(string animation, double FPS)`](#api-setanimationspeed) | Sets an animation's frames per second without emitting Changed. |
| [`public double GetAnimationSpeed(string animation)`](#api-getanimationspeed) | Returns an animation's frames per second. |
| [`public void SetAnimationLoopMode(string animation, LoopMode loopMode)`](#api-setanimationloopmode) | Sets the endpoint behavior without emitting Changed. |
| [`public LoopMode GetAnimationLoopMode(string animation)`](#api-getanimationloopmode) | Returns the animation's endpoint behavior. |
| [`public void SetAnimationLoop(string animation, bool loop)`](#api-setanimationloop) | Sets Linear for true or None for false. |
| [`public bool GetAnimationLoop(string animation)`](#api-getanimationloop) | Tests whether the loop mode is Linear; PingPong returns false. |
| [`public void AddFrame(string animation, Texture? texture, float duration = 1f, int atPosition = -1)`](#api-addframe) | Adds a borrowed frame and emits Changed. |
| [`public void SetFrame(string animation, int index, Texture? texture, float duration = 1f)`](#api-setframe) | Replaces a frame and emits Changed even when its values stay equal. |
| [`public void RemoveFrame(string animation, int index)`](#api-removeframe) | Removes an existing frame and emits Changed. |
| [`public int GetFrameCount(string animation)`](#api-getframecount) | Returns the frame count of an existing animation. |
| [`public Texture? GetFrameTexture(string animation, int index)`](#api-getframetexture) | Returns the borrowed texture for a frame. |
| [`public float GetFrameDuration(string animation, int index)`](#api-getframeduration) | Returns the relative duration of a frame. |
| [`public void Clear(string animation)`](#api-clear) | Removes every frame from an existing animation and emits Changed, even when already empty. |
| [`public void ClearAll()`](#api-clearall) | Removes every animation and recreates the empty default animation without emitting Changed. |

## Protected hooks

| Declaration | Contract |
| --- | --- |
| [`protected override Resource CreateDuplicateInstance()`](#api-createduplicateinstance) | Creates a fresh exact-type SpriteFrames resource for the inherited duplication session. |
| [`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`](#api-copycustomstateto) | Copies independent animation/name/frame containers. Shallow copies borrow textures; deep copies use duplicateSubresource, preserving graph aliases and the base internal/external resource policy. The forceDuplicateSubresource parameter is unused because frames have no always-duplicate edge. |
| [`protected override void Dispose(bool disposing)`](#api-dispose) | Disconnects owned subscriptions or releases frame containers, then delegates inherited cleanup. Borrowed resources are not disposed. Public use after disposal throws ObjectDisposedException. |

## Constructors descriptions

<a id="api-spriteframes"></a>
### SpriteFrames

`public SpriteFrames()`

Creates the empty default animation.

## Methods descriptions

<a id="api-addanimation"></a>
### AddAnimation

`public void AddAnimation(string animation)`

Adds an empty animation at five frames per second with linear looping, without emitting Changed.

Parameter `animation`: The exact case-sensitive name; an empty name is allowed.

`ArgumentException`: The name already exists.

`ArgumentNullException`: The name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-hasanimation"></a>
### HasAnimation

`public bool HasAnimation(string animation)`

Tests whether an exact animation name exists.

Parameter `animation`: The non-null name.

Returns: Whether the animation exists.

`ArgumentNullException`: The name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-getanimationnames"></a>
### GetAnimationNames

`public string[] GetAnimationNames()`

Returns a new array of animation names sorted using ordinal order.

Returns: An independent array, possibly empty.

`ObjectDisposedException`: The library is disposed.

<a id="api-duplicateanimation"></a>
### DuplicateAnimation

`public void DuplicateAnimation(string from, string to)`

Copies an animation to a new name, sharing its textures but not its frame collection.

Parameter `from`: The existing animation.

Parameter `to`: The unused destination name.

Contract: Does not emit Changed.

`ArgumentException`: The source is absent or the destination exists.

`ArgumentNullException`: A name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-removeanimation"></a>
### RemoveAnimation

`public void RemoveAnimation(string animation)`

Removes an animation if present, without emitting Changed.

Parameter `animation`: The non-null name.

`ArgumentNullException`: The name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-renameanimation"></a>
### RenameAnimation

`public void RenameAnimation(string animation, string newName)`

Renames an animation, placing it last in insertion order, without emitting Changed.

Parameter `animation`: The existing name.

Parameter `newName`: The unused new name, including an empty name.

`ArgumentException`: The source is absent or the new name exists, including the original name.

`ArgumentNullException`: A name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-setanimationspeed"></a>
### SetAnimationSpeed

`public void SetAnimationSpeed(string animation, double FPS)`

Sets an animation's frames per second without emitting Changed.

Parameter `animation`: The existing name.

Parameter `FPS`: A finite nonnegative rate. Zero freezes advancement without pausing playback.

`ArgumentOutOfRangeException`: The rate is negative or nonfinite.

`ArgumentException`: The name does not exist.

`ArgumentNullException`: The name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-getanimationspeed"></a>
### GetAnimationSpeed

`public double GetAnimationSpeed(string animation)`

Returns an animation's frames per second.

Parameter `animation`: The existing name.

Returns: The finite nonnegative rate, initially five.

`ArgumentException`: The name does not exist.

`ArgumentNullException`: The name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-setanimationloopmode"></a>
### SetAnimationLoopMode

`public void SetAnimationLoopMode(string animation, LoopMode loopMode)`

Sets the endpoint behavior without emitting Changed.

Parameter `animation`: The existing name.

Parameter `loopMode`: A defined loop mode.

`ArgumentOutOfRangeException`: The loop mode is undefined.

`ArgumentException`: The name does not exist.

`ArgumentNullException`: The name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-getanimationloopmode"></a>
### GetAnimationLoopMode

`public LoopMode GetAnimationLoopMode(string animation)`

Returns the animation's endpoint behavior.

Parameter `animation`: The existing name.

Returns: The loop mode, initially Linear.

`ArgumentException`: The name does not exist.

`ArgumentNullException`: The name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-setanimationloop"></a>
### SetAnimationLoop

`public void SetAnimationLoop(string animation, bool loop)`

Sets Linear for true or None for false.

Parameter `animation`: The existing name.

Parameter `loop`: Whether to use linear repetition.

Contract: Uses SetAnimationLoopMode validation and emits no Changed event.

<a id="api-getanimationloop"></a>
### GetAnimationLoop

`public bool GetAnimationLoop(string animation)`

Tests whether the loop mode is Linear; PingPong returns false.

Parameter `animation`: The existing name.

Returns: Whether the animation loops linearly.

Contract: Uses GetAnimationLoopMode validation.

<a id="api-addframe"></a>
### AddFrame

`public void AddFrame(string animation, Texture? texture, float duration = 1f, int atPosition = -1)`

Adds a borrowed frame and emits Changed.

Parameter `animation`: The existing name.

Parameter `texture`: A live borrowed texture, or null for an empty frame.

Parameter `duration`: Finite relative duration, clamped to at least 0.01.

Parameter `atPosition`: Insert before an existing index; any negative index or index at/beyond the end appends.

`ArgumentException`: The animation does not exist.

`ArgumentNullException`: The name is null.

`ArgumentOutOfRangeException`: Duration is nonfinite.

`ObjectDisposedException`: The library or supplied texture is disposed.

`Exception`: A change handler throws after commitment.

<a id="api-setframe"></a>
### SetFrame

`public void SetFrame(string animation, int index, Texture? texture, float duration = 1f)`

Replaces a frame and emits Changed even when its values stay equal.

Parameter `animation`: The existing name.

Parameter `index`: Nonnegative frame index. At/beyond the end is a no-op.

Parameter `texture`: A live borrowed texture or null.

Parameter `duration`: Finite relative duration, clamped to at least 0.01.

Contract: A past-end index returns before validating texture/duration.

`ArgumentException`: The animation does not exist.

`ArgumentNullException`: The name is null.

`ArgumentOutOfRangeException`: The index is negative or the applied duration is nonfinite.

`ObjectDisposedException`: The library or applied texture is disposed.

`Exception`: A change handler throws after commitment.

<a id="api-removeframe"></a>
### RemoveFrame

`public void RemoveFrame(string animation, int index)`

Removes an existing frame and emits Changed.

Parameter `animation`: The existing name.

Parameter `index`: An existing frame index.

`ArgumentException`: The animation does not exist.

`ArgumentNullException`: The name is null.

`ArgumentOutOfRangeException`: The index is outside the frame list.

`ObjectDisposedException`: The library is disposed.

`Exception`: A change handler throws after commitment.

<a id="api-getframecount"></a>
### GetFrameCount

`public int GetFrameCount(string animation)`

Returns the frame count of an existing animation.

Parameter `animation`: The existing name.

Returns: The nonnegative frame count.

`ArgumentException`: The animation does not exist.

`ArgumentNullException`: The name is null.

`ObjectDisposedException`: The library is disposed.

<a id="api-getframetexture"></a>
### GetFrameTexture

`public Texture? GetFrameTexture(string animation, int index)`

Returns the borrowed texture for a frame.

Parameter `animation`: The existing name.

Parameter `index`: Nonnegative frame index.

Returns: The borrowed texture, or null for an empty frame or past-end index.

`ArgumentException`: The animation does not exist.

`ArgumentNullException`: The name is null.

`ArgumentOutOfRangeException`: The index is negative.

`ObjectDisposedException`: The library is disposed.

<a id="api-getframeduration"></a>
### GetFrameDuration

`public float GetFrameDuration(string animation, int index)`

Returns the relative duration of a frame.

Parameter `animation`: The existing name.

Parameter `index`: Nonnegative frame index.

Returns: The stored relative duration, or one for a past-end index. Seconds equal duration divided by FPS and absolute playing speed.

`ArgumentException`: The animation does not exist.

`ArgumentNullException`: The name is null.

`ArgumentOutOfRangeException`: The index is negative.

`ObjectDisposedException`: The library is disposed.

<a id="api-clear"></a>
### Clear

`public void Clear(string animation)`

Removes every frame from an existing animation and emits Changed, even when already empty.

Parameter `animation`: The existing name.

`ArgumentException`: The animation does not exist.

`ArgumentNullException`: The name is null.

`ObjectDisposedException`: The library is disposed.

`Exception`: A change handler throws after commitment.

<a id="api-clearall"></a>
### ClearAll

`public void ClearAll()`

Removes every animation and recreates the empty default animation without emitting Changed.

`ObjectDisposedException`: The library is disposed.

## Protected hooks descriptions

<a id="api-createduplicateinstance"></a>
### CreateDuplicateInstance

`protected override Resource CreateDuplicateInstance()`

Creates a fresh exact-type SpriteFrames resource for the inherited duplication session.

<a id="api-copycustomstateto"></a>
### CopyCustomStateTo

`protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)`

Copies independent animation/name/frame containers. Shallow copies borrow textures; deep copies use duplicateSubresource, preserving graph aliases and the base internal/external resource policy. The forceDuplicateSubresource parameter is unused because frames have no always-duplicate edge.

<a id="api-dispose"></a>
### Dispose

`protected override void Dispose(bool disposing)`

Disconnects owned subscriptions or releases frame containers, then delegates inherited cleanup. Borrowed resources are not disposed. Public use after disposal throws ObjectDisposedException.

## Duplication, scene ownership and verification

CopyFromResource preserves target identity, replaces frame containers through the existing custom-state hook, and follows base notification/error rules; it is not transactional. Inherited ResourceLocalToScene defaults false. A local library used in PackedScene participates in the existing graph session; duplicated local/built-in textures preserve aliases and are owned by the instantiated scene root. Ordinary Duplicate results and their copied subresources remain caller-owned.

[AnimatedSpriteTests](../../tests/Electron2D.Tests/AnimatedSpriteTests.cs) covers defaults, sorted names and insertion fallback, silent/Changed edits, empty names, duration clamping, past-end/no-op behavior, deprecated loop aliases, errors, shallow/deep copies, CopyFromResource, scene-local ownership, worker notifications and borrowed lifetime. [AnimatedSpriteRenderingTests](../../tests/Electron2D.Tests/AnimatedSpriteRenderingTests.cs) verifies consumers on native backends. Asset import and disk serialization are outside this resource slice. See ADRs [0013](../decisions/resources.md#adr-0013), [0014](../decisions/resources.md#adr-0014), and the [playback timing audit](AnimatedSprite.md#timing-contract-and-source-audit).
