# Threaded resource loading

Last updated: 2026-10-10

## Executable scope

[ResourceLoader](../classes/ResourceLoader.md) now provides typed threaded requests, status/progress and blocking result collection under [ADR 0013](../decisions/resources.md#adr-0013) and [ADR 0104](../decisions/jobs.md#adr-0104). It uses the existing static retained service, formats, compiled factories/codecs and all five cache modes. CancellationToken and optional publication SceneTree are concrete ownership extensions.

[ResourceLoadGraph](../classes/ResourceLoadGraph.md) prepares fresh per-file state without publishing cache paths or refreshing existing resources. Independent external file hooks execute on multiple workers; dependency edges reject external cycles and enforce depth 64. Pending graphs permit at most 1024 files, and the service permits 128 uncollected paths. Callback/factory allocations are explicit loading work. Prepared resource-reference remapping preserves final identities, root cycles, aliases and shared parent ownership; opaque application types override the protected hook.

The scene owner or standalone consuming caller publishes ready cache state and delivers replacement Changed callbacks. Owner status polling attempts publication without blocking on the synchronous cache gate; SceneTree.Defer also publishes. Get blocks for preparation, performs owner publication and consumes one matching request. Until publication finishes status stays InProgress; terminal states report progress one. Invalid/consumed paths report InvalidResource. Identical duplicates share work; conflicts and incompatible gets reject. Every success/failure/cancellation must be collected.

Worker waits help only the selected dependency group, avoiding unrelated callbacks in loader scopes. Collection of an older root task uses active-stack/cycle checks and a non-consuming preparation wait; publication then retires its pool owner ticket. Public worker wait guards remain intact. A load callback cannot consume an unpublished threaded request, and a publication callback cannot collect its own active request; off-owner scene collection rejects before waiting.

## Ownership and cleanup

New resources remain owned by their prepared file contexts. On publication, newly created external roots have shared leases retained by every owning parent; existing cached resources remain borrowed. Compatible replacements retain source file graph leases through success or partial callback failure. Cancellation is cooperative at file-stage boundaries and before publication; blocked hooks drain before cleanup. Failure attempts every untransferred new graph cleanup and keeps borrowed resources alive. Scene instances retain loaded graphs after template disposal. Closed publication trees fail and still require collection.

Format loaders are borrowed snapshots; callers retain their hooks during pending operations. Unregister affects later snapshots. Factories/hooks execute on workers, return independent owned payload and must not mutate unrelated registered resources or scene/native state. Cache path assignment for fresh staged resources remains metadata-only until publication.

## Verification

ThreadedResourceLoaderTests covers blocking get, duplicate/concurrent collectors, status/progress, wrong result type and conflicting options, ordinary/deep cache replacement, aliases/root cycles, parallel diamond graphs and copied-parent retention, deep ignore, two simultaneous blocked format hooks, no partial cache, dependency ownership/cleanup, cancellation, failed files, external cycle rejection, opaque shader default remapping, off-owner rejection, 128 retained-path capacity/reuse, 1025 closed-owner failures without ticket exhaustion and older worker collection. Warm terminal status/progress polling repeats 1024 times with zero managed allocated bytes.

[AsyncGallery](../../examples/AsyncGallery/README.md) is built as a separate public-only consumer. A first process saves image files and a PackedScene; fresh native test processes load that authored scene. Each Linux Wayland GPU/compatibility run renders 30 frames while both real image hooks are blocked, then releases I/O, instantiates on owner and verifies red/blue pixels and cleanup at frame 40. This proves the exercised programmatic authoring/loading/rendering route, not general editor/tool acceptance.

Native/OS allocation totals, foreign target execution, AOT, threaded browser bootstrap and human acceptance remain unverified. Runtime cold setup/loading may allocate; only the named warmed polling operation has a zero-byte claim. Further resource import/pack/text/schema/editor APIs remain their own coverage obligations.
