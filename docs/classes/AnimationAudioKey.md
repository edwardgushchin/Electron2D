# AnimationAudioKey

Last updated: 2026-10-04

- **Source:** [Animation.AudioTracks.cs](../../src/Scene/Resources/Animation.AudioTracks.cs)
- **Namespace:** `Electron2D`
- **Declaration:** `public readonly struct AnimationAudioKey`

## Description

Borrowed nullable AudioStream and finite start/end trim in seconds. A receiving Animation audio track validates finite values and clamps negative trims to zero. Generic key storage/copies retain resource borrowing; deep Animation copies duplicate direct sources through the resource graph session.

## Examples

```csharp
var key = new AnimationAudioKey(sound, startOffset: .05, endOffset: .1);
// sound is an existing borrowed AudioStream.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public AnimationAudioKey(Electron2D.AudioStream stream, System.Double startOffset = 0, System.Double endOffset = 0)` | Creates typed audio key data, normalized when accepted by a track. |

## Constructor Descriptions

<a id="member-56071a09b98c"></a>
### .ctor

`public AnimationAudioKey(Electron2D.AudioStream stream, System.Double startOffset = 0, System.Double endOffset = 0)`

Creates typed audio key data, normalized when accepted by a track.

stream: Borrowed source or null for an empty cue.

startOffset: Finite start trim.

endOffset: Finite end trim.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Double EndOffset { get; set; }` | Gets the end trim. |
| `public System.Double StartOffset { get; set; }` | Gets the start trim. |
| `public Electron2D.AudioStream Stream { get; set; }` | Gets the borrowed source. |

## Property Descriptions

<a id="member-fa7a62559459"></a>
### EndOffset

`public System.Double EndOffset { get; set; }`

Gets the end trim.

Value: Nonnegative seconds after track normalization.

<a id="member-1f58dd9cb24c"></a>
### StartOffset

`public System.Double StartOffset { get; set; }`

Gets the start trim.

Value: Nonnegative seconds after track normalization.

<a id="member-0fec0e5637b5"></a>
### Stream

`public Electron2D.AudioStream Stream { get; set; }`

Gets the borrowed source.

Value: The supplied resource or null.


## Verification and limits

[Audio track tests](../../tests/Electron2D.Tests/AnimationAudioTrackTests.cs) cover typed storage, clamps, invalid input and shallow/deep copies, plus isolated native playback/hosts. [The component](../components/scene-animation.md#audio-tracks) defines transport, timing, preparation, lifetime and current verification limits. [ADR 0093](../decisions/scene-animation.md#adr-0093) owns the typed key projection.
