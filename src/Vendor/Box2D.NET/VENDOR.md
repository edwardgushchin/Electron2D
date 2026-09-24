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

The library remains an internal backend. Electron2D public signatures use only Electron2D-owned types. Review every upstream update against this pinned version, license, internalization and integration tests before replacing these files.
