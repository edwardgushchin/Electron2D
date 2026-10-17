# InstancePlaceholder API coverage

Last updated: 2026-10-17

Godot source: [doc/classes/InstancePlaceholder.xml](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InstancePlaceholder.xml) at `4.7.2-stable` (`ed1daf0bf001b61586d9930840f2f1394092c079`).

Godot base: [Node](Node.md). Electron2D type: —.

Inherited declarations are recorded on their declaring base-class pages; the base link above gives the complete chain.

| Godot API | Electron2D API | State | Reason / implementation trigger |
| --- | --- | --- | --- |
| [`class InstancePlaceholder`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InstancePlaceholder.xml) | — | Blocked | Loader and actual linked runtime templates/overrides exist under ADRs 0013/0023; prerequisite: deferred scene-record storage, typed pending-property queries and failure-safe instance replacement/ownership together with Node load-placeholder flag producers. A path flag alone cannot defer a concrete scene. Raw Dictionary values remain excluded by ADR 0017; typed property queries must preserve stored order. |
| [`method create_instance(bool replace = false, PackedScene custom_scene = null) -> Node`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InstancePlaceholder.xml) | — | Blocked | Loader and actual linked runtime templates/overrides exist under ADRs 0013/0023; prerequisite: deferred scene-record storage, typed pending-property queries and failure-safe instance replacement/ownership together with Node load-placeholder flag producers. A path flag alone cannot defer a concrete scene. Raw Dictionary values remain excluded by ADR 0017; typed property queries must preserve stored order. |
| [`method get_instance_path() -> String`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InstancePlaceholder.xml) | — | Blocked | Loader and actual linked runtime templates/overrides exist under ADRs 0013/0023; prerequisite: deferred scene-record storage, typed pending-property queries and failure-safe instance replacement/ownership together with Node load-placeholder flag producers. A path flag alone cannot defer a concrete scene. Raw Dictionary values remain excluded by ADR 0017; typed property queries must preserve stored order. |
| [`method get_stored_values(bool with_order = false) -> Dictionary`](https://github.com/godotengine/godot/blob/ed1daf0bf001b61586d9930840f2f1394092c079/doc/classes/InstancePlaceholder.xml) | — | Blocked | Loader and actual linked runtime templates/overrides exist under ADRs 0013/0023; prerequisite: deferred scene-record storage, typed pending-property queries and failure-safe instance replacement/ownership together with Node load-placeholder flag producers. A path flag alone cannot defer a concrete scene. Raw Dictionary values remain excluded by ADR 0017; typed property queries must preserve stored order. |
