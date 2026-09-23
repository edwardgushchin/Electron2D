# Marshalls API coverage

Last updated: 2026-09-23

Godot source: [doc/classes/Marshalls.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marshalls.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class Marshalls`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marshalls.xml) | — | Excluded | Base64 and UTF-8 conversion use System.Convert and System.Text; Variant serialization is excluded by ADR 0001. No engine-owned wrapper is needed. |
| [`method base64_to_raw(String base64_str) -> PackedByteArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marshalls.xml) | — | Excluded | Base64 and UTF-8 conversion use System.Convert and System.Text; Variant serialization is excluded by ADR 0001. No engine-owned wrapper is needed. |
| [`method base64_to_utf8(String base64_str) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marshalls.xml) | — | Excluded | Base64 and UTF-8 conversion use System.Convert and System.Text; Variant serialization is excluded by ADR 0001. No engine-owned wrapper is needed. |
| [`method base64_to_variant(String base64_str, bool allow_objects = false) -> Variant`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marshalls.xml) | — | Excluded | Base64 and UTF-8 conversion use System.Convert and System.Text; Variant serialization is excluded by ADR 0001. No engine-owned wrapper is needed. |
| [`method raw_to_base64(PackedByteArray array) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marshalls.xml) | — | Excluded | Base64 and UTF-8 conversion use System.Convert and System.Text; Variant serialization is excluded by ADR 0001. No engine-owned wrapper is needed. |
| [`method utf8_to_base64(String utf8_str) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marshalls.xml) | — | Excluded | Base64 and UTF-8 conversion use System.Convert and System.Text; Variant serialization is excluded by ADR 0001. No engine-owned wrapper is needed. |
| [`method variant_to_base64(Variant variant, bool full_objects = false) -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/Marshalls.xml) | — | Excluded | Base64 and UTF-8 conversion use System.Convert and System.Text; Variant serialization is excluded by ADR 0001. No engine-owned wrapper is needed. |
