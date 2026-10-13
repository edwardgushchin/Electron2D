# Explicit canvas frame execution

Last updated: 2026-10-13

## Executable scope

The retained [RenderingServer](../classes/RenderingServer.md) can draw the current native scene immediately, synchronize pending GPU work and schedule a one-shot completion action. [Engine.GetFramesDrawn](../classes/Engine.md) accounts for completed canvas frames. These operations support controlled rendering and capture on the existing native host. They supply runtime prerequisites for [ADR 0090](../decisions/agent-native.md#adr-0090), not a complete editor/batch-host implementation or tool acceptance from compilation.

ForceDraw(bool swapBuffers = true, double frameStep = 0) uses the same scene capture, retained commands, materials, eligible viewport targets, compute/readback and screen-visibility pipeline as automatic rendering. It bypasses RenderLoopEnabled. With swapBuffers=false, the root texture and offscreen targets are still updated without presenting the root window. Existing target update/clear policy still applies. The current native window must have a scene tree; absent host, off-owner calls and reentrant rendering fail explicitly.

frameStep advances only the rendering clock, using the existing rollover setting. It never advances scene process or physics callbacks. Negative/nonfinite/overflowing steps are rejected before animated-texture updates, observers, native work or callback consumption. GetFramesDrawn is a thread-safe process-lifetime count consistent with existing process/physics counters. One successful canvas interval counts once, including all of its viewports and explicit non-presenting frames. Headless work and disabled automatic rendering add none; a manual ForceDraw still draws. Native completion remains counted if a subsequent observer/action throws.

## Synchronization and completion

ForceSync flushes the renderer-owned command/readback pipeline and waits for preceding GPU operations. The primary backend waits for its SDL GPU device. Compatibility uses a bounded one-pixel readback and restores the previous render target, providing a driver completion point without allocating a full image. This is a cold blocking operation with a synchronization cost. It does not draw, present, advance clocks/counters or consume frame requests. Calls before an in-flight frame finishes submission are rejected; post-draw observers/actions can synchronize.

RequestFrameDrawnCallback(Action callback) accepts an application-owned action. The owner-thread service preserves request order. Requests queued before or during capture join the completed frame; requests after native submission, including post-draw/completion requests, wait for a later frame. Selected actions run after normal readback/post-draw processing and each executes once. A failure in another observer/action does not discard selected actions. Errors propagate after native commitment and frame cleanup; multiple errors are aggregated. If capture/submission fails, pending requests remain for the next successfully completed frame. Callback captures are released before/after invocation, and teardown releases pending captures without invoking them.

Actions execute inside the protected rendering interval: nested ForceDraw remains invalid. Scene changes made after completion affect later captures; the application owns captured object validity and external side effects. Ordinary FramePreDraw/FramePostDraw retain their existing multicast semantics. The one-shot queue does not authorize off-owner scene, device or renderer access.

## Verification

ManualRenderFrameTests verifies absent host and, in a separate profile that preserves the general suite's counter baseline, headless accounting, then runs independent Linux Wayland sessions on GPU and hardware compatibility with fallback disabled. Through public APIs it draws root and SubViewport pixels with the automatic loop disabled, advances only the rendering clock, synchronizes actual deferred device readback, checks wrong-thread/reentrant/invalid-step rejection, request ordering across phases, observer/action failure after native commitment, weak capture release and session teardown. Internal probes measure the rendering clock; rendered output is read from public viewport textures. Both native sessions completed 71 manually driven frames, including 64 prepared frames with cached one-shot actions at 0 owner-thread managed bytes. Physical display perception, foreign platforms, broad driver throughput and complete tooling acceptance are separate gates.

## Decisions

[ADR 0028](../decisions/rendering.md#adr-0028) owns the shared GPU/compatibility canvas path; [ADR 0004](../decisions/product.md#adr-0004) owns the typed Action/error boundary; [ADR 0095](../decisions/singleton-services.md#adr-0095) owns static retained service identity. No native handle or public host wrapper is introduced.
