# Native device timestamps

RenderingDevice.CaptureTimestamp records a name, CPU time and a native GPU query at the current position in its ordered command stream. It supports local compute/draw devices and the borrowed renderer device. This adds actual GPU measurements for user workloads; CPU elapsed time does not substitute for GPU query results.

## Publication and clocks

Recorded and submitted markers remain private until native completion. Local Submit followed by Sync publishes a consistent names/CPU/GPU batch. Existing implicit completion before another command acquisition can also publish it. The renderer submits and completes its own device before FramePostDraw callbacks. Readback callbacks see the committed timestamp batch. GetCapturedTimestampsCount is zero initially; its three indexed getters reject negative or out-of-range indices. GetCapturedTimestampsFrame is zero initially and otherwise the one-based completed device-submission sequence. A completed batch with no markers replaces the previous results with count zero; a window frame can contain multiple such device submissions.

GetCapturedTimestampCPUTime returns Time.GetTicksUSEC-domain microseconds. GetCapturedTimestampGPUTime returns native device-clock nanoseconds, computed from hardware ticks and timestampPeriod using an exact binary32/integer product, preserving low bits even when native long double has only double precision. CPU and GPU epochs are independent, so subtract values only within the same clock domain. Narrow hardware counters can wrap; this adapter preserves their masked native counter behavior. It does not claim CPU/GPU clock calibration or a shared engine-start epoch. This follows the pinned native driver's nanosecond conversion, despite the comparison XML's microsecond wording; [ADR 0028](../decisions/rendering.md#adr-0028) makes the Electron2D units explicit.

## Recording and ownership

Empty and repeated names are retained exactly. Null names reject. Capture is permitted outside a list or before the first draw/dispatch in an active list. Once that list contains work, end it before adding another marker. This preserves native render-pass/query ordering rather than permitting a marker to split a retained draw command. All methods retain ordinary device owner, disposal, renderer-access and readback-memory reentry guards.

ProjectSettings.MaxTimestampQueryElements registers debug/settings/profiler/max_timestamp_query_elements with default 256 and positive validation. A device captures the effective feature-overridden setting at construction. Its fixed native pool and two reusable managed result banks use that capacity; changing the project setting affects new devices. Exhaustion throws before writing a marker and preserves the current batch. Old results remain readable while the next batch records.

The pool initializes lazily on first capture. A separate reset submission handles first capture inside an already-open empty render pass, where an inline reset is invalid. Later command acquisition records resets before any render pass; only completed queries are read and reused. Names and numerical arrays swap only after the complete native read succeeds. Disposal cancels recorded work, waits submitted work and releases query storage before the device lease. Prepared capture/submission/query intervals use retained storage; first use is cold allocation-capable work.

## Backend boundary

The private source/hash/version/revision-bound SDL 3.4.18 Vulkan bridge provides query-pool creation, timestamp-support detection on the selected queue, reset, ordered write and completed readback. No native device or query handle enters the public API. Missing bridge entry points or an unsupported native timestamp backend throw NotSupportedException at capture. Query allocation/read/reset failures throw explicitly. Ordinary completed-result getters remain usable before capture.

This scope measures the RenderingDevice stream. [Viewport render time](viewport-render-time.md) separately brackets authoritative canvas commands/fences using the shared private GPUTimestampQueries owner; named markers do not time an unrelated canvas command buffer. Foreign backends, NativeAOT, independent physical timing calibration and complete native driver heap accounting retain separate qualification requirements.

## Verification

Build the runtime and tests with the matching native bridge, then run the compiled Release linux-x64 test DLL with ELECTRON2D_TEST_GPU_TIMESTAMPS=local and renderer, SDL_VIDEODRIVER=wayland and SDL_GPU_DRIVER=vulkan. The local profile verifies actual compute values, timestamp order, Unicode/empty/repeated names, old/new batch publication, captured capacity, null/index/owner/disposal/list guards, pool reuse and cancellation/submitted retirement. It measures 64 active capture/submit/sync intervals and 64 idle query intervals after warmup. A fresh graphics device first captures inside an empty native render pass and checks real red output. The renderer profile checks two completed marker frames, real window pixels and borrowed-device retirement.

Configure tools/render-native with the pinned SDL core library, build Electron2DRenderTimestampCheck and run it. The native check covers invalid ABI/capacity/index/count, querying unwritten slots without waiting, ordered actual GPU queries, partial-unavailable result rejection, full-width/fractional/subnormal scaling, overflow and pool retirement. An additional deterministic 20,000-case native comparison against exact rational arithmetic passed on the qualification host. Run the existing compute, device graphics, asynchronous readback and project-settings checks when changing these shared boundaries. These checks do not establish foreign-host or owner acceptance.

Native semantics: [vkCmdWriteTimestamp](https://docs.vulkan.org/refpages/latest/refpages/source/vkCmdWriteTimestamp.html), [vkGetQueryPoolResults](https://docs.vulkan.org/refpages/latest/refpages/source/vkGetQueryPoolResults.html).
