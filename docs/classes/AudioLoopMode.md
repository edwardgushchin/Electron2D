# AudioLoopMode

Last updated: 2026-10-03

**Declaration:** `public enum Electron2D.AudioLoopMode` · **Source:** [AudioLoopMode.cs](../../src/Scene/Resources/AudioLoopMode.cs) · **Component:** [Audio playback](../components/audio-playback.md).

Shared sample traversal policy for WAV metadata and AudioSample.

## Description

One shared type carries WAV and AudioSample traversal meanings under ADR 0051. Values retain 0–3. Loop bounds use source frames; native snapshots have an exclusive end, while streamed WAV retains its documented inclusive batch boundary. Undefined selectors reject.

## API summary

| Full signature | Contract |
| --- | --- |
| `public const Electron2D.AudioLoopMode Backward = 3` | Traverse the loop backward. Numeric value: 3. |
| `public const Electron2D.AudioLoopMode Disabled = 0` | Play once. Numeric value: 0. |
| `public const Electron2D.AudioLoopMode Forward = 1` | Wrap forward at the loop boundary. Numeric value: 1. |
| `public const Electron2D.AudioLoopMode PingPong = 2` | Reflect direction at both loop boundaries. Numeric value: 2. |

## Enumeration Descriptions

### Disabled

Finite playback without repetition. Native snapshots ignore loop bounds in this mode.

### Forward

Traverse forward and wrap from the end to the beginning of the enabled loop interval.

### PingPong

Reflect traversal at both ends. Native preparation includes each endpoint once per reflected turn.

### Backward

Native preparation retains the forward prefix before the loop and reverses the loop interval. Streamed WAV follows its separate cursor/traversal contract.

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.
