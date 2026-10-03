# AnimationPlayer

Last updated: 2026-10-04

**Inherits:** [AnimationMixer](AnimationMixer.md)

- **Source:** [`src/Scene/Animation/AnimationPlayer.cs`](../../src/Scene/Animation/AnimationPlayer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class AnimationPlayer : AnimationMixer`

> Named clip playback, reverse, seek, queues, marker sections and endpoint loops.

## Description

Named clip playback, reverse, seek, queues, marker sections and endpoint loops. The implemented contract, public authoring example, ordering, errors and verification are described in [Scene animation](../components/scene-animation.md). [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed binding and lifetime decisions.

## API

The source XML describes every own declaration. Public/protected declaration accounting and applicable remaining work are maintained in [AnimationPlayer coverage](../coverage/classes/AnimationPlayer.md). Inherited members remain on the declaring base page.

## Member reference

| Declaration | Kind |
| --- | --- |
| `public event Action<String, String> AnimationChanged` | event |
| `public event Action<String> CurrentAnimationChanged` | event |
| `public String AnimationGetNext(String animationFrom)` | method |
| `public Void AnimationSetNext(String animationFrom, String animationTo)` | method |
| `public Void ClearQueue()` | method |
| `protected override Void Dispose(Boolean disposing)` | method |
| `public Double GetPlayingSpeed()` | method |
| `public AnimationMixer.AnimationCallbackModeProcess GetProcessCallback()` | method |
| `protected override Collections.Generic.IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | method |
| `public String[] GetQueue()` | method |
| `public String GetRoot()` | method |
| `public Double GetSectionEndTime()` | method |
| `public Double GetSectionStartTime()` | method |
| `public Boolean HasSection()` | method |
| `public Boolean IsAnimationActive()` | method |
| `public Boolean IsPlaying()` | method |
| `protected override Void OnNotification(Int32 what)` | method |
| `public Void Pause()` | method |
| `public Void Play(String name = "", Double customBlend = -1, Double customSpeed = 1, Boolean fromEnd = false)` | method |
| `public Void PlayBackwards(String name = "", Double customBlend = -1)` | method |
| `public Void PlaySection(String name = "", Double startTime = -1, Double endTime = -1, Double customBlend = -1, Double customSpeed = 1, Boolean fromEnd = false)` | method |
| `public Void PlaySectionBackwards(String name = "", Double startTime = -1, Double endTime = -1, Double customBlend = -1)` | method |
| `public Void PlaySectionWithMarkers(String name = "", String startMarker = "", String endMarker = "", Double customBlend = -1, Double customSpeed = 1, Boolean fromEnd = false)` | method |
| `public Void PlaySectionWithMarkersBackwards(String name = "", String startMarker = "", String endMarker = "", Double customBlend = -1)` | method |
| `public Void Queue(String name)` | method |
| `public Void ResetSection()` | method |
| `public Void Seek(Double seconds, Boolean update = false, Boolean updateOnly = false)` | method |
| `public Void SetProcessCallback(AnimationMixer.AnimationCallbackModeProcess mode)` | method |
| `public Void SetRoot(String path)` | method |
| `public Void SetSection(Double startTime = -1, Double endTime = -1)` | method |
| `public Void SetSectionWithMarkers(String startMarker = "", String endMarker = "")` | method |
| `public Void Stop(Boolean keepState = false)` | method |
| `public String AssignedAnimation { get; set; }` | property |
| `public String Autoplay { get; set; }` | property |
| `public String CurrentAnimation { get; set; }` | property |
| `public Double CurrentAnimationLength { get;  }` | property |
| `public Double CurrentAnimationPosition { get;  }` | property |
| `public Double SpeedScale { get; set; }` | property |

## Verification and limits

[SceneAnimationTests](../../tests/Electron2D.Tests/SceneAnimationTests.cs) exercises the complete current property-track playback profile and Linux Wayland GPU/compatibility readback hosts. Capture, weighted mixing, other track kinds, packed/disk persistence and other-platform acceptance remain unimplemented; the type's existence does not close those family rows.
