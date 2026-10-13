# AnimationNodePlaybackInfo

Last updated: 2026-10-13

**Namespace:** `Electron2D` · **Declaration:** `public struct Electron2D.AnimationNodePlaybackInfo` · **Source:** [AnimationNodeExtension.cs](../../src/Scene/Animation/AnimationNodeExtension.cs).

## Description

Immutable input containing eight playback fields. Time is the requested absolute position and Delta is signed elapsed time. Ordinary graph Start is zero; End is minus one initially and the prior successfully reported length thereafter. Crossing flags describe the prior looping timeline. Weight is the signed scalar contribution before track-path filtering.

## Example

```csharp
var input = new AnimationNodePlaybackInfo(0, 0, 0, -1, true, false, Animation.LoopedFlag.None, 1);
```

## Member summary

| Member | Contract |
| --- | --- |
| `public AnimationNodePlaybackInfo(System.Double time, System.Double delta, System.Double start, System.Double end, System.Boolean seeked, System.Boolean isExternalSeeking, Electron2D.Animation.LoopedFlag loopedFlag, System.Double weight)` | .ctor |
| `public System.Double Delta { get;  }` | Delta |
| `public System.Double End { get;  }` | End |
| `public System.Boolean IsExternalSeeking { get;  }` | IsExternalSeeking |
| `public Electron2D.Animation.LoopedFlag LoopedFlag { get;  }` | LoopedFlag |
| `public System.Boolean Seeked { get;  }` | Seeked |
| `public System.Double Start { get;  }` | Start |
| `public System.Double Time { get;  }` | Time |
| `public System.Double Weight { get;  }` | Weight |

## Member descriptions

<a id="member-1b380c4f253f"></a>
### .ctor

`public AnimationNodePlaybackInfo(System.Double time, System.Double delta, System.Double start, System.Double end, System.Boolean seeked, System.Boolean isExternalSeeking, Electron2D.Animation.LoopedFlag loopedFlag, System.Double weight)`

Creates finite typed values. Invalid nonfinite values, negative output length or undefined enums throw ArgumentOutOfRangeException.

<a id="member-3a1370115669"></a>
### Delta

`public System.Double Delta { get;  }`

Gets the immutable Delta field described above.

<a id="member-9da5d9f20690"></a>
### End

`public System.Double End { get;  }`

Gets the immutable End field described above.

<a id="member-ebae62a66f6c"></a>
### IsExternalSeeking

`public System.Boolean IsExternalSeeking { get;  }`

Gets the immutable IsExternalSeeking field described above.

<a id="member-b552453625ed"></a>
### LoopedFlag

`public Electron2D.Animation.LoopedFlag LoopedFlag { get;  }`

Gets the immutable LoopedFlag field described above.

<a id="member-b109b7457227"></a>
### Seeked

`public System.Boolean Seeked { get;  }`

Gets the immutable Seeked field described above.

<a id="member-e00b3f0bb124"></a>
### Start

`public System.Double Start { get;  }`

Gets the immutable Start field described above.

<a id="member-f55f35e97c11"></a>
### Time

`public System.Double Time { get;  }`

Gets the immutable Time field described above.

<a id="member-509af52cb393"></a>
### Weight

`public System.Double Weight { get;  }`

Gets the immutable Weight field described above.

## Verification and limits

[AnimationNodeExtensionTests](../../tests/Electron2D.Tests/AnimationNodeExtensionTests.cs) checks inputs, loops, infinite/end policies, crossing, failure and zero warmed managed allocations. [AnimationGraphPersistenceTests](../../tests/Electron2D.Tests/AnimationGraphPersistenceTests.cs) reconstructs an exact application extension factory and executes loaded graphs in a separate process. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the contract. Sectioned timeline authoring, editor UI and foreign-platform acceptance remain separate.
