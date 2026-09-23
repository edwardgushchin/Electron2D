# AnimatedSprite

Last updated: 2026-09-23

**Inherits:** [Entity](Entity.md), [CanvasItem](CanvasItem.md), [Node](Node.md)

- **Declaration:** `public class AnimatedSprite : Entity`
- **Source:** [AnimatedSprite.cs](../../src/Scene/2D/AnimatedSprite.cs)
- **Component:** [Canvas rendering](../components/canvas-rendering.md)

## Description

Plays named texture-frame animations from a borrowed [SpriteFrames](SpriteFrames.md) library. It is a sibling of Sprite directly under Entity. Tree membership and processing come from Node; retained drawing, visibility, sampling, materials and modulation come from CanvasItem; spatial placement comes from Entity. There is no sheet-grid API here: frame textures may be AtlasTexture views.

Playback uses internal idle processing, independently of ProcessEnabled and visibility. It follows tree pause/process mode and the delivered scaled delta; physics frames do not advance it. Engine.Run supplies the clock and time scaling; explicit SceneTree.ProcessFrame callers supply already chosen delta. A detached node can be playing but cannot advance. Autoplay runs after ready handling, only if its name exists; changing Autoplay after ready does not immediately play. RequestReady enables another ready cycle.

Attached mutations require the scene owner thread. Library Changed on that thread reconciles the current index/progress and redraw immediately. Worker Changed marks an atomic pending update: the next owner Frame/FrameProgress read or enter/ready/internal-process/draw notification reconciles it. For an attached node, no scene event runs on that worker. Detached library notifications run on the mutating thread. Getters on a different thread do not flush pending work and are not a synchronization mechanism. Resource disposal remains caller-coordinated. Scene capture may reject a pending reconciliation rather than mutating captured nodes.

Events run synchronously in subscription order. Exceptions retain committed state and abort the operation. Play/Pause/Stop finally reconcile the internal scheduling flag with the final playback flag, including reentrant calls and throwing observers. Disposal during an event stops further playback access; borrowed resources survive node disposal. Callback Pause affects following process calls; the current time-consumption loop may still finish its remaining iterations, matching the playback contract below.

## Example

A detached setup; attaching it to a SceneTree triggers ready and autoplay. Null textures deliberately produce blank frames.

```csharp
using var frames = new SpriteFrames();
frames.AddFrame("default", null);
frames.AddFrame("default", null, duration: 2);
frames.SetAnimationLoopMode("default", SpriteFrames.LoopMode.None);
using var sprite = new AnimatedSprite { SpriteFrames = frames, Autoplay = "default" };
sprite.AnimationFinished += () => Console.WriteLine("Finished");
```

## Timing contract and source audit

The pinned Godot 4.7.2 [C++ implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/scene/2d/animated_sprite_2d.cpp) and [XML documentation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/AnimatedSprite2D.xml) disagree about some timing details. Existing ADR 0008 compatibility is retained against the implementation; this slice does not silently introduce a corrected timing algorithm.

- Ordinary per-frame rate is `FPS * SpeedScale * customSpeed / duration`. GetPlayingSpeed excludes FPS and duration. Zero FPS or multiplier leaves IsPlaying true without advancement.
- Reaching exactly progress one/zero does not emit a transition until a later positive tick. Nonlooping completion pauses at the endpoint and emits AnimationFinished without FrameChanged.
- The transition iteration consumes time using the rate calculated for the previous frame. Later iterations/process calls use the new duration. Example: durations `(2, 1)` at 2 FPS, followed by deltas `1, .25, .125`, produce states `(0, 1), (1, .25), (1, .5)`.
- PingPong retains the endpoint frame, reverses custom speed, emits AnimationLooped, then resets progress using the previous direction and emits FrameChanged. Endpoint dwell therefore depends on tick boundaries and can repeat; it is not guaranteed to match the XML promise of one endpoint display. Two frames at 1 FPS with deltas `1, 1, .25, .5` produce `(0, 1), (1, 1), (1, .25), (0, .75)` with reversal on the third tick.
- Each process call consumes at most `frame count + 1` loop iterations. Remaining time is discarded to bound callback work. Large deltas therefore do not promise mathematically exact catch-up.
- Resource edits recalculate the current duration only when Changed is emitted/reconciled. Speed and loop values are read during processing even though their setters are silent. A missing or empty selected animation does not advance; an explicit Play on an empty animation is a no-op.

These details are regression-tested. A future change to documentation-style timing requires an explicit compatibility decision and new expected traces. Typed C# adaptations reject nonfinite state/rates, undefined loop values and disposed borrowed resources with exceptions; rate-product overflow throws instead of hanging.

## Constructors

| Declaration | Contract |
| --- | --- |
| [`public AnimatedSprite()`](#api-animatedsprite) | Creates a centered, stopped sprite with no frame library and the default animation selected. |

## Properties

| Declaration | Contract |
| --- | --- |
| [`public SpriteFrames? SpriteFrames { get; set; }`](#api-spriteframes) | Gets or sets the borrowed frame library. |
| [`public string Animation { get; set; }`](#api-animation) | Gets or sets the selected animation. |
| [`public string Autoplay { get; set; }`](#api-autoplay) | Gets or sets the animation played after ready handling. |
| [`public int Frame { get; set; }`](#api-frame) | Gets or sets the displayed frame index. |
| [`public float FrameProgress { get; set; }`](#api-frameprogress) | Gets or sets progress through the current frame. |
| [`public float SpeedScale { get; set; }`](#api-speedscale) | Gets or sets the signed playback multiplier. |
| [`public bool Centered { get; set; }`](#api-centered) | Gets or sets whether the texture is centered around Offset. |
| [`public Vector2 Offset { get; set; }`](#api-offset) | Gets or sets the finite local drawing offset. |
| [`public bool FlipH { get; set; }`](#api-fliph) | Gets or sets horizontal flipping without moving the drawing origin. |
| [`public bool FlipV { get; set; }`](#api-flipv) | Gets or sets vertical flipping without moving the drawing origin. |

## Methods

| Declaration | Contract |
| --- | --- |
| [`public bool IsPlaying()`](#api-isplaying) | Tests whether playback is enabled, including when its speed is zero or the node is detached. |
| [`public float GetPlayingSpeed()`](#api-getplayingspeed) | Returns the signed product of SpeedScale and the custom speed passed to Play. |
| [`public void Play(string name = "", float customSpeed = 1, bool fromEnd = false)`](#api-play) | Starts or resumes a named animation. |
| [`public void PlayBackwards(string name = "")`](#api-playbackwards) | Starts or resumes reverse playback using Play(name, -1, true). |
| [`public void Pause()`](#api-pause) | Pauses playback while retaining frame, progress and custom speed. |
| [`public void Stop()`](#api-stop) | Stops playback, resets custom speed to one and resets frame/progress when a library is assigned. |
| [`public void SetFrameAndProgress(int frame, float progress)`](#api-setframeandprogress) | Sets the frame index and its progress together. |

## Events

| Declaration | Contract |
| --- | --- |
| [`public event Action? AnimationChanged`](#api-animationchanged) | Occurs when the selected animation name changes. |
| [`public event Action? AnimationFinished`](#api-animationfinished) | Occurs after non-looping playback pauses at an endpoint. |
| [`public event Action? AnimationLooped`](#api-animationlooped) | Occurs at a wrap or direction reversal, before the following FrameChanged event. |
| [`public event Action? FrameChanged`](#api-framechanged) | Occurs on a frame transition or an explicit requested index different from the prior index. |
| [`public event Action? SpriteFramesChanged`](#api-spriteframeschanged) | Occurs after library replacement and its stop/property-list/redraw stages. |

## Protected hooks

| Declaration | Contract |
| --- | --- |
| [`protected override void OnNotification(int what)`](#api-onnotification) | Calls inherited handling first, then reconciles frame-resource changes and performs ready autoplay/internal idle playback. |
| [`protected override void OnDraw()`](#api-ondraw) | Draws the current non-null texture through virtual DrawRectRegion. Derived classes call base.OnDraw to retain the animation image. The full logical texture is used with white command modulation, no transpose, and clipping enabled; source view policy can refine clipping. Centering/offset are applied locally, then optional viewport transform snapping, then flips via signed dimensions. Nonfinite/negative dimensions fail explicitly. |
| [`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`](#api-getpropertydescriptors) | Appends typed stored descriptors for SpriteFrames, Animation, Autoplay, Frame, FrameProgress, SpeedScale, Centered, Offset, FlipH and FlipV. The resource restores before selection and frame state. |
| [`protected override PropertyDescriptor? ValidateProperty(PropertyDescriptor property)`](#api-validateproperty) | Returns a read-only, nonstored Frame descriptor while playing; otherwise delegates inherited validation. Playback state and custom speed are not stored. |
| [`protected override Func<Node> CreateSceneInstanceFactory()`](#api-createsceneinstancefactory) | Uses a static method to create a detached exact-type AnimatedSprite. Derived classes must supply their own existing Node factory contract. |
| [`protected override void Dispose(bool disposing)`](#api-dispose) | Disconnects owned subscriptions or releases frame containers, then delegates inherited cleanup. Borrowed resources are not disposed. Public use after disposal throws ObjectDisposedException. |

## Constructors descriptions

<a id="api-animatedsprite"></a>
### AnimatedSprite

`public AnimatedSprite()`

Creates a centered, stopped sprite with no frame library and the default animation selected.

## Properties descriptions

<a id="api-spriteframes"></a>
### SpriteFrames

`public SpriteFrames? SpriteFrames { get; set; }`

Gets or sets the borrowed frame library.

Value: Null by default.

Contract: Replacement selects the first inserted animation when the current one is absent, clears invalid autoplay, stops playback, notifies the property list, requests redraw and emits SpriteFramesChanged. Equal assignment is a no-op. Assigning null retains the selected name and frame/progress.

`ObjectDisposedException`: The node or new library is disposed.

`InvalidOperationException`: Mutation is unavailable on this thread or during capture.

`Exception`: A callback fails after commitment.

<a id="api-animation"></a>
### Animation

`public string Animation { get; set; }`

Gets or sets the selected animation.

Value: The default name initially.

Contract: A changed name emits AnimationChanged before resetting frame/progress. Selection preserves playing state for a nonempty animation and chooses the end when the current playing speed is negative. Empty animations stop. Invalid selection commits the name/event before stopping and reporting the error.

`ArgumentNullException`: The name is null.

`ArgumentException`: The selected nonempty name is absent from the library.

`InvalidOperationException`: No library exists, or mutation is unavailable.

`ObjectDisposedException`: The node or library is disposed.

`Exception`: An event handler fails after commitment.

<a id="api-autoplay"></a>
### Autoplay

`public string Autoplay { get; set; }`

Gets or sets the animation played after ready handling.

Value: Empty by default. An absent name has no ready-time effect.

Contract: Setting this after ready does not immediately start playback. RequestReady permits a later ready cycle.

`ArgumentNullException`: The name is null.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

<a id="api-frame"></a>
### Frame

`public int Frame { get; set; }`

Gets or sets the displayed frame index.

Value: Zero initially.

Contract: Uses SetFrameAndProgress, resetting progress to one for negative playing speed and zero otherwise. An assignment without a library does nothing. Worker library changes are reconciled before an owner-thread read.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node or library is disposed.

`Exception`: A frame or property-list subscriber fails after commitment.

<a id="api-frameprogress"></a>
### FrameProgress

`public float FrameProgress { get; set; }`

Gets or sets progress through the current frame.

Value: Zero initially; ordinary playback moves between zero and one.

Contract: Any finite value is retained without clamping, redraw or an event.

`ArgumentOutOfRangeException`: Progress is not finite.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

<a id="api-speedscale"></a>
### SpeedScale

`public float SpeedScale { get; set; }`

Gets or sets the signed playback multiplier.

Value: One initially. Zero freezes advancement while IsPlaying stays true.

`ArgumentOutOfRangeException`: The multiplier or its product with custom speed is not finite.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

<a id="api-centered"></a>
### Centered

`public bool Centered { get; set; }`

Gets or sets whether the texture is centered around Offset.

Value: True initially.

Contract: Changes request redraw and emit ItemRectChanged.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

`Exception`: An ItemRectChanged subscriber fails after commitment.

<a id="api-offset"></a>
### Offset

`public Vector2 Offset { get; set; }`

Gets or sets the finite local drawing offset.

Value: Zero initially.

Contract: Changes request redraw and emit ItemRectChanged.

`ArgumentException`: The offset is not finite.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

`Exception`: An ItemRectChanged subscriber fails after commitment.

<a id="api-fliph"></a>
### FlipH

`public bool FlipH { get; set; }`

Gets or sets horizontal flipping without moving the drawing origin.

Value: False initially.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

<a id="api-flipv"></a>
### FlipV

`public bool FlipV { get; set; }`

Gets or sets vertical flipping without moving the drawing origin.

Value: False initially.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

## Methods descriptions

<a id="api-isplaying"></a>
### IsPlaying

`public bool IsPlaying()`

Tests whether playback is enabled, including when its speed is zero or the node is detached.

Returns: The playback flag.

`ObjectDisposedException`: The node is disposed.

<a id="api-getplayingspeed"></a>
### GetPlayingSpeed

`public float GetPlayingSpeed()`

Returns the signed product of SpeedScale and the custom speed passed to Play.

Returns: Zero when paused; otherwise the multiplier, excluding animation FPS and relative frame duration.

`ObjectDisposedException`: The node is disposed.

<a id="api-play"></a>
### Play

`public void Play(string name = "", float customSpeed = 1, bool fromEnd = false)`

Starts or resumes a named animation.

Parameter `name`: The exact name, or empty to use the current selection.

Parameter `customSpeed`: A finite signed multiplier; zero still enables playback.

Parameter `fromEnd`: Start a newly selected animation at its last frame/progress one.

Contract: An empty animation is a no-op. The same animation resumes, except at the corresponding completed endpoint, where forward Play or reverse Play with fromEnd restarts. New selection emits FrameChanged before AnimationChanged. Processing, property-list notification and redraw follow the selection events. Internal scheduling follows the final playback flag even when an observer throws or reenters.

`ArgumentNullException`: The name is null.

`ArgumentException`: The animation is absent.

`ArgumentOutOfRangeException`: Custom speed or its scaled product is nonfinite.

`InvalidOperationException`: No library exists or mutation is unavailable.

`ObjectDisposedException`: The node or library is disposed.

`Exception`: A callback fails after commitment.

<a id="api-playbackwards"></a>
### PlayBackwards

`public void PlayBackwards(string name = "")`

Starts or resumes reverse playback using Play(name, -1, true).

Parameter `name`: The animation name, or empty for the current selection.

Contract: Shares all Play validation, events and lifetime rules.

<a id="api-pause"></a>
### Pause

`public void Pause()`

Pauses playback while retaining frame, progress and custom speed.

Contract: Notifies the property list before disabling internal processing.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node is disposed.

`Exception`: A property-list subscriber fails after commitment.

<a id="api-stop"></a>
### Stop

`public void Stop()`

Stops playback, resets custom speed to one and resets frame/progress when a library is assigned.

Contract: Notifies the property list before disabling internal processing.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node or library is disposed.

`Exception`: A subscriber fails after commitment.

<a id="api-setframeandprogress"></a>
### SetFrameAndProgress

`public void SetFrameAndProgress(int frame, float progress)`

Sets the frame index and its progress together.

Parameter `frame`: Requested index; negatives clamp to zero and valid animation indices clamp to the last frame.

Parameter `progress`: Any finite progress, without clamping.

Contract: Without a library this is a no-op. If the selected animation is absent, positive indices are retained. FrameChanged compares the request with the old index, before clamping. Progress-only changes do not redraw.

`ArgumentOutOfRangeException`: Progress is not finite.

`InvalidOperationException`: Mutation is unavailable.

`ObjectDisposedException`: The node or library is disposed.

`Exception`: A frame subscriber fails after commitment.

## Events descriptions

<a id="api-animationchanged"></a>
### AnimationChanged

`public event Action? AnimationChanged`

Occurs when the selected animation name changes.

<a id="api-animationfinished"></a>
### AnimationFinished

`public event Action? AnimationFinished`

Occurs after non-looping playback pauses at an endpoint.

<a id="api-animationlooped"></a>
### AnimationLooped

`public event Action? AnimationLooped`

Occurs at a wrap or direction reversal, before the following FrameChanged event.

<a id="api-framechanged"></a>
### FrameChanged

`public event Action? FrameChanged`

Occurs on a frame transition or an explicit requested index different from the prior index.

Contract: A clamped assignment may emit even if the resulting index stays equal. Automatic transitions do not emit ItemRectChanged.

<a id="api-spriteframeschanged"></a>
### SpriteFramesChanged

`public event Action? SpriteFramesChanged`

Occurs after library replacement and its stop/property-list/redraw stages.

## Protected hooks descriptions

<a id="api-onnotification"></a>
### OnNotification

`protected override void OnNotification(int what)`

Calls inherited handling first, then reconciles frame-resource changes and performs ready autoplay/internal idle playback.

Contract: Handles resource reconciliation, ready-time autoplay and internal idle playback after inherited handling.

<a id="api-ondraw"></a>
### OnDraw

`protected override void OnDraw()`

Draws the current non-null texture through virtual DrawRectRegion. Derived classes call base.OnDraw to retain the animation image. The full logical texture is used with white command modulation, no transpose, and clipping enabled; source view policy can refine clipping. Centering/offset are applied locally, then optional viewport transform snapping, then flips via signed dimensions. Nonfinite/negative dimensions fail explicitly.

Contract: Draws the current non-null frame through its texture's virtual region hook. Calls to base.OnDraw preserve the animation image in derived classes. Transform snapping rounds only the local drawing offset.

<a id="api-getpropertydescriptors"></a>
### GetPropertyDescriptors

`protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()`

Appends typed stored descriptors for SpriteFrames, Animation, Autoplay, Frame, FrameProgress, SpeedScale, Centered, Offset, FlipH and FlipV. The resource restores before selection and frame state.

<a id="api-validateproperty"></a>
### ValidateProperty

`protected override PropertyDescriptor? ValidateProperty(PropertyDescriptor property)`

Returns a read-only, nonstored Frame descriptor while playing; otherwise delegates inherited validation. Playback state and custom speed are not stored.

Contract: During playback, tooling exposes Frame as read-only and does not store its transient value.

<a id="api-createsceneinstancefactory"></a>
### CreateSceneInstanceFactory

`protected override Func<Node> CreateSceneInstanceFactory()`

Uses a static method to create a detached exact-type AnimatedSprite. Derived classes must supply their own existing Node factory contract.

<a id="api-dispose"></a>
### Dispose

`protected override void Dispose(bool disposing)`

Disconnects owned subscriptions or releases frame containers, then delegates inherited cleanup. Borrowed resources are not disposed. Public use after disposal throws ObjectDisposedException.

## Lifecycle, drawing and verification limits

Resource replacement disconnects the old library, reconciles selection/autoplay, stops, notifies properties, redraws, then emits SpriteFramesChanged. Assigning the identical library is a no-op. Null retains selection/frame/progress because SetFrameAndProgress has no library to reset. Shrinking an animation can clamp the current index without FrameChanged: that event compares the requested index to the old index before clamping. Progress-only edits do not request redraw.

Centered and Offset emit ItemRectChanged when changed. Flips, frame changes and library changes redraw but do not emit that geometry event. Direct texture geometry/view changes do not forward through SpriteFrames; call QueueRedraw or emit the library's Changed as appropriate. Existing pixel updates remain visible without rerecording commands. Null frames and absent/empty animations draw nothing. A source texture is borrowed throughout retained drawing.

[AnimatedSpriteTests](../../tests/Electron2D.Tests/AnimatedSpriteTests.cs) verifies deterministic forward/reverse/loop/ping-pong traces, duration transitions, exact boundaries, bounded catch-up, zero speed, tree pause/process mode, ready/reentry, event ordering, worker reconciliation, virtual drawing/pixel snapping, packing/local ownership, callback errors/reentry/disposal and allocation-free repeated internal notifications after warmup. This allocation check does not establish whole-frame performance.

[AnimatedSpriteRenderingTests](../../tests/Electron2D.Tests/AnimatedSpriteRenderingTests.cs) checks native pixel readback for texture/atlas/null frames, flips, centering, worker library edits, pixel updates, visibility, timed host completion and error cleanup. Verified on Linux Wayland with compatibility and GPU/Vulkan, including HLSL/GLSL canvas materials, and dummy/software compatibility. Windows, macOS, other platform backends, owner visual acceptance, published deployment and performance remain unverified. This does not add AnimationPlayer, AnimationMixer, an editor, asset import or disk scene serialization. See ADRs [0008](../decisions/scene.md#adr-0008), [0028](../decisions/rendering.md#adr-0028), and [0021](../decisions/product.md#adr-0021).
