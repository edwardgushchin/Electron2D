# ResourceSaver API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/ResourceSaver.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ResourceSaver`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`enum SaverFlags`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`enum_value FLAG_BUNDLE_RESOURCES [SaverFlags] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`enum_value FLAG_CHANGE_PATH [SaverFlags] = 4`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`enum_value FLAG_COMPRESS [SaverFlags] = 32`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`enum_value FLAG_NONE [SaverFlags] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`enum_value FLAG_OMIT_EDITOR_PROPERTIES [SaverFlags] = 8`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`enum_value FLAG_RELATIVE_PATHS [SaverFlags] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`enum_value FLAG_REPLACE_SUBRESOURCE_PATHS [SaverFlags] = 64`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`enum_value FLAG_SAVE_BIG_ENDIAN [SaverFlags] = 16`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method add_resource_format_saver(ResourceFormatSaver format_saver, bool at_front = false) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method get_recognized_extensions(Resource type) -> PackedStringArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method get_resource_id_for_path(String path, bool generate = false) -> int`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method remove_resource_format_saver(ResourceFormatSaver format_saver) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method save(Resource resource, String path = "", int flags [ResourceSaver.SaverFlags] = 0) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method set_uid(String resource, int uid) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ResourceSaver.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
