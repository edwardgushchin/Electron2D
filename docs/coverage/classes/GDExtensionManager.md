# GDExtensionManager API coverage

Last updated: 2026-09-22

Godot source: [doc/classes/GDExtensionManager.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Object](Object.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class GDExtensionManager`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`enum LoadStatus`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`enum_value LOAD_STATUS_ALREADY_LOADED [LoadStatus] = 2`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`enum_value LOAD_STATUS_FAILED [LoadStatus] = 1`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`enum_value LOAD_STATUS_NEEDS_RESTART [LoadStatus] = 4`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`enum_value LOAD_STATUS_NOT_LOADED [LoadStatus] = 3`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`enum_value LOAD_STATUS_OK [LoadStatus] = 0`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`method get_extension(String path) -> GDExtension`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`method get_loaded_extensions() -> PackedStringArray`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`method is_extension_loaded(String path) -> bool`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`method load_extension(String path) -> int [GDExtensionManager.LoadStatus]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`method load_extension_from_function(String path, const GDExtensionInitializationFunction* init_func) -> int [GDExtensionManager.LoadStatus]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`method reload_extension(String path) -> int [GDExtensionManager.LoadStatus]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`method unload_extension(String path) -> int [GDExtensionManager.LoadStatus]`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`signal extension_loaded(GDExtension extension) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`signal extension_unloading(GDExtension extension) -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
| [`signal extensions_reloaded() -> void`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/GDExtensionManager.xml) | — | Blocked | Trigger: an accepted typed scripting or extension-host contract and its first executable slice (ADR 0001). |
