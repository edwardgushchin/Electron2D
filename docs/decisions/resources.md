# Electron2D resources decisions

Last updated: 2026-09-21

This bounded log owns the complete architectural records for resources. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0013](#adr-0013), [0014](#adr-0014).

<a id="adr-0013"></a>
## ADR 0013: Managed typed Resource contract

Last updated: 2026-09-21

### Status

Accepted. Lifetime policy clarified by [ADR 0014](resources.md#adr-0014). Local-scene behavior and the narrow Resources↔Scene dependency are implemented and refined by [ADR 0023](scene.md#adr-0023).

### Context

The reference Resource API inherits reference-counted lifetime, discovers stored properties through Variant metadata, duplicates nested object graphs, participates in a global path cache, and integrates with packed scenes, render resource IDs, loaders/savers, and editor-only path-ID tables.

Electron2D already commits to typed C#, managed deterministic lifetime, C# events, one Electron2D-owned assembly, and no fictional API for absent domains. The first resource layer must preserve useful data semantics without introducing Variant, reflection-driven copying, a second lifetime protocol, or empty integration stubs.

### Decision

`Resource` inherits `ElectronObject` directly. The managed runtime owns object memory, while inherited `IDisposable` owns deterministic logical cleanup and future native-handle release. A public manual reference-count API is not reproduced. This does not prohibit an asset manager from using internal reference-counted leases for deterministic native-asset retention, as clarified by ADR 0014.

Resource base state is explicit and typed. Registered paths use a process-wide ordinal weak-reference cache. Ordinary assignment enforces one live owner, takeover is explicit, and `SetPathCache` deliberately stores an unregistered loader-oriented value.

Signals are typed synchronous events. `Changed` is the content-change channel. The obsolete setup event is retained because it has executable ordering semantics and a future packed-scene consumer; setup invokes the event before the virtual hook and aggregates dual failure.

Duplication is opt-in for every derived type through two protected hooks: construct a fresh exact-type target and copy all stored typed state. A graph session registers targets before recursion, preserving aliases and cycles. Deep policy controls ordinary nested resources; the hook also receives a force-duplicate delegate, while direct assignment expresses a never-duplicate field. The derived type controls its collection containers. Path and scene IDs are never copied. Every created target is disposed if duplication fails.

`CopyFromResource` requires exact runtime types, preserves target identity, resets non-stored state, shallow-copies stored state, and coalesces notifications. It is non-transactional for arbitrary derived state and therefore emits one change even after a failed attempt.

Invalid scene IDs throw without mutation. Automatic replacement by a random ID is rejected because silently discarding caller input is unsuitable for the typed C# boundary; `GenerateSceneUniqueId` remains explicit.

Renderer RID, packed-scene owner/setup automation, loader/saver cache modes, editor path-ID mapping, and automatic serialization discovery are deferred to their missing domains. No methods are added for them yet.

### Consequences

- Derived resource authors must write small explicit copy hooks, but adding a field cannot silently produce an incomplete duplicate.
- Graph duplication supports repeated references and cycles without runtime reflection.
- Base resource behavior is independently testable before concrete assets exist.
- The weak path cache does not extend object lifetime and remains safe when disposal is omitted.
- Future serialization must define stored-property discovery and call existing hooks rather than changing current copy semantics accidentally.
- Future packed scenes must own automatic local duplication, local-scene association, and setup timing.

ADR 0023 subsequently implemented that final consequence. Its new `Resource.GetLocalScene()` surface and Scene dependency are current behavior; the original deferral above remains the historical decision boundary of this ADR.

### Rejected alternatives

- Public manual reference counting on every resource: duplicates managed lifetime and permits contradictory ownership state. Internal asset-manager leases remain available under ADR 0014.
- Reflection over all public properties: cannot distinguish stored, computed, identity, non-stored, or ownership-sensitive state safely.
- A Variant property bag: contradicts the typed API decision.
- Constructor-only duplication with no custom copy contract: silently loses derived fields.
- Empty RID, local-scene, and editor-ID methods: would claim unavailable domains and hide missing behavior.
- Vendoring a general cloning dependency: unnecessary for the explicit resource graph contract.

### Verification

Executable checks cover path ownership and races, event timing and failures, all copy policies, aliases, cycles, invalid factories, non-transactional copy notification, cleanup rollback, and disposal. Living class/component/domain documents contain the current API matrix and deferred boundaries.

<a id="adr-0014"></a>
## ADR 0014: Managed Resource lifetime and realtime allocation

Last updated: 2026-09-20

### Status

Accepted. Clarifies ADR 0003 and amends the Resource lifetime wording in ADR 0013.

### Context

Electron2D runs on .NET and must keep gameplay frame times predictable. Three separate concerns must not be conflated:

1. The runtime reclaims managed object memory through garbage collection.
2. `IDisposable` provides deterministic logical teardown and release of native handles, but does not free managed object memory.
3. A shared native-backed asset may need deterministic retention until its final consumer releases ownership.

Copying the reference engine's public `RefCounted` API would not remove Electron2D objects from the managed heap or prevent garbage collection. It would add a second caller-visible lifetime protocol, atomic counter traffic, cycle/error risks, and unclear interaction with `IDisposable`. Conversely, relying on garbage collection to release SDL, audio, renderer, or physics handles would make scarce native-resource lifetime nondeterministic.

### Decision

`ElectronObject` and `Resource` do not expose public manual reference counting. Managed object memory remains owned by the .NET runtime.

`IDisposable` remains the deterministic contract for logical shutdown, event/cache detachment, owned child cleanup, and release of native resources. Native handles are wrapped in `SafeHandle`-derived types so forgotten disposal has a narrowly scoped fallback; ordinary engine objects do not gain finalizers.

When the first concrete loader and native-backed asset establish real shared-ownership transitions, the Resources domain may add a resource manager with internal disposable leases. Acquiring a lease retains the manager-owned asset payload; disposing a lease releases that retention. Reaching zero leases makes the native payload eligible for deterministic release under the manager's cache policy. It does not force collection of the managed `Resource` wrapper and is not exposed as `Ref()`, `Unref()`, or a public counter on `Resource`.

No lease type or resource manager is implemented before that integration boundary. Their exact API, cache eviction rules, thread affinity, reload behavior, and failure semantics must be decided from the concrete loader/native backend rather than speculative scaffolding.

Steady-state frame, fixed-step physics, future rendering, input-dispatch, and audio-mixing hot paths must avoid managed allocations. Implementations prefer preallocated storage, value types, bounded reusable buffers, and pools where measurements show repeated allocation. Resource construction, loading, scene transitions, and tooling are not automatically allocation-free.

GC latency modes and no-GC regions are host-wide performance controls, not default engine semantics. They may be enabled only after allocation and frame-time profiling establishes a measurable need, a memory budget, recovery behavior, and target-platform validation. Electron2D does not claim hard real-time guarantees.

### Consequences

- `Resource` remains a direct `ElectronObject` subclass; its existing API and binary behavior do not change.
- Pure managed resource wrappers may outlive logical disposal until garbage collection; disposed instances remain unusable.
- Native payload release can become deterministic without pretending that managed memory was freed.
- Callers cannot corrupt object lifetime through mismatched public increment/decrement calls.
- A future asset manager owns lease counts and cache policy in one place instead of distributing counters across resources.
- Realtime performance is enforced by measured allocation budgets and hot-path tests, not by assuming either GC or manual reference counting is free.

### Rejected alternatives

- Public `RefCounted` inheritance for every resource: it does not replace managed garbage collection and creates two lifetime protocols.
- Using `Dispose` as if it freed managed memory: this is false and would produce misleading performance assumptions.
- Releasing native payloads only from garbage collection/finalization: timing is nondeterministic and can retain scarce resources too long.
- Implementing a speculative resource manager or lease API now: no loader, native-backed asset, cache policy, or ownership transition exists to validate it.
- Enabling a process-wide low-latency or no-GC mode by default: it can increase heap growth or fail under an incorrect allocation budget and must be measured per host and target.

### Verification boundary

This ADR changes architecture and documentation only; no runtime behavior is added. Existing Resource tests continue to verify deterministic logical disposal, weak path caching, graph duplication, and callback failure handling. Future resource-manager work must add lease-count, concurrent acquire/release, cache eviction, native-handle lifetime, allocation-budget, and frame-time tests before claiming completion.

### References

- [.NET `IDisposable` contract](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable)
- [.NET garbage-collector latency modes](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/latency)
- [.NET no-GC regions](https://learn.microsoft.com/en-us/dotnet/api/system.gc.trystartnogcregion)
