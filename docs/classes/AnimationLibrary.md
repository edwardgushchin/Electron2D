# AnimationLibrary

Last updated: 2026-10-04

**Inherits:** [Resource](Resource.md)

- **Source:** [`src/Scene/Resources/AnimationLibrary.cs`](../../src/Scene/Resources/AnimationLibrary.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public sealed class AnimationLibrary : Resource`

> Named borrowed animation resources with replacement, rename and forwarded changes.

## Description

Named borrowed animation resources with replacement, rename and forwarded changes. The implemented contract, public authoring example, ordering, errors and verification are described in [Scene animation](../components/scene-animation.md). [ADR 0093](../decisions/scene-animation.md#adr-0093) defines the typed binding and lifetime decisions.

## API

The source XML describes every own declaration. Public/protected declaration accounting and applicable remaining work are maintained in [AnimationLibrary coverage](../coverage/classes/AnimationLibrary.md). Inherited members remain on the declaring base page.

## Member reference

| Declaration | Kind |
| --- | --- |
| `public event Action<String> AnimationAdded` | event |
| `public event Action<String> AnimationChanged` | event |
| `public event Action<String> AnimationRemoved` | event |
| `public event Action<String, String> AnimationRenamed` | event |
| `public Void AddAnimation(String name, Animation animation)` | method |
| `protected override Void CopyCustomStateTo(Resource target, Boolean deep, DeepDuplicateMode mode, Func<Resource, Resource> duplicate, Func<Resource, Resource> force)` | method |
| `protected override Resource CreateDuplicateInstance()` | method |
| `protected override Void Dispose(Boolean disposing)` | method |
| `public Animation GetAnimation(String name)` | method |
| `public String[] GetAnimationList()` | method |
| `public Int32 GetAnimationListSize()` | method |
| `public Boolean HasAnimation(String name)` | method |
| `public Void RemoveAnimation(String name)` | method |
| `public Void RenameAnimation(String name, String newName)` | method |

## Verification and limits

[SceneAnimationTests](../../tests/Electron2D.Tests/SceneAnimationTests.cs) exercises the complete current property-track playback profile and Linux Wayland GPU/compatibility readback hosts. All own library rows are implemented. Wider clip tracks, mixing/capture, packed/disk persistence, inherited Resource formats and other-platform acceptance retain their separate family states.
