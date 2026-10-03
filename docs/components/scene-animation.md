# Scene animation

Last updated: 2026-10-04

[ADR 0093](../decisions/scene-animation.md#adr-0093) owns reusable typed animation. The executable chain is [Animation](../classes/Animation.md) → [AnimationLibrary](../classes/AnimationLibrary.md) → [AnimationMixer](../classes/AnimationMixer.md) / [AnimationPlayer](../classes/AnimationPlayer.md) → an ordinary typed node property setter → the existing renderer.

## Author and execute

```csharp
var position = new PropertyDescriptor<Entity, Vector2>(nameof(Entity.Position),
    n => n.Position, (n, value) => n.Position = value);
var clip = new Animation { Length = 1 };
var track = clip.AddTrack(position);
clip.TrackSetPath(track, "Sprite:Position");
clip.TrackInsertKey(track, 0, new Vector2(16, 16));
clip.TrackInsertKey(track, 1, new Vector2(48, 16));
var library = new AnimationLibrary();
library.AddAnimation("move", clip);
var player = new AnimationPlayer();
player.AddAnimationLibrary("", library);
root.AddChild(player); // Sprite is another child of root.
player.Play("move");
player.Advance(0); // Optional immediate application.
```

Each track has an exact compile-time value type and writable descriptor. Relative paths resolve a node of the descriptor's owner type; the optional colon suffix must equal its property name. Missing/wrong-type targets contribute no writes. `Node2D`'s scene role is `Entity`; neutral targets remain `Node`. Built-in interpolation reuses the typed Tween math profile. Noninterpolatable values default to nearest holding, while callers may provide their own typed linear interpolation function. No reflection property setter or arbitrary value container is involved.

Keys are sorted by finite double time. Equal times replace, changing a key time also replaces an occupied destination, and generic access rejects the wrong value type. Continuous tracks evaluate on every update. Discrete tracks write every crossed key in direction order, including repeated loop seams, and avoid repeated setters when no key was crossed. A new binding or explicit seek initializes its sampled value. If an enormous discrete delta cannot make representable progress, evaluation fails and pauses rather than entering an endless loop.

Linear looping connects last/first keys when loop-wrap is enabled; disabling wrap holds edge values. Nearest, linear, time-aware cubic and scalar float/double angle modes execute. Cubic uses the track's typed linear interpolator, so custom interpolators must also support extrapolation. Integer rounding, nonlinear math values, out-of-duration keys and ping-pong edge controls still require the wider interpolation audit recorded as Partial in coverage; the scalar/vector sample checks do not prove every possible generic profile. Capture mode admits snapshot fades through the mixer.

Markers have unique names and times within approximate equality. Adding at an existing time replaces its marker and resets its color to white. Queries return time order; previous includes an exact marker time and next is strictly later. Marker edits notify Changed so resource consumers see committed edits. Clear removes tracks and resets length/loop, while retaining markers and the step hint.

A mixer rejects duplicate library namespaces and duplicate registration of the same library instance. Libraries borrow resources, forward their change names, sort queries ordinally and detach subscriptions deterministically. Replacing a name commits its new entry before removed/added events; reentrant removal is respected. Shallow copies retain resource aliases; deep copies duplicate nested resources through the existing graph session. Clip key containers copy independently; nested resource keys follow deep-copy policy. Arbitrary custom reference values remain caller-owned.

## Playback and lifetime

Idle/physics modes use Node's internal callbacks and inherited process/pause eligibility. Manual mode uses only Advance; detached manual execution is supported. RootNode defaults to the parent. Qualified names use `library/clip`, with an empty library namespace admitting plain names. Play applies on the next update, Pause retains assignment/position and clears queued names and capture caches, and Stop clears the queue and resets position/custom speed; keepState preserves target values. Seek clamps to the section and does not emit completion. Stopped AssignedAnimation rewinds without starting; active assignment switches using the current signed speed. Setting CurrentAnimation to empty or the stop sentinel queues stopping through the attached SceneTree; detached manual playback stops synchronously. Zero speed keeps playback enabled. Finite playback holds its endpoint and emits completion once, then consumes a queued or configured-next name. Linear/ping-pong loops retain overshoot and do not finish.

Resource edits, tree changes, selected clips, root changes and ClearCaches rebuild binding containers on next evaluation. Warmed playback performs no path parsing or value boxing. Each evaluation snapshots bindings and stops after a reentrant control/resource change. Setter failures pause unchanged playback and propagate. Disposed or queued targets and removed/disposed clip sources are handled without transferring resource ownership to the player.

Method/Bézier/audio/nested-animation tracks and persistence remain separate applicable work. Three-dimensional transform/blend-shape tracks and their root-motion extraction are excluded by the 2D product decision. PackedScene refuses owned animation nodes because their state has no reconstruction factory; empty factories would silently lose libraries. No editor, disk loader/saver or file-authoring acceptance is claimed.

## Verification

[SceneAnimationTests](../../tests/Electron2D.Tests/SceneAnimationTests.cs) checks authoring, typed errors, time-aware scalar/angle curves, marker semantics, library replacement/rename/copies, forward/reverse/sections/queues, exact endpoints, multiple-loop discrete order, automatic phases/pause, target replacement, resource edits, callback failure/reentry and packing refusal. It measures zero managed allocation for 256 warmed ordinary tree passes, angle-cubic passes and discrete loop passes.

Its public host mode renders the same moving rectangle through ordinary Entity.Position setters and checks actual pixels at x=16,32,48, including clearing the old position. Two complete startup/draw/shutdown cycles pass on each Linux Wayland GPU and compatibility renderer. This is rendered execution, distinct from the headless SceneTree checks. Other platforms, human visual acceptance, disk round trips and editor workflows remain unverified.

## Weighted transitions and capture

AnimationPlayer now accepts positive custom blend durations. SetBlendTime configures a named transition; PlaybackDefaultBlendTime provides fallback, with exact, source-wildcard and destination-wildcard lookup order. Wildcard names still need registered animations for SetBlendTime validation. Zero duration switches immediately. GetBlendTime queries the explicit pair only. Outgoing clips keep their time, speed, loop/section state and independent remaining contribution; they continue sampling as the new clip advances. Finite completion clears outgoing clips and holds the current endpoint.

The pinned implementation computes the main contribution from outgoing weights before their decay and samples outgoing contributions after decay. For constant values 10→30 with duration 1 and successive quarter-second advances, outputs are 10, 16.6667, 23.3333 and approximately 30; an additional zero advance yields exactly 30. Interrupted transitions retain multiple sources and follow the same ordering. This is a source behavior audit, not a frame-rate-invariance or monotonicity claim.

Each property has a typed accumulator shared by compatible descriptors for the same target ID/name. Conflicting value types fail at preparation. Non-deterministic mode normalizes total participating weight separately for each property; absent tracks leave other properties untouched. Deterministic mode accumulates deltas against zero or an enabled first RESET key, so missing contributions use that base. Discrete Dominant preserves crossed key writes over continuous values; Recessive lets continuous output replace them; ForceContinuous samples nearest values on every update. Discrete ranges still preserve loop/section crossing order. Duplicate tracks share one weight per source instance while all their authored contributions are accumulated.

Bool and integer components round after all contributions; Int64 promotion uses decimal to preserve near-limit values. Scalar/vector/color/rectangle accumulators use reusable numeric scratch. Transform deltas compose through affine group operations. Angle interpolation unwraps around the RESET angle. Strings blend Unicode scalar codes and length, replacing invalid resulting scalar values with the standard replacement rune. Supported typed arrays blend elements, zero-pad missing elements and preserve base tails when unnormalized total weight is below one. String/array outputs are independent values and may allocate; mutable custom reference values remain caller-owned. Custom typed linear interpolation supplies accumulation for other interpolatable value types. No untyped engine value accessor is introduced.

Capture reads enabled Capture-track targets into a separate owned Animation snapshot. Getter/preparation failure does not replace a valid existing snapshot. PlayWithCapture starts a clip with explicit curve/easing; PlaybackAutoCapture does the same for ordinary Play by default. Negative duration derives the maximum first-key interval, or last-key interval when starting from the end. Custom clip speed does not change the capture clock; absolute SpeedScale does. Pause/Stop and ClearCaches cancel snapshots. Source/target rebind cancels a stale snapshot. Borrowed clips, libraries and targets are never disposed by the capture cache. Nested Advance is rejected; setters and typed postprocess hooks may change the controller, with later stale commits abandoned.

SceneAnimationBlendTests verifies numeric/array/string output, uninterrupted/interrupted fades, sparse tracks and RESET, capture/default settings, timing, discrete precedence, postprocessing target IDs, reentry/failure, owned snapshot cleanup and zero managed bytes on 256 warmed crossfade, capture and qualified-name passes. Its public host checks eight actual pixel stages for an outgoing/new pose and a captured external pose in two complete Linux Wayland GPU and two compatibility lifecycles. Other platforms, human visual acceptance, native allocation, disk round trips and editor workflows remain separate gates.
