# Electron2D repository instructions

These instructions apply to the entire repository.

## Documentation is part of the implementation

Every engine domain, component, and production type must have a living document that describes its current implemented state exactly.

- Domain documents live in `docs/domains/`.
- Component documents live in `docs/components/`.
- Production class, struct, interface, enum, and delegate documents live in `docs/classes/`.
- `docs/decisions/index.md` routes durable architectural decisions to bounded domain logs under `docs/decisions/`; ADR records use permanent `adr-NNNN` anchors. See ADR 0030 in `docs/decisions/product.md`.
- `docs/inventory.md` is the exhaustive map from domains to components, production types, sources, and documents.
- Test-only helpers under `tests/` and generated files do not need separate class pages; their relevant coverage belongs in the documented production type or component.

A code change is incomplete until all affected documents are updated in the same change. This includes behavior, public API, ownership, lifecycle, threading, error behavior, dependencies, limitations, and verification. Never document planned behavior as implemented. Mark absent or proposed behavior explicitly.

When adding, renaming, moving, or deleting a production type:

1. Create, move, or remove its class document.
2. Update its component and domain documents.
3. Update `docs/inventory.md` and every affected link.
4. Append a new numbered ADR record to the narrowest owning decision log and update `docs/decisions/index.md` when the change introduces or reverses a durable design decision.

Before architectural work, read `docs/decisions/index.md`, the affected bounded log, and only cross-domain logs explicitly referenced by relevant ADRs. ADR numbers and anchors are permanent. Do not silently rewrite an accepted decision after the architecture changes; append a new ADR to the appropriate log, mark the old record superseded, update the routing index, and update current-state documents. Do not recreate one-file-per-decision ADRs or one repository-wide monolith. Keep each decision log below 500 lines; split it along a cohesive subdomain boundary before adding a record that would exceed that limit.

## Required document contents

Class documents must state:

- source path and declaration;
- responsibility and ownership;
- complete current public/protected API;
- lifecycle and state transitions;
- invariants and error behavior;
- threading guarantees and non-guarantees;
- dependencies and interactions;
- verified tests and known limitations.

Component documents must state their scope, owned types, runtime flow, dependencies, invariants, current implementation status, exclusions, and verification.

Domain documents must state their responsibility, component inventory, public surface, dependency direction, domain-wide invariants, current limitations, and relevant ADRs.

Update each document's `Last updated` date whenever its semantic content changes.

## Definition of done for "implement X"

When the user says "implement X" (including the Russian "реализуй X"), treat the request as a complete production-ready vertical slice, not as a minimal compiling skeleton.

Before implementation:

1. Compare `X` with the current official Godot API for the corresponding type or concept and its complete inheritance chain.
2. Build an explicit coverage inventory of relevant properties, methods, signals, notifications, lifecycle callbacks, state transitions, invariants, ordering rules, error behavior, and threading rules.
3. Classify every inventoried item as implemented now, intentionally adapted to typed C#, blocked by a missing engine domain, or permanently excluded by an accepted Electron2D decision. Do not silently omit items.

Implementation is complete only when:

- every item that does not depend on an absent domain is implemented with production behavior rather than a placeholder;
- lifecycle, callback and event ordering, ownership, disposal, exception safety, re-entrancy boundaries, thread affinity, and edge cases are handled explicitly;
- positive, negative, boundary, and failure-injection checks cover the contract, including exceptions thrown by user callbacks where callbacks can affect engine state;
- all affected class, component, domain, inventory, and decision documents describe the resulting current state exactly;
- a fresh post-implementation audit compares the delivered surface and semantics against the coverage inventory and official Godot contract;
- all required formatting, build, and executable checks pass.

Do not declare the implementation complete while any known P0, P1, or P2 defect remains in the implemented scope. Fix those defects first. If a required behavior depends on an engine domain that does not exist yet, record it explicitly in the owning documents as deferred with the missing dependency and intended integration boundary. Do not add empty methods, inert state, or misleading compatibility stubs.

The completion report must distinguish implemented behavior, deliberate typed-C# adaptations, dependency-blocked work, permanent exclusions, verification performed, and verification limits.

Electron2D has no first public release or compatibility users yet. Do not preserve behavior that the audit proves incorrect solely for compatibility. Correct it to the accepted contract, add boundary tests for the changed behavior, update XML and living documentation, and record the correction in an ADR. This rule does not authorize unrelated source breakage or silent changes to behavior that is already correct.

Implementation scope includes foreseeable members of the same public type family. When implementing or materially completing a type, audit its corresponding 2D sibling types as well. If a sibling belongs to the accepted Electron2D direction and has a concrete foreseeable role in an accepted future domain, implement that sibling production-ready in the same vertical slice even when no current code consumes it. Lack of a current consumer is not by itself a reason to defer such a type. Do not extend this rule to speculative types, excluded 3D families, or empty compatibility shells. See ADR 0035.

## Git completion and atomic commits

Every completed implementation must end with one task-scoped atomic local commit after code, tests, XML documentation, living documentation, ADRs, the post-implementation audit, and all required checks are complete.

- The commit message must follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/): `<type>[optional scope][!]: <description>`. Use an appropriate type such as `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `build`, `ci`, or `chore`; record breaking changes with `!` and/or a `BREAKING CHANGE:` footer.
- Stage only the implementation's cohesive code, tests, documentation, and repository metadata. Do not include unrelated user changes, generated output, local work logs, or temporary files.
- Do not create intermediate implementation commits. If verification or the post-implementation audit finds a defect, fix it before creating the single final commit.
- Do not declare an implementation complete until that commit exists and the implementation's tracked working tree is clean. Report its hash and subject in the completion response.
- Never amend, rebase, squash, rewrite, or push commits unless the user explicitly requests that operation.

## XML documentation is part of the public API

Every public production API declaration must have complete C# XML documentation in source. This includes namespaces where supported, classes, structs, interfaces, enums and every enum value, delegates, constructors, methods, properties, fields, constants, events, operators, indexers, and public generic parameters. Protected extension points must be documented to the same standard because engine consumers override them.

For an API corresponding to Godot, its XML documentation must faithfully reproduce the semantic content of the current official Godot documentation across the complete inheritance chain: purpose, units, coordinate space, defaults, ordering, lifecycle timing, ownership, side effects, constraints, caveats, and related members. Translate and adapt that contract to Electron2D's typed C# names and actual behavior; do not copy claims about Variant, GDScript, editor, renderer, input, physics, serialization, networking, or other behavior that Electron2D does not implement.

Godot is an internal design and API-coverage reference, not part of Electron2D's shipped identity. Never mention Godot by name in production source code, source-code comments, C# XML documentation, generated XML documentation, or any `README.md` file. Describe Electron2D behavior directly and self-sufficiently. Compatibility comparisons and source-attribution notes belong only in internal design documents such as ADRs, coverage inventories, and agent reports. Every documentation change must verify this rule with a case-insensitive repository search scoped to production code, generated XML documentation, and all `README.md` files.

XML documentation must use the applicable tags rather than placing the whole contract in `<summary>`:

- `<summary>` for the concise contract;
- `<param>` and `<typeparam>` for every parameter and generic parameter;
- `<returns>` or `<value>` for results and property value semantics;
- `<exception>` for every exception intentionally exposed by the implementation;
- `<remarks>` for lifecycle/order/threading details, deliberate typed-C# behavior, and limitations;
- `<see cref="..."/>` and `<paramref name="..."/>` for checked references.

Documentation must describe current executable behavior exactly. A public declaration with missing, stale, misleading, placeholder, or dependency-fiction XML documentation makes the implementation incomplete. The post-implementation audit must compare source XML documentation, living Markdown documentation, tests, implementation, and the official Godot contract. Required build verification must compile the generated XML documentation with missing/malformed documentation warnings treated as errors.

## Current architectural decisions

- Electron2D is exclusively a 2D engine. Do not add 3D types, APIs, render paths, physics, assets, examples, or abstractions. Its high-level API follows Godot's 2D concepts and naming where they fit typed C#. See ADR 0004 and `docs/decisions/product.md#adr-0012`, which amends only ADR 0004's external-dependency packaging rule.
- The Electron2D runtime targets Linux, Windows, macOS, Android, and iOS. Runtime APIs and semantics must remain portable across all five targets; isolate unavoidable platform code behind internal backends, fail explicitly for unavailable capabilities, and document build, implementation, packaging, and native-host verification separately. Do not claim cross-platform verification until every applicable target has been exercised. A future editor may support a narrower host set, but it must not change the portable runtime contract. See ADR 0021.
- Electron2D-owned runtime production code remains one managed assembly named `Electron2D.dll`; do not split runtime domains into additional assemblies. The separately shipped editor executable and game/example executables are consumers of that runtime, not additional runtime-domain assemblies. Separately shipped managed runtime dependencies are allowed only through an accepted ADR. `Box2D.NET` is approved as the future 2D-physics dependency and must remain an external package rather than vendored or merged source; it is not integrated yet. Native SDL packaging remains unresolved until SDL3-CS is integrated and must not be described as complete. See ADR 0012 and ADR 0027.
- Electron2D is a typed C# API. It has no `Variant`, `dynamic`, string-based `Get`/`Set`/`Call`, or untyped metadata bag. See `docs/decisions/product.md#adr-0001`.
- Electron2D owns its public math vocabulary. The canonical vector family is `Vector2`, `Vector2I`, `Vector4`, and `Vector4I`; the canonical rectangle and affine-transform types are `Rect` and `Transform`. Every engine domain uses these types in public/protected API and engine-owned state. `Vector4` and `Vector4I` are numeric tuples, not 3D/4D scene geometry. `Vector`, `VectorI`, old rectangle/transform names, external numerics vectors, and external matrix types do not cross the engine contract and have no compatibility aliases. See ADR 0032 and ADR 0033.
- Game signals are typed C# events. Frame, physics, rendering, and tree traversal use direct calls rather than events. See `docs/decisions/product.md#adr-0002`.
- Managed object memory is reclaimed by the runtime; `IDisposable` provides deterministic logical cleanup and native-handle release, not managed-memory reclamation. Native SDL handles must be wrapped in `SafeHandle` instead of adding finalizers to every engine object. See ADR 0003 and ADR 0014.
- The single thread that wins disposal may inspect the object from pre-delete and derived cleanup callbacks; every other caller is rejected after disposal starts. See `docs/decisions/core-object-runtime.md#adr-0009`.
- Notifications retain Godot's numeric IDs but use `Notify(int)` and overridable `OnNotification(int)`; editor exposure uses typed `PropertyDescriptor<TOwner, TValue>` rather than Variant dictionaries. See `docs/decisions/core-object-runtime.md#adr-0005`.
- `SceneTree` owns the node tree, processes one captured deferred batch at a time, and applies queued deletion after deferred actions. Scene mutation, flushing, and disposal are owner-thread operations. See `docs/decisions/scene.md#adr-0006`.
- `Timer` is the reusable Node-based countdown. It uses Node's internal process/physics lanes independently of public gameplay callbacks, emits one typed timeout at most once per frame, and receives both scaled and original Engine deltas so `IgnoreTimeScale` remains meaningful even when `TimeScale` is zero. `Start()`/`Start(double)` replace sentinel arguments. See `docs/decisions/scene.md#adr-0036`.
- `Tween` is the SceneTree-owned typed interpolation sequence. It uses generic values, explicit property accessors, delegates, and typed event accessors rather than dynamic values, reflection, or string paths; SceneTree advances captured process/physics batches after nodes and lightweight timers and before deferred work. Node-bound, nested, pause, looping, time-scale, failure, disposal, and owner-thread behavior follow ADR 0037. Do not add a second clock, task scheduler, public reference counting, or live-tween serialization placeholders. See `docs/decisions/scene.md#adr-0037`.
- Localization is provided by the process-wide, thread-safe `TranslationServer`; objects opt in per instance through a domain and translation-enabled flag. See `docs/decisions/localization.md#adr-0007`.
- Electron2D has one unified `Node`; there is no separate spatial-node subclass. `Node` combines hierarchy/lifecycle with `Vector2`/`Transform` 2D state, visibility, Z ordering, paths, groups, and process configuration. See `docs/decisions/scene.md#adr-0008` and ADR 0033.
- Electron2D is a Node-based, scene-oriented engine. `Node` is the primary public game-object model, `SceneTree` owns the active Node hierarchy, and a scene is a reusable packed Node hierarchy that may represent one game object, a composed subsystem, or a complete level. Reuse and composition happen through `PackedScene`; do not introduce a parallel public entity/ECS object model or treat scenes as level-only containers. Current packed scenes are detached, typed, and in-memory; file persistence, nested/inherited authoring, and editor workflows remain separate future work. See `docs/decisions/scene.md#adr-0031`.
- `Resource` uses managed memory, deterministic logical disposal, typed events, weak single-owner path registration, explicit derived duplication hooks that preserve graph identity, and a scene-local owner association used by packed-scene instancing. Do not add a public `RefCounted` lifetime protocol. A future resource manager may use internal disposable leases with reference counts solely to retain and release shared native-backed asset payloads; add that mechanism only with the first concrete loader/native-backed asset and verified ownership transitions. Renderer IDs, asset loading/saving, import IDs, and general resource serialization remain absent rather than stubbed. See ADR 0013, ADR 0014, and ADR 0023.
- `PackedScene` provides typed in-memory capture and detached runtime instantiation through storage-enabled property descriptors, `Node.Owner`, persistent groups, reusable source-independent node factories, live `SceneState` metadata, and alias-preserving scene-local resource duplication. File formats/loaders, scene inheritance authoring, editor edit states, placeholders, and persistent typed event endpoints remain explicitly deferred to their missing domains. See ADR 0023.
- Frame, fixed-step physics, future rendering, input dispatch, and audio-mixing hot paths must avoid steady-state managed allocations. Prefer preallocation, value types, bounded reusable buffers, and pools; add GC latency tuning or no-GC regions only after allocation and frame-time measurements demonstrate a need and define a safe memory budget. See ADR 0014.
- `MainLoop` is the owner-thread application lifecycle boundary and `SceneTree` is its concrete scene implementation. The process-wide `Engine` attaches one loop and converts host-supplied elapsed time into bounded fixed-step and process callbacks with time scaling, metrics, and no steady-state idle allocation. SDL still owns the real clock, event pump, waiting/presentation policy, native system-event generation, and permission requests. See ADR 0015 and ADR 0016.
- `ConfigFile` stores typed values addressed by `ConfigKey<T>`, uses JSON tokens inside a sectioned text format, merges only after complete parsing, writes through same-directory temporary files, and uses authenticated encryption. It does not expose `Variant`, `object`, JSON DOM values, virtual resource paths, or reference-counted lifetime. See ADR 0018.
- `ProjectSettings` uses exact `ProjectSetting<T>` identities, transactional `ConfigFile` persistence, deterministic feature overrides, coalesced typed change events, and directory-backed lexical `res://`/`user://` resolution. Engine timing settings come from the process registry. Script classes, resource packs, editor-only settings, and absent-domain settings remain deferred rather than stubbed. See ADR 0019.
- `FileAccess` provides lock-serialized blocking raw and whole-file transformed I/O over ordinary and directory-backed virtual paths. It uses typed modes, C# exceptions, strict UTF-8, BCL compression/hash/cryptography, authenticated AES-GCM envelopes, atomic transformed commits, native Unix permissions, and Linux/macOS/Windows extended attributes. FastLZ, Zstandard, and packed archives remain explicit dependency gaps; universal-value I/O and shared numeric error state are excluded. See ADR 0020.
- `DirAccess` provides scoped, lock-serialized directory state, streaming and sorted enumeration, nonrecursive mutations, deterministic temporary-directory ownership, links, native filesystem identity, and platform capability failures. Virtual instances capture one root; static two-path operations resolve both paths from one root snapshot. Android/iOS link and drive enumeration remain explicit host-integration gaps rather than false success. See ADR 0022.
- `Color` is a sequential four-float typed value with complete RGBA, HSV, OKHSL, packing, HTML/named parsing, arithmetic, comparison, and invariant-format behavior; `Colors` owns the 146-entry named catalog. Ordinary math retains HDR/IEEE values, while packed/HTML output uses deterministic clamped quantization. Finite colors use a strict four-field `ConfigFile` schema and are stored directly by typed packed scenes. See ADR 0024.
- `Mathf` is the canonical stateless scalar-math API. Its audited surface contains seven single-precision constants and 127 integer/float/double/decimal overloads; `Epsilon` is `1e-6f`, double approximation uses `1e-14`, and matching geometry/color/transform/node formulas must route through it. Do not add a legacy tolerance, compatibility switch, generic-math facade, injectable provider, or duplicate helper without a concrete missing contract. See ADR 0034.
- `Rect` and `RectI` are sequential 16-byte axis-aligned values over `Vector2` and `Vector2I`. Both preserve signed components, normalize only through `Abs`, use half-open point containment, provide typed conversions, use strict `Position`/`Size` `ConfigFile` schemas, and are stored directly by typed packed scenes. Floating `Rect` additionally provides approximation, finiteness, support, optional border intersection, and forward/reverse `Transform` bounds operators. `Side` retains four stable edge identities. See ADR 0035.
- `Transform` is a sequential 24-byte value over three `Vector2` columns with complete affine math, rectangle operators, strict finite configuration persistence, direct packed-scene storage, and direct `Node` integration. Do not add old-name aliases, automatic public external-numerics conversions, or a 3D transform type. See ADR 0026, ADR 0029, ADR 0032, and ADR 0033.
- The Electron2D editor and every first-party game/example must be written on the public Electron2D runtime API. Runtime source stays under `src/`; editor-only production source belongs to the separate future executable project under `editor/Electron2D.Editor/`; game/example projects belong under `examples/<Game>/`. Dependency direction is editor/game to `Electron2D.dll`, never the reverse. Do not grant the editor blanket friend-assembly access, compile runtime source into it, or use another UI/game framework as a parallel application model. The editor is not implemented yet; its tracked directory records only the repository boundary. See ADR 0027.
- The future 2D renderer uses the SDL3 GPU API as its primary backend and supports engine-provided and user-authored 2D shaders there. SDL_Renderer is the reduced-capability fallback for baseline 2D drawing and must never silently ignore or emulate arbitrary shaders. The public rendering API remains backend-neutral and typed, exposes capabilities explicitly, and fails before drawing when a required capability is unavailable. No rendering or shader API is implemented yet; do not add placeholders ahead of the first complete rendering vertical slice. See ADR 0028.

Do not contradict these decisions incidentally. If a task requires changing one, update the ADR chain and all affected current-state documents.

Runtime production source belongs to `Electron2D.csproj`. ADR 0027 authorizes a separate future editor executable project under `editor/Electron2D.Editor/` and independent game/example projects under `examples/<Game>/`; neither may own runtime-domain source. Any other production `.csproj` still requires an explicit architecture decision.

All runtime production C# source files live under `src/`. Their directories mirror the corresponding upstream engine module when one exists, for example `src/Core/Object/`, `src/Core/Config/`, and `src/Scene/Main/`. Directory placement does not change the flat public `Electron2D` namespace. Do not place editor application source under `src/Editor/`: editor-only production code belongs under `editor/Electron2D.Editor/` and must use its own namespace and project. Do not create empty runtime module directories for future code. See ADR 0017 and ADR 0027.

## Verification

Run the smallest relevant checks after every implementation and documentation change. The current full local checks are:

```bash
dotnet format Electron2D.csproj --verify-no-changes --no-restore
dotnet format tests/Electron2D.Tests/Electron2D.Tests.csproj --verify-no-changes --no-restore
dotnet build Electron2D.csproj -c Release
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
! rg -n -i 'godot' --glob '*.cs' --glob '*.csproj' --glob '**/[Rr][Ee][Aa][Dd][Mm][Ee].[Mm][Dd]' --glob '**/bin/**/*.xml' --glob '**/obj/**/*.xml' .
```

Record only commands that actually ran. Passing builds and tests do not prove runtime, visual, performance, or owner acceptance beyond their tested scope.
