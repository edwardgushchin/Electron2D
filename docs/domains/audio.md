# Audio domain

Last updated: 2026-10-01

Owns audio resource decoding, independent playback state, non-spatial scene playback and output bus routing. The [audio playback component](../components/audio-playback.md) implements the first output path under [ADR 0047](../decisions/audio.md#adr-0047). AudioStream, AudioStreamPlayback, AudioStreamPlaybackResampled, AudioStreamWAV, AudioWAVImportOptions, AudioStreamPlayer and AudioServer expose engine-owned types only.

Resources provide immutable prepared PCM; playback holds cursors/history; scene nodes borrow streams and own playback/voices; AudioServer owns native output and bus configuration. The runtime embeds internal FAudio# and qoa-fu source in Electron2D.dll and ships the pinned native FAudio library sharing SDL3. Engine teardown closes output without disposing borrowed stream resources or the process singleton.

Linux x64 output and packaging are checked locally. Other platform execution, multichannel speakers and physical listening remain unverified. The remaining codec, effects, sample-driver, input, music/composite, spatial, device-switch, loader and editor dependencies are explicit in [coverage](../coverage/index.md). The completed first mixer is no longer their blanket blocker. No audio capture or batch audio-export public capability is claimed under [ADR 0090](../decisions/agent-native.md#adr-0090).
