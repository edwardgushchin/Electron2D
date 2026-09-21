# ADR 0013: Managed typed Resource contract

Last updated: 2026-09-21

## Status

Accepted. Lifetime policy clarified by [ADR 0014](0014-managed-resource-lifetime.md). Local-scene behavior and the narrow Resources↔Scene dependency are implemented and refined by [ADR 0023](0023-typed-packed-scenes.md).

## Context

The reference Resource API inherits reference-counted lifetime, discovers stored properties through Variant metadata, duplicates nested object graphs, participates in a global path cache, and integrates with packed scenes, render resource IDs, loaders/savers, and editor-only path-ID tables.

Electron2D already commits to typed C#, managed deterministic lifetime, C# events, one Electron2D-owned assembly, and no fictional API for absent domains. The first resource layer must preserve useful data semantics without introducing Variant, reflection-driven copying, a second lifetime protocol, or empty integration stubs.

## Decision

`Resource` inherits `ElectronObject` directly. The managed runtime owns object memory, while inherited `IDisposable` owns deterministic logical cleanup and future native-handle release. A public manual reference-count API is not reproduced. This does not prohibit an asset manager from using internal reference-counted leases for deterministic native-asset retention, as clarified by ADR 0014.

Resource base state is explicit and typed. Registered paths use a process-wide ordinal weak-reference cache. Ordinary assignment enforces one live owner, takeover is explicit, and `SetPathCache` deliberately stores an unregistered loader-oriented value.

Signals are typed synchronous events. `Changed` is the content-change channel. The obsolete setup event is retained because it has executable ordering semantics and a future packed-scene consumer; setup invokes the event before the virtual hook and aggregates dual failure.

Duplication is opt-in for every derived type through two protected hooks: construct a fresh exact-type target and copy all stored typed state. A graph session registers targets before recursion, preserving aliases and cycles. Deep policy controls ordinary nested resources; the hook also receives a force-duplicate delegate, while direct assignment expresses a never-duplicate field. The derived type controls its collection containers. Path and scene IDs are never copied. Every created target is disposed if duplication fails.

`CopyFromResource` requires exact runtime types, preserves target identity, resets non-stored state, shallow-copies stored state, and coalesces notifications. It is non-transactional for arbitrary derived state and therefore emits one change even after a failed attempt.

Invalid scene IDs throw without mutation. Automatic replacement by a random ID is rejected because silently discarding caller input is unsuitable for the typed C# boundary; `GenerateSceneUniqueId` remains explicit.

Renderer RID, packed-scene owner/setup automation, loader/saver cache modes, editor path-ID mapping, and automatic serialization discovery are deferred to their missing domains. No methods are added for them yet.

## Consequences

- Derived resource authors must write small explicit copy hooks, but adding a field cannot silently produce an incomplete duplicate.
- Graph duplication supports repeated references and cycles without runtime reflection.
- Base resource behavior is independently testable before concrete assets exist.
- The weak path cache does not extend object lifetime and remains safe when disposal is omitted.
- Future serialization must define stored-property discovery and call existing hooks rather than changing current copy semantics accidentally.
- Future packed scenes must own automatic local duplication, local-scene association, and setup timing.

ADR 0023 subsequently implemented that final consequence. Its new `Resource.GetLocalScene()` surface and Scene dependency are current behavior; the original deferral above remains the historical decision boundary of this ADR.

## Rejected alternatives

- Public manual reference counting on every resource: duplicates managed lifetime and permits contradictory ownership state. Internal asset-manager leases remain available under ADR 0014.
- Reflection over all public properties: cannot distinguish stored, computed, identity, non-stored, or ownership-sensitive state safely.
- A Variant property bag: contradicts the typed API decision.
- Constructor-only duplication with no custom copy contract: silently loses derived fields.
- Empty RID, local-scene, and editor-ID methods: would claim unavailable domains and hide missing behavior.
- Vendoring a general cloning dependency: unnecessary for the explicit resource graph contract.

## Verification

Executable checks cover path ownership and races, event timing and failures, all copy policies, aliases, cycles, invalid factories, non-transactional copy notification, cleanup rollback, and disposal. Living class/component/domain documents contain the current API matrix and deferred boundaries.
