# GPUCanvasBackend.InstanceParameters

Last updated: 2026-10-18

**Declaration:** `private struct` · **Source:** [GPUCanvasBackend.MultiMeshes.cs](../../src/Servers/Rendering/GPUCanvasBackend.MultiMeshes.cs).

Sequential 112-byte private compute uniform block: canvas basis/origin, modulation, raw layout/capacity/flags, source geometry offsets, LOD metadata and interpolation/dispatch dimensions. It is not serialized or public API.

[Mesh buffer contract and verification](../components/meshes.md#writable-instance-device-buffers). Internal implementation; no consumer API.
