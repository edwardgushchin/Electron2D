# InstanceUniformState

Last updated: 2026-10-11

- Declaration: `internal sealed class InstanceUniformState`
- Source: [InstanceUniformState.cs](../../src/Servers/Rendering/InstanceUniformState.cs)
- Component: [shader-materials](../components/shader-materials.md#per-item-shader-parameters)
- Visibility: internal; unavailable to engine consumers.

Owns one canvas item's copied typed values and their nullable stored descriptors. Private Value/Value<T> carriers retain logical declaration shape, signedness and boolean width. Set validates before replacing an existing value; null erases its authored storage entry. Get returns a compatible value/default or an absent nullable result. ShaderUniform supplies one canonical scalar/vector packing path.

Prepare weakly caches only buffers containing instance declarations for each immutable program. Alternating mesh surface programs reuse independent prepared buffers; retired program keys do not remain retained by the item. It clears padding and writes matching values at reflected offsets before each native draw. Shared MaterialState buffers remain untouched. First use of a program allocates cold buffers; prepared set/get/submission reuses storage. StoredParameter reconstructs supported nullable dynamic descriptors during packed/file scene restoration without an attached shader. CanvasItem disposal drops the state.

InstanceUniformTests exercises scalar/vector layouts, reload compatibility, archive reconstruction, independent pixels and managed allocation gates. Native and foreign platform limits follow the linked component contract.
