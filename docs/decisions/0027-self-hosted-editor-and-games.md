# ADR 0027: Self-hosted editor and game project boundary

Last updated: 2026-09-21

## Status

Accepted. This decision amends ADR 0004 by limiting its one-assembly rule to the Electron2D runtime engine, and amends ADR 0017 by placing editor application source outside the runtime `src/` tree.

## Context

Electron2D is both a reusable 2D runtime and the foundation on which its own editor and games are built. The editor must exercise the same engine-facing scene, rendering, input, resource, UI, and application lifecycle capabilities available to games instead of becoming a parallel application framework with privileged private behavior.

The current repository contains one runtime project, one executable test project, an empty tracked `examples/` root, and no editor implementation. The runtime compiles only `src/**/*.cs` into `Electron2D.dll`. Mixing future editor classes into that project would ship editor-only code to every game and mobile runtime, while compiling the editor from the same `src/` tree into another project would blur ownership and risk duplicate type definitions.

## Decision

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

## Consequences

- Games and the editor continuously dogfood the public runtime contract.
- Editor-only code and dependencies cannot leak into `Electron2D.dll` or mobile game deployments.
- Runtime and editor can have different entry points, target frameworks, host matrices, and packaging while sharing the same engine API revision.
- The repository may contain more than one first-party project/assembly even though engine runtime code still produces exactly one Electron2D-owned DLL.
- A future editor feature cannot justify a private shortcut around normal runtime API ownership and verification.
- The current build remains unchanged because no fake editor project or placeholder type is introduced.

## Rejected alternatives

- **Compile editor source into `Electron2D.dll`:** rejected because every game would receive editor-only code and dependencies.
- **Place editor source under `src/Editor` but compile it separately:** rejected because `src/` is the enforced runtime compilation root and duplicate include/exclude rules would make ownership fragile.
- **Use an unrelated desktop UI framework for the editor:** rejected because it would not validate Electron2D's game-facing UI/render/input stack and would create a second application model.
- **Give the editor blanket internal access:** rejected because it would let the editor succeed while ordinary games cannot reproduce the same workflows.
- **Create an empty editor project now:** rejected because its target framework, host, entry point, SDL integration, and UI/render dependencies do not exist yet; a compiling shell would be a misleading placeholder.
- **Move the editor to another repository:** rejected because the editor and runtime must evolve and be verified against the same source revision.

## Verification boundary

The repository boundary is verified by directory placement and current project compile includes. Existing runtime checks prove only that reserving `editor/Electron2D.Editor/` does not alter `Electron2D.dll`. Self-hosting, editor startup, UI, rendering, packaging, and desktop-platform behavior remain unimplemented and unverified.
