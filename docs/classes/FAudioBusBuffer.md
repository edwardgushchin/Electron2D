# FAudioBusBuffer

Last updated: 2026-10-03

**Declaration:** `internal sealed class Electron2D.FAudioBusBuffer` · **Source:** [FAudioBusBuffer.cs](../../src/Servers/Audio/FAudioBusBuffer.cs) · **Component:** [Audio playback](../components/audio-playback.md#linked-compression-and-sidechain).

## Description

Internal prepared current-quantum stereo-pair buffers for named detection. Each bus gets one array per pair at graph preparation. Input taps initialize direct-source PCM before all public bus effects. Ordered effect hooks replace the buffer at their processing position; the final gain stage publishes post-gain PCM and accumulates active sends into later targets. A read of an unused pair activates it and supplies zero PCM. The shared native mix gate serializes writes, reads and named lookup publication. Routing rebuilds replace buffers while retaining public effect instances; closure discards lookup and native tap ownership. No application API is exposed.

## Verification and limits

[AudioCompressorTests](../../tests/Electron2D.Tests/AudioCompressorTests.cs) exercises both bus orders, missing/self/named lookup, effects/gain/sends, rename/removal and warmed native allocation. Generic effect, sample and stream checks verify integration with the existing graph. This mirrors current processing-point semantics; it is not a public capture/export service. See [ADR 0047](../decisions/audio.md#adr-0047).
