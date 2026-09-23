# ArrayMesh API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/ArrayMesh.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Mesh](Mesh.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ArrayMesh`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method add_blend_shape(StringName name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method add_surface_from_arrays(int primitive [Mesh.PrimitiveType], Array arrays, Array[] blend_shapes = [], Dictionary lods = {}, int flags [Mesh.ArrayFormat] = 0) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method clear_blend_shapes() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method clear_surfaces() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method get_blend_shape_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method get_blend_shape_name(int index) -> StringName`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method lightmap_unwrap(Transform3D transform, float texel_size) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`method regen_normal_maps() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method set_blend_shape_name(int index, StringName name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_find_by_name(String name) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_get_array_index_len(int surf_idx) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_get_array_len(int surf_idx) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_get_format(int surf_idx) -> int [Mesh.ArrayFormat]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_get_name(int surf_idx) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_get_primitive_type(int surf_idx) -> int [Mesh.PrimitiveType]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_remove(int surf_idx) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_set_name(int surf_idx, String name) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_update_attribute_region(int surf_idx, int offset, PackedByteArray data) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_update_skin_region(int surf_idx, int offset, PackedByteArray data) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`method surface_update_vertex_region(int surf_idx, int offset, PackedByteArray data) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`property int blend_shape_mode [Mesh.BlendShapeMode] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
| [`property AABB custom_aabb = AABB(0, 0, 0, 0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
| [`property ArrayMesh shadow_mesh`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ArrayMesh.xml) | — | Blocked | Trigger: first typed 2D mesh-data and MeshInstance2D renderer slice (ADR 0028). |
