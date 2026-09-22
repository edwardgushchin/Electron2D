# Texture API coverage

Last updated: 2026-09-22

The reference Texture and Texture2D contracts share one Electron2D Texture page under [ADR 0004](../../decisions/product.md#adr-0004). Each source declaration remains accounted for below.

## Godot Texture

Godot source: [doc/classes/Texture.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Resource](Resource.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Texture`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |

## Godot Texture2D

Godot source: [doc/classes/Texture2D.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Texture](Texture.md#godot-texture). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Texture2D`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _draw(RID to_canvas_item, Vector2 pos, Color modulate, bool transpose) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _draw_rect(RID to_canvas_item, Rect2 rect, bool tile, Color modulate, bool transpose) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method _draw_rect_region(RID to_canvas_item, Rect2 rect, Rect2 src_rect, Color modulate, bool transpose, bool clip_uv) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method _get_format() -> int [Image.Format]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _get_height() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _get_image() -> Image`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _get_mipmap_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _get_width() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _has_alpha() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _has_mipmaps() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method _is_pixel_opaque(int x, int y) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method create_placeholder() -> Resource`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method draw(RID canvas_item, Vector2 position, Color modulate = Color(1, 1, 1, 1), bool transpose = false) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method draw_rect(RID canvas_item, Rect2 rect, bool tile, Color modulate = Color(1, 1, 1, 1), bool transpose = false) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method draw_rect_region(RID canvas_item, Rect2 rect, Rect2 src_rect, Color modulate = Color(1, 1, 1, 1), bool transpose = false, bool clip_uv = true) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | Trigger: first SDL3 GPU 2D rendering slice (ADR 0028). |
| [`method get_format() -> int [Image.Format]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_height() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_image() -> Image`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_mipmap_count() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_size() -> Vector2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method get_width() -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method has_alpha() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
| [`method has_mipmaps() -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Texture2D.xml) | — | Blocked | GUI: trigger is the first typed 2D GUI and theme slice after rendering (ADR 0028). |
