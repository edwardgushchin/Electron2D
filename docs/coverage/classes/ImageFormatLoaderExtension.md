# ImageFormatLoaderExtension API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/ImageFormatLoaderExtension.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImageFormatLoaderExtension.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [ImageFormatLoader](ImageFormatLoader.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class ImageFormatLoaderExtension`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImageFormatLoaderExtension.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method _get_recognized_extensions() -> PackedStringArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImageFormatLoaderExtension.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method _load_image(Image image, FileAccess fileaccess, int flags [ImageFormatLoader.LoaderFlags], float scale) -> int [Error]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImageFormatLoaderExtension.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method add_format_loader() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImageFormatLoaderExtension.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
| [`method remove_format_loader() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/ImageFormatLoaderExtension.xml) | — | Blocked | Assets: trigger is the first concrete loader and native-backed asset slice (ADR 0013/0023). |
