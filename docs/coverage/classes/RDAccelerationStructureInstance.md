# RDAccelerationStructureInstance API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/RDAccelerationStructureInstance.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDAccelerationStructureInstance.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [RefCounted](RefCounted.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class RDAccelerationStructureInstance`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDAccelerationStructureInstance.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property RID blas = RID()`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDAccelerationStructureInstance.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int flags [RenderingDevice.AccelerationStructureInstanceFlagBits] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDAccelerationStructureInstance.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int hit_sbt_range = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDAccelerationStructureInstance.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int id = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDAccelerationStructureInstance.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property int mask = 255`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDAccelerationStructureInstance.xml) | — | Excluded | Direct rendering-device public types conflict with the backend-neutral 2D API decision (ADR 0028). |
| [`property Transform3D transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/RDAccelerationStructureInstance.xml) | — | Excluded | 3D-only signature is outside ADR 0004; no implementation trigger. |
