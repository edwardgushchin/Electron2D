# GPUDeviceFactory

Last updated: 2026-10-29

Source: [GPUDeviceFactory.cs](../../src/Servers/Rendering/GPUDeviceFactory.cs).

Internal runtime helper. Acquire returns an independent RenderHandle lease on the
active process-local SDL GPU device. First acquisition negotiates optional Vulkan
sized points, independent optional FP64/Int64 and mirror-clamp, the core fallback and the existing Android relaxation policy.
SizedPoints and Relaxed stay fixed for that device lifetime; Metal uses native
point support. Debug mode remains disabled. Temporary native properties are
released after negotiation. CPU-only selection never calls this helper.

## Shared device owner

The private SharedDevice owns the canonical SafeHandle, capabilities and lease
count. Acquisition and final native destruction share one cold-operation gate.
Every lease calls this owner directly; it does not retain an earlier lease or
window. Native resources retain their own lease through ordinary RenderHandle
ownership. Final resource retirement disposes the canonical device and clears
the active owner, with no idle cache. SafeHandle fallback also releases leases.
Changing native driver hints takes effect after all active consumers retire.

See [device/window lifetime](../components/gpu-device-lifetime.md) and
[ADR 0054](../decisions/physics-backends.md#adr-0054).

Contract and verification: [canvas point programs](../components/canvas-points.md).

Native memory diagnostics use lightweight admission/counting to retain physical lifetime independently of the creation monitor. Final release closes admission and joins these readers before destruction; no user callbacks occur in native reads. See [native render memory](../components/render-memory.md).

The [FP64 contract](../components/local-compute.md#native-fp64-arithmetic-and-data) records Vulkan feature opt-in and fallback; retained private device properties identify successful feature negotiation.

The [wide integer profile](../components/local-compute.md#native-64-bit-integer-arithmetic-and-data) tries both, either and neither precision feature, preserving ordinary creation and each available capability.

The [multisample storage contract](../components/graphics-storage-images.md#multisample-storage-images) adds independently negotiated Vulkan storage-image samples. The bounded shader guard checks the declared capability and actual multisample storage type before native publication; optional feature refusal retains the other precision, storage and raster capabilities.
