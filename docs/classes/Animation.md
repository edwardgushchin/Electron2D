# Animation

Last updated: 2026-10-04

**Inherits:** [Resource](Resource.md)

- **Source:** [`src/Scene/Resources/Animation.cs`](../../src/Scene/Resources/Animation.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class Animation : Resource`

> Reusable typed property-key timelines, timing, interpolation and markers.

## Description

Reusable typed property-key timelines, timing, interpolation and markers. The implemented contract, public authoring example, ordering, errors and verification are described in [Scene animation](../components/scene-animation.md). [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed binding and lifetime decisions.

## API

The source XML describes every own declaration. Public/protected declaration accounting and applicable remaining work are maintained in [Animation coverage](../coverage/classes/Animation.md). Inherited members remain on the declaring base page.

## Member reference

| Declaration | Kind |
| --- | --- |
| `public Void AddMarker(String name, Double time)` | method |
| `public Int32 AddTrack<TOwner, TValue>(PropertyDescriptor<TOwner, TValue> property, Int32 atPosition = -1, Func<TValue, TValue, Double, TValue> interpolate = null)` | method |
| `public Void Clear()` | method |
| `protected override Void CopyCustomStateTo(Resource target, Boolean deep, DeepDuplicateMode mode, Func<Resource, Resource> duplicate, Func<Resource, Resource> force)` | method |
| `public Void CopyTrack(Int32 track, Animation toAnimation)` | method |
| `protected override Resource CreateDuplicateInstance()` | method |
| `protected override Void Dispose(Boolean disposing)` | method |
| `public Int32 FindTrack(String path, Animation.TrackType type = Value)` | method |
| `public String GetMarkerAtTime(Double time)` | method |
| `public Color GetMarkerColor(String name)` | method |
| `public String[] GetMarkerNames()` | method |
| `public Double GetMarkerTime(String name)` | method |
| `public String GetNextMarker(Double time)` | method |
| `public String GetPrevMarker(Double time)` | method |
| `protected override Collections.Generic.IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | method |
| `public Int32 GetTrackCount()` | method |
| `public Boolean HasMarker(String name)` | method |
| `public Void RemoveMarker(String name)` | method |
| `public Void RemoveTrack(Int32 track)` | method |
| `public Void SetMarkerColor(String name, Color color)` | method |
| `public Int32 TrackFindKey(Int32 track, Double time, Animation.FindMode findMode = Approx, Boolean limit = false, Boolean backward = false)` | method |
| `public Boolean TrackGetInterpolationLoopWrap(Int32 track)` | method |
| `public Animation.InterpolationType TrackGetInterpolationType(Int32 track)` | method |
| `public Int32 TrackGetKeyCount(Int32 track)` | method |
| `public Double TrackGetKeyTime(Int32 track, Int32 key)` | method |
| `public Double TrackGetKeyTransition(Int32 track, Int32 key)` | method |
| `public TValue TrackGetKeyValue<TValue>(Int32 track, Int32 key)` | method |
| `public String TrackGetPath(Int32 track)` | method |
| `public Animation.TrackType TrackGetType(Int32 track)` | method |
| `public Int32 TrackInsertKey<TValue>(Int32 track, Double time, TValue value, Double transition = 1)` | method |
| `public Boolean TrackIsCompressed(Int32 track)` | method |
| `public Boolean TrackIsEnabled(Int32 track)` | method |
| `public Boolean TrackIsImported(Int32 track)` | method |
| `public Void TrackMoveDown(Int32 track)` | method |
| `public Void TrackMoveTo(Int32 track, Int32 toIndex)` | method |
| `public Void TrackMoveUp(Int32 track)` | method |
| `public Void TrackRemoveKey(Int32 track, Int32 key)` | method |
| `public Void TrackRemoveKeyAtTime(Int32 track, Double time)` | method |
| `public Void TrackSetEnabled(Int32 track, Boolean enabled)` | method |
| `public Void TrackSetImported(Int32 track, Boolean imported)` | method |
| `public Void TrackSetInterpolationLoopWrap(Int32 track, Boolean interpolation)` | method |
| `public Void TrackSetInterpolationType(Int32 track, Animation.InterpolationType interpolation)` | method |
| `public Void TrackSetKeyTime(Int32 track, Int32 key, Double time)` | method |
| `public Void TrackSetKeyTransition(Int32 track, Int32 key, Double transition)` | method |
| `public Void TrackSetKeyValue<TValue>(Int32 track, Int32 key, TValue value)` | method |
| `public Void TrackSetPath(Int32 track, String path)` | method |
| `public Void TrackSwap(Int32 track, Int32 withTrack)` | method |
| `public Animation.UpdateMode ValueTrackGetUpdateMode(Int32 track)` | method |
| `public TValue ValueTrackInterpolate<TValue>(Int32 track, Double time, Boolean backward = false)` | method |
| `public Void ValueTrackSetUpdateMode(Int32 track, Animation.UpdateMode mode)` | method |
| `public Boolean CaptureIncluded { get;  }` | property |
| `public Double Length { get; set; }` | property |
| `public SpriteFrames.LoopMode LoopMode { get; set; }` | property |
| `public Double Step { get; set; }` | property |

## Verification and limits

[SceneAnimationTests](../../tests/Electron2D.Tests/SceneAnimationTests.cs) exercises the complete current property-track playback profile and Linux Wayland GPU/compatibility readback hosts. Other track kinds, packed/disk persistence and other-platform acceptance remain unimplemented; the type's existence does not close those family rows.

Typed weighted transitions, capture, RESET and postprocess behavior are detailed in [the component](../components/scene-animation.md#weighted-transitions-and-capture) and tested by [SceneAnimationBlendTests](../../tests/Electron2D.Tests/SceneAnimationBlendTests.cs).
