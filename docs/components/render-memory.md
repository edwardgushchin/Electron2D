# Native render memory

Last updated: 2026-10-28

`RenderingDevice.GetMemoryUsage(MemoryType)` reads texture regions, buffer
regions or committed allocation blocks in bytes. The same producers feed
RenderingServer.TextureMemUsed/BufferMemUsed/VideoMemUsed and Performance's
RenderTextureMemUsed/RenderBufferMemUsed/RenderVideoMemUsed selectors.
Server/Performance reads return zero without an active renderer and never create
a local device. Direct device reads retain ordinary owner/lifetime guards.

## GPU allocation source

The private `Electron2DRender` bridge reads the pinned SDL 3.4.18 Vulkan
allocator under its existing native mutex. Texture and buffer values use actual
native resource allocation requirements. Total sums committed native blocks,
including free space retained for reuse. It is not the sum of public descriptor
sizes, a host RAM query, a physical adapter budget or other applications' VRAM.
Private mutable-format images add their exact native allocation requirements to texture and total values once. Ordinary shared views add no image payload. These counters are read under their own native cache mutex after releasing the SDL allocator mutex; concurrent mutation can fall between the two snapshots. See [shared texture views](shared-texture-views.md). Native driver internals outside these allocators are excluded.

[ADR 0054](../decisions/physics-backends.md#adr-0054) shares one physical SDL
payload between independent renderer, local-compute and GPU-physics leases.
These queries observe that shared allocator once, including resources of other
live leases. Logical resource namespaces and ownership remain independent.
Native cycling, transfer storage, support images and pending retirement retain
their real memory cost. A borrowed source is shared; preparing a graphics-storage
support image can still allocate separate memory. Repeated cached exports reuse it.
FreeRID/Dispose need not immediately lower a native value: completion can defer
retirement and free allocation blocks may remain reserved.

Queries neither submit nor wait for GPU work. Several reads are nearby snapshots,
not one atomic group. Lightweight readers retain physical lifetime independently
of the device-creation monitor; final release first closes admission, joins readers
and then destroys the device. Native reads contain no user callbacks.
Completed renderer closure returns zero even when another local lease survives.

The bridge compiles a private ABI prefix from the exact source archive and checks
both defining source hashes. Runtime version and revision must match the pinned
core before opaque access. Unknown cores and GPU drivers without an adapter throw
NotSupportedException; allocator integrity failures throw InvalidOperationException.
The current adapter is Vulkan. Metal/D3D12 qualification and their memory adapters
remain separate work. No private SDL pointer or structure enters the public API.

## Compatibility source

Compatibility counts actual engine-owned SDL texture payloads when their
SafeHandles acquire and release storage. This includes uploads, viewport/history
targets, backbuffers and native-line coverage/composition images. Logical aliases
add no duplicate source payload. The engine supplies no explicit device-buffer
objects to this backend, so its buffer count is zero and total equals texture
payload. Opaque SDL driver storage and driver allocation overhead are excluded.
This boundary differs from the explicit GPU allocator source.

## Delivery and verification

`tools/render-native` pins SDL source by archive SHA-256 and exports only
`e2d_render_memory`. Desktop source targets, cross/static and Windows producers,
platform package inventories and consumer asset layout include the private bridge.
Games consume the matching platform package; they do not compile this helper.
The existing SDL license accompanies generated native declarations.

The public-only [RenderMemoryContract](../../tests/Portability/RenderMemoryContract.cs)
runs through root and portability consumers. It checks cold service-free reads,
actual allocations, native alignment/reservation, physical sharing, borrowed support
storage, original source pixels, two renderer lifetimes, worker/teardown reads and
prepared managed allocation budgets. The root lifetime check holds the creation
gate to prove that diagnostic reads do not acquire it. Native `Electron2DRenderCheck`
checks 32 allocation/retirement cycles with a concurrent reader; layout tests reject
modified source input. Select `--render-memory managed|local|gpu|compatibility`,
or append `gpu` for the GPU-physics world. Root selectors are
`ELECTRON2D_TEST_RENDER_MEMORY` and `ELECTRON2D_TEST_MEMORY_PHYSICS`.

The final source-bound Linux cohort passes fourteen CLR/strict trimmed NativeAOT profiles: cold/local reads, Vulkan and software/OpenGL compatibility, CPU/GPU worlds with an actually stepped awake body, twenty renderer sessions and 1,280 complete prepared render/read cycles at 0/0 owner/all-thread managed bytes. Native controls pass 32 concurrent allocation/retirement cycles and reject a mismatched core before opaque access.

Receipts live under `bin/render-memory-monitors/2026-10-27/`. Source build and
local execution do not establish a published native bundle, foreign adapters,
native-heap allocation freedom, physical simulation throughput or complete goal
acceptance. Performance.GetMonitor remains Partial for current/peak runtime
allocation memory and four other pipeline-compilation sources.

The pinned reference
[RenderingDevice implementation](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/servers/rendering/rendering_device.cpp)
separates resource allocations from total native allocator usage. The
[Performance selectors](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Performance.xml)
retain that distinction. Host process RSS or descriptor-byte sums cannot implement it.
