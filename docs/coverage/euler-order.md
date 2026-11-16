# Euler order applicability audit

Last updated: 2026-11-16

Pinned reference: `4.7.2-stable`, commit `ed1daf0bf001b61586d9930840f2f1394092c079`. Owning decision: [ADR 0004](../decisions/product.md#adr-0004). This audit applies the existing 2D boundary; it introduces no architectural adaptation or runtime declaration.

## Declarations and consumers

The [global declaration](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/@GlobalScope.xml) contains `EulerOrder` and six values. All seven coverage rows are Excluded, without an implementation trigger:

| Declaration | Value | Decision |
| --- | ---: | --- |
| EulerOrder | enum | Selects a sequence of spatial rotations in excluded consumers below. |
| EULER_ORDER_XYZ | 0 | Intrinsic XYZ spatial rotation order; excluded by ADR 0004. |
| EULER_ORDER_XZY | 1 | Intrinsic XZY spatial rotation order; excluded by ADR 0004. |
| EULER_ORDER_YXZ | 2 | Intrinsic YXZ spatial rotation order and default; excluded by ADR 0004. |
| EULER_ORDER_YZX | 3 | Intrinsic YZX spatial rotation order; excluded by ADR 0004. |
| EULER_ORDER_ZXY | 4 | Intrinsic ZXY spatial rotation order; excluded by ADR 0004. |
| EULER_ORDER_ZYX | 5 | Intrinsic ZYX spatial rotation order; excluded by ADR 0004. |

| Public reference consumer | Contract | Existing coverage decision |
| --- | --- | --- |
| [Basis.from_euler](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Basis.xml#L82-L104) | Vector3 angles, int order=2; composes spatial rotation. | [Basis](classes/Basis.md): Excluded, ADR 0004. |
| [Basis.get_euler](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Basis.xml#L128-L140) | int order=2; decomposes spatial rotation. | Basis: Excluded, ADR 0004. |
| [Quaternion.get_euler](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Quaternion.xml#L115-L121) | int order=2; decomposes spatial rotation. | [Quaternion](classes/Quaternion.md): Excluded, ADR 0004. |
| [Quaternion.from_euler](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Quaternion.xml#L94-L100) | Fixed YXZ; no selectable order parameter. | Quaternion: Excluded, ADR 0004. |
| [Node3D.rotation_order](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Node3D.xml) | EulerOrder property default=2, with set_rotation_order/get_rotation_order. | [Node3D](classes/Node3D.md): Excluded, ADR 0004. |

The Basis and Quaternion XML parameters are plain `int`, with the enum relationship in prose. Searching enum-typed parameters alone misses them. Composition and decomposition reverse the application sequence; this distinction does not change applicability. Fixed YXZ and all six selectable orders belong to the same excluded spatial rotation contract.

## Preserved numeric roles

[ADR 0033](../decisions/core-math.md#adr-0033) retains Vector3/Vector3i and Vector4/Vector4i as ordinary numeric tuples, including shader uniforms. [ADR 0028](../decisions/rendering.md#adr-0028) retains numeric matrices, column layout/repacking, shader arithmetic and numeric axis-angle helpers. Their component count or ability to carry rotation-like numbers does not exclude them. These roles do not require the reference EulerOrder selector. The Basis/Quaternion decisions concern their spatial transformation role, not every numeric matrix.

Only the seven global classification rows change. No Vector3, shader, Basis, Quaternion or Node3D row changes, no public API is added or removed, and no numeric enum or inert adapter is introduced. Compiled API snapshot equality and the bounded coverage delta guard this boundary. Runtime/native/visual profiles need no repeat for this documentation-only classification change; they are not newly qualified here.

## Complete source search

The pinned archive was searched for `EulerOrder` and `EULER_ORDER_` across C++ headers/sources, C#, GDScript and class XML (`.h`, `.cpp`, `.cs`, `.gd`, `.xml`). Hits occur in 22 files; the durable QA receipt records each path, line and full-file SHA-256.

- `core/math/math_defs.h` declares XYZ, XZY, YXZ, YZX, ZXY, ZYX in order; `core/core_constants.cpp` exports all six. None is deprecated.
- `core/math/basis.h/.cpp` additionally implement rotate/rotated, get_euler_normalized, set_euler and set_euler_scale. These operate on the excluded spatial Basis, including all six composition/decomposition cases.
- `core/math/quaternion.h/.cpp` delegates ordered decomposition to Basis. `core/variant/variant_call.cpp` binds the three selectable math methods with YXZ defaults; `variant_caster.h` registers enum conversion. These bindings introduce no independent numeric consumer.
- `scene/3d/node_3d.h/.cpp` stores and exposes rotation order; `scene/3d/path_3d.cpp` locks spatial path-follow rotation using YXZ.
- The generic 6DOF joint implementation uses XYZ decomposition. Jolt generic 6DOF and joint code use ZYX spatial frames. All are outside the 2D physics boundary of ADR 0004.
- `modules/mono/glue/GodotSharp/GodotSharp/Core/Basis.cs` and `Quaternion.cs` expose the same spatial math contracts. `tests/core/math/test_basis.cpp` and `test_quaternion.cpp` verify them; they add no product role.
- The remaining four files are the global, Basis, Quaternion and Node3D class XML listed above. No other matching source consumer was found in the scanned languages.

The 2D renderer's numeric `VisualShaderNodeMatrixCompose`, for example, composes Vector3 columns directly into numeric matrix data without an Euler-order selector. Its source, all other production code, the compiled API and existing numeric coverage rows remain unchanged.
