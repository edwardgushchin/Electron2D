# MultiMeshGPUDraw

Last updated: 2026-10-18

**Declaration:** `internal readonly record struct` · **Source:** [MultiMeshGPUDraw.cs](../../src/Servers/Rendering/MultiMeshGPUDraw.cs).

Immutable canvas-batch description of raw instance state, source topology/index ranges/LODs, full canvas transform/modulation, canonical schema/capacity, indirect mode and interpolation policy. It borrows retained arrays and owns no native handle.

[Mesh buffer contract and verification](../components/meshes.md#writable-instance-device-buffers). Internal implementation; no consumer API.

Original instance identity is carried as two exact numerical 16-bit halves in private translation padding. Canvas vertex composition restores the original 32-bit builtin for indirect subranges and per-instance LOD commands; legacy streams retain their native builtin. GLSL/HLSL command pixels include source ID 65537. Private buffers that were never exported retire their device metadata after pending work, while expired exported identities remain available for explicit FreeRID.
