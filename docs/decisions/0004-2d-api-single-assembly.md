# 0004: Build a 2D-only scene-oriented engine in one assembly

Last updated: 2026-09-20

- Status: Accepted; external-dependency packaging amended by [0012](0012-external-runtime-dependencies.md), runtime target matrix defined by [0021](0021-cross-platform-runtime-targets.md)
- Scope: Entire product architecture and packaging

The one-assembly rule remains fully effective for every production type and domain owned by Electron2D. ADR 0012 changes only the treatment of explicitly approved third-party runtime dependencies: they may ship as separate assemblies instead of being internalized into `Electron2D.dll`.

## Context

Electron2D is intended to provide a familiar high-level API modeled on Godot's 2D engine concepts while using SDL3-CS below that API. The product is a focused 2D engine rather than a general 2D/3D engine. Its engine surface must be distributed as one DLL named `Electron2D.dll`.

## Decision

- Electron2D supports only two-dimensional games.
- Three-dimensional rendering, physics, transforms, cameras, assets, nodes, compatibility aliases, and speculative shared 2D/3D abstractions are outside scope.
- The high-level API follows Godot's 2D concepts, lifecycle, composition model, and recognizable naming where they remain compatible with the typed C# decisions in ADR 0001 and ADR 0002.
- All production engine domains and components compile into `Electron2D.csproj` with assembly name `Electron2D`, producing one managed engine assembly: `Electron2D.dll`.
- Tests, examples, benchmarks, analyzers, and development tools may use separate projects because they are not shipped as parts of the engine.
- SDL3-CS is the intended low-level backend but is not integrated yet.

## Packaging boundary

The current verified engine artifact is the managed `Electron2D.dll`. No SDL binding or native SDL binary is currently part of the build. When SDL3-CS is integrated, its managed binding must not create a second shipping engine assembly.

Native SDL deployment is a separate unresolved platform constraint. This ADR does not claim that native SDL code has already been embedded into the managed DLL or that a physical one-file native deployment has been achieved. That decision requires an implemented and verified SDL integration.

## Consequences

- Production code remains easy to reference: consumers add one engine assembly.
- Domain boundaries are namespaces and documentation boundaries, not assembly boundaries.
- Features designed only to preserve a possible future 3D path must be rejected.
- Godot familiarity does not imply GDScript, Variant, binary, scene-format, or source compatibility where another accepted ADR explicitly differs.
- A managed dependency that would be copied as another shipping engine DLL must be internalized, source-integrated, embedded, or rejected after a specific packaging review.

## Rejected alternatives

- Add a minimal 3D layer for future use: rejected because 3D is outside the product.
- Split domains into separate production assemblies: rejected because the engine contract is one `Electron2D.dll`.
- Claim literal single-file SDL deployment before integration: rejected because it would document an unverified state.
