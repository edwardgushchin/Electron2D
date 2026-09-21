# 0012: Permit external runtime dependencies and select Box2D.NET

Last updated: 2026-09-20

- Status: Accepted
- Scope: Product boundary, deployment packaging, and future 2D physics
- Amends: external-dependency packaging in [0004](0004-2d-api-single-assembly.md); its one-assembly rule for Electron2D-owned code remains accepted

## Context

All code owned by Electron2D is constrained to a single managed DLL. The original packaging decision also applied that constraint to third-party managed dependencies, which would require vendoring their source or merging their assemblies. Vendoring would transfer upstream synchronization and local-fork maintenance to Electron2D, while assembly merging would add build and diagnostics complexity without improving the public API.

The engine remains exclusively 2D and should continue exposing one coherent public API from `Electron2D.dll`. Physical deployment, however, may contain separately maintained runtime dependencies when an explicit architectural decision accepts them.

`Box2D.NET` is a pure managed C# port suitable for the intended .NET 8 physics domain. The dependency has been selected, but no package reference or physics implementation exists in the current build.

## Decision

- Electron2D remains a 2D-only engine. Three-dimensional types, behavior, and speculative shared 2D/3D abstractions remain outside scope.
- Every Electron2D-owned production domain and public or internal production type compiles into `Electron2D.csproj` and `Electron2D.dll`. Electron2D code must never be split into additional assemblies.
- Explicitly approved third-party runtime dependencies may ship as separate assemblies because they are not Electron2D-owned code. A physical one-DLL deployment of the complete dependency graph is not required.
- The future 2D collision and rigid-body domain will use the pure managed `Box2D.NET` package maintained at `ikpil/Box2D.NET` as an external runtime dependency.
- `Box2D.NET` source must not be vendored, copied into, or merged with `Electron2D.dll`. The exact package version will be pinned when the physics vertical slice is implemented.
- Electron2D's public API must not expose dependency-owned types. Physics nodes, resources, queries, contacts, and errors will use Electron2D types, with dependency translation kept behind the physics-domain boundary.
- Dependency upgrades are explicit changes requiring license review, release-note review, compatibility tests, regression tests, and physics benchmarks appropriate to the affected behavior.
- The package must not be added before executable physics behavior uses it. Selection is an accepted design decision, not an implemented physics feature.
- SDL3-CS and native SDL deployment remain unresolved and require their own implemented packaging decision.

## Consequences

- The one-DLL rule remains an invariant for Electron2D-owned code. Once physics is integrated, applications will deploy that one `Electron2D.dll` together with the selected external `Box2D.NET` runtime assembly and any later explicitly approved platform dependencies.
- Upstream physics fixes can be consumed through package upgrades instead of maintaining an Electron2D source fork.
- Consumers see Electron2D-owned physics APIs and are insulated from ordinary dependency upgrades, subject to Electron2D's documented compatibility policy.
- Electron2D must preserve the dependency's license notice in distributions as required by its license.
- The current build remains unchanged and still produces only `Electron2D.dll`; no claim of implemented physics or dependency packaging is made until the physics vertical slice lands.

## Rejected alternatives

- Vendor the dependency source: rejected because it makes upstream synchronization and local modifications an Electron2D maintenance responsibility.
- Merge the dependency assembly into `Electron2D.dll`: rejected because it complicates builds, symbols, diagnostics, licensing audits, trimming, and upgrades solely to preserve a physical-file constraint.
- Use the native upstream library: rejected for the current design because it adds platform-specific native binaries and interop ownership when a managed backend is available.
- Implement a new rigid-body solver: rejected because physics-engine development is not Electron2D's differentiating scope.
- Add the selected package immediately without a physics implementation: rejected as an unused runtime dependency.
