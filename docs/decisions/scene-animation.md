# Scene animation decisions

Last updated: 2026-10-04

<a id="adr-0093"></a>
## ADR 0093: Typed reusable scene animation

- Status: Accepted
- Date: 2026-10-04
- Depends on: [0001: C# typed API](product.md#adr-0001), [0002: typed events](product.md#adr-0002), [0005: property descriptors](core-object-runtime.md#adr-0005), [0008: node roles](scene.md#adr-0008), [0014: resource lifetime and copying](resources.md#adr-0014), [0037: typed interpolation](scene.md#adr-0037), [0051: enum identity](product.md#adr-0051)

### Context

Runtime tweens execute one-off instructions. Games also need reusable named clips, authored keys, relative scene targets, repeatable seeking and playback sections. Arbitrary property strings or a general value container would conflict with the typed property decision. A clip must work on multiple node instances without storing a target instance in the asset.

### Decision

- `Animation : Resource` stores heterogeneous track metadata, with every key container and evaluator implemented as a concrete generic value type. `AddTrack<TOwner,TValue>` requires a writable `PropertyDescriptor<TOwner,TValue>`. Its optional typed interpolator uses the same role as the tween interpolator; no reflection property setter, dynamic dictionary or universal value accessor is introduced.
- A track's relative path resolves its node from the mixer's root. An optional property suffix must equal the descriptor name. Descriptors are immutable and shared by copies. Values are read and written through generic APIs; mismatched key types fail explicitly. Node-role substitution applies to the descriptor owner, not merely to names.
- `AnimationLibrary : Resource` borrows named animations and detaches observers on replace/remove/dispose. Names use ordinal comparison. Resource duplication copies containers; deep copying passes nested resource keys and library members through the resource graph session. Managed resources and nodes retain their existing owner-thread authoring rules.
- `AnimationMixer : Node` owns library namespaces, root-path binding caches and automatic idle/physics/manual selection. `AnimationPlayer : AnimationMixer` owns clip selection, time, queue, speed, sections and completion. Node's internal callbacks provide automatic advancement and inherited pause/lifetime policy. No second clock or SceneTree registry is added.
- Track/resource changes, tree mutation, root changes, selection and explicit cache clearing invalidate the typed target bindings. Evaluation snapshots the current binding array and abandons that pass when a callback changes its resource or selection. Disposed/queued targets do not receive values. A setter failure pauses the unchanged playback and propagates through existing Node/SceneTree error handling.
- Endpoint loops share `SpriteFrames.LoopMode`, whose meanings and valid values already match this domain. The existing two-value `ProcessPhase` stays unchanged: animation's three-value callback domain is distinct because it admits Manual.
- Failure-return methods use exceptions consistently with existing typed resources. Mutation commits before observer notification. Library replacement emits removed then added, with observers seeing committed replacement state; reentrant removal cannot leak a subscription.
- The first executable profile is value-property keys and single-clip playback: continuous/nearest/discrete interpolation, float/double angular curves, time-aware typed cubic curves, named markers, queues, reverse, sections and automatic phases. Discrete evaluation writes every crossed key in direction order, including repeated loop seams; bindings initialize their sampled value after construction or seek. Positive crossfade requests fail before mutation. Full multi-source accumulation/capture, method/Bézier/audio/nested-animation tracks, root-motion extraction, compression/optimization and persistence remain applicable follow-up slices, with exact current states in coverage. Three-dimensional transform and blend-shape tracks follow the 2D exclusion policy.
- Animation nodes have no empty packed-scene factory: packing them fails explicitly until library/track persistence and constructor state restoration exist. In-memory authoring and execution are independent of disk format, editor tooling or agent file-load acceptance.

### Consequences

A game can author a clip against a typed property, attach it by library name and execute it in a real rendered scene. Warmed steady playback avoids per-frame generic value boxing or path parsing. Editing/rebinding and queued transitions may allocate. Existing rendering consumes the ordinary setters and needs no animation-specific backend path. Linux host/readback checks establish rendered execution; other platforms, editor/disk round trips and human visual acceptance remain separate gates.
