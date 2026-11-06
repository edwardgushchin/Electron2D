# Native floating-point shader atomics

The existing RenderingDevice shader, StorageBuffer and Image families execute
native FP32 atomic load, store, exchange and addition. The FP64 buffer/workgroup
branch below adds double accumulation through the same API. This supports parallel
floating accumulation and reductions without moving their counters to the CPU.
No public type or signature is added. [ADR 0028](../decisions/rendering.md#adr-0028)
owns the existing source/import/runtime boundary.

## Features and publication

The pinned Vulkan 1.2 device profile queries VK_EXT_shader_atomic_float and six independent
physical features: base operations and addition for storage buffers, workgroup
memory and images. Device creation explicitly requests the extension and matching
feature chain. Only a successfully created logical device records those enabled
bits. An unavailable export or refused optional request preserves ordinary device
creation and its existing precision/storage profile.

Every module, including specialization templates and binary imports, classifies
actual typed atomic pointers before publishing a shader or binary. Buffer storage
includes both logical encodings and physical-storage buffer addresses; shared
storage uses its independent feature, and image pointers require the image feature.
AtomicFloat32AddEXT requires its declared extension/capability. Even an unused
addition capability requires an enabled addition feature. Missing operands,
inconsistent floating result types and missing declarations reject before native
pipeline creation. Unsupported floating precision, vector or min/max capabilities
reject explicitly.

TextureIsFormatSupportedForUsage accepts StorageAtomic with R32SFLOAT only when
at least one native image atomic feature is enabled and the ordinary native format
and usage query succeeds. The concrete shader still requires its specific base/add
feature. StorageAtomic is a capability role; an Image descriptor independently
requires Storage. Existing ordinary R32SFLOAT Storage allocations may execute
supported atomics too. SupportsImageAtomic32Bit retains its existing signed/unsigned
integer capability meaning; query the concrete floating format/usage separately.

## Execution and ownership

GLSL GL_EXT_shader_atomic_float and imported SPIR-V preserve the native operation,
returned prior value, Device/Workgroup scope, memory semantics and coherent/volatile
qualifiers. Atomic load/store with explicit acquire/release semantics uses
GL_KHR_memory_scope_semantics. The current HLSL frontend's floating-interlocked
source vocabulary is not qualified by these GLSL/SPIR-V checks.

Scalar and fixed-array Image descriptors execute over ordinary and independent-layer
R32SFLOAT images, including available multisample images. Descriptor lowering chooses
the native image before creating the texel pointer and performing the atomic; it
preserves the coordinate/sample and scope/memory operands. Invalid descriptor
selectors return zero or suppress a store. Descriptor bounds adaptation does not
make invalid texel coordinates valid. The common helper keeps the original constant for ordinary images instead of
turning it into a function parameter; multisample helpers keep their selected sample.
Ordinary image pointers retain a constant-zero
sample operand. Original exported modules remain unchanged.

Compute and independently enabled writable vertex/fragment stages share the existing
GPU timeline. Completed draw/dispatch boundaries order subsequent work and readback.
Within a draw/dispatch, the algorithm owns synchronization and race freedom for
unrelated data and workgroups. Native FP32 arithmetic retains device rounding,
NaN/denormal behavior and order-dependent addition; arbitrary reduction order is
not a reproducible summation guarantee. Exchange preserves the transferred scalar
bits. Physical pointers retain the [address ownership contract](buffer-device-addresses.md).
FreeRID invalidates dependent sets and retires pending native storage normally.

FP16, floating min/max, vector atomics, sparse images, floating images beyond
R32SFLOAT and encoded ImageBuffer floating atomics retain separate native
precision/resource adapters. Integer
ImageBuffer operations remain available. This slice does not add those adapters.

## FP64 buffer and workgroup atomics

The Vulkan branch executes double atomic load/store/exchange/add in storage
buffers and workgroup memory, with native arithmetic and atomic flags enabled
independently. FP64 images and encoded ImageBuffer floating operations keep their
resource gates; this adds no double texture/vertex format or public declaration.
The existing raw buffer, copied double specialization, ordinary graphics storage
and physical-address contracts remain authoritative.

The native query ABI1 retains its six FP32 bits. ABI2 appends four independent
buffer/shared base/add bits. The factory retries ABI1 only for the older bridge's
unsupported-ABI status; ABI2 allocation failure has a distinct error status and
propagates. It requests double atomic flags only with an enabled shaderFloat64
profile. Logical FP32 and FP64 masks are retained separately. If the wide optional
device request fails, retry the existing FP32 atomic profile before dropping its
optional extension. Ordinary FP64 arithmetic does not depend on FP64 atomics.

Shader validation classifies 32/64-bit typed pointers, matching result types and
the separate AtomicFloat32AddEXT/AtomicFloat64AddEXT declarations. A declared but
unused add capability still requires an enabled add feature of that precision.
Buffer atomics include logical and physical-storage pointers; shared atomics use
their own bits. Source modules, scope/memory operands and eight-byte array strides
remain unchanged. The native operation retains double rounding and ordering,
including order-dependent addition; it is not a reproducible summation algorithm.

[FloatAtomic64.Consumer](../../tests/FloatAtomic64.Consumer/README.md) supplies the
common scenarios: 64 contending increments of 2^-52 above one, unique prior values,
NaN payload/signed zero, infinity/subnormal exchange, explicit scoped base operations,
add-only/shared-only profiles, mixed precision, helper arrays and stale descriptors,
double specialization, both graphics stages with actual color pixels, a following
compute consumer, physical buffers and an unused capability. Prepared buffer cycles
assert zero owner-thread managed bytes. The test shim exercises base/add and
buffer/shared logical gates separately, real wide-request and arithmetic refusal,
and healthy FP32 buffer/shared/image consumers. The actual preceding ABI1 bridge
checks fallback and continued ordinary double arithmetic.

Historical Linux x64/Wayland Vulkan execution on RTX 3090 Ti passed source authoring
(48 assertions) and the six refusal profiles plus the preceding ABI1 bridge.
The interrupted qualification did not establish managed-package or strict full-trim
NativeAOT execution, and its ignored native/fixture receipts were not retained in
the Windows migration. These results are partial platform progress under ADR 0021.
Windows x64/Vulkan requalification on RTX 3090 Ti (driver 621.14) passes 48 source
and 25 compiler-free binary assertions in each of ProjectReference, fresh managed
package and strict full-trim NativeAOT execution (.NET 10.0.14, SDK 10.0.403 for
AOT). All six refusal profiles pass in CLR and AOT: base 28, add 23, buffer 26,
shared 25, wide 29 and precision 29 assertions. A complete module relinked with the
exact preceding ABI1 query passes 29 assertions in each host; qualified module
bytes are restored afterward. The root guards pass 152 checks. Pinned spirv-val
validates 13 original and 14 production-adapted stages, including the double
specialization override. FP32 atomics (74 assertions), Int64 atomics and ordinary
GLSL/HLSL FP64 compute/graphics also pass. The native checker reports physical
FP32 mask 63 / FP64 mask 15 and passes its ABI/resource/lifetime guards.

The Windows x64 native closure passes architecture, exact exports, private loader
identities and SHA-256 audit. The managed-package consumer uses this source-built
closure through explicit overrides of the preceding platform package; this is
not qualification of the complete Windows package/RID cohort. Windows D3D12,
other desktop architectures, macOS, Linux/X11 and ARM64, Android, Android TV, iOS,
tvOS and Web delivered-package/host behavior remain open;
missing adapters and absent evidence do not establish equivalent behavior. Other
native/driver heap totals, validation layers, broad performance and physical owner
acceptance also remain unverified. Receipts, fixtures, rendered pixels and exact
source/native/package identities are retained outside the repository under
`C:/Users/eduar/.codex/artifacts/Electron2D/float64-20261106`.
The current common module SHA-256 is
`75ec47177a2fd62b241424c59d07779366b7052b2ae2b95e6c17f4a60c91cbc0`;
the managed package SHA-256 is
`a870df8333130b6e801048a28d355e9d6d83bf46602a5a8d008d3cc703e95af8`.

## Verification

[FloatAtomic32.Consumer](../../tests/FloatAtomic32.Consumer/README.md) uses only the
public API. It verifies 64 contending buffer/shared/image contributions and unique
prior values, NaN payload and negative-zero exchange bits, sequential dispatches,
ordinary/fixed descriptors, independent layers and four samples, invalid descriptor
selection, explicit scoped load/store/exchange, both graphics stages and actual
color pixels, physical buffers and descriptor retirement. Prepared buffer/shared
update/dispatch/span-readback cycles assert zero owner-thread managed bytes.

The root runner's ELECTRON2D_TEST_FLOAT_ATOMIC_REQUIREMENTS=1 checks storage classes,
base/add distinctions, unsupported capabilities, malformed/result-mismatched operands
and the feature-chain ABI. ELECTRON2D_TEST_FLOAT_ATOMICS=1 runs source authoring;
a fixture directory runs binary-only execution. The native checker validates query
ABI/null guards, bounded/repeatable feature masks and adjacent resource behavior.
The Linux test-only SDL dynamic shim refuses the extension request, verifying
fallback, false format availability, shader/binary refusal and healthy ordinary
FP32 compute. The preceding render bridge checks the missing-export path separately.

Local qualification covers Linux x64 Vulkan on the RTX 3090 Ti, original/derived
SPIR-V validation, a managed package consumer and strict full-trim NativeAOT with
reflection-based JSON disabled and trimming/compiler warnings treated as errors.
Foreign platforms/backends, native heap totals, broad performance and owner
acceptance remain unverified. Package bytes and local run identities are retained
in ignored bin/float-atomics/2026-10-29 evidence, outside the engine commit.

Native requirements follow the [Vulkan feature contract](https://docs.vulkan.org/refpages/latest/refpages/source/VkPhysicalDeviceShaderAtomicFloatFeaturesEXT.html)
and [SPIR-V addition extension](https://raw.githubusercontent.com/KhronosGroup/SPIRV-Registry/main/extensions/EXT/SPV_EXT_shader_atomic_float_add.asciidoc).
