# Electron2D repository instructions

These instructions apply to the whole repository. Keep this file about how to work. [The decision index](docs/decisions/index.md) routes to the current architectural decisions; ADRs define the product and its boundaries. If guidance here conflicts with an accepted ADR, follow the ADR and correct the guidance before implementing. If the user's task changes that decision, revise the ADR and affected current-state documents together.

## Find the relevant source of truth

- For architecture work, read `docs/decisions/index.md`, the affected bounded decision document, and only the cross-domain ADRs it links. Update the active ADR in place when its decision changes, following [ADR 0030](docs/decisions/product.md#adr-0030).
- For current behavior, read the affected `docs/domains/`, `docs/components/`, and `docs/classes/` pages. `docs/inventory.md` maps implemented production types; it is not a compatibility register.
- For API comparison and its roadmap, use `docs/coverage/index.md` and its linked tables. Read [the maintenance contract](docs/maintaining.md) for implementation, documentation, XML, and audit requirements when changing code or those documents.
- Runtime source is under `src/` and built by `Electron2D.csproj`; executable checks are under `tests/Electron2D.Tests/`. `editor/` and `examples/` contain separate consumers when implemented. Consumer code and projects use only the public Electron2D API; backend dependencies and probes stay in the runtime project and tests. Verify the current project layout before adding files.

## Make a change

- Keep every acronym in Electron2D-owned function and method names fully uppercase, regardless of its length or position: `LoadPNGFromBuffer`, `SaveJPGToBuffer`, `GetGLVersion`. Follow [ADR 0045](docs/decisions/product.md#adr-0045); update callers, XML, class pages and coverage together when renaming.
- Preserve unrelated work and keep changes task-scoped. For a requested implementation, deliver a complete executable vertical slice under the accepted architecture; do not add inert compatibility stubs. Audit the relevant API and behavior in both directions, including applicable sibling types, and record accepted adaptations, exclusions, and dependency triggers in coverage.
- Update affected source XML documentation, class/component/domain pages, inventory, and coverage in the same change as behavior or public API. Change an ADR only when an architectural decision changes. Document actual behavior and verification limits, never planned behavior as implemented.
- Godot is an internal comparison source, not shipped Electron2D identity. Never mention it in production source or comments, C# XML or generated XML documentation, or any `README.md`. Keep compatibility discussion in internal design and coverage documents. Check this with the repository search below when documentation changes.
- Run the smallest checks that establish the changed contract. Fix known P0/P1/P2 defects in the implemented scope before declaring completion. Distinguish local builds and tests from native, visual, cross-platform, performance, and owner acceptance; follow the current platform gate in [ADR 0021](docs/decisions/product.md#adr-0021).

## Finish in Git

- End completed implementation and repository-documentation work with one cohesive local [Conventional Commit](https://www.conventionalcommits.org/en/v1.0.0/) after verification. Stage only relevant files; exclude generated output, local work logs, temporary files, and unrelated user changes. Report the hash and subject.
- Never mention Godot in a commit message, regardless of case, including its subject, body, footers, and trailers. Describe the Electron2D change itself.
- Do not amend, rebase, squash, rewrite, or push commits unless the user explicitly requests it.
- After completing work on a branch, merge it into local `main`, verify the result there, and delete the branch and its worktree. Do not report a branch-only result as finished.

## Verification commands

Use the relevant commands, not necessarily all of them for a documentation-only change. Record only checks that actually ran.

```bash
dotnet format Electron2D.csproj --verify-no-changes --no-restore --exclude src/Vendor/SDL3-CS
dotnet format tests/Electron2D.Tests/Electron2D.Tests.csproj --verify-no-changes --no-restore
dotnet build Electron2D.csproj -c Release
dotnet run --project tests/Electron2D.Tests/Electron2D.Tests.csproj -c Release
tools/coverage/check.sh
! rg -n -i 'godot' --glob '*.cs' --glob '*.csproj' --glob '**/[Rr][Ee][Aa][Dd][Mm][Ee].[Mm][Dd]' --glob '**/bin/**/*.xml' --glob '**/obj/**/*.xml' .
```
