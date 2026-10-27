# Marker2D API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Marker2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marker2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node2D](Node2D.md). Electron2D type: [`public class Electron2D.Marker`](../../classes/Marker.md).

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Marker2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marker2D.xml) | [`public class Electron2D.Marker`](../../classes/Marker.md) | Implemented | ADRs 0004/0008/0023/0027/0078/0090: typed canvas guides execute real editor-role drawing and passive workspace preview, retained clear/restore, inherited transforms/layout, scene/file factories and guarded stored settings. Public CLR/NativeAOT consumers and Linux Wayland GPU/compatibility pixels verify the connected behavior; native allocator, other platforms and owner acceptance remain separate. See docs/components/editor-canvas-guides.md. |
| [`property float gizmo_extents = 10.0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marker2D.xml) | [`public System.Single GizmoExtents { get; set; }`](../../classes/Marker.md) | Implemented | ADRs 0004/0008/0023/0027/0078/0090: typed canvas guides execute real editor-role drawing and passive workspace preview, retained clear/restore, inherited transforms/layout, scene/file factories and guarded stored settings. Public CLR/NativeAOT consumers and Linux Wayland GPU/compatibility pixels verify the connected behavior; native allocator, other platforms and owner acceptance remain separate. See docs/components/editor-canvas-guides.md. |
