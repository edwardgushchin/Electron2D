# AnimationNodeTimeInfo

Last updated: 2026-10-13

**Namespace:** `Electron2D` · **Declaration:** `public struct Electron2D.AnimationNodeTimeInfo` · **Source:** [AnimationNodeExtension.cs](../../src/Scene/Animation/AnimationNodeExtension.cs).

## Description

Immutable output containing all six timeline fields. Length is finite and nonnegative; Position and Delta are finite and may be signed. LoopMode must be defined. WillEnd reports a reached loop break boundary; IsInfinite reports an unpredictable end. Default values describe a zero-length nonlooping timeline.

## Example

```csharp
var timeline = new AnimationNodeTimeInfo(2, .5, .25, SpriteFrames.LoopMode.Linear);
var remaining = AnimationNodeExtension.GetRemainingTime(timeline, breakLoop: true);
```

## Member summary

| Member | Contract |
| --- | --- |
| `public AnimationNodeTimeInfo(System.Double length, System.Double position, System.Double delta, Electron2D.SpriteFrames.LoopMode loopMode = None, System.Boolean willEnd = false, System.Boolean isInfinite = false)` | .ctor |
| `public System.Double Delta { get;  }` | Delta |
| `public System.Boolean IsInfinite { get;  }` | IsInfinite |
| `public System.Double Length { get;  }` | Length |
| `public Electron2D.SpriteFrames.LoopMode LoopMode { get;  }` | LoopMode |
| `public System.Double Position { get;  }` | Position |
| `public System.Boolean WillEnd { get;  }` | WillEnd |

## Member descriptions

<a id="member-ec4efc6134b7"></a>
### .ctor

`public AnimationNodeTimeInfo(System.Double length, System.Double position, System.Double delta, Electron2D.SpriteFrames.LoopMode loopMode = None, System.Boolean willEnd = false, System.Boolean isInfinite = false)`

Creates finite typed values. Invalid nonfinite values, negative output length or undefined enums throw ArgumentOutOfRangeException.

<a id="member-9cdb1f91970a"></a>
### Delta

`public System.Double Delta { get;  }`

Gets the immutable Delta field described above.

<a id="member-9809f0f599bb"></a>
### IsInfinite

`public System.Boolean IsInfinite { get;  }`

Gets the immutable IsInfinite field described above.

<a id="member-ee49289049e3"></a>
### Length

`public System.Double Length { get;  }`

Gets the immutable Length field described above.

<a id="member-da93fa4cfcda"></a>
### LoopMode

`public Electron2D.SpriteFrames.LoopMode LoopMode { get;  }`

Gets the immutable LoopMode field described above.

<a id="member-be3c3c99de98"></a>
### Position

`public System.Double Position { get;  }`

Gets the immutable Position field described above.

<a id="member-2e1565963fba"></a>
### WillEnd

`public System.Boolean WillEnd { get;  }`

Gets the immutable WillEnd field described above.

## Verification and limits

[AnimationNodeExtensionTests](../../tests/Electron2D.Tests/AnimationNodeExtensionTests.cs) checks inputs, loops, infinite/end policies, crossing, failure and zero warmed managed allocations. [AnimationGraphPersistenceTests](../../tests/Electron2D.Tests/AnimationGraphPersistenceTests.cs) reconstructs an exact application extension factory and executes loaded graphs in a separate process. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the contract. Sectioned timeline authoring, editor UI and foreign-platform acceptance remain separate.
