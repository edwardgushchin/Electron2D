# Electron2D audio decisions

Last updated: 2026-10-01

This bounded log owns the architectural decisions for audio. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0047](#adr-0047).

<a id="adr-0047"></a>
## ADR 0047: Use FAudio over SDL3 with managed audio decoders

Last updated: 2026-10-01

### Status

Accepted. The first WAV/non-spatial stream/bus output slice executes through FAudio on Linux x64; remaining capabilities are recorded in coverage.

### Context

Electron2D needs audio playback, bus routing and effects through an engine-owned public API. The applicable `AudioServer` contract includes sends between buses and ordered effects. SDL_mixer 3 supports playback and flat mixing groups, but its group API does not directly provide that bus graph. FAudio provides source and submix voices, sends and effect chains over SDL3. The reference audio import surface accepts WAV, Ogg Vorbis and MP3 sources; imported WAV samples can use PCM, IMA ADPCM or QOA storage. Decoding those sources and storage formats is separate from mixing them.

### Decision

- Use FAudio on top of SDL3 as the native audio mixing and output backend. Model Electron2D audio buses, routing and effects using FAudio voices, submixes, sends and effect chains without exposing FAudio or SDL types to applications.
- Preserve the applicable reference audio API and behavior under [ADR 0004](product.md#adr-0004). FAudio's available operations do not by themselves prove `AudioServer`, audio stream, decoder or effect compatibility; audit their contracts and record any gaps in coverage when implementation begins.
- Compile the upstream FAudio# managed binding source into `Electron2D.dll` and ship pinned native FAudio and SDL dependencies through the engine project, under [ADR 0012](product.md#adr-0012).
- Accept WAV with PCM or IEEE-float samples, Ogg Vorbis, and MP3 as audio sources. Use the existing SDL3-CS audio bindings for WAV loading, with engine-owned validation and metadata/loop handling needed by the public contract. Use [NVorbis](https://github.com/NVorbis/NVorbis) for Ogg Vorbis and [NLayer](https://github.com/naudio/NLayer) for MP3 decoding. Do not require NAudio to use NLayer.
- Support PCM, IMA ADPCM and QOA as the WAV stream's internal sample formats. Use the [qoa-fu](https://github.com/pfusik/qoa-fu) C# translation for QOA encoding and decoding; implement the small IMA ADPCM codec in engine-owned C#. FAudio's MS ADPCM support is a different codec and does not implement IMA ADPCM. QOA and IMA ADPCM are import/storage choices, not additional accepted source-file formats.
- Compile the selected managed decoder sources into `Electron2D.dll` as internal types. Feed their decoded PCM to FAudio in bounded buffers; retain the source format, seek/loop behavior and resource ownership required by the applicable public audio API. No native decoder library or per-codec P/Invoke layer is selected. Pin source revisions, retain licenses and verify interoperability, corrupt-input handling, playback and target packaging when the executable audio/import slice is integrated. This decision does not select SDL_mixer as the mixer or decoder.
- Establish packaging and executable behavior for every claimed platform, especially Web, Android and iOS, under [ADR 0021](product.md#adr-0021). If FAudio cannot satisfy a target, resolve its backend integration through a new explicit decision before claiming audio support there; never silently substitute different semantics.

### Consequences

- SDL3 remains the platform foundation and FAudio supplies the mix graph. Public audio types and lifecycle remain Electron2D-owned; audio decoding adds no further native deployment library.
- The managed decoder source and FAudio# binding join the single engine assembly only with an executable slice. FAudio 26.09 and qoa-fu are now pinned/internal in the first executable output slice; NVorbis and NLayer remain selected for subsequent codec slices. Current behavior and native/platform/physical verification limits are documented in the [audio component](../components/audio-playback.md).

### Rejected alternatives

- Use SDL_mixer 3 as the complete audio engine: rejected because its documented groups do not directly represent the required routed bus and effect-chain contract.
- Use SDL_mixer solely for decoding: rejected because the selected managed decoders cover the required source formats without a second native audio extension.
- Bind native libogg/libvorbis, dr_mp3 and qoa.h separately: rejected because managed implementations cover the selected formats without additional native packaging and per-codec interop.
- Build a custom mixer before validating the selected backend: rejected because FAudio already supplies a mix graph suited to the required bus roles.
