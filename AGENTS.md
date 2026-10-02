# Electron2D repository instructions

These instructions apply to the whole repository. Keep this file about how to work. [The decision index](docs/decisions/index.md) routes to the current architectural decisions; ADRs define the product and its boundaries. If guidance here conflicts with an accepted ADR, follow the ADR and correct the guidance before implementing. If the user's task changes that decision, revise the ADR and affected current-state documents together.

## Find the relevant source of truth

- For architecture work, read `docs/decisions/index.md`, the affected bounded decision document, and only the cross-domain ADRs it links. Update the active ADR in place when its decision changes, following [ADR 0030](docs/decisions/product.md#adr-0030).
- For project/scene tooling, editor automation, batch hosts or capture, follow [ADR 0090](docs/decisions/agent-native.md#adr-0090). Verify the exercised programmatic workflow and distinguish authoring, headless simulation and real rendered output; record absent prerequisites instead of claiming tool or agent acceptance from compilation alone.
- For current behavior, read the affected `docs/domains/`, `docs/components/`, and `docs/classes/` pages. `docs/inventory.md` maps implemented production types; it is not a compatibility register.
- For API comparison and its roadmap, use `docs/coverage/index.md` and its linked tables. Read [the maintenance contract](docs/maintaining.md) for implementation, documentation, XML, and audit requirements when changing code or those documents.
- Runtime source is under `src/` and built by `Electron2D.csproj`; executable checks are under `tests/Electron2D.Tests/`. `editor/` and `examples/` contain separate consumers when implemented. Consumer code and projects use only the public Electron2D API; backend dependencies and probes stay in the runtime project and tests. Verify the current project layout before adding files.
- Keep tool output focused: locate relevant declarations with `rg`, read the ranges needed for the audit, and inspect every result of a batch while returning a compact summary. Read full files when necessary. For broad API work, finish one connected vertical slice and update coverage before moving on.

## Make a change

- Preserve the accepted reference API and behavior under all existing ADRs. Godot `Node` maps to Electron2D `Node` with the same applicable API; `Node2D` maps to `Entity` with the same applicable API. Follow [ADR 0008](docs/decisions/scene.md#adr-0008) for inheritance and role-based type substitutions. Keep `CanvasItem` separate and apply the mapping across consumers, parameters, return types, events, factories and coverage; a rename does not authorize missing members or merged responsibilities.

- Keep every acronym in Electron2D-owned function, method and property names fully uppercase, regardless of its length or position: `LoadPNGFromBuffer`, `SaveJPGToBuffer`, `GetGLVersion`, `GetFPS`, `MaxFPS`. Follow [ADR 0045](docs/decisions/product.md#adr-0045); update callers, XML, class pages and coverage together when renaming.
- Preserve unrelated work and keep changes task-scoped. For a requested implementation, deliver a complete executable vertical slice under the accepted architecture; do not add inert compatibility stubs. Audit the relevant API and behavior in both directions, including applicable sibling types, and record accepted adaptations, exclusions, and dependency triggers in coverage.
- Update affected source XML documentation, class/component/domain pages, inventory, and coverage in the same change as behavior or public API. Change an ADR only when an architectural decision changes. Document actual behavior and verification limits, never planned behavior as implemented.
- Godot is an internal comparison source, not shipped Electron2D identity. Never mention it in production source or comments, C# XML or generated XML documentation, or any `README.md`. Keep compatibility discussion in internal design and coverage documents. Check this with the repository search below when documentation changes.
- Run the smallest checks that establish the changed contract. Fix known P0/P1/P2 defects in the implemented scope before declaring completion. Distinguish local builds and tests from native, visual, cross-platform, performance, and owner acceptance; follow the current platform gate in [ADR 0021](docs/decisions/product.md#adr-0021).

## Finish in Git

- Before **every** repository commit, build the Release runtime, verify the compiled API snapshot with `tools/coverage/check.sh`, then regenerate the wiki from that assembly and its XML documentation using `python3 -B tools/wiki/generate.py` (default candidate: ignored `bin/wiki/`). Run the generator again with `--check`. When updating the public wiki, generate into a clone of `Electron2D.wiki.git` and inspect the diff. The wiki uses the existing overview/syntax/member-reference format; never include the name of the external comparison engine or a compatibility-profile section in any wiki page. Keep unpublished/generated output out of the engine commit.
- End completed implementation and repository-documentation work with one cohesive local [Conventional Commit](https://www.conventionalcommits.org/en/v1.0.0/) after verification. Stage only relevant files; exclude generated output, local work logs, temporary files, and unrelated user changes. Report the hash and subject.
- Never mention Godot in a commit message, regardless of case, including its subject, body, footers, and trailers. Describe the Electron2D change itself.
- Do not amend, rebase, squash, rewrite, or push commits unless the user explicitly requests it.
- After completing work on a branch, merge it into local `main`, verify the result there, and delete the branch and its worktree. Do not report a branch-only result as finished.

## Verification commands

Use the relevant commands, not necessarily all of them for a documentation-only change. Record only checks that actually ran.

```bash
dotnet format Electron2D.csproj --verify-no-changes --no-restore --exclude src/Vendor
dotnet format tests/Electron2D.Tests/Electron2D.Tests.csproj --verify-no-changes --no-restore
dotnet build Electron2D.csproj -c Release
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
! rg -n -i 'godot' --glob '*.cs' --glob '*.csproj' --glob '**/[Rr][Ee][Aa][Dd][Mm][Ee].[Mm][Dd]' --glob '**/bin/**/*.xml' --glob '**/obj/**/*.xml' .
```
