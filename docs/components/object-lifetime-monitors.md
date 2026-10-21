# Object lifetime monitors

Last updated: 2026-10-21

[Performance](../classes/Performance.md) exposes four on-demand selectors under [ADR 0016](../decisions/core-object-runtime.md#adr-0016). [ObjectCensus](../classes/ObjectCensus.md) owns a process-wide weak registration list; ElectronObject construction shares its weak identity with existing engine borrowers. Resource supplies the existing synchronized path cache. No service instance, scene owner, renderer or finalizer is required to read counts.

| Selector | Value | Counted population |
| --- | ---: | --- |
| ObjectCount | 7 | Reachable ElectronObject identities before committed disposal, including retained services and compiled subclasses |
| ObjectResourceCount | 8 | Live owners of registered nonempty resource paths; unregistered logical aliases and uncached resources are excluded |
| ObjectNodeCount | 9 | Live nodes inside Engine.MainLoop when it is a SceneTree, including its root; zero otherwise |
| ObjectOrphanNodeCount | 10 | Live nodes outside that attached tree; detached subtrees and independent SceneTrees are included |

## Lifetime and concurrency

Registration occurs in the base constructor before derived construction and preserves invocation-local factory provenance. It allocates one shared weak identity and amortized list storage. An object whose constructor fails remains observable only while reachable, unless the engine's failed-construction helper commits its terminal state. Ordinary collection clears abandoned objects and cyclic graphs; the registry neither owns them nor invokes Dispose. Explicit committed disposal excludes retained objects immediately. Queued deletion and a pending/canceled pre-delete attempt remain counted. Cleanup or subscriber errors cannot restore a terminal object. Resource takeover and path movement count the current registered owner once.

Reads scan weak entries under the registration lock, or cache entries under the existing path lock. They invoke no user code, force no GC, allocate no managed memory, and do not touch a native device. Dead/committed registrations are compacted during scans and periodically after a number of constructions proportional to retained entries, with a minimum interval of 64. Storage follows the historical live peak plus the bounded compaction interval rather than total process construction. This is a diagnostic O(n) scan, not a constant-time frame counter. Registry capacity is retained for reuse. Normal frames do not scan unless an application asks for a count.

Workers may read all selectors. Tree membership is published through a volatile reference; queries do not traverse a mutable hierarchy. Each scan captures the attached tree identity once. Concurrent construction, collection, disposal and membership transitions can change the observed population during a scan; multiple selectors do not form an atomic snapshot. Counts describe the Engine attachment, not every independently driven scene. ObjectOrphanNodeCount is useful in both Debug and Release; the separate debug-only Node.GetOrphanNodeIDs/PrintOrphanNodes APIs preserve their existing detached-node semantics.

```csharp
double before = Performance.GetMonitor(Performance.Monitor.ObjectCount);
var resource = new Resource { ResourcePath = "local://game/diagnostic" };
double cached = Performance.GetMonitor(Performance.Monitor.ObjectResourceCount);
resource.Dispose();
double after = Performance.GetMonitor(Performance.Monitor.ObjectCount);
```

The path must be available. before/after may differ if another thread or collection changes the population. The values are object counts, not managed heap or native allocation bytes.

## Verification and limits

[ObjectCensusChecks](../../tests/ObjectCensus.Consumer/ObjectCensusChecks.cs) is shared with the root test executable and a public-only consumer. It checks exact disposal, cancellation, throwing cleanup, failed construction, weak cyclic graph collection, resource cache registration/alias/takeover/move/disposal/collection, independent and Engine-attached trees, detachment, deferred deletion, worker reads, concurrent construction/retirement and zero warmed owner managed bytes. The native profile counts a real drawing node, verifies framebuffer pixels, observes detachment and checks teardown. Strict NativeAOT and software display/backend results belong to the slice evidence; full-suite, hardware GPU, foreign platforms, native allocation totals and human acceptance remain separate.

MemoryStatic, MemoryStaticMax and MemoryMessageBufferMax require coherent managed/native allocation and actual deferred-message buffer instrumentation. GC heap size or process RSS alone cannot supply those semantics. Audio latency retains its separate existing producer-adapter trigger. No unsupported constant-return selector is added.
