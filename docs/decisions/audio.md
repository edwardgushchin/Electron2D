# Electron2D audio decisions

Last updated: 2026-09-23

This bounded log owns the architectural decisions for audio. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0047](#adr-0047).

<a id="adr-0047"></a>
## ADR 0047: Use FAudio over SDL3 for the audio engine

Last updated: 2026-09-23

### Status

Accepted. The audio domain and FAudio integration are not yet implemented.

### Context

Electron2D needs audio playback, bus routing and effects through an engine-owned public API. The applicable `AudioServer` contract includes sends between buses and ordered effects. SDL_mixer 3 supports playback and flat mixing groups, but its group API does not directly provide that bus graph. FAudio provides source and submix voices, sends and effect chains over SDL3.

### Decision

- Use FAudio on top of SDL3 as the native audio mixing and output backend. Model Electron2D audio buses, routing and effects using FAudio voices, submixes, sends and effect chains without exposing FAudio or SDL types to applications.
- Preserve the applicable reference audio API and behavior under [ADR 0004](product.md#adr-0004). FAudio's available operations do not by themselves prove `AudioServer`, audio stream, decoder or effect compatibility; audit their contracts and record any gaps in coverage when implementation begins.
- Compile the selected managed FAudio binding source into `Electron2D.dll` and ship pinned native FAudio and SDL dependencies through the engine project, under [ADR 0012](product.md#adr-0012). Choose and pin compressed-audio decoders with the first asset/import slice; this decision does not select SDL_mixer as the mixer or decoder.
- Establish packaging and executable behavior for every claimed platform, especially Web, Android and iOS, under [ADR 0021](product.md#adr-0021). If FAudio cannot satisfy a target, resolve its backend integration through a new explicit decision before claiming audio support there; never silently substitute different semantics.

### Consequences

- SDL3 remains the platform foundation and FAudio supplies the mix graph. Public audio types and lifecycle remain Electron2D-owned.
- This decision does not add dependencies now or claim audio playback, codec support, bus behavior or platform acceptance before an executable slice is verified.

### Rejected alternatives

- Use SDL_mixer 3 as the complete audio engine: rejected because its documented groups do not directly represent the required routed bus and effect-chain contract.
- Build a custom mixer before validating the selected backend: rejected because FAudio already supplies a mix graph suited to the required bus roles.
