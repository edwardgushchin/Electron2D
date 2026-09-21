# Electron2D product architecture decisions

Last updated: 2026-09-21

This bounded log owns the complete architectural records for product architecture. Use [the decision index](index.md) to route other work; read only the affected logs and explicitly linked dependencies.

Decisions in this log: [0001](#adr-0001), [0002](#adr-0002), [0004](#adr-0004), [0012](#adr-0012), [0017](#adr-0017), [0021](#adr-0021), [0027](#adr-0027), [0030](#adr-0030).

<a id="adr-0001"></a>
## ADR 0001: Use typed C# without Variant

Last updated: 2026-09-20

- Status: Accepted
- Scope: Entire engine API

### Context

Godot uses `Variant` as a universal value container for scripting, dynamic property access, generic signals, serialization, and editor integration. Electron2D is a C# engine library and currently has no GDScript-compatible runtime or editor that requires this dynamic boundary.

### Decision

Electron2D will not implement `Variant`. Public APIs use concrete types, generics, overloads, typed collections, properties, methods, delegates, and events. The engine will not recreate Variant through pervasive `object`, `dynamic`, or untyped metadata dictionaries.

### Consequences

- Invalid type combinations are rejected at compile time.
- IDE completion, navigation, and refactoring work normally.
- The engine avoids boxing and dynamic dispatch imposed by a universal container.
- Electron2D is not source-compatible with GDScript or Godot's string-based `Get`, `Set`, and `Call` APIs.
- Serialization, when introduced, must use typed models rather than a universal runtime value.

### Rejected alternatives

- Clone Godot `Variant`: rejected because its main consumers do not exist in Electron2D.
- Use `object` or `dynamic` as an implicit Variant: rejected because it loses type safety while providing a weaker contract.
- Add Variant pre-emptively for a future editor or scripting language: rejected until such a boundary is actually designed.

<a id="adr-0002"></a>
## ADR 0002: Represent game signals with typed C# events

Last updated: 2026-09-20

- Status: Accepted; connection lifecycle extended by [0010](core-object-runtime.md#adr-0010)
- Scope: Public event and callback APIs

### Context

Game objects need decoupled notifications such as death, health changes, collisions, and tree membership changes. Godot's core signal implementation supports dynamic names, `Callable`, `Variant` arguments, editor connections, deferred delivery, and one-shot flags. Electron2D has chosen a typed C# API without Variant.

### Decision

Discrete game notifications use typed C# events, normally `event Action` or `event Action<T...>`. Subscription uses `+=` and removal uses `-=`. Concrete types declare their own events; `ElectronObject` is not a string-addressed signal registry.

Core contracts that are known to be required may be declared before their producer component when their semantics are stable. `ElectronObject.ScriptChanged` is such a contract: the protected notifier exists now because scripting is confirmed for a later phase, while script attachment and initialization remain explicitly unimplemented.

Per-frame work does not use events. Implemented scene and fixed-step processing use direct `SceneTree` calls and virtual `Node` callbacks; future rendering, input routing, and other traversal follow the same direct-call rule.

### Lifetime rule

A shorter-lived subscriber must unsubscribe from a longer-lived publisher as part of its lifecycle. Direct `+=` subscriptions require matching `-=` cleanup; ADR 0010 adds `EventConnection` as the preferred owned token when deterministic cleanup, one-shot, or deferred delivery is needed. `ElectronObject` and `Node` clear the event subscriber lists they own during disposal, but this cannot remove a disposed subscriber from a different longer-lived publisher. Automatic weak events are not implemented.

### Consequences

- Signal names and arguments are compile-time checked.
- Renames are refactorable and discoverable by the IDE.
- There is no `Connect`, `Disconnect`, `EmitSignal`, `SignalName`, or `Callable` compatibility layer.
- Original decision: deferred and one-shot delivery were not signal features. ADR 0010 supersedes this point with typed `EventConnection` wrappers while deferred scheduling remains a separate `SceneTree` responsibility under ADR 0006.
- Exceptions from handlers follow normal multicast delegate behavior.

### Rejected alternatives

- Recreate Godot's dynamic signal bus: rejected because it requires the Variant/string infrastructure intentionally excluded by ADR 0001.
- Use events for every frame: rejected because direct calls make hot control flow and ordering explicit.
- Add Reactive Extensions: rejected because standard C# events cover the current requirement without another dependency.

<a id="adr-0004"></a>
## ADR 0004: Build a 2D-only scene-oriented engine in one assembly

Last updated: 2026-09-21

- Status: Accepted; external-dependency packaging amended by [0012](product.md#adr-0012), runtime target matrix defined by [0021](product.md#adr-0021), editor/game product boundary amended by [0027](product.md#adr-0027), and rendering backend strategy defined by [0028](rendering.md#adr-0028)
- Scope: Entire product architecture and packaging

The one-assembly rule remains fully effective for every production runtime-engine type and domain owned by Electron2D. ADR 0012 changes the treatment of explicitly approved third-party runtime dependencies: they may ship as separate assemblies instead of being internalized into `Electron2D.dll`. ADR 0027 clarifies that separately shipped editor and game executable assemblies are runtime consumers rather than engine-domain assemblies.

### Context

Electron2D is intended to provide a familiar high-level API modeled on Godot's 2D engine concepts while using SDL3-CS below that API. The product is a focused 2D engine rather than a general 2D/3D engine. Its engine surface must be distributed as one DLL named `Electron2D.dll`.

### Decision

- Electron2D supports only two-dimensional games.
- Three-dimensional rendering, physics, transforms, cameras, assets, nodes, compatibility aliases, and speculative shared 2D/3D abstractions are outside scope.
- The high-level API follows Godot's 2D concepts, lifecycle, composition model, and recognizable naming where they remain compatible with the typed C# decisions in ADR 0001 and ADR 0002.
- All production runtime-engine domains and components compile into `Electron2D.csproj` with assembly name `Electron2D`, producing one managed engine assembly: `Electron2D.dll`.
- Tests, examples, benchmarks, analyzers, and development tools may use separate projects because they are not shipped as parts of the engine.
- The first-party editor is a separately shipped self-hosted application and games are separate executables; both consume the public runtime under ADR 0027 and are not runtime-domain assemblies.
- SDL3-CS is the intended low-level backend but is not integrated yet. ADR 0028 selects its GPU API as the primary future 2D renderer and SDL_Renderer as a reduced-capability fallback without claiming either as implemented.

### Packaging boundary

The current verified engine artifact is the managed `Electron2D.dll`. No SDL binding or native SDL binary is currently part of the build. When SDL3-CS is integrated, its managed binding must not create a second shipping engine assembly.

Native SDL deployment is a separate unresolved platform constraint. This ADR does not claim that native SDL code has already been embedded into the managed DLL or that a physical one-file native deployment has been achieved. That decision requires an implemented and verified SDL integration.

### Consequences

- Production code remains easy to reference: consumers add one engine assembly.
- Domain boundaries are namespaces and documentation boundaries, not assembly boundaries.
- Features designed only to preserve a possible future 3D path must be rejected.
- Godot familiarity does not imply GDScript, Variant, binary, scene-format, or source compatibility where another accepted ADR explicitly differs.
- A managed dependency that would be copied as another shipping engine DLL must be internalized, source-integrated, embedded, or rejected after a specific packaging review.

### Rejected alternatives

- Add a minimal 3D layer for future use: rejected because 3D is outside the product.
- Split domains into separate production assemblies: rejected because the engine contract is one `Electron2D.dll`.
- Claim literal single-file SDL deployment before integration: rejected because it would document an unverified state.

<a id="adr-0012"></a>
## ADR 0012: Permit external runtime dependencies and select Box2D.NET

Last updated: 2026-09-21

- Status: Accepted
- Scope: Product boundary, deployment packaging, and future 2D physics
- Amends: external-dependency packaging in [0004](product.md#adr-0004); its one-assembly rule for Electron2D-owned code remains accepted

### Context

All code owned by Electron2D is constrained to a single managed DLL. The original packaging decision also applied that constraint to third-party managed dependencies, which would require vendoring their source or merging their assemblies. Vendoring would transfer upstream synchronization and local-fork maintenance to Electron2D, while assembly merging would add build and diagnostics complexity without improving the public API.

The engine remains exclusively 2D and should continue exposing one coherent public API from `Electron2D.dll`. Physical deployment, however, may contain separately maintained runtime dependencies when an explicit architectural decision accepts them.

`Box2D.NET` is a pure managed C# port suitable for the intended .NET 8 physics domain. The dependency has been selected, but no package reference or physics implementation exists in the current build.

### Decision

- Electron2D remains a 2D-only engine. Three-dimensional types, behavior, and speculative shared 2D/3D abstractions remain outside scope.
- Every Electron2D-owned production domain and public or internal production type compiles into `Electron2D.csproj` and `Electron2D.dll`. Electron2D code must never be split into additional assemblies.
- Explicitly approved third-party runtime dependencies may ship as separate assemblies because they are not Electron2D-owned code. A physical one-DLL deployment of the complete dependency graph is not required.
- The future 2D collision and rigid-body domain will use the pure managed `Box2D.NET` package maintained at `ikpil/Box2D.NET` as an external runtime dependency.
- `Box2D.NET` source must not be vendored, copied into, or merged with `Electron2D.dll`. The exact package version will be pinned when the physics vertical slice is implemented.
- Electron2D's public API must not expose dependency-owned types. Physics nodes, resources, queries, contacts, and errors will use Electron2D types, with dependency translation kept behind the physics-domain boundary.
- Dependency upgrades are explicit changes requiring license review, release-note review, compatibility tests, regression tests, and physics benchmarks appropriate to the affected behavior.
- The package must not be added before executable physics behavior uses it. Selection is an accepted design decision, not an implemented physics feature.
- ADR 0028 selects the future SDL GPU and SDL_Renderer roles, but SDL3-CS integration and native SDL deployment remain unresolved and require an implemented packaging decision.

### Consequences

- The one-DLL rule remains an invariant for Electron2D-owned code. Once physics is integrated, applications will deploy that one `Electron2D.dll` together with the selected external `Box2D.NET` runtime assembly and any later explicitly approved platform dependencies.
- Upstream physics fixes can be consumed through package upgrades instead of maintaining an Electron2D source fork.
- Consumers see Electron2D-owned physics APIs and are insulated from ordinary dependency upgrades, subject to Electron2D's documented compatibility policy.
- Electron2D must preserve the dependency's license notice in distributions as required by its license.
- The current build remains unchanged and still produces only `Electron2D.dll`; no claim of implemented physics or dependency packaging is made until the physics vertical slice lands.

### Rejected alternatives

- Vendor the dependency source: rejected because it makes upstream synchronization and local modifications an Electron2D maintenance responsibility.
- Merge the dependency assembly into `Electron2D.dll`: rejected because it complicates builds, symbols, diagnostics, licensing audits, trimming, and upgrades solely to preserve a physical-file constraint.
- Use the native upstream library: rejected for the current design because it adds platform-specific native binaries and interop ownership when a managed backend is available.
- Implement a new rigid-body solver: rejected because physics-engine development is not Electron2D's differentiating scope.
- Add the selected package immediately without a physics implementation: rejected as an unused runtime dependency.

<a id="adr-0017"></a>
## ADR 0017: Source-tree module layout

Last updated: 2026-09-21

### Status

Accepted for runtime source. The earlier planned `src/Editor` placement is superseded by [ADR 0027](product.md#adr-0027), which places the editor application in a separate project root.

### Context

Runtime production C# files previously lived at the repository root. That made the growing engine surface difficult to navigate and did not expose the architectural ownership already recorded by the domain and component documents.

The reference engine does not divide runtime source into only `Core` and `Editor`: the current object, main-loop, engine, resource, localization, node, and scene-tree types belong to `core/object`, `core/os`, `core/config`, `core/io`, `core/string`, and `scene/main`. Electron2D currently has no editor production code.

### Decision

All Electron2D-owned runtime production C# files live under `src/`. When a type corresponds to a reference-engine type, its directory mirrors that source module with C# casing: `src/Core/Object`, `src/Core/OS`, `src/Core/Config`, `src/Core/IO`, `src/Core/String`, or `src/Scene/Main`.

Physical directories express source ownership only. All current public types retain the flat `Electron2D` namespace, so this refactor does not break consumers or create nested API namespaces. `Electron2D.csproj` disables default compile discovery and includes only `src/**/*.cs`; tests remain under `tests/` and documentation under `docs/`.

No `src/Editor` application directory is created. ADR 0027 supersedes that earlier future placement: editor-only production source belongs to the separate executable project root `editor/Electron2D.Editor/`, while reusable runtime capabilities remain in their owning `src/` modules.

### Consequences

- Production source has an explicit root and cannot accidentally include test or repository-support C# files.
- File placement exposes Core-versus-Scene ownership while the public API remains source-compatible.
- The reserved editor root makes application ownership explicit without implying that an editor project or implementation exists today.
- Moving a production type between modules requires updating its class page, component/domain ownership, inventory, and this decision chain when architectural ownership changes.

### Rejected alternatives

- Only `src/Core` and `src/Editor`: current nodes and scene-tree types belong to the Scene module, while no editor implementation exists.
- Nested public namespaces matching every directory: this would be an unnecessary breaking API change for a physical-layout refactor.
- Keeping implicit SDK compilation for every repository C# file: an explicit `src/**/*.cs` boundary more accurately enforces the production-source rule.

### Verification boundary

The Release build and executable test project verify that all moved sources still compile into `Electron2D.dll` with unchanged public namespaces and behavior. They do not validate future module placement for types that do not yet exist.

### References

- [Object module](https://github.com/godotengine/godot/blob/master/core/object/object.h)
- [Main-loop module](https://github.com/godotengine/godot/blob/master/core/os/main_loop.h)
- [Engine configuration module](https://github.com/godotengine/godot/blob/master/core/config/engine.h)
- [Resource module](https://github.com/godotengine/godot/blob/master/core/io/resource.h)
- [Translation module](https://github.com/godotengine/godot/blob/master/core/string/translation_server.h)
- [Node module](https://github.com/godotengine/godot/blob/master/scene/main/node.h)
- [Scene-tree module](https://github.com/godotengine/godot/blob/master/scene/main/scene_tree.h)

<a id="adr-0021"></a>
## ADR 0021: Cross-platform runtime target matrix

Last updated: 2026-09-21

### Status

Accepted.

### Context

The product is a 2D engine whose runtime must be usable on desktop and mobile systems. Earlier decisions fixed the 2D-only scope, the single Electron2D-owned assembly, and the future SDL3-CS host boundary, but did not define the complete supported target matrix. Without an explicit matrix, platform-specific code could accidentally turn a development host into the product boundary or make unverified portability claims.

### Decision

The Electron2D runtime targets exactly these operating-system families:

- Linux;
- Windows;
- macOS;
- Android;
- iOS.

One public runtime API and one set of documented semantics applies across all five targets. Platform-specific implementation belongs behind internal backends or host integration boundaries and must not create divergent public type sets. Portable .NET facilities and the selected low-level backend are preferred; direct native calls are used only for capabilities they cannot provide.

An unavailable platform capability must fail explicitly with the documented exception or capability result. Empty implementations, silent no-ops, and success results without performed work are prohibited.

Target intent, implemented code, successful compilation, application packaging, automated tests, and native-device verification are separate states. Documentation must state each state accurately. A feature may be described as cross-platform verified only after it has run on every applicable target.

The one-assembly rule continues to cover Electron2D-owned runtime code. Native libraries, approved external managed dependencies, platform application hosts, signing, and store packaging remain deployment concerns and are not implied to be contained in `Electron2D.dll`.

A future editor or development tool may intentionally support fewer host platforms when its document and ADR state that narrower boundary. That exception cannot leak a desktop-only requirement into the game runtime or its public data model.

### Current implementation boundary

The current project targets `net8.0` and its executable verification has run on Linux. Some file-system code contains macOS and Windows backends, but they have not been exercised on native hosts. There is no SDL application host, mobile target project, Android package, iOS application bundle, signing pipeline, or five-platform CI matrix. Therefore this ADR establishes the required product target, not a claim that distributable applications for all five platforms already exist.

ADR 0028 selects a capability-driven GPU-primary and SDL_Renderer-fallback architecture for future rendering. It does not establish that either backend initializes, renders, or supports shaders on any target yet; those claims require backend-specific native-host verification.

### Consequences

- Every runtime domain and component must preserve the five-platform contract as it evolves.
- Platform-specific dependencies require an accepted packaging and lifecycle decision before integration.
- Platform support reports must distinguish compilation from native execution and packaging.
- Mobile lifecycle, permissions, storage, input, suspension, and graphics integration remain work for their owning future domains rather than placeholders in Core.
- A platform-specific optimization is acceptable only when a portable behaviorally equivalent path or an explicit documented capability boundary remains.

### Rejected alternatives

- Treat desktop support as the product and add mobile later: rejected because Android and iOS are first-class runtime targets.
- Maintain separate public APIs per operating system: rejected because game code needs one portable engine contract.
- Claim support from successful compilation alone: rejected because native dependencies, lifecycle, packaging, and device behavior remain unverified.
- Require the future editor to run on mobile: rejected because editor-host support is separate from runtime portability.

### Related decisions

- [0004: 2D API in one Electron2D-owned assembly](product.md#adr-0004)
- [0012: External runtime dependencies and Box2D.NET](product.md#adr-0012)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0015: Main-loop lifecycle and host boundary](core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](core-object-runtime.md#adr-0016)
- [0017: Source-tree module layout](product.md#adr-0017)
- [0020: Typed file access and transformed-file containers](core-data-io.md#adr-0020)
- [0028: GPU-first 2D rendering, shaders, and SDL_Renderer fallback](rendering.md#adr-0028)

<a id="adr-0027"></a>
## ADR 0027: Self-hosted editor and game project boundary

Last updated: 2026-09-21

### Status

Accepted. This decision amends ADR 0004 by limiting its one-assembly rule to the Electron2D runtime engine, and amends ADR 0017 by placing editor application source outside the runtime `src/` tree.

### Context

Electron2D is both a reusable 2D runtime and the foundation on which its own editor and games are built. The editor must exercise the same engine-facing scene, rendering, input, resource, UI, and application lifecycle capabilities available to games instead of becoming a parallel application framework with privileged private behavior.

The current repository contains one runtime project, one executable test project, an empty tracked `examples/` root, and no editor implementation. The runtime compiles only `src/**/*.cs` into `Electron2D.dll`. Mixing future editor classes into that project would ship editor-only code to every game and mobile runtime, while compiling the editor from the same `src/` tree into another project would blur ownership and risk duplicate type definitions.

### Decision

The repository has three one-way product layers:

1. `src/` and `Electron2D.csproj` contain only portable runtime engine code and produce the single Electron2D-owned runtime assembly `Electron2D.dll`.
2. `editor/Electron2D.Editor/` is the reserved root for a future standalone editor executable project and all editor-only production source. The editor project will reference `Electron2D.csproj`/`Electron2D.dll`; it will not compile runtime source files into its own assembly.
3. `examples/<Game>/` is the root for first-party example games and templates. Each game is an independent executable project that references `Electron2D.csproj`/`Electron2D.dll`.

The editor and every first-party game must be built with Electron2D's public runtime API. Their scenes, UI, rendering, input, resources, and lifecycle must use Electron2D facilities as those domains become implemented. A minimal .NET entry point, platform launcher, packaging metadata, and backend bootstrap are allowed, but they must only start/host Electron2D; they must not become an alternative game or editor UI framework.

Dependency direction is strict:

```text
Electron2D.Editor executable ─┐
                             ├──> Electron2D.dll ──> approved runtime dependencies
Game/example executable ─────┘
```

`Electron2D.dll` must never reference the editor or any game/example assembly. The editor must not receive blanket friend-assembly access, use reflection to bypass runtime encapsulation, or link runtime source directly. When editor work exposes a missing reusable capability, that capability must be designed and implemented in its owning runtime domain through the normal production-ready process. Truly editor-only behavior remains in the editor project.

The editor is a separately shipped first-party product assembly and therefore does not violate the one-runtime-DLL rule. Its packaging may contain its executable assembly, `Electron2D.dll`, approved managed/native dependencies, and content. Runtime portability remains Linux, Windows, macOS, Android, and iOS; the editor may support a narrower documented desktop host matrix without changing runtime semantics.

No editor project, executable, domain, component, or production type is implemented by this ADR. The tracked directory is only a repository boundary. The first editor implementation must add its real project, tests, XML documentation, living class/component/domain documents, inventory rows, build verification, and packaging status atomically.

### Consequences

- Games and the editor continuously dogfood the public runtime contract.
- Editor-only code and dependencies cannot leak into `Electron2D.dll` or mobile game deployments.
- Runtime and editor can have different entry points, target frameworks, host matrices, and packaging while sharing the same engine API revision.
- The repository may contain more than one first-party project/assembly even though engine runtime code still produces exactly one Electron2D-owned DLL.
- A future editor feature cannot justify a private shortcut around normal runtime API ownership and verification.
- The current build remains unchanged because no fake editor project or placeholder type is introduced.

### Rejected alternatives

- **Compile editor source into `Electron2D.dll`:** rejected because every game would receive editor-only code and dependencies.
- **Place editor source under `src/Editor` but compile it separately:** rejected because `src/` is the enforced runtime compilation root and duplicate include/exclude rules would make ownership fragile.
- **Use an unrelated desktop UI framework for the editor:** rejected because it would not validate Electron2D's game-facing UI/render/input stack and would create a second application model.
- **Give the editor blanket internal access:** rejected because it would let the editor succeed while ordinary games cannot reproduce the same workflows.
- **Create an empty editor project now:** rejected because its target framework, host, entry point, SDL integration, and UI/render dependencies do not exist yet; a compiling shell would be a misleading placeholder.
- **Move the editor to another repository:** rejected because the editor and runtime must evolve and be verified against the same source revision.

### Verification boundary

The repository boundary is verified by directory placement and current project compile includes. Existing runtime checks prove only that reserving `editor/Electron2D.Editor/` does not alter `Electron2D.dll`. Self-hosting, editor startup, UI, rendering, packaging, and desktop-platform behavior remain unimplemented and unverified.

### Related decisions

- [0004: 2D API in one Electron2D-owned assembly](product.md#adr-0004)
- [0021: Cross-platform runtime target matrix](product.md#adr-0021)
- [0028: GPU-first 2D rendering, shaders, and SDL_Renderer fallback](rendering.md#adr-0028)

<a id="adr-0030"></a>
## ADR 0030: Use bounded domain decision logs

Last updated: 2026-09-21

### Status

Accepted.

### Context

The first 29 decisions lived in separate files, which forced maintainers and language models to discover many small documents and their refinement links. A trial consolidation into one 1,577-line file removed discovery overhead but would consume excessive context even for a change affecting one domain.

Core is substantially larger than the other domains, so one file for all Core decisions would recreate the same problem at a smaller scale.

### Decision

`docs/decisions/index.md` is the lightweight routing entry point. Complete ADR records are grouped into bounded logs:

- `product.md`;
- `core-object-runtime.md`;
- `core-data-io.md`;
- `core-math.md`;
- `scene.md`;
- `resources.md`;
- `localization.md`;
- `rendering.md`.

A maintainer reads the index, the affected log, and only the other logs explicitly referenced by relevant decisions. ADR numbers and `adr-NNNN` anchors remain permanent. New decisions are appended to the narrowest owning log and added to the index. When a log would exceed 500 lines, it is split along a cohesive subdomain boundary before adding the decision.

Class, component, domain, and repository instruction documents may link directly to stable anchors, but they are not additional decision logs. Reversals append a new ADR and mark the old record superseded rather than rewriting history.

### Consequences

- Architecture work loads relevant context instead of all historical decisions.
- The routing index remains small enough to read on every architectural task.
- Core decisions stay grouped by meaningful subdomain rather than one oversized bucket.
- Git history preserves the provenance of former one-file-per-ADR paths.
- Routing metadata must be updated with each new or moved record.

### Rejected alternatives

- **Keep one file per ADR:** rejected because discovery and chained reads were excessive.
- **Use one repository-wide decision file:** rejected because the file alone would consume too much context.
- **Use one file for all Core decisions:** rejected because Core already contains several independent subdomains.
- **Keep summaries instead of full records:** rejected because rationale, alternatives, verification boundaries, and supersession history are required.
- **Retain redirect files:** rejected because duplicate files recreate discovery noise and can drift.

### Verification boundary

The migration preserves ADR 0001 through ADR 0029 as complete records, adds this routing decision as ADR 0030, and updates repository references to the new stable domain-log anchors. Link, anchor, size, and repository checks verify the current structure; Git history retains the old paths.
