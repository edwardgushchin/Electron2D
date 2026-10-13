# AnimationNodeExtension

Last updated: 2026-10-13

**Namespace:** `Electron2D` · **Declaration:** `public abstract class Electron2D.AnimationNodeExtension` · **Source:** [AnimationNodeExtension.cs](../../src/Scene/Animation/AnimationNodeExtension.cs).

## Description

Application-defined animation graph resource with a required typed processing callback. Derive this type, add it as an ordinary blend-tree child and return complete timeline output. The callback runs on the scene owner thread. The engine commits timeline state only after successful real evaluation; test-only calls suppress engine commits but cannot undo application side effects. Files require the exact application factory.

## Example

```csharp
sealed class ClipNode : AnimationNodeExtension
{
    protected override AnimationNodeTimeInfo OnProcessAnimationNode(in AnimationNodePlaybackInfo info, bool testOnly)
    {
        BlendAnimation("walk", info.Time, info.Delta, info.Seeked, info.IsExternalSeeking, 1);
        return new AnimationNodeTimeInfo(1, info.Time, info.Delta);
    }
}
```

## Member summary

| Member | Contract |
| --- | --- |
| `protected AnimationNodeExtension()` | .ctor |
| `public static System.Double GetRemainingTime(Electron2D.AnimationNodeTimeInfo nodeInfo, System.Boolean breakLoop = false)` | GetRemainingTime |
| `public static System.Boolean IsLooping(Electron2D.AnimationNodeTimeInfo nodeInfo)` | IsLooping |
| `protected abstract Electron2D.AnimationNodeTimeInfo OnProcessAnimationNode(in Electron2D.AnimationNodePlaybackInfo playbackInfo, System.Boolean testOnly)` | OnProcessAnimationNode |

## Member descriptions

<a id="member-ad20b3a5cd33"></a>
### .ctor

`protected AnimationNodeExtension()`

Initializes an application-defined graph resource.

<a id="member-fb8df799ab7a"></a>
### GetRemainingTime

`public static System.Double GetRemainingTime(Electron2D.AnimationNodeTimeInfo nodeInfo, System.Boolean breakLoop = false)`

Returns 31540000 for infinite or unbroken looping output, zero for a reported loop break boundary, otherwise Length minus Position with near-zero normalization. Overshoot may be negative. Finite subtraction overflow throws ArgumentOutOfRangeException.

<a id="member-81feebb2325e"></a>
### IsLooping

`public static System.Boolean IsLooping(Electron2D.AnimationNodeTimeInfo nodeInfo)`

Returns true when LoopMode differs from None; infinite output alone does not imply looping.

<a id="member-6f8ec3e91ec9"></a>
### OnProcessAnimationNode

`protected abstract Electron2D.AnimationNodeTimeInfo OnProcessAnimationNode(in Electron2D.AnimationNodePlaybackInfo playbackInfo, System.Boolean testOnly)`

Required application callback. Inherited blend and typed parameter helpers are available only during evaluation. Return complete timeline output. Exceptions propagate and prevent committing this node timeline; child/application side effects are not a global rollback.

## Verification and limits

[AnimationNodeExtensionTests](../../tests/Electron2D.Tests/AnimationNodeExtensionTests.cs) checks inputs, loops, infinite/end policies, crossing, failure and zero warmed managed allocations. [AnimationGraphPersistenceTests](../../tests/Electron2D.Tests/AnimationGraphPersistenceTests.cs) reconstructs an exact application extension factory and executes loaded graphs in a separate process. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the contract. Sectioned timeline authoring, editor UI and foreign-platform acceptance remain separate.
