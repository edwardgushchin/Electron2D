# GPUCanvasBackend.InstanceStream

Last updated: 2026-10-18

**Declaration:** `private sealed class` · **Source:** [GPUCanvasBackend.MultiMeshes.cs](../../src/Servers/Rendering/GPUCanvasBackend.MultiMeshes.cs).

Reusable native output instance and indirect-command allocations with independent capacity growth. Backend disposal releases both handles; failed native handle creation leaves the prior completed allocation valid.

[Mesh buffer contract and verification](../components/meshes.md#writable-instance-device-buffers). Internal implementation; no consumer API.

Original instance identity is carried as two exact numerical 16-bit halves in private translation padding. Canvas vertex composition restores the original 32-bit builtin for indirect subranges and per-instance LOD commands; legacy streams retain their native builtin. GLSL/HLSL command pixels include source ID 65537. Private buffers that were never exported retire their device metadata after pending work, while expired exported identities remain available for explicit FreeRID.
