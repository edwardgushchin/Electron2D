# Runtime diagnostics

[Object lifetime monitors](object-lifetime-monitors.md) add live object, cached-resource, attached-node and orphan-node populations without owning targets, forcing collection or opening a native device.

Last updated: 2026-10-29

[Frame timing monitors](frame-monitors.md) add four actual Engine producers: FPS and per-window peak process, physics-step and navigation durations. They retain independent cached reads and explicit reset, error, presentation and direct-host boundaries.

## Direct native allocation ownership

[The internal allocation ledger](../coverage/performance-producers.md#direct-owned-native-storage) uses a pure-C core in the existing private native bundle, shared with the source-instrumented NativeAOT host. Loading this core opens no device or network service. It records requested CPU native payload bytes at successful alloc/resize/free boundaries for the runtime's direct allocations and FAudio allocator callbacks. Failed reallocations preserve the old block and counters; transient allocations contribute to the process peak even without an intervening read. Native callback allocation failures return null. Managed heap and remaining native producer composition are still open, so this source does not expose or certify MemoryStatic/MemoryStaticMax.

## Deferred message memory

MemoryMessageBufferMax (identity 6) is the process-lifetime peak sum of reserved
SceneTree deferred-message storage in bytes. Both ordinary enqueue/captured arrays,
all processing-group message arrays and pooled typed invocation objects with their
cached delegates contribute across live independent and Engine-owned trees.
Draining retains prepared storage and the published peak. Retiring a group releases
its empty queue array; its typed slots stay available to the tree. Disposal or actual
collection removes a tree from subsequent totals. The peak never resets on Engine
attachment, drain, teardown or collection.

Growth brackets only the actual queue allocation or typed invocation constructor
with GC.GetAllocatedBytesForCurrentThread. A replacement array replaces its prior
contribution; this is reserved live capacity rather than cumulative allocation or
pending-message count. Measured managed headers/alignment are included. Caller
callbacks and referenced payload graphs, dictionary/free-list bookkeeping, deletion
requests and unrelated engine queues are excluded. No object-layout constants,
GC heap estimate or process RSS stand in for the producer. The existing weak
object census serializes growth accounting without taking another tree's work gate.
Queries read one cached atomic integer, allocate nothing after preparation and
never collect, initialize a device or invoke a callback.

This adapts the pinned [retained message pages](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/core/object/message_queue.cpp)
to managed scene-owned queues; it does not establish total current/peak memory.
[DeferredMemoryContract](../../tests/Portability/DeferredMemoryContract.cs), selected
by public `--deferred-memory` or root `ELECTRON2D_TEST_DEFERRED_MEMORY=1`, checks
actual first/replacement allocation sizes, both buffers, typed/group capacity,
worker delivery through callback failures, retirement and disposed/collected trees,
independent live-tree aggregation, inline 2 KiB payloads, exclusion of a caller-owned
1 MiB graph and 256 prepared enqueue/drain/read cycles with zero owner/all-thread
managed bytes. Linux CLR root/public and strict NativeAOT public profiles pass the
same contract with a 262,192-byte peak; the seven CLR and two NativeAOT
profiles retain deferred delivery and existing diagnostics. Source/PDB proofs
cover the complete runtime inputs and actual test consumers. See the local
[validation receipt](../../bin/deferred-memory/2026-10-27/validation.json).
Foreign-platform and total native-memory acceptance remain separate.

## Canvas pipeline diagnostics

[Canvas pipeline compilation](canvas-counters.md#canvas-pipeline-compilation)
adds the session-cumulative successful canvas graphics-pipeline creation count,
independently of completed-frame statistics and their publication window.
Performance and RenderingServer share the same atomic producer, including
initialization, cache hits/retirement, error and teardown semantics. Other source
categories are pinned 3D scene-renderer sources excluded by ADR 0004; current/peak runtime allocation memory remains open. See the [producer audit](../coverage/performance-producers.md).

## Scope and owned types

[Performance](../classes/Performance.md) is the retained process-wide diagnostics service under [ADR 0095](../decisions/singleton-services.md#adr-0095). Its existing built-in [physics statistics](physics-statistics.md) read completed producer snapshots. Custom monitors supply application-defined values through direct typed callbacks; [Performance.CustomMonitor](../classes/Performance.CustomMonitor.Generic.md) identifies one registration and [MonitorType](../classes/Performance.MonitorType.md) describes presentation units.

## Server monitor snapshots

Performance.Monitor retains all ten aggregate navigation identities 24–33 and
all ten planar identities 39–48. Both groups read the same committed
NavigationServer.ProcessInfo counters in this planar engine. Reads acquire the
existing server gate, never synchronize, schedule avoidance or flush staged
changes. Geometry, membership, disabled regions, active maps and retirement use
exactly the producer's semantics under [ADR 0097](../decisions/navigation.md#adr-0097).
Disabled regions and their authored geometry remain counted until their geometry
or active-map membership changes. Each read is a nearby snapshot; several reads
are not an atomic group under concurrent synchronization.

AudioOutputLatency retains identity 23 and returns seconds from the active
AudioServer context's last valid output-buffer sample. Reading it does not create
the lazy audio service, claim its configuration owner, open an output device or
call a native audio function. It returns zero before output initialization, after
completed closure and for an active offline recording graph. Output creation and
retirement publish the context reference with release/acquire semantics; latency
uses its existing atomic floating-point storage. A reader racing replacement or
closure can observe the preceding valid sample or zero. It never dereferences a
retired native handle. AudioServer.GetOutputLatency keeps its separate owner-only
explicit preparation behavior. Device changes and the existing mix callback update the native producer; the
monitor introduces no periodic sampler or stale-value cache of its own.

Both families permit prepared reads on any thread with no managed allocation.
First use may initialize managed service metadata; it opens no native device.
These selectors do not complete current/peak runtime allocation memory. The other pinned pipeline categories belong to excluded 3D scene producers, as traced in the [producer audit](../coverage/performance-producers.md). Native render memory has its own [executing producer](render-memory.md). Performance/GetMonitor and
its enum remain Partial in the full declaration audit.

The source reference is pinned [Performance.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Performance.xml).
The public-only [ServerMonitorContract](../../tests/Portability/ServerMonitorContract.cs)
checks exact identities and nonzero topology/member counts, staged publication,
disabled geometry, map activity/retirement, worker reads, undefined selectors and
256 prepared batches with zero owner/all-thread managed bytes. It runs through
both the root suite and standalone portability consumer (`--server-monitors`).
`--server-monitors-audio` adds a public native window/audio lifecycle with a silent
loop, actual positive output-buffer latency, a paused-producer worker comparison,
256 zero-owner-byte reads and zero after Engine teardown.
[AudioOutputTests](../../tests/Electron2D.Tests/AudioOutputTests.cs) additionally
checks real native output-buffer data, selected-device changes, exact worker
reads while the mix producer is paused, repeated closure/reopen, a concurrent diagnostic reader during teardown,
offline recording and warmed cached reads. Native profiles must identify the
actual driver; a dummy-output run does not establish physical-device latency or
foreign-platform acceptance.

The Linux CLR and strict NativeAOT public profiles pass for both headless
navigation reads and active dummy-output audio. The final eight-profile cohort
also includes root audio teardown/offline checks plus frame/custom-monitor
regressions. Source/PDB fingerprints match the authored C# sources. Receipts are
retained under `bin/server-monitors/2026-10-27/validation.json`; native/compiler
inputs and explicit allocation boundaries remain separate from hardware/native
heap qualification. Physics declaration coverage remains 1358/1369 (99.20%),
with 11 open; 21 newly implemented selector rows are in the broader API audit.

## Runtime flow

AddCustomMonitor accepts an exact ordinal ID, a compiled Func returning a known value type and unit metadata. Typed arguments are captured by ordinary C# closures or the explicit argument overload; tuple arguments cover multiple values. Registration allocates and retains the provider without invoking it. Empty IDs and arbitrary slash-separated IDs retain literal identity; category presentation belongs to a display client.

GetCustomMonitor validates the exact registration handle under the service lock, copies its provider and invokes outside the lock on the calling thread. Results preserve numeric precision and reference identity; the service does not cache, box or convert them. This raw read contract is distinct from a future numeric display's nonnegative/clamping/formatting policy. Quantity describes integer-style presentation, Memory expects bytes, Time expects seconds for millisecond presentation, and Percentage expects a fractional ratio such as 0.5 for 50 percent. Metadata does not transform the returned value.

A thread-local identity set rejects direct and indirect cycles on one thread. Nested acyclic reads and concurrent readers remain valid. Provider exceptions propagate and release the guard. First reads and deeper nesting may allocate guard capacity; warmed reads of prepared depth allocate no engine-managed memory. Provider allocations remain application-owned.

RemoveCustomMonitor detaches the provider and captured arguments without disposing them or waiting for an in-flight read. A read that already copied its provider may finish. Stale handles reject even if the same ID is registered again. Caller-held handles retain only ID/unit metadata after removal. Application code must unregister providers whose captures have shorter lifetimes than the process; the engine does not infer delegate receiver lifetime or synchronize application state.

## Registry invariants

IDs are ordinal and unique; undefined units, null providers/IDs and duplicate registrations reject before mutation. Missing removal or stale reads throw typed exceptions. Names and types are independent copied snapshots in registration order; removing/readding an ID moves it to the end. Each query is atomic, but separate queries need application synchronization when their alignment must survive concurrent mutation.

Modification time starts at zero and records successful add/remove ticks in microseconds relative to initialization of the retained diagnostics registry. It is nondecreasing, survives native session changes and is available in headless use. Reads and failures do not change it; multiple mutations can share one microsecond, so it is not a unique revision counter. Wall-clock changes do not affect it.

## Verification and limits

[CustomMonitorTests](../../tests/Electron2D.Tests/CustomMonitorTests.cs) exercises exact Int64 and raw typed/reference values, capture arguments, units, order, copied containers, modification ticks, invalid input, stale handles, failure recovery, recursive and nested reads, in-flight removal/replacement, self-removal, captured receiver collection and zero warmed managed bytes. The public native host reads live scene position monitors and checks actual pixels and permanent service/registry identity across Linux Wayland GPU and compatibility sessions.

Other built-in producers remain separate coverage dependencies. No debugger chart UI, persistence or foreign-platform execution is supplied by the custom-monitor API. Native/driver allocations and human acceptance remain unverified.

## Native render allocation memory

See [native render memory](render-memory.md) for actual allocation regions, retained physical reservation, shared compute/physics leases, compatibility ownership and explicit native-adapter limits. Performance.GetMonitor retains its separate current/peak runtime-allocation gaps; the [producer audit](../coverage/performance-producers.md) distinguishes excluded 3D compilation sources.
