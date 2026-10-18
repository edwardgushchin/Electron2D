# MultiMeshGPUState

Last updated: 2026-10-18

**Declaration:** `internal sealed class` · **Source:** [MultiMeshGPUState.cs](../../src/Servers/Rendering/MultiMeshGPUState.cs).

Weak source/session ownership of current, private previous and optional command device buffers. Exact merged dirty spans preserve unrelated GPU records. Deferred history copies/resets obey explicit snapshot replacement. Command layouts retain identities across content revisions, replacing them for mesh/reference/layout changes. Preparation guards callback reentry and unstable resource/mesh state; expired private history retires through RenderingDevice.

[Mesh buffer contract and verification](../components/meshes.md#writable-instance-device-buffers). Internal implementation; no consumer API.
