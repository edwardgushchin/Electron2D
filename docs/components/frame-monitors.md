# Frame timing monitors

Last updated: 2026-10-18

## Slice selection

| Candidate | User API and dependent work | Current feasibility |
| --- | --- | --- |
| Inherited scenes | Actor/level variants, base-state inspection and editable scene overlays | Deferred: an editor-owned operation must assign inherited origin separately from the MainInherited loading policy. The pinned PackedScene wrapper records instance state for that mode, not inherited origin. Public-only editor consumers currently have no such authoring producer. Base-state/override/deletion records enter that producer's first executable slice. Do not reinterpret the mode as the missing operation. |
| Node duplication | Eight missing declarations supply live subtree copying and signal/group/script policies | Deferred: the Scripts flag needs live Script owner association and initialization under ADR 0091; the Script asset/catalog already exists; a packed snapshot cannot supply it. |
| Frame timing monitors | Four absent selectors connect actual execution to public diagnostics and future profiling tools | Selected: existing Engine cadence, window presentation and navigation owners supply the complete observable path. |

## Scope and public API

Performance.Monitor adds TimeFPS=0, TimeProcess=1, TimePhysicsProcess=2 and TimeNavigationProcess=3. GetMonitor retains its static access and existing physical/custom monitor families. It reads completed cached values on any thread without starting a native session, synchronizing a device, invoking a callback or allocating managed memory.

TimeFPS reads Engine.FramesPerSecond. Successful Engine frames contribute their unscaled host elapsed seconds, including fixed movie cadence where selected by the host. The other values are wall-clock durations in seconds, measured with Stopwatch rather than scaled simulation delta. Once a completed sampling window reaches one unscaled host second, it replaces each published value with that window's largest completed observation. Fractional elapsed remainder carries to the next window. Repeated reads and shorter partial windows retain the last published result.

TimeProcess covers the process callback, its ordinary Input/frame cleanup and settings flush. Engine.Run/RunAsync additionally include actual Window.Render submission/presentation; their frame-limit waits, event pumping before AdvanceFrame and movie encoding remain outside that interval. Manual AdvanceFrame has no implicit render work. TimePhysicsProcess selects the largest individual fixed step, including scene groups, navigation and physical solving; catch-up steps are not summed. A complete frame with no fixed step contributes zero to that lane.

TimeNavigationProcess measures synchronization/map publication and avoidance inside each physics step. It includes owner-side waits and observer delivery, including calls made by a custom MainLoop. Nested rejected/reentrant calls do not double-count their enclosing span. Thread-local cumulative ticks prevent unrelated independent hosts from contaminating the Engine owner's sample. Background topology computation is represented by its observed owner-side wait/publication, not a sum of worker CPU times. Geometry queries and baking outside these measured operations are not navigation-step durations.

## Lifecycle and errors

Before the first completed window, durations are zero. Successful Engine.Start resets all timing values alongside Engine FPS; failed initialization preserves the preceding run's diagnostics. Stop retains published values. Invalid/off-owner calls and failed process, physical or render frames do not advance the duration window. A failed render may follow completed Engine callback counters/FPS; those existing observations remain independent of presentation completion. Reads of several selectors do not form an atomic multi-value snapshot.

Direct MainLoop.Process/PhysicsProcess, SceneTree frame calls and NavigationServer.Step/Synchronize without Engine scheduling retain their existing behavior and do not publish Engine frame diagnostics. Independent batch hosts can attach their MainLoop and supply elapsed time through Engine.Start/AdvanceFrame/Stop. TimeScale0 still measures real execution. No native GPU timestamp, CPU-only computation claim or hardware-performance guarantee is added.

## Integration

Engine owns frame/window accumulation and reset. Performance owns retained published duration values. NavigationServer records only per-thread elapsed processing ticks, without retaining a tree, callback or native object. All repeated instrumentation uses value fields and monotonic timestamps. Existing scene and native lifetime/error ownership remains unchanged.

## Verification

The public-only shared FrameMonitorContract exercises real navigation avoidance callbacks, process/physics work, seconds and selector identities, partial windows, reset/stop, initialization and callback failures, cross-thread reads, direct-host isolation and zero simulation scale. It requires 256 prepared active Engine/navigation frames and reads to allocate zero owner/all-thread managed bytes. FrameMonitorTests additionally checks exact window maxima, remainder and empty-lane replacement.

Native profiles exercise both rendering methods with actual red pixels, a moving physical body, deliberate draw cost included in TimeProcess and a failed presentation that must not publish a completed window. Managed/public JIT, strict NativeAOT and both native rendering profiles pass on Linux/Wayland with the pinned software driver and no observed hardware-driver modules. Both managed consumers verify the prepared 0/0 owner/all-thread allocation interval. The ordinary full software regression passed in 1548.32 seconds on the frozen 7480becf source base plus this slice; runtime/test source hashes matched the tested payload. After integrating the newer local-activation and typed-iteration commits through 12bf2f8a, the Engine-order, both native rendering, public JIT and strict NativeAOT profiles were rebuilt and passed again. That focused integration result is separate from the preceding frozen full run. Native allocator totals, hardware/device timing, foreign-platform execution and human acceptance remain separate.

## Decisions

ADRs [0016](../decisions/core-object-runtime.md#adr-0016), [0014](../decisions/resources.md#adr-0014) and [0095](../decisions/singleton-services.md#adr-0095) own scheduling, allocation and static service access. Coverage retains the other monitor families' exact producer prerequisites.
