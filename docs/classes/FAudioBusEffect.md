# FAudioBusEffect

Last updated: 2026-10-03

**Declaration:** `internal sealed unsafe partial class Electron2D.FAudioBusEffect` · **Source:** [FAudioBusEffect.cs](../../src/Servers/Audio/FAudioBusEffect.cs) · **Component:** [Audio playback](../components/audio-playback.md).

## Description

Internal SafeHandle owning a native FAPOBase block, persistent registration/delegate state and a GCHandle-held managed processor. A public effect role prepares one attached AudioEffectInstance per output stereo pair plus shared scratch arrays; source resources remain borrowed. Internal gain and input-tap roles reuse the same native hook without public instances. Native voice references release before managed instance teardown. Cleanup attempts every instance and collects errors.

Prepared bus buffer publication mirrors each public effect and final gain. A separate input tap captures direct sources before public bus processing. Both roles share finite-PCM containment and lifetime management. The per-pair Activity state tracks source/send/read usage, recent post-gain peaks and inactivity thresholds; callbacks advance it once per quantum, and named reads can activate an unused silent pair. Exact pair PCM determines silence rather than the native partial silence hint.

Hook failures clear the whole effect output, publish silence into detector buffers and report once on the next owner frame. Further callbacks remain silent until structural recreation. The gain setter resets its separate failure state. Buffer/control edits occur under the shared native gate; active hooks use prepared storage and allocate no measured managed memory after warmup.

## Verification and limits

[AudioEffectTests](../../tests/Electron2D.Tests/AudioEffectTests.cs) checks native ordered chains, bypass, tails, ownership and failures; concrete effect checks verify PCM and allocation boundaries. [AudioCompressorTests](../../tests/Electron2D.Tests/AudioCompressorTests.cs) verifies the additional input/public/final buffer roles. This internal type exposes no application effect or backend identity; see [ADR 0047](../decisions/audio.md#adr-0047).
