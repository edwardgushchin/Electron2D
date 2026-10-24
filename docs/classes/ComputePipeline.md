# ComputePipeline

Last updated: 2026-10-24

**Declaration:** `internal sealed unsafe class ComputePipeline : IDisposable`

**Source:** [ComputePipeline.cs](../../src/Servers/Rendering/ComputePipeline.cs) ·
**Component:** [Local compute](../components/local-compute.md#metal-storage-buffer-lengths)

Owns one native compute pipeline and, when its Metal translation needs runtime
array lengths, a 128-byte readonly size table and upload transfer. Each native
resource retains the device through `RenderHandle`. Disposal releases both table
resources and the pipeline; construction failure releases every adopted handle.

`BufferLayout` records original uniform, readonly and writable buffer counts.
`PrepareBufferSizes` validates complete native binding counts, clears a stack
table and records actual byte sizes at their original Metal buffer indices. A
cycled upload captures each dispatch independently. `BindBufferSizes` binds that
table after the original readonly buffers. Other backends and programs without
runtime array-length queries use the ordinary pipeline without these resources.

RenderingDevice owns this object with its shader program, including specialized
variants and binary/placeholder creation. Retained particle step, process,
subemission, ordering and vertex pipelines use the same owner. Physics and
instance-pack kernels retain the internal factory that requires no implicit
size-table binding.

The six focused particle cohorts pass on native Metal for ARM64 and x64 at
`025f1514f`; see [platform receipts](../platform-verification.md#runtime-suite-receipts).
Complete platform suites, other dispatch variants and physical performance
retain their separate acceptance gates.
