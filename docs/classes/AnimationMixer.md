# AnimationMixer

Last updated: 2026-10-04

**Inherits:** [Node](Node.md)

- **Source:** [`src/Scene/Animation/AnimationMixer.cs`](../../src/Scene/Animation/AnimationMixer.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public class AnimationMixer : Node`

> Scene animation namespaces, typed target caches and idle/physics/manual scheduling.

## Description

Scene animation namespaces, typed target caches and idle/physics/manual scheduling. The implemented contract, public authoring example, ordering, errors and verification are described in [Scene animation](../components/scene-animation.md). [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed binding and lifetime decisions.

## API

The source XML describes every own declaration. Public/protected declaration accounting and applicable remaining work are maintained in [AnimationMixer coverage](../coverage/classes/AnimationMixer.md). Inherited members remain on the declaring base page.

## Member reference

| Declaration | Kind |
| --- | --- |
| `public event Action<String> AnimationFinished` | event |
| `public event Action AnimationLibrariesUpdated` | event |
| `public event Action AnimationListChanged` | event |
| `public event Action<String> AnimationStarted` | event |
| `public event Action CachesCleared` | event |
| `public Void AddAnimationLibrary(String name, AnimationLibrary library)` | method |
| `public Void Advance(Double delta)` | method |
| `public Void Capture(String name, Double duration, Tween.TransitionType transitionType = Linear, Tween.EaseType easeType = In)` | method |
| `public Void ClearCaches()` | method |
| `protected override Void Dispose(Boolean disposing)` | method |
| `public String FindAnimation(Animation animation)` | method |
| `public String FindAnimationLibrary(Animation animation)` | method |
| `public Animation GetAnimation(String name)` | method |
| `public AnimationLibrary GetAnimationLibrary(String name)` | method |
| `public String[] GetAnimationLibraryList()` | method |
| `public String[] GetAnimationList()` | method |
| `protected override Collections.Generic.IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | method |
| `public Boolean HasAnimation(String name)` | method |
| `public Boolean HasAnimationLibrary(String name)` | method |
| `protected override Void OnNotification(Int32 what)` | method |
| `protected virtual TValue OnPostProcessKeyValue<TValue>(Animation animation, Int32 track, TValue value, UInt64 objectID, Int32 objectSubIndex = -1)` | method |
| `public Void RemoveAnimationLibrary(String name)` | method |
| `public Void RenameAnimationLibrary(String name, String newName)` | method |
| `public Boolean Active { get; set; }` | property |
| `public AnimationMixer.AnimationCallbackModeDiscrete CallbackModeDiscrete { get; set; }` | property |
| `public AnimationMixer.AnimationCallbackModeProcess CallbackModeProcess { get; set; }` | property |
| `public Boolean Deterministic { get; set; }` | property |
| `public String RootNode { get; set; }` | property |

## Verification and limits

[SceneAnimationTests](../../tests/Electron2D.Tests/SceneAnimationTests.cs) exercises the complete current property-track playback profile and Linux Wayland GPU/compatibility readback hosts. Other track kinds, packed/disk persistence and other-platform acceptance remain unimplemented; the type's existence does not close those family rows.

Typed weighted transitions, capture, RESET and postprocess behavior are detailed in [the component](../components/scene-animation.md#weighted-transitions-and-capture) and tested by [SceneAnimationBlendTests](../../tests/Electron2D.Tests/SceneAnimationBlendTests.cs).

[Animation graph execution](../components/scene-animation.md#animation-graphs) supplies per-track weights through AnimationTree. The public Animation.LoopedFlag domain is consumed by BlendAnimation; event effects remain tied to unfinished non-property schedulers.
