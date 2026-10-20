# Render-thread callbacks

Last updated: 2026-10-20

## Scope and public workflow

[RenderingServer](../classes/RenderingServer.md) provides a typed bridge from worker code to the exact active native rendering session. Both current canvas backends execute on the scene owner thread. IsOnRenderThread reports that identity safely from any thread. It requires an active, non-retiring service; IsAvailable remains the separate availability observation. Neither query reserves the session or changes ordinary resource/scene access guards.

While Engine.Run owns a native session, a worker can request an actual canvas change:

```csharp
RenderingServer.CallOnRenderThread(
    () => RenderingServer.SetDefaultClearColor(Colors.Blue));
```

The worker returns after enqueueing. Invocation occurs on the native owner. The callback can obtain RenderingServer.GetRenderingDevice, create programs/resources and record commands through the ordinary APIs; Compatibility retains its null device result. The application keeps captured objects valid until invocation and owns external side effects. A successful enqueue does not acknowledge execution or extend the session lifetime. No separate thread, native handle or public host object is introduced.

## Ordering, phase and errors

An owner call drains the previously queued FIFO snapshot, then invokes its own action inline. A nested owner call runs inline without recursively draining. Concurrent producers are serialized only while enqueueing; each producer retains order and every selected callback executes once. User code runs outside the queue lock. Requests accepted during a drain remain for a later boundary, preventing a continuously writing producer from monopolizing the current frame.

Automatic and forced render attempts drain before canvas capture. This includes attempts with RenderLoopEnabled=false, so disabling drawing cannot strand worker work. Enqueueing conservatively invalidates rendering; a low-processor loop observes the new revision and drains the request before testing for an unchanged frame. ForceSync drains before flushing/waiting for native work and completing readbacks. ForceDraw validates its arguments before consuming requests. Existing owner, command-list, submission and resource-lifetime rules still apply inside an action.

All selected actions are attempted despite a failure. A single error retains its identity; multiple errors aggregate. An owner action still runs after a failure in prior queued work. ForceSync completes native work/readbacks before reporting callback errors. When rendering is enabled and native capture succeeds, the frame commits before selected errors are reported; disabled rendering and native failures report after their selected work/cleanup. A callback that manually draws and consumes the pending revision cannot hide its later error through idle-frame skipping. FramePreDraw/FramePostDraw and one-shot completed-frame callbacks retain their existing phase contracts. Ordinary actions are distinct from RequestFrameDrawnCallback, which runs after a completed frame.

## Ownership and retirement

The retained session owns two reusable action lists and a small enqueue lock. Each selected slot is cleared before invocation; completed/canceled lists release captured references. Shutdown rejects new requests and cancels unconsumed actions before native resources retire. Requests never migrate to a later session. A disposing service rejects both operations, including the pre-delete interval. The bridge does not take ownership of captured resources or authorize use of an expired RID.

No queue allocation is required after the two lists have reached the workload's maximum prepared capacity. Larger batches and application-created closures can allocate cold storage. Callback error aggregation also allocates. Engine-owned native and driver allocations remain separate from managed counters.

## Verification and limits

[RenderThreadTests](../../tests/Electron2D.Tests/RenderThreadTests.cs) exercises unavailable/null calls, owner/worker identity, direct off-owner rejection, owner FIFO/inline ordering, nested calls, requests arriving during a drain, four concurrent producers with 256 exactly-once requests, error continuation/aggregation, capture release, canceled requests, reopening and failure after native frame commitment. [The native driver](../../tests/Electron2D.Tests/RenderThreadTests.Driver.cs) creates the actual GLSL/HLSL compute program on a worker-requested owner callback, executes changing buffer arithmetic and queued readback, and changes real canvas pixels. Compatibility executes the same callback/canvas protocol with its ordinary device gate.

The prepared producer and owner counters cover 32 changing enqueue/render/compute/readback cycles after warmup. Disabled rendering drains without drawing; unchanged low-processor frames skip submission; a subsequent worker request resumes output. Dedicated Linux software Vulkan/llvmpipe profiles exercise GPU GLSL/HLSL and Compatibility on Wayland and X11. Native allocator totals, physical GPU/display acceptance, foreign hosts and the full engine suite remain independent verification gates.

## Decisions and reference

[ADR 0028](../decisions/rendering.md#adr-0028) owns the owner/queue integration; [ADR 0095](../decisions/singleton-services.md#adr-0095) preserves the static facade over the exact retained session. Typed Action and C# exception behavior follow [ADR 0004](../decisions/product.md#adr-0004). The pinned [reference owner dispatch](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/rendering_server_default.h#L1212) flushes pending requests before its immediate owner call; Electron2D preserves that ordering with its existing single-owner backend and explicit session cancellation/error projection.
