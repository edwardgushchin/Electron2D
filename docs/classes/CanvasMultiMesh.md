# CanvasMultiMesh

Last updated: 2026-10-18

Internal rendering helper; source [CanvasMultiMesh.cs](../../src/Servers/Rendering/CanvasMultiMesh.cs).

Retains borrowed MultiMesh and a reusable CanvasMesh. Append serializes against resource mutation, obtains interpolated visible bounds, conservatively grows bounds by one framebuffer pixel for point/line width and pixel snapping, culls against framebuffer/scissor, then emits surface-ordered geometry. Eligible unskinned triangle surfaces use frame-owned GPU instance records; compatibility, snapping and compositor/notifier geometry retain CPU expansion. Custom callback storage/revision/disposal changes reject before native submission. Clear releases borrowed resource/cache references while retaining the reusable wrapper; reset copies current resource records into previous presentation storage. Cold wrapper preparation is separate from warmed replay.


[ADR 0092](../decisions/mesh.md#adr-0092) and [mesh component](../components/meshes.md#repeated-instance-resources) define ownership and verification. This helper adds no public API. MultiMeshTests and MultiMeshRenderingTests exercise successful, invalid, callback-failure and warmed paths.

## Executable mesh skin integration

Attached palette skin is now passed to CanvasMesh. Replay bypasses authored/rest bounds conservatively for live palette attachments so deformation into the viewport survives culling; ordinary instances keep their existing bounds/scissor checks.

Owned canvas command clear/record/replay reuses that wrapper without retaining removed mesh resources; RenderingCanvasTests measures 128 combined Mesh/MultiMesh command cycles.

The internal Mesh accessor supports parameter discovery from surface shaders before recording. Append forwards its canvas owner to surface/hardware batches. See [per-item shader parameters](../components/shader-materials.md#per-item-shader-parameters).

## Writable instance consumer

Exported/indirect MultiMesh storage selects a retained GPU consumer. CanvasMesh captures source vertices/indices, palette deformation, LOD metadata and finite authored canvas state. CanvasMultiMesh retains manual/authored bounds and resource guards. GPUCanvasBackend owns conversion outputs, native index/LOD buffers and indirect commands; the private kernel interpolates raw device records and chooses each instance's LOD. CanvasVertexProgram snaps ordinary instance positions before the authored stage and procedural homogeneous output after it; built-in instance vertices support snapping and group screen UVs. Group color premultiplication, masks and original vertex IDs remain connected. Relaxed Android devices bind each LOD instance separately when nonzero indirect first-instance support is unavailable. See [ownership and Linux-native verification limits](../components/meshes.md#writable-instance-device-buffers).
