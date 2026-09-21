# Sprite3D API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/Sprite3D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [SpriteBase3D](SpriteBase3D.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Sprite3D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property int frame = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property Vector2i frame_coords = Vector2i(0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property int hframes = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property bool region_enabled = false`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property Rect2 region_rect = Rect2(0, 0, 0, 0)`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property Texture2D texture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`property int vframes = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal frame_changed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Excluded | 3D/XR product scope is excluded by ADR 0004; no implementation trigger. |
| [`signal texture_changed() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Sprite3D.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
