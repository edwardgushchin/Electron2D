# ADR 0014: Managed Resource lifetime and realtime allocation

Last updated: 2026-09-20

## Status

Accepted. Clarifies ADR 0003 and amends the Resource lifetime wording in ADR 0013.

## Context

Electron2D runs on .NET and must keep gameplay frame times predictable. Three separate concerns must not be conflated:

1. The runtime reclaims managed object memory through garbage collection.
2. `IDisposable` provides deterministic logical teardown and release of native handles, but does not free managed object memory.
3. A shared native-backed asset may need deterministic retention until its final consumer releases ownership.

Copying the reference engine's public `RefCounted` API would not remove Electron2D objects from the managed heap or prevent garbage collection. It would add a second caller-visible lifetime protocol, atomic counter traffic, cycle/error risks, and unclear interaction with `IDisposable`. Conversely, relying on garbage collection to release SDL, audio, renderer, or physics handles would make scarce native-resource lifetime nondeterministic.

## Decision

`ElectronObject` and `Resource` do not expose public manual reference counting. Managed object memory remains owned by the .NET runtime.

`IDisposable` remains the deterministic contract for logical shutdown, event/cache detachment, owned child cleanup, and release of native resources. Native handles are wrapped in `SafeHandle`-derived types so forgotten disposal has a narrowly scoped fallback; ordinary engine objects do not gain finalizers.

When the first concrete loader and native-backed asset establish real shared-ownership transitions, the Resources domain may add a resource manager with internal disposable leases. Acquiring a lease retains the manager-owned asset payload; disposing a lease releases that retention. Reaching zero leases makes the native payload eligible for deterministic release under the manager's cache policy. It does not force collection of the managed `Resource` wrapper and is not exposed as `Ref()`, `Unref()`, or a public counter on `Resource`.

No lease type or resource manager is implemented before that integration boundary. Their exact API, cache eviction rules, thread affinity, reload behavior, and failure semantics must be decided from the concrete loader/native backend rather than speculative scaffolding.

Steady-state frame, fixed-step physics, future rendering, input-dispatch, and audio-mixing hot paths must avoid managed allocations. Implementations prefer preallocated storage, value types, bounded reusable buffers, and pools where measurements show repeated allocation. Resource construction, loading, scene transitions, and tooling are not automatically allocation-free.

GC latency modes and no-GC regions are host-wide performance controls, not default engine semantics. They may be enabled only after allocation and frame-time profiling establishes a measurable need, a memory budget, recovery behavior, and target-platform validation. Electron2D does not claim hard real-time guarantees.

## Consequences

- `Resource` remains a direct `ElectronObject` subclass; its existing API and binary behavior do not change.
- Pure managed resource wrappers may outlive logical disposal until garbage collection; disposed instances remain unusable.
- Native payload release can become deterministic without pretending that managed memory was freed.
- Callers cannot corrupt object lifetime through mismatched public increment/decrement calls.
- A future asset manager owns lease counts and cache policy in one place instead of distributing counters across resources.
- Realtime performance is enforced by measured allocation budgets and hot-path tests, not by assuming either GC or manual reference counting is free.

## Rejected alternatives

- Public `RefCounted` inheritance for every resource: it does not replace managed garbage collection and creates two lifetime protocols.
- Using `Dispose` as if it freed managed memory: this is false and would produce misleading performance assumptions.
- Releasing native payloads only from garbage collection/finalization: timing is nondeterministic and can retain scarce resources too long.
- Implementing a speculative resource manager or lease API now: no loader, native-backed asset, cache policy, or ownership transition exists to validate it.
- Enabling a process-wide low-latency or no-GC mode by default: it can increase heap growth or fail under an incorrect allocation budget and must be measured per host and target.

## Verification boundary

This ADR changes architecture and documentation only; no runtime behavior is added. Existing Resource tests continue to verify deterministic logical disposal, weak path caching, graph duplication, and callback failure handling. Future resource-manager work must add lease-count, concurrent acquire/release, cache eviction, native-handle lifetime, allocation-budget, and frame-time tests before claiming completion.

## References

- [.NET `IDisposable` contract](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable)
- [.NET garbage-collector latency modes](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/latency)
- [.NET no-GC regions](https://learn.microsoft.com/en-us/dotnet/api/system.gc.trystartnogcregion)
