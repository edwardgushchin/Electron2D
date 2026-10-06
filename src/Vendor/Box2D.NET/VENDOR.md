# Box2D.NET source provenance

- Upstream: https://github.com/ikpil/Box2D.NET
- Release tag: `3.1.654`
- Commit: `5efc96def866edbb4e5a9368d84de5bf8c2dcaca`
- Imported: all 233 C# files from `src/Box2D.NET/`, with the upstream MIT [`LICENSE`](LICENSE)
- Assembly: source is compiled into `Electron2D.dll`; no separate managed package is shipped

Local boundary and compiler adaptations:

1. Change 258 namespace-level `public` type/delegate declarations to `internal`; nested members remain in their upstream form. The resulting assembly export has zero Box2D.NET declarations.
2. Add `#nullable disable` to every imported file, matching the upstream project's nullability setting while keeping Electron2D-owned source strict.
3. Suppress the upstream-only XML comment diagnostics `CS1570` in `B2Delegates.cs` and `B2DynamicTrees.cs`, and `CS1587` in `B2MathFunction.cs`, `B2Contacts.cs`, `B2Ids.cs`, and `B2Solvers.cs`. Suppress the upstream constant-branch `CS0162` in `B2Solvers.cs`. These directives do not change runtime behavior.
4. Reuse a `B2StepContext` and graph-color block array per world, resetting transient fields on each step. Skip allocation of dummy overflow-contact state when there are no overflow contacts. These hot-path changes make the tested warmed contact, resting and moving body steps allocate zero managed bytes on the current Linux/.NET 8 path. The reusable-buffer portion is proposed to current upstream in [ikpil/Box2D.NET#101](https://github.com/ikpil/Box2D.NET/pull/101); the pinned release's overflow-contact methods no longer exist on upstream `main`.
5. Reuse the existing identity body state for static-body revolute warm-start and solve branches, which only read it. This removes 576 managed bytes per four-substep active pin frame in the checked Linux/.NET 10 path. The same change is proposed to current upstream in [ikpil/Box2D.NET#102](https://github.com/ikpil/Box2D.NET/pull/102).
6. Apply the same read-only identity-state reuse to static-body wheel warm-start and solve branches. The checked four-substep active groove path also drops from 576 to zero managed bytes per warmed frame. This companion change is included in [ikpil/Box2D.NET#102](https://github.com/ikpil/Box2D.NET/pull/102).

7. Preserve the removed reference instance in the spare slot during array swap-removal. New contact/island handles consume already constructed array slots. Sleeping solver-set buffers retain capacity across ordinary sleep/wake/destruction and release it only when the world is destroyed, including dormant cached slots. Runtime body/contact-cap preparation bounds the measured sandbox topology; denser or newly configured topology can still grow storage.
8. Extend wide contact arithmetic, indices, gather/scatter and layout checks to eight lanes using `Vector256<float>` storage so the JIT can retain vector locals. Skip the absent second normal/friction point only when all eight second-point normal masses are zero. Vector comparisons and conditional selection preserve the scalar NaN and signed-zero choices. Multiply/add remains separate. .NET supplies accelerated or portable fallback implementations. The single-worker stage executes its blocks directly; retained multiworker jobs preserve the existing graph-color barriers. A step-context worker-failure field lets a failed constraint job signal its peers instead of leaving the coordinator spinning. The field resets with the reusable context. The engine-owned scheduler is internal and does not replace the pinned solver. Lane checks, serial/parallel state hashes, native oracle and integration results are recorded in the [performance report](../../../docs/components/box2d-performance.md).

The library remains an internal backend. Electron2D public signatures use only Electron2D-owned types. Review every upstream update against this pinned version, license, internalization and integration tests before replacing these files.
