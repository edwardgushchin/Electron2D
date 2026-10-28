# Private render allocator and sampler bridge

This producer compiles the allocator/sampler declarations of SDL 3.4.18 from the archive
SHA-256 pinned in CMakeLists.txt. It does not build or replace SDL. The selected
target's SDL core is supplied through SDL3_LIBRARIES. The generated declarations
retain SDL's notice; the platform package includes licence/SDL-LICENSE.txt.

e2d_render_memory preserves ABI 1 and returns 1 for a coherent native snapshot, 0 for
an unsupported core/revision/driver, and -1 for invalid arguments or inconsistent
allocator data. Output contains three uint64 fields: textures, buffers and total.
The current native adapter is Vulkan. It locks the native allocator without
submitting, waiting for GPU work or invoking application callbacks.

Build Electron2DRender for delivery and Electron2DRenderCheck for the native
concurrency/allocation check. Run test_generate_layout.py with Python -B for
bounded extraction and modified-source rejection. Runtime acceptance requires
the selected matching core; a successful compilation alone does not qualify it.

`e2d_render_sampler` uses ABI 1, copies the SDL sampler fields, normalized
U/V address identities (0..4), border color (0..5), and a zero/one unnormalized
flag. It validates the pinned Vulkan core, physical LOD-bias bounds and the
unnormalized filter/mip/LOD/address/anisotropy/compare restrictions. Ordinary
normalized samplers without border/mirror-clamp use SDL creation directly. Extended states replace the
unpublished Vulkan handle while SDL retains command references and deferred
release. Failure returns null with SDL_GetError.

`e2d_render_sampler_view` uses ABI 1 and the pinned TextureCommonHeader. It
returns 1 only for an ordinary single-layer/single-level/single-sample native
texture, 0 for an incompatible view and -1 for an invalid/unsupported call.
It records no commands and invokes no application callbacks. The managed
binding owner validates shader operations and lifetime before native sampling.

The public sampler consumers check real normalized and texel-coordinate
float/integer/depth pixels, descriptor arrays, mixed/filtering modes and
operation/view rejection. Mirror-clamp requires the private device capability
recorded only after successful VK_KHR_sampler_mirror_clamp_to_edge negotiation.
The factory retries creation without this optional extension on failure, retaining
the existing point and relaxation policy. Raw samplers without that capability
reject mirror-clamp; unnormalized coordinates still allow only addresses 2/3.
The Linux test_mirror_mask.c SDL3_DYNAMIC_API shim refuses extension requests
through the pinned dynapi table, leaving all ordinary native operations intact.
It is test-only and is not linked into this producer.

`e2d_render_limits` uses ABI 1 and exactly 37 uint64 output elements. It reads the immutable selected Vulkan physical-device limits without allocation, submission or waiting. Invalid pointers/count/ABI return -1; unsupported core/revision/driver returns 0; success returns 1. Failure leaves output untouched. Slots 11/13/14 are zero for excluded image roles. Managed code applies the executing profile and logical descriptor remapping; packages must carry this export with the existing three exports.

The Linux test_fp64_mask.c SDL3_DYNAMIC_API shim refuses shaderFloat64 feature requests while delegating ordinary device creation. DeviceFP64.Consumer unavailable verifies explicit FP64 refusal and executing float32 fallback. It is a development fixture, never packaged with the runtime. See the [FP64 contract](../../docs/components/local-compute.md#native-fp64-arithmetic-and-data).

The Linux test_int64_mask.c development shim selects int64/fp64/both through ELECTRON2D_TEST_MASK_PRECISION. DeviceInt64.Consumer verifies independent fallback and executing remaining precision; the fixture is excluded from runtime packaging.

The Linux test_atomic64_mask.c development shim selects buffer/shared/both/api through ELECTRON2D_TEST_MASK_ATOMIC64. DeviceAtomic64.Consumer checks independent storage fallback and rejection before shader/binary publication. The api profile refuses Vulkan 1.2 requests. This shim is excluded from runtime packaging.

`e2d_render_shader16_support` preserves ABI 1 and returns the selected Vulkan 1.2 physical device's supported buffer/uniform/push/input-output storage bits (0..3) and Float16 arithmetic bit (4). ABI 2 additionally returns core Int16 arithmetic in bit 5; ABI 1 remains unchanged. ABI 3 additionally returns Int8 arithmetic in bit 6 and byte buffer/uniform/push storage in bits 7/8/9; the ABI 1/2 masks are preserved. Zero means no supported narrow profile; invalid arguments return -1. The mask describes support before optional device creation, not enabled device features. The managed factory retires the unpublished probe before creating a final device and retains the accepted mask privately. The export is included in all three export lists and the native package closure.

The Linux test_shader16_mask.c development shim selects integer/arithmetic/buffer/uniform/push/io/all/api through ELECTRON2D_TEST_MASK_SHADER16. DeviceFP16.Consumer and DeviceInt16.Consumer verify retained sibling profiles, original-module/binary refusal and legacy HLSL. Never package this shim.

The private narrow feature property retains only successfully enabled features. The Linux test_shader8_mask.c fixture refuses byte arithmetic/buffer/uniform/push, Int16/Float16 arithmetic, all byte features or Vulkan 1.2. DeviceInt8.Consumer checks available siblings and pre-publication failures; never package the shim.

`e2d_render_graphics_storage_support` ABI 1 returns physical vertex/fragment store-and-atomic support in bits 0/1 (zero on unsupported backends, -1 for invalid arguments). The factory requests those core features separately and retains only enabled bits. `e2d_render_storage_barrier` ABI 1 validates the pinned device/command ownership and records an all-command read/write memory dependency outside a pass; returns 1 on success or -1 on rejection. SDL only maintains pass/submission flags in debug mode, where the bridge checks them too; Release pass lifetime is enforced by the managed owner. The generated pinned command prefix includes descriptor bindings, caches and used-texture tracking, and both symbols enter every export list/package closure. `Electron2DRenderCheck` runs debug idle/pass guards; the public consumer runs ordinary Release devices. No vendored source is modified.

The four `e2d_render_empty_*` ABI 1 exports implement explicit-extent attachmentless
passes on the pinned Vulkan backend. `support` returns available 1/2/4/8 sample
bits (zero for missing procedures/backend, -1 for invalid ABI/device). `create`
returns a private owner object containing a zero-attachment render pass and
framebuffer, or null; `begin` validates command/device/pass ownership and returns
an ordinary SDL render-pass token after initializing dynamic state. End continues
through SDL's existing pass cleanup. `release` destroys both native objects only
after the managed owner completes or cancels all using commands. Failed creation
retires a partially created render pass. No image or buffer is allocated, and no
public native handle is exposed. The C check verifies debug guards and unchanged
native image/buffer totals. Public CLR/NativeAOT consumers and `mask-empty.c`
exercise storage effects, ordinary graphics/canvas, missing support and injected
creation failure. Native/driver heap and foreign-backend qualification are separate.

`e2d_render_raster_support` and `e2d_render_raster_pipeline` ABI 1 supply primitive
discard through a call-local renderer view, shared locked layout caches and four
reserved globally unique descriptor-layout IDs per descriptor-count profile.
A device-owned cache serializes creation and keeps repeated profiles from growing
SDL's indexed descriptor pools; its cleanup retires the private profile records. The thread-local callback sets
real native rasterizer discard and includes only the vertex stage. Shared function
pointers remain unchanged; native pipeline failure restores both shader references.
The generated declarations also pin SDL_gpu_vulkan_vkfuncs.h. The C check covers
concurrent creation/retirement, layout IDs and injected failure reference counts;
the public graphics-storage consumer covers source/binary, pixels and managed
allocations. Other backends need their own pipeline adapters.

The private line-support and line-pipeline entries query physical width range/precision and execute copied width through the call-local raster adapter. Logical wideLines ownership is required for nonunit widths; shared native callbacks are never mutated. See [the consumer contract](../../docs/components/wide-lines.md) and test_wide_mask.c for independent optional-feature refusal.

The multisample support/pipeline entries add physical sampleRateShading/alphaToOne queries and copied native mask/fraction/flags through the existing call-local creator. Neither sample mask nor disabled optional state requires these features. The factory retains independent logical feature ownership; test_multisample_mask.c refuses either/both creation requests. See [sample controls](../../docs/components/multisample-state.md).

The two `e2d_render_depth_*` ABI 1 exports query per-face/bounds support and create copied depth/stencil pipelines beside existing raster/multisample state. The native package now carries 29 exports. Independent references remove only SDL's shared dynamic reference selector. The managed factory negotiates depthBounds independently in the bounded optional raster profile. `test_depth_mask.c` is a development-only device-creation refusal shim; never package it. See [the native state contract](../../docs/components/depth-stencil-state.md).

The six graphics-image ABI 1 entries query adapter availability, create mixed image
pipelines, transition original images, bind base-mip views, prepare command-owned
resource sets and retire device-owned layouts. The generated pinned command prefix
includes descriptor pools/bindings and used-texture ownership. Resource sets use
actual STORAGE_IMAGE descriptors beside sampled images/samplers/buffers; SDL retains
uniform binding and command-pool reset. Profiles reserve IDs once after successful
native layout creation and retire before the last shared physical device is destroyed.
`Electron2DRenderCheck` covers 260-set rollover/reuse, guards, texture tracking and
idle retirement; mask-images.c checks explicit missing-adapter refusal. See
[graphics image ownership and qualification](../../docs/components/graphics-storage-images.md).

The two logic ABI 1 entries query physical logicOp and create copied logical color
pipelines through the shared call-local raster path, preserving images/depth/samples.
The factory independently retains logical feature ownership within at most 32 raster
subprofiles. test_logic_mask.c refuses only that feature request. See
[logical color operations](../../docs/components/logic-operations.md) for semantics,
refusal and verification limits. Test masks are never packaged.

Array storage uses view ABI 2: array selector 2 retains all selected layers in one
single-mip compute-write view. ABI 1 ordinary/shared views remain available.
`e2d_render_prepare_compute_images` ABI 2 replaces only compute's readonly set when
a sampler shares physical storage with a writable image; shared descriptors use
GENERAL and independent descriptors preserve READ_ONLY. The prepare flag enters
or leaves the deduplicated complete-root image scope, retaining image-write views
and bypassing SDL’s single-subresource compute-image transitions. Graphics scopes
also resolve shared views to their original full allocation before transitioning.
Descriptor-pool ownership
and retirement remain on SDL commands. View ABI 2 checks the required descriptor
procedures before publication. `e2d_render_images_pipeline` and the image-aware
logic request also accept ABI 2, with actual writing-stage bits 16..17 above the
existing 16 image-kind bits; ABI 1 retains its inferred writing-stage guard.
Readonly arrays therefore need no graphics-store feature. `array-storage-check.comp.glsl`
is an optional external compute fixture for Electron2DRenderCheck, supplied through
ELECTRON2D_TEST_ARRAY_STORAGE_SPIRV; the check forwards real Vulkan descriptor writes
while observing the exact shared/independent layouts and verifies layer-two bytes.

## Device timestamps

The ABI-1 timestamp adapter owns Vulkan query pools, reset/write commands and completed raw-clock readback. It uses the pinned command-buffer layout, validates physical queue timestamp support, masks valid counter bits and scales device ticks to nanoseconds. First-use reset can complete through its own short command buffer so an empty active render pass permits capture; later reuse resets at command-buffer acquisition. Managed owners retain pools through command cancellation/completion and destroy them before releasing the device lease. See [the component contract](../../docs/components/gpu-timestamps.md).

Build Electron2DRenderTimestampCheck and run it against the configured native library. It checks invalid ABI/capacity/index/count, unwritten queries, real GPU marker order and pool retirement. This does not account for native driver heap costs or qualify other backends.
