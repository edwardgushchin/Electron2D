# Private render allocator and sampler bridge

This producer compiles the allocator/sampler declarations of SDL 3.4.18 from the archive
SHA-256 pinned in CMakeLists.txt. It does not build or replace SDL. The selected
target's SDL core is supplied through SDL3_LIBRARIES. The generated declarations
retain SDL's notice; the platform package includes licence/SDL-LICENSE.txt.

e2d_render_memory uses ABI 1 and returns 1 for a coherent native snapshot, 0 for
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
