# ADR 0017: Source-tree module layout

Last updated: 2026-09-21

## Status

Accepted for runtime source. The earlier planned `src/Editor` placement is superseded by [ADR 0027](0027-self-hosted-editor-and-games.md), which places the editor application in a separate project root.

## Context

Runtime production C# files previously lived at the repository root. That made the growing engine surface difficult to navigate and did not expose the architectural ownership already recorded by the domain and component documents.

The reference engine does not divide runtime source into only `Core` and `Editor`: the current object, main-loop, engine, resource, localization, node, and scene-tree types belong to `core/object`, `core/os`, `core/config`, `core/io`, `core/string`, and `scene/main`. Electron2D currently has no editor production code.

## Decision

All Electron2D-owned runtime production C# files live under `src/`. When a type corresponds to a reference-engine type, its directory mirrors that source module with C# casing: `src/Core/Object`, `src/Core/OS`, `src/Core/Config`, `src/Core/IO`, `src/Core/String`, or `src/Scene/Main`.

Physical directories express source ownership only. All current public types retain the flat `Electron2D` namespace, so this refactor does not break consumers or create nested API namespaces. `Electron2D.csproj` disables default compile discovery and includes only `src/**/*.cs`; tests remain under `tests/` and documentation under `docs/`.

No `src/Editor` application directory is created. ADR 0027 supersedes that earlier future placement: editor-only production source belongs to the separate executable project root `editor/Electron2D.Editor/`, while reusable runtime capabilities remain in their owning `src/` modules.

## Consequences

- Production source has an explicit root and cannot accidentally include test or repository-support C# files.
- File placement exposes Core-versus-Scene ownership while the public API remains source-compatible.
- The reserved editor root makes application ownership explicit without implying that an editor project or implementation exists today.
- Moving a production type between modules requires updating its class page, component/domain ownership, inventory, and this decision chain when architectural ownership changes.

## Rejected alternatives

- Only `src/Core` and `src/Editor`: current nodes and scene-tree types belong to the Scene module, while no editor implementation exists.
- Nested public namespaces matching every directory: this would be an unnecessary breaking API change for a physical-layout refactor.
- Keeping implicit SDK compilation for every repository C# file: an explicit `src/**/*.cs` boundary more accurately enforces the production-source rule.

## Verification boundary

The Release build and executable test project verify that all moved sources still compile into `Electron2D.dll` with unchanged public namespaces and behavior. They do not validate future module placement for types that do not yet exist.

## References

- [Object module](https://github.com/godotengine/godot/blob/master/core/object/object.h)
- [Main-loop module](https://github.com/godotengine/godot/blob/master/core/os/main_loop.h)
- [Engine configuration module](https://github.com/godotengine/godot/blob/master/core/config/engine.h)
- [Resource module](https://github.com/godotengine/godot/blob/master/core/io/resource.h)
- [Translation module](https://github.com/godotengine/godot/blob/master/core/string/translation_server.h)
- [Node module](https://github.com/godotengine/godot/blob/master/scene/main/node.h)
- [Scene-tree module](https://github.com/godotengine/godot/blob/master/scene/main/scene_tree.h)
