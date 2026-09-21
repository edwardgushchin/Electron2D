# ADR 0019: Typed project settings and directory-backed virtual paths

Last updated: 2026-09-21

## Status

Accepted.

## Context

The reference project-settings singleton combines a universal-value registry, defaults and editor metadata, feature-tag overrides, dirty/change notification, project/custom persistence, project discovery, resource/user path conversion, script-class discovery, and resource-pack mounting. Electron2D has already rejected `Variant`, `object`, and `dynamic` as public value boundaries, has no editor/scripting/resource-pack domains, and must not introduce inert settings for systems that do not exist. It does have a production `ConfigFile`, typed property descriptors, process Engine, and directory-backed development projects.

## Decision

`ProjectSetting<T>` is the immutable public setting identity: full name, JSON-snapshotted default, and optional typed validator. `ProjectSettings` accepts only exact registered definitions for value access. Internally heterogeneous definitions are type-erased behind private generic entries; no untyped value crosses the public boundary.

`ProjectSettings.Instance` is the non-disposable runtime registry and an Engine built-in singleton. Public constructors provide isolated disposable registries. Every registry starts with implemented application/timing definitions; other domains add definitions only with executable consumers. Engine timing properties use active feature overrides from the process registry, so the settings layer is the single source rather than a duplicate bag.

Values persist through `ConfigFile`. Unknown entries survive loading. Registration validates preloaded data. The current per-registry initial value is implicit: changing it preserves the observable current value, reset removes explicit base storage, and save omits a registered base value equal to its initial value. Main load replaces the document only after complete parsing and validation; custom load merges transactionally and preserves unsaved names that existed before the merge. Re-entrant mutation from a validator is rejected during load; validators must otherwise be pure and thread-safe. Saves reorder known entries using setting order, atomically replace the destination, and clear internal unsaved tracking only on success. The public changed-setting list is instead the current coalesced notification batch and is consumed only after event delivery. Standard C# exceptions replace numeric error codes.

Feature overrides are explicit typed operations. Tags normalize to lowercase; the first matching stored override wins. Built-in active tags describe managed runtime, release/debug build, current OS, and process architecture. Custom feature changes queue one coalesced typed event for affected override owners. Engine flushes one pending event after a successful process callback; standalone hosts can call `FlushChanges()` directly.

`res://` and `user://` map to configured directories. Resolution blocks lexical `..` escape, supports reverse localization and upward project discovery, and explicitly does not claim symbolic-link or packed-filesystem security/semantics.

Script global classes, pack mounting, editor-only metadata/UI, and settings for absent domains remain documented deferrals. Three-dimensional settings are permanently excluded.

## Consequences

- Callers get compile-time value types, reusable definitions, validator-backed writes, mutable snapshot isolation, and deterministic override selection.
- Unknown custom settings can be loaded before the owning game/domain registers their type.
- Process timing configuration is persistable and feature-aware without adding allocation to warmed empty frames.
- File I/O, serializers, validators, metadata snapshots, and path conversion remain non-realtime operations.
- Directory virtual paths are useful now without pretending exported resource packs exist.
- `project.e2d` and `override.cfg` use Electron2D's `ConfigFile` format; third-party project-file compatibility is not promised.

## Rejected alternatives

- `Dictionary<string, object>`, `dynamic`, JSON DOM, or string-based generic conversion: these recreate the rejected universal-value boundary.
- One static property per possible setting: it cannot support game-defined settings and would add inert absent-domain configuration.
- Reflection discovery of arbitrary fields/properties: it hides registration, validation, persistence ownership, and trimming behavior.
- Immediate event delivery from every setter: it permits event storms and does not match end-of-frame change observation.
- Silent empty methods for global classes or packs: their required domains do not exist.
- Treating lexical normalization as a secure filesystem sandbox: symlinks require a different threat model and OS-specific handle validation.

## Verification boundary

Executable tests cover the typed registry, exact identity, mutable snapshots, validators/rollback/re-entry, metadata/property discovery, feature precedence/normalization, unsaved/version/event semantics, persistence/overrides/order/unknown entries, path traversal and discovery, concurrency, disposal, and Engine integration/allocation. They do not verify editor behavior, archive mounts, hostile symlink races, unavailable domain settings, or every filesystem's crash durability.

## References

- [Reference ProjectSettings documentation](https://docs.godotengine.org/en/stable/classes/class_projectsettings.html)
- [Reference ProjectSettings header](https://github.com/godotengine/godot/blob/master/core/config/project_settings.h)
- [Reference ProjectSettings implementation](https://github.com/godotengine/godot/blob/master/core/config/project_settings.cpp)
- [ADR 0018: Typed configuration files](0018-typed-config-files.md)
- [ADR 0001: Typed C# without Variant](0001-typed-csharp-without-variant.md)
