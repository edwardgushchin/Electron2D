# Electron2D product architecture decisions

Last updated: 2026-09-25

This bounded document owns the current product architecture decisions. Use [the decision index](index.md) to route other work; read only the affected documents and explicitly linked dependencies.

Decisions in this log: [0001](#adr-0001), [0002](#adr-0002), [0004](#adr-0004), [0012](#adr-0012), [0017](#adr-0017), [0021](#adr-0021), [0027](#adr-0027), [0030](#adr-0030), [0045](#adr-0045), [0051](#adr-0051).

<a id="adr-0001"></a>
## ADR 0001: Use typed C# without Variant

Last updated: 2026-09-24

- Status: Accepted
- Scope: Entire engine API

### Context

Godot uses `Variant` as a universal value container for scripting, dynamic property access, generic signals, serialization, and editor integration. Electron2D is a C# engine library and currently has no GDScript-compatible runtime or editor that requires this dynamic boundary.

### Decision

Electron2D will not implement `Variant`. Public APIs use concrete types, generics, overloads, typed collections, properties, methods, delegates, and events. The engine will not recreate Variant through pervasive `object`, `dynamic`, or untyped metadata dictionaries.

Godot's typed `Packed*Array` container classes have no Electron2D-owned equivalents. Applicable public parameters and properties use C# typed arrays, spans, or other standard typed collections with ownership and copying specified at each API boundary. This excludes the duplicate container classes and their ordinary collection methods, not an in-scope feature merely because its reference signature uses a packed array. Specialized byte encoding, decoding, and compression operations require their own typed API mapping or explicit exclusion after a semantic audit.

This rule does not prohibit a dedicated JSON document model. JSON syntax trees are values within the JSON utility only; they are not a general engine value type, property store, signal payload, or settings container. [ADR 0048](core-data-io.md#adr-0048) defines that utility. [ADR 0018](core-data-io.md#adr-0018) and [ADR 0019](core-data-io.md#adr-0019) keep configuration and project settings typed.

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

Last updated: 2026-09-23

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

Last updated: 2026-09-24

- Status: Accepted; managed-dependency packaging specified by [0012](product.md#adr-0012), runtime target matrix defined by [0021](product.md#adr-0021), editor/game product boundary amended by [0027](product.md#adr-0027), and rendering backend strategy defined by [0028](rendering.md#adr-0028)
- Scope: Entire product architecture and packaging

The one-assembly rule covers Electron2D-owned code and the managed source of selected runtime dependencies under ADR 0012. SDL3-CS and the pinned Box2D.NET managed source are compiled into `Electron2D.dll`; the first executable physics profile is recorded in ADR 0054. ADR 0027 clarifies that separately shipped editor and game executable assemblies are runtime consumers rather than engine-domain assemblies.

### Context

Electron2D is intended to provide a familiar high-level API modeled on Godot's 2D engine concepts while using SDL3-CS below that API. The product is a focused 2D engine rather than a general 2D/3D engine. Its engine surface must be distributed as one DLL named `Electron2D.dll`.

### Decision

- Electron2D supports only two-dimensional games.
- Three-dimensional rendering, physics, transforms, cameras, assets, nodes, compatibility aliases, and speculative shared 2D/3D abstractions are outside scope.
- Three-component numeric values are allowed for ordinary data and shader uniforms under ADR 0033; their component count does not add a three-dimensional scene domain.
- The public image-texture resource is `Texture`; `ImageTexture` derives directly from it. All image textures are two-dimensional by product definition, so there is no `Texture2D` suffix or empty dimension-neutral parent. The reference `Texture` and `Texture2D` contracts share one `Texture.md` coverage page. Coverage names for `Texture2DArray`, `Texture2DArrayRD`, and `Texture2DRD` are `TextureArray`, `TextureArrayRD`, and `TextureRD`. Source identities and declarations remain intact inside the comparison; these names do not authorize new runtime types or change implementation states. Shader-language intrinsic names and internal backend identifiers retain their native spelling.
- The reference `Geometry2D` maps to Electron2D `Geometry`, because the product has no 3D geometry counterpart. Keep `Geometry2D` only as the pinned source identity in internal coverage; the public class page and source use `Geometry`. This naming change preserves the applicable geometry API obligation and does not mark absent methods implemented.
- The reference `AStar2D` and `AStarGrid2D` map to Electron2D `AStar` and `AStarGrid`. Both algorithms operate only in two dimensions, and their graph/grid roles remain distinct without dimensional suffixes. Keep the pinned names in internal coverage; ADRs 0052 and 0053 own their separate behavior contracts.
- The applicable 2D physics source types `Shape2D`, `CircleShape2D`, `RectangleShape2D`, `CollisionShape2D`, `CollisionObject2D`, `PhysicsBody2D`, `RigidBody2D` and `StaticBody2D` map to `Shape`, `CircleShape`, `RectangleShape`, `CollisionShape`, `CollisionObject`, `PhysicsBody`, `RigidBody` and `StaticBody`. The dimensional suffix is redundant in this product; ADR 0054 preserves their inheritance and tracks the first executable scene-body profile without claiming missing members complete.
- The reference `Line2D` maps to Electron2D `Line : Entity`, because this product has no 3D line-node counterpart. Keep `Line2D` only as the pinned source identity in internal coverage; production source, public signatures and class documentation use `Line`. The rename preserves all applicable line-node members and their behavior; unfinished members remain coverage gaps.
- The reference `Marker2D` maps to Electron2D `Marker : Entity`, without a redundant dimensional suffix. Keep `Marker2D` only as the pinned source identity in internal coverage. The rename preserves the applicable inherited spatial API and the editor gizmo obligation; it does not imply that editor drawing exists in the runtime.
- The reference `Polygon2D` maps to Electron2D `Polygon : Entity`, because this product has no 3D polygon-node counterpart. `Polygon2D` remains the pinned source identity in internal coverage; production source and public signatures use `Polygon`. The reference `polygon` vertex property maps to `Vertices`: C# cannot declare a member with the same name as its enclosing `Polygon` type. This preserves the vertex-list role; remaining behavior and dependencies are tracked per member in coverage.
- The reference `Parallax2D` maps to Electron2D `Parallax : Entity`. The public type has no redundant dimensional suffix; the pinned name remains only in internal compatibility coverage. Camera-relative scrolling, manual/automatic offsets, limits and repeated canvas subtree rendering remain distinct from CanvasLayer's independent canvas role.
- The reference `NoiseTexture2D` maps to Electron2D `NoiseTexture : Texture`, without a dimensional suffix. Its 3D-space sampling option is excluded; all applicable 2D settings and inherited Texture/Resource behavior remain API obligations. The pinned source name remains in internal coverage.
- Gradient resources retain distinct roles without dimensional suffixes: `GradientTexture1D` maps to `GradientRampTexture`, and `GradientTexture2D` maps to `GradientTexture`, both derived directly from `Texture`. They share source validation and storage behavior while retaining their own applicable APIs; see [ADR 0013](resources.md#adr-0013).
- Scalar `Curve` and spatial `Curve2D` remain separate Resource types. `Curve2D` keeps the reference type name and its applicable two-dimensional Bézier API; typed mappings and correctness adaptations are recorded in [ADR 0013](resources.md#adr-0013).
- The image-drawing scene node is named `Sprite`, without a redundant dimensional suffix or 3D sibling. Its inheritance is `Sprite : Entity : CanvasItem : Node : ElectronObject` under [ADR 0008](scene.md#adr-0008); implementation and remaining API gaps are tracked there. The internal coverage page retains the reference identity.
- The entire in-scope public API must correspond to Godot's API and behavior under all previously accepted decisions. Godot `Node` maps to `Node` with the same applicable API; Godot `Node2D` maps to `Entity` with the same applicable API. Keep the separate `CanvasItem` layer and spatial/UI branches under ADR 0008. Preserve inheritance, members, defaults, values, lifecycle and ordering except for specifically accepted adaptations (including typed C#, events, managed lifetime, strict 2D scope, math/resource naming, shaders and acronym casing). Current omissions remain implementation gaps. Familiarity alone, backend convenience or a naming change does not authorize a reduced or redesigned API.
- The public and protected runtime API is bounded by the reference engine's public 2D capabilities. A C# projection may use constructors, typed values, events, disposal, and explicit library-host entry points to express an existing capability or lifecycle. Such a projection must name its reference concept in the bidirectional coverage register and must not add an independent game-facing capability. Backend helpers and observations available only to the implementation stay internal. The runtime owns the ordinary windowed scene lifecycle; game-specific policy remains in the consumer. Review the complete compiled surface, including existing declarations, against this rule; a rationale for an Electron2D-only row is not itself permission to expand the semantic scope.
- All production runtime-engine domains and components compile into `Electron2D.csproj` with assembly name `Electron2D`, producing one managed engine assembly: `Electron2D.dll`.
- Tests, examples, benchmarks, analyzers, and development tools may use separate projects because they are not shipped as parts of the engine.
- The first-party editor is a separately shipped self-hosted application and games are separate executables; both consume the public runtime under ADR 0027 and are not runtime-domain assemblies.
- SDL3-CS core source is integrated as the internal managed binding for the DisplayServer window and event pump. The ordinary public entry point is `Engine.Run(Window)`: users configure a window, add scene children, and run it. The runtime owns the native window, monotonic clock, event pump, frame limiting, and failure-safe shutdown. `SceneTree.Quit` requests termination and supplies the returned exit code; a root close request quits by default unless `SceneTree.AutoAcceptQuit` is disabled. No separate public application-host wrapper is required. ADR 0028 selects SDL's GPU API as the primary 2D renderer and SDL_Renderer as a reduced-capability fallback. The root canvas executes on both; the shader path and verified backend limits are documented in the Rendering domain.

### Packaging boundary

The managed runtime artifact is `Electron2D.dll`, with internal SDL3-CS core binding source from pinned release `v3.4.16.1`. The Linux x64 self-contained example packages native SDL 3.4.16 beside the application files. The native library is not embedded in the managed DLL; other target packages remain unverified.

Native SDL deployment remains a separate platform constraint outside the verified Linux x64 package. This ADR does not claim that native SDL code has already been embedded into the managed DLL or that a physical one-file native deployment has been achieved. That decision requires an implemented and verified SDL integration.

### Consequences

- Production code remains easy to reference: consumers add one engine assembly.
- Domain boundaries are namespaces and documentation boundaries, not assembly boundaries.
- Features designed only to preserve a possible future 3D path must be rejected.
- New public declarations require a semantic reference counterpart or a documented C# projection of one; ordinary application lifecycle belongs to Engine; consumer-specific policy belongs to the executable.
- Godot familiarity does not imply GDScript, Variant, binary, scene-format, or source compatibility where another accepted ADR explicitly differs.
- SDL3-CS and Box2D.NET must be source-vendored into the managed engine assembly when integrated; other managed runtime dependencies require their own decision.

### Rejected alternatives

- Add a minimal 3D layer for future use: rejected because 3D is outside the product.
- Split domains into separate production assemblies: rejected because the engine contract is one `Electron2D.dll`.
- Claim literal single-file SDL deployment before integration: rejected because it would document an unverified state.

<a id="adr-0012"></a>
## ADR 0012: Vendor SDL3-CS and Box2D.NET managed source

Last updated: 2026-09-25

- Status: Accepted
- Scope: Managed dependency ownership, deployment packaging, and future 2D physics, text and audio
- Refines: the one-managed-assembly rule in [0004](product.md#adr-0004)

### Context

Electron2D distributes one managed runtime assembly and exposes one engine-owned public API. SDL3-CS is the selected managed SDL binding; Box2D.NET is the selected managed 2D physics backend. Keeping either as a separate package would add a managed assembly to the application deployment. Source vendoring keeps the managed dependency graph inside `Electron2D.dll`, with upstream updates and local patches owned by Electron2D maintainers.

SDL3-CS core binding source is pinned in `src/Vendor/SDL3-CS`, and Box2D.NET 3.1.654 source is pinned in `src/Vendor/Box2D.NET`; both compile into `Electron2D.dll`. The first scene-body physics slice is executable under ADR 0054, while server, area, joint and broad shape/query APIs remain incomplete. Native SDL is a separate platform-specific library supplied transitively by the runtime project to desktop applications.

### Decision

- Keep all Electron2D runtime code and the managed source of selected backends, including SDL3-CS, Box2D.NET and the future FAudio binding, in `Electron2D.csproj`, producing only `Electron2D.dll` as the managed engine artifact. Do not ship these bindings as separate managed runtime assemblies or depend on their managed NuGet packages in a completed integration.
- Keep the SDL3-CS core source vendored under `src/` and refresh it by release tag with `tools/update-sdl3-cs.sh`. Keep the pinned managed source of `ikpil/Box2D.NET` under `src/Vendor/Box2D.NET`, including its MIT notice and scoped patch record; ADR 0054 owns the executing scene-body profile.
- The first rendering slice must include the complete SDL3-CS ShaderCross binding module from the same pinned release and extend the source-refresh script accordingly. Its types remain internal to `Electron2D.dll`. The common SPIR-V reflection and backend-translation path uses SDL_shadercross under [ADR 0028](rendering.md#adr-0028). HLSL and GLSL source compilation belongs to project import/build tooling; account for those pinned compiler integrations separately from the runtime libraries required for SPIR-V reflection and backend translation. This decision does not restore a Silk.NET/shaderc runtime dependency. Record licenses, versions, delivery and native verification for the components actually integrated.
- Image decoding uses SDL_image through the complete SDL3-CS Image binding module from that same release. The user-approved Linux dependency is `SDL3-CS.Linux.Image`, pinned to `3.4.6.9` (SDL_image 3.4.6), owned by `Electron2D.csproj` and delivered transitively to consumers. This adds native libraries while keeping managed bindings internal to `Electron2D.dll`; public codec integration and platform acceptance are tracked under [ADR 0039](resources.md#adr-0039). On Linux all SDL bindings and native extensions must share one core library; the engine resolves core imports by `libSDL3.so.0` and loads that core before SDL_image or SDL_shadercross.
- The first text slice vendors the complete SDL3-CS TTF binding module and packages SDL_ttf 3 with HarfBuzz under [ADR 0046](rendering.md#adr-0046). The first audio slice compiles its selected managed FAudio binding into `Electron2D.dll` and packages native FAudio over SDL3 under [ADR 0047](audio.md#adr-0047). Neither backend is a production dependency until its executable slice is integrated and verified; both retain the one-managed-assembly and target-platform rules.
- Pin each vendored source to an upstream release and commit, retain its required license notices, record local patches, and make upgrades explicit reviewable changes. Verify the compiled assembly, dependent behavior, and target-specific packaging after each update.
- License Electron2D-authored code under `licence/Electron2D-LICENSE.txt` with Eduard Gushchin's copyright notice. Keep all vendored, adapted, reference-data, native-package, and runtime license texts in `licence/`; the reference-data notice is source-only. Publish applicable texts and `licence/THIRD_PARTY_NOTICES.md` in that single directory. Its Linux inventory records the remaining Vkd3d LGPL source-provenance requirement. A change of RID, runtime pack, native package, or published native payload requires a renewed artifact audit before release.
- Keep vendored types behind internal implementation boundaries. The public and protected Electron2D API must not expose SDL3-CS, Box2D.NET or FAudio binding types; verify the exported assembly surface when integrating each source tree. Physics, text and audio APIs use Electron2D types.
- Engine consumers, including examples, games, and the editor, use only Electron2D's public API. They must not reference, import, or call SDL3-CS, Box2D.NET, FAudio or their native APIs, and must not declare backend package dependencies in their projects. Platform packages required by the engine flow from `Electron2D.csproj` into published applications. This rule applies to bootstrap code as well as scene code; backend probes belong in engine tests.
- Native SDL remains a target-specific deployment dependency. Its binary packaging, host lifecycle, and native verification belong to the SDL integration and platform slices under ADR 0021. Vendor source does not imply a single physical deployment file.
- ADR 0028 selects the SDL GPU and SDL_Renderer roles, HLSL/GLSL import/build compilation, the common SPIR-V path and SDL_shadercross backend integration. The initial canvas and shader integration implements these boundaries with the limits documented in the Rendering domain. The first physics scene-body profile is executable under ADR 0054; broader physics services remain coverage gaps.

### Consequences

- SDL3-CS and Box2D.NET become maintained vendored source rather than separate managed packages. Upstream fixes require an explicit source refresh and regression checks.
- Applications deploy `Electron2D.dll` plus any required target-specific native SDL library and application host files. One managed engine DLL is not a claim of single-file native deployment.
- A fresh self-contained Linux x64 example publish after physics integration contains `Electron2D.dll` and native `libSDL3.so`, without `SDL3-CS.dll` or `Box2D.NET.dll`. Box2D.NET managed types are internal to the engine DLL. This packaging check does not establish physics behavior in the example or other platform packages.

### Rejected alternatives

- Ship SDL3-CS or Box2D.NET as separate managed packages: rejected because the selected managed dependencies belong in the one engine assembly.
- Merge prebuilt dependency assemblies into `Electron2D.dll`: rejected because source vendoring keeps provenance, patches, and build behavior reviewable.
- Use native Box2D instead of Box2D.NET: rejected for the current physics design because it adds platform-specific native binaries and interop ownership when a managed backend is available. This exclusion does not apply to native SDL or SDL_shadercross.
- Implement a new rigid-body solver: rejected because physics-engine development is not Electron2D's differentiating scope.
- Add Box2D.NET source immediately without physics behavior: rejected as unused code.

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
## ADR 0021: Runtime and editor target platforms

Last updated: 2026-09-25

### Status

Accepted.

### Context

The product is a 2D engine whose games must run on desktop, mobile, Android TV, Apple TV, and browser targets. Its separately shipped editor has a desktop host boundary. An explicit matrix prevents platform-specific code from turning a development host into the product boundary or making unverified portability claims.

### Decision

| Product | Target platforms |
| --- | --- |
| Game runtime | Windows, macOS, Linux on X11 and Wayland, Android, iOS, Android TV, tvOS, Web |
| Editor | Windows, macOS, Linux on X11 and Wayland |

Web is a browser game target rather than an operating system. Android TV shares Android RIDs and packages but needs its own TV host and input/lifecycle verification. tvOS uses tvOS RIDs and native packages. Tizen and webOS are outside this accepted target matrix. The editor has no mobile, TV, or Web target. Its desktop-only dependencies must stay outside `Electron2D.dll`.

One public runtime API and one set of documented semantics applies across all listed targets. Platform-specific implementation belongs behind internal backends or host integration boundaries and must not create divergent public type sets. Host, display, storage, input, audio, graphics, lifecycle, and packaging differences must be handled explicitly. Neither Linux display protocol may be treated as covered solely because the other works.

An unavailable platform capability must fail explicitly with the documented exception or capability result. Empty implementations, silent no-ops, and success results without performed work are prohibited.

Target intent, implemented code, successful compilation, host integration, application packaging, automated tests, and native or browser verification are separate states. Documentation must state each state accurately. A feature may be described as cross-platform verified only after it has run on every applicable target, including X11 and Wayland when Linux display behavior applies.

For the current development and release-readiness stage, Linux under Wayland is the only required platform for executable native behavior, application-host packaging, and release verification. X11, Windows, macOS, Android, iOS, Android TV, tvOS, and Web remain product targets, but their host integration and native or browser checks do not block completion at this stage. Each additional platform becomes a release gate when its integration is explicitly taken into scope. A Linux/Wayland result must be reported as such, never as verification of X11 or the full target matrix.

The one-assembly rule covers Electron2D-owned runtime code and the selected vendored managed dependencies. Native libraries, platform application hosts, signing, and store packaging remain deployment concerns and are not implied to be contained in `Electron2D.dll`.

The editor targets the three desktop operating systems and both Linux display protocols. This narrower boundary cannot leak a desktop-only requirement into the game runtime or its public data model. Its source and dependency direction follow ADR 0027.

### Current implementation boundary

The project targets `net10.0`, with `net10.0-android`, `net10.0-ios`, and `net10.0-tvos` selected for Android, iOS, and tvOS RIDs. The generic desktop project references the pinned Windows, macOS and Linux native package families so a referencing application can select the correct assets by RID even when its project reference is restored without that RID. Android, iOS and tvOS keep their target-specific package selection; Android TV uses Android packages. `browser-wasm` is accepted without an SDL package until the Web host and dependency model are implemented. A self-contained Linux x64 example publishes with its packaged SDL and .NET 10 runtime; the previous Wayland scene/input observation was made before this migration and needs renewal. Some file-system code contains macOS and Windows backends, but they have not been exercised on native hosts. There is no production Web browser host/build/package/test pipeline, mobile or TV host application, iOS or tvOS application bundle, editor executable, signing pipeline, or complete target CI matrix. A macOS CI workflow now prepares .NET 10 library builds for iOS/tvOS device and simulator RIDs; no run or native acceptance has been observed. A diagnostic Android SDLActivity under `tests/Electron2D.AndroidProbe` now packages and runs the public Engine.Run canvas path on an Android arm64 phone and an Android TV armeabi-v7a device; this verifies that display/render slice only, not production host lifecycle, input, audio, storage, or release packaging. The current Wayland-only gate does not establish distributable applications on other targets.

The [platform verification matrix](../platform-verification.md) records the diagnostic Android and browser results. An isolated SDL/Emscripten browser probe displayed a direct SDL_Renderer frame and ran managed physics, but had zero SDL GPU drivers and no persistent engine frame loop. The first Web runtime vertical slice must choose and verify a browser-compatible host and dependency model, then integrate the required rendering, input, storage, lifecycle, and packaging capabilities. No placeholder API or unverified browser package is authorized by this target decision.

ADR 0028 selects a capability-driven GPU-primary and SDL_Renderer-fallback architecture. Both canvas backends have native Linux Wayland pixel checks; shader/material output is verified on Wayland/Vulkan. Additional backend and platform claims still require their own native or browser verification.

### Consequences

- Every runtime domain and component must preserve the accepted target contract as it evolves.
- Current feature and release-readiness reviews require Linux/Wayland execution and packaging evidence for applicable native behavior; they do not require the rest of the target matrix until its integration is taken into scope.
- The editor retains its three-desktop-operating-system target, but current-stage verification requires only Linux/Wayland when editor work exists.
- Platform-specific dependencies require an accepted packaging and lifecycle decision before integration.
- Platform support reports must distinguish compilation from native or browser execution and packaging.
- Mobile lifecycle, permissions, storage, input, suspension, and graphics integration remain work for their owning future domains rather than placeholders in Core.
- Browser lifecycle, storage, input, and graphics integration remain work for the first Web host and its owning runtime domains.
- A platform-specific optimization is acceptable only when a portable behaviorally equivalent path or an explicit documented capability boundary remains.

### Rejected alternatives

- Treat desktop support as the product and add mobile later: rejected because Android and iOS are first-class runtime targets.
- Keep Web outside the runtime matrix: rejected because browser games are a product target.
- Maintain separate public APIs per operating system: rejected because game code needs one portable engine contract.
- Claim support from successful compilation alone: rejected because native/browser dependencies, lifecycle, packaging, and device behavior remain unverified.
- Require the editor to run on mobile or Web: rejected because its product target is desktop.
- Count one Linux display protocol as verification of the other: rejected because their host paths differ.

### Related decisions

- [0004: 2D API in one Electron2D-owned assembly](product.md#adr-0004)
- [0012: Vendored SDL3-CS and Box2D.NET](product.md#adr-0012)
- [0014: Managed Resource lifetime and realtime allocation](resources.md#adr-0014)
- [0015: Main-loop lifecycle and host boundary](core-object-runtime.md#adr-0015)
- [0016: Process-wide Engine runtime and host-driven scheduling](core-object-runtime.md#adr-0016)
- [0017: Source-tree module layout](product.md#adr-0017)
- [0020: Typed file access and transformed-file containers](core-data-io.md#adr-0020)
- [0028: GPU-first 2D rendering, shaders, and SDL_Renderer fallback](rendering.md#adr-0028)
- [0027: Self-hosted editor and game project boundary](product.md#adr-0027)

<a id="adr-0027"></a>
## ADR 0027: Self-hosted editor and game project boundary

Last updated: 2026-09-22

### Status

Accepted. This decision amends ADR 0004 by limiting its one-assembly rule to the Electron2D runtime engine, and amends ADR 0017 by placing editor application source outside the runtime `src/` tree. The platform matrix is set by [ADR 0021](#adr-0021).

### Context

Electron2D is both a reusable 2D runtime and the foundation on which its own editor and games are built. The editor must exercise the same engine-facing scene, rendering, input, resource, UI, and application lifecycle capabilities available to games instead of becoming a parallel application framework with privileged private behavior.

The current repository contains one runtime project, an executable test project, a first user-facing window/input example, and no editor implementation. The runtime compiles only `src/**/*.cs` into `Electron2D.dll`. Mixing future editor classes into that project would ship editor-only code to every game and mobile runtime, while compiling the editor from the same `src/` tree into another project would blur ownership and risk duplicate type definitions.

### Decision

The repository has three one-way product layers:

1. `src/` and `Electron2D.csproj` contain only portable runtime engine code and produce the single Electron2D-owned runtime assembly `Electron2D.dll`.
2. `editor/Electron2D.Editor/` is the reserved root for a future standalone editor executable project and all editor-only production source. The editor project will reference `Electron2D.csproj`/`Electron2D.dll`; it will not compile runtime source files into its own assembly.
3. `examples/<Example>/` is the root for user-facing examples and game templates. Each example is an independent executable project that references `Electron2D.csproj`/`Electron2D.dll`.

Examples teach a user how to build with the public engine API. They may cover individual features such as shaders, lighting, audio, and networking, or complete small games such as a platformer or top-down game. Their code and explanations must be useful as application examples. Example source and project files must not name or depend on SDL, Box2D, or another implementation backend; this includes the application bootstrap. API conformance probes, injected events, failure fixtures, diagnostic harnesses, and coverage checks belong in `tests/` or development tools, even when they run an example's production path. Examples follow executable user API. The window/input consumer configures Window and calls Engine.Run; its former duplicate ApplicationHost has been removed. Native acceptance probes and failure fixtures remain in tests.

The editor and every first-party game must be built with Electron2D's public runtime API. Their scenes, UI, rendering, input, resources, and lifecycle must use Electron2D facilities as those domains become implemented. A minimal .NET entry point, platform launcher, and packaging metadata are allowed, but they must only start/host Electron2D through its public API; they must not become an alternative game or editor UI framework.

Dependency direction is strict:

```text
Electron2D.Editor executable ─┐
                             ├──> Electron2D.dll ──> approved runtime dependencies
Game/example executable ─────┘
```

`Electron2D.dll` must never reference the editor or any game/example assembly. The editor must not receive blanket friend-assembly access, use reflection to bypass runtime encapsulation, or link runtime source directly. When editor work exposes a missing reusable capability, that capability must be designed and implemented in its owning runtime domain through the normal production-ready process. Truly editor-only behavior remains in the editor project.

The editor is a separately shipped first-party product assembly and therefore does not violate the one-runtime-DLL rule. Its packaging may contain its executable assembly, `Electron2D.dll`, approved managed/native dependencies, and content. The game runtime targets Windows, macOS, Linux on X11 and Wayland, Android, iOS, Android TV, tvOS, and Web; the editor targets Windows, macOS, and Linux on X11 and Wayland under ADR 0021. The editor's desktop-only dependencies cannot change runtime semantics.

No editor project, executable, domain, component, or production type is implemented by this ADR. The tracked directory is only a repository boundary. The first editor implementation must add its real project, tests, XML documentation, living class/component/domain documents, inventory rows, build verification, and packaging status atomically.

### Consequences

- Games and the editor continuously dogfood the public runtime contract.
- Editor-only code and dependencies cannot leak into `Electron2D.dll` or mobile/Web game deployments.
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
- [0021: Runtime and editor target platforms](product.md#adr-0021)
- [0028: GPU-first 2D rendering, shaders, and SDL_Renderer fallback](rendering.md#adr-0028)

<a id="adr-0030"></a>
## ADR 0030: Use bounded domain decision documents

Last updated: 2026-09-22

### Status

Accepted.

### Context

The first 29 decisions lived in separate files, which forced maintainers and language models to discover many small documents and their refinement links. A trial consolidation into one 1,577-line file removed discovery overhead but would consume excessive context even for a change affecting one domain.

Core is substantially larger than the other domains, so one file for all Core decisions would recreate the same problem at a smaller scale.

### Decision

`docs/decisions/index.md` is the lightweight routing entry point. Current ADRs are grouped into bounded documents:

- `product.md`;
- `core-object-runtime.md`;
- `core-data-io.md`;
- `core-math.md`;
- `scene.md`;
- `resources.md`;
- `localization.md`;
- `rendering.md`.

A maintainer reads the index, the affected document, and only the other documents explicitly referenced by relevant ADRs. These documents describe decisions currently in force, not an append-only history. Revise an existing ADR in place when its decision changes, remove obsolete ADRs and links, and keep the number and anchor of each retained ADR stable. Add a numbered ADR only for a distinct new decision. Git history retains earlier versions. When a document would exceed 500 lines, split it along a cohesive subdomain boundary.

Class, component, domain, and repository instruction documents may link directly to stable anchors, but they are not additional decision documents. Update those links when an ADR is removed or moved.

### Consequences

- Architecture work loads relevant current decisions instead of obsolete records.
- The routing index remains small enough to read on every architectural task.
- Core decisions stay grouped by meaningful subdomain rather than one oversized bucket.
- Git history preserves the provenance of former one-file-per-ADR paths.
- Routing metadata must be updated with each new or moved record.

### Rejected alternatives

- **Keep one file per ADR:** rejected because discovery and chained reads were excessive.
- **Use one repository-wide decision file:** rejected because the file alone would consume too much context.
- **Use one file for all Core decisions:** rejected because Core already contains several independent subdomains.
- **Keep summaries instead of full records:** rejected because rationale, alternatives, and verification boundaries are required.
- **Retain redirect files:** rejected because duplicate files recreate discovery noise and can drift.

### Verification boundary

The migration preserves ADR 0001 through ADR 0029 as complete records, adds this routing decision as ADR 0030, and updates repository references to the new stable domain-log anchors. Link, anchor, size, and repository checks verify the current structure; Git history retains the old paths.


<a id="adr-0045"></a>
## ADR 0045: Keep acronyms uppercase in function, method and property names

Last updated: 2026-09-22

### Status

Accepted. The rule applies equally to existing and new declarations.

### Decision

Every acronym in an Electron2D-owned function, method or property name is written entirely in uppercase, regardless of its length or position in the identifier. This includes public, protected, internal and private methods, local functions and properties. Ordinary words retain the surrounding C# casing convention.

Examples: `LoadPNGFromBuffer`, `LoadJPGFromBuffer`, `LoadBMPFromBuffer`, `LoadTGAFromBuffer`, `SavePNG`, `SaveJPGToBuffer`, `GetGLVersion`, `CompileHLSL`, `CompileGLSL`, `GetGPUInfo`, `GetInstanceID`, `ReadUTF8` and `GetFPS`; a property uses `MaxFPS`. `FPS` follows the same uppercase rule as the other acronyms. These illustrate spelling; they do not introduce or claim implementation of those APIs. The rule also applies to other acronyms; this list is not exhaustive.

Color APIs use `ToHTML`, `FromHTML`, `HTMLIsValid`, `LinearToSRGB`, `SRGBToLinear`, `FromHSV`, `ToHSV`, `FromOKHSL`, `FromRGBE9995` and the `ToABGR32`/`ToARGB32`/`ToRGBA32` families, including their 64-bit variants. Component properties retain their suffix: `OKHSLH`, `OKHSLS`, `OKHSLL` (the final `L` is the lightness component after the `OKHSL` acronym).

The same rule gives `ReadCSVLine`, `GetMD5`, `GetSHA256`, `EOFReached`, `SetIMEActive`, `InstanceID`, `AdjustBCS` and `DBToLinear` in other API families. Review the whole identifier for acronyms; matching one of the examples is not the criterion.

Do not turn acronyms into title-case words such as `Png`, `Jpg`, `Gl`, `Gpu`, `Utf8` or `Fps`. Compound names retain ordinary words while capitalizing their acronym parts: `WebP` and `OpenGL`; `SPIR-V` is written `SPIRV` inside an identifier, where a hyphen cannot be used.

Apply the rule to new functions, methods and properties immediately. Existing nonconforming names are migration work, not a second accepted convention. A rename updates every affected call site, source XML, current class/component documentation and bidirectional coverage mapping in the same change. Preserve behavior while changing spelling. Do not add aliases solely to retain the rejected casing.

Externally prescribed override/interface member names and vendored upstream declarations retain the spelling required by their defining contract. Electron2D-owned wrapper methods and properties follow this rule. This decision concerns function, method and property names; it does not impose an unrelated rename of types, fields, file extensions, serialized keys or shader entry points.

### Rationale and consequences

Acronyms identify formats, technologies and protocols consistently throughout the API. The project deliberately uses uppercase acronyms even where general C# naming guidance would use title case for a longer acronym. Method-name adaptations remain explicit in the reference coverage register under [ADR 0004](#adr-0004).

The migration can change public source and binary compatibility and must be recorded with the affected API. Existing class pages continue to describe actual declarations until their corresponding code is renamed.

### Verification boundary

The decision, routing index and maintenance instructions establish the rule. Build, call-site and coverage checks accompany each code migration; a documentation-only adoption does not prove that all existing identifiers comply.

<a id="adr-0051"></a>
## ADR 0051: Project selected enum families to explicit public type names

Last updated: 2026-09-24

### Status

Accepted.

### Context

Reference enum names are scoped by their owner, while C# properties can have the same short name. Earlier Electron2D slices mixed nested `Enum` suffixes, owner-prefixed namespace types, and unchanged nested types. A single implicit collision rule cannot express the selected public names. ADR 0004 still requires each applicable enum value and behavior; placement is a C# API naming choice.

### Decision

The following names are exact public type identities. All listed targets are top-level in the flat `Electron2D` namespace except `Camera.CameraProcessCallback`, which remains nested:

- `CanvasItemMaterial.BlendMode` → `BlendMode`; `CanvasItem.TextureFilter` → `TextureFilter`; `CanvasItem.TextureRepeat` → `TextureRepeat`; `Window.Mode` → `WindowMode`.
- `Control.FocusMode` → `FocusMode`; `Control.LayoutPreset` → `LayoutPreset`; `Control.GrowDirection` → `GrowDirection`; `Control.LayoutDirection` → `LayoutDirection`; `Control.LayoutPresetMode` → `LayoutPresetMode`; `Control.MouseFilter` → `MouseFilter`.
- `FileAccess.CompressionMode` → `FileCompressionMode`; `FileAccess.ModeFlags` → `FileAccessModeFlags`; `FileAccess.UnixPermissionFlags` → `UnixPermissionFlags`; `Node.ProcessMode` → `ProcessMode`; `PackedScene.GenEditState` → `PackedSceneEditState`; `Resource.DeepDuplicateMode` → `DeepDuplicateMode`; `SceneTree.GroupCallFlags` → `GroupCallFlags`; `Timer.TimerProcessCallback` → `TimerProcessCallback`.
- `Gradient.InterpolationMode` → `InterpolationMode`; `GradientTexture2D.Fill` → `FillEnum`; `GradientTexture2D.Repeat` → `Repeat`; `Camera2D.AnchorMode` → `AnchorMode`; `Camera2D.Camera2DProcessCallback` → `Camera.CameraProcessCallback`; `Line2D.LineCapMode` → `LineCapMode`; `Line2D.LineJointMode` → `LineJointMode`; `Line2D.LineTextureMode` → `LineTextureMode`.
- `FastNoiseLite.NoiseType` → `NoiseType`; `FastNoiseLite.FractalType` → `FractalType`; `FastNoiseLite.CellularDistanceFunction` → `CellularDistanceFunction`; `FastNoiseLite.CellularReturnType` → `CellularReturnType`; `FastNoiseLite.DomainWarpType` → `DomainWarpType`; `FastNoiseLite.DomainWarpFractalType` → `DomainWarpFractalType`.

The owning class keeps its applicable property names. The enum's numeric values and observable behavior do not change with its location. Unlisted enum families retain their current placement; a future public name collision requires an explicit decision update. Do not ship former type spellings, compatibility aliases, or duplicate public enum types. Keep the bidirectional coverage mappings, source XML, consumers, and class pages synchronized with these identities.

### Consequences

The migration breaks source and binary compatibility for the moved or renamed enum types. The selected spellings are explicit exceptions, not a new automatic naming rule for every future enum. ADR 0045 continues to govern acronyms in function, method, and property names, not enum type names.
