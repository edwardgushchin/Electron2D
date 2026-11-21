# Native device timestamps

RenderingDevice.CaptureTimestamp records a name, CPU time and a native GPU query at the current position in its ordered command stream. It supports local compute/draw devices and the borrowed renderer device. This adds actual GPU measurements for user workloads; CPU elapsed time does not substitute for GPU query results.

## Publication and clocks

Recorded and submitted markers remain private until native completion. Local Submit followed by Sync publishes a consistent names/CPU/GPU batch. Existing implicit completion before another command acquisition can also publish it. The renderer submits and completes its own device before FramePostDraw callbacks. Readback callbacks see the committed timestamp batch. GetCapturedTimestampsCount is zero initially; its three indexed getters reject negative or out-of-range indices. GetCapturedTimestampsFrame is zero initially and otherwise the one-based completed device-submission sequence. A completed batch with no markers replaces the previous results with count zero; a window frame can contain multiple such device submissions.

GetCapturedTimestampCPUTime returns Time.GetTicksUSEC-domain microseconds. GetCapturedTimestampGPUTime returns native device-clock nanoseconds, rounded down to an integer. Vulkan masks the hardware counter to the queue family's valid bits and multiplies by the exact binary32 timestamp period. Direct3D 12 computes `ticks × 1,000,000,000 ÷ frequency` with integer arithmetic using the executing queue's frequency. Narrow counters can wrap. Subtract values only within the same clock domain. [ADR 0028](../decisions/rendering.md#adr-0028) defines the units and clock epochs.

## Recording and ownership

Empty and repeated names are retained exactly. Null names reject. Capture is permitted outside a list or before the first draw/dispatch in an active list. Once that list contains work, end it before adding another marker. This preserves native render-pass/query ordering rather than permitting a marker to split a retained draw command. All methods retain ordinary device owner, disposal, renderer-access and readback-memory reentry guards.

ProjectSettings.MaxTimestampQueryElements registers debug/settings/profiler/max_timestamp_query_elements with default 256 and positive validation. A device captures the effective feature-overridden setting at construction. Its fixed native pool and two reusable managed result banks use that capacity; changing the project setting affects new devices. Exhaustion throws before writing a marker and preserves the current batch. Old results remain readable while the next batch records.

The pool initializes lazily on first capture. Vulkan initializes the query slots with a separate reset submission so first capture can occur in an already-open empty render pass. Later Vulkan command acquisition records the reset before any render pass. On Direct3D 12 each write overwrites a reused slot, and command acquisition clears the count of written slots for the new batch. Names and numerical arrays swap only after the complete native read succeeds. Disposal cancels recorded work, waits for submitted work and releases query storage before the device lease. Prepared capture, submission and query intervals use retained storage.

## Backend boundary

The private source-, version- and revision-bound SDL 3.4.18 bridge supports Vulkan and Direct3D 12. The Direct3D 12 adapter allocates a timestamp heap and readback buffer on the SDL device. It records `EndQuery` followed by `ResolveQueryData` for each written slot on the SDL-owned direct command list. The existing submission fence protects CPU reads and slot reuse. Missing bridge entry points or an unsupported native timestamp backend throw NotSupportedException at capture. Query allocation, read and reset failures throw explicitly. Ordinary completed-result getters remain usable before capture. [Direct3D 12 timestamp and queue frequency](https://learn.microsoft.com/en-us/windows/win32/direct3d12/timing).

This scope measures the RenderingDevice stream. [Viewport render time](viewport-render-time.md) separately brackets authoritative canvas commands/fences using the shared private GPUTimestampQueries owner; named markers do not time an unrelated canvas command buffer. Foreign backends, NativeAOT, independent physical timing calibration and complete native driver heap accounting retain separate qualification requirements.

## Verification

Windows x64 verification covered native, local-device and borrowed-device timestamps on Direct3D 12 and Vulkan with stock SDL 3.4.18 on an NVIDIA GeForce RTX 3090 Ti (driver 621.14). The local profiles verified the compute and draw outputs, including 0 managed bytes in both warmed measurement intervals. Each borrowed profile verified two completed batches with matching GPU copy bytes and window pixels.

Build the runtime and tests with the matching native bridge. Run the compiled Release test DLL with `ELECTRON2D_TEST_GPU_TIMESTAMPS=local` and `renderer`. On Windows x64 set `SDL_VIDEODRIVER=windows` and run both profiles with `SDL_GPU_DRIVER=direct3d12` and `vulkan`. On Linux x64 use `SDL_VIDEODRIVER=wayland` and `SDL_GPU_DRIVER=vulkan`. The local profile verifies compute output, timestamp order, exact names, publication, capacity, API guards, reuse, cancellation and retirement. It measures 64 active capture/submit/sync intervals and 64 idle query intervals after warmup. A fresh graphics device first captures inside an empty draw list and verifies red output. The renderer profile brackets a GPU buffer copy, checks the bytes and completed markers in the readback callback, then verifies window pixels and borrowed-device retirement. With a preceding Direct3D 12 bridge lacking the create export, run the `unsupported` profile to verify the explicit refusal.

Configure tools/render-native with the pinned SDL core library, build Electron2DRenderTimestampCheck and run it once per driver. The native check covers invalid ABI, bounds, unwritten slots, ordered GPU queries, overflow and pool retirement. It exercises Vulkan period scaling and Direct3D 12 integer frequency scaling, including inputs whose intermediate product exceeds 64 bits. An additional deterministic 20,000-case native comparison verified Vulkan scaling against exact rational arithmetic on the original Linux qualification host.

Native semantics: [vkCmdWriteTimestamp](https://docs.vulkan.org/refpages/latest/refpages/source/vkCmdWriteTimestamp.html), [vkGetQueryPoolResults](https://docs.vulkan.org/refpages/latest/refpages/source/vkGetQueryPoolResults.html).

## Window frame profiling

`ProjectSettings.PrintGPUProfile` registers `debug/settings/stdout/print_gpu_profile` as a Boolean defaulting to false. The owned window host samples the effective setting when it opens the renderer, including for a custom main loop. `--gpu-profile` before `--` or `++` enables it independently of the project value. Editor-hint runs require the switch. Manual headless loops have no renderer to profile.

On the Vulkan canvas backend, four hardware timestamp markers on the rendering queue bracket setup, canvas work (including pending particles), and presentation. The intervals measure GPU-clock time between submissions and can include idle gaps. The presentation interval covers queued rendering work through the last marker and does not measure scan-out. Offscreen draws submit the same marker sequence. Work submitted before the first marker and on separate devices is outside the measured frame.

When a frame queue slot retires, the renderer reads that slot's completed sample. Interrupted frames and timestamp counter wraps are skipped. About once per unscaled second, the diagnostic prints the latest completed frame duration and the mean stage durations for samples collected since the previous report, in milliseconds. Ordinary Logger and stdout rules apply. Each queue slot owns its timestamp pool until renderer shutdown. Disabled runs allocate no profiler resources and submit no markers.

Renderers without an integrated adapter report unavailability once and continue rendering. Desktop OpenGL still needs a frame-wide integration with its context, SDL command flushing and disjoint sample retirement. Direct3D 12 frame profiling needs a separate canvas integration with the device timestamp adapter. Metal needs a hardware timestamp adapter. Software rendering has no GPU timeline.

Windows x64 checks use `ELECTRON2D_TEST_GPU_FRAME_PROFILE` with `setting`, `switch`, `editor`, `disabled`, and `unsupported`. The Vulkan profiles exercise startup feature overrides, an offscreen submission, report cadence, viewport pixels, stdout suppression, logger failure and a fresh renderer lifetime. The unsupported compatibility profile verifies the diagnostic and continued viewport rendering.
