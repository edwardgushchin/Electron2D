# Electron2D Agent Guide

Root `AGENTS.md` is a compact project index and non-negotiable guardrails file. It must not duplicate the detailed workflow in `docs/repository/agent-workflow.md`.

Detailed sources:

- Agent workflow: `docs/repository/agent-workflow.md`.
- Goal task workflow: `.codex/prompts/goal-task-workflow.md`.
- Audit package contract: `docs/release-management/audit-package.md`.
- External auditor request: `docs/release-management/AUDIT-REQUEST.md`.

## Repository Map

- `src/` - production code.
- `tests/` - integration tests and verifier tests.
- `eng/` - repository build tool and audit automation.
- `docs/` - domain documents and durable process contracts.
- `TASKS.md` - active task board; not a durable source for behavioral rules.
- `data/dev-diary/` - append-only agent working diary.
- `data/completed-tasks/` - monthly archives for accepted tasks.
- `data/documentation/` - generated local documentation index.
- `.codex/prompts/` - tracked prompts and workflow models.
- `.temp/` - local drafts, audit ZIPs, clean checkouts, and conversation state.

## Core Commands

- Build tool: `dotnet build eng/Electron2D.Build/Electron2D.Build.csproj --no-restore -v:minimal`
- Fast audit contracts: `dotnet run --project eng/Electron2D.Build -- verify audit-contracts`
- Focused integration test: `dotnet test tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj --filter "FullyQualifiedName~<Name>" --no-restore -v:minimal`
- Audit medium slice: `dotnet run --project eng/Electron2D.Build -- test --integration-slice audit-medium --no-build --no-restore`
- Audit heavy acceptance slice: `dotnet run --project eng/Electron2D.Build -- test --integration-slice audit-heavy --no-build --no-restore`
- Audit follow-up verifier: `dotnet run --project eng/Electron2D.Build -- verify audit-followups`
- Docs update/check: `dotnet run --project eng/Electron2D.Build -- update docs`, then `dotnet run --project eng/Electron2D.Build -- update docs --check`
- Docs verifier: `dotnet run --project eng/Electron2D.Build -- verify docs`
- License verifier: `dotnet run --project eng/Electron2D.Build -- verify licenses`
- Whitespace check: `git diff --check`

License rule: every tracked hand-written C# source file in `src/`, `tests/`, `eng/`, and `data/templates/` must keep the Electron2D MIT header. Run `dotnet run --project eng/Electron2D.Build -- verify licenses` after C# source changes.

## Guardrails

- Read `docs/repository/agent-workflow.md` before substantive repository work.
- More specific `AGENTS.md` or `AGENTS.override.md` files may narrow these rules for subdirectories; follow the closest applicable file.
- Follow the feature gate from `docs/repository/agent-workflow.md`: domain document, failing or updated test, implementation, green checks, synchronized document.
- Keep `TASKS.md` and the diary according to `docs/repository/agent-workflow.md`; do not close or archive tasks without explicit user acceptance.
- Treat unknown worktree changes as user work. Do not revert, overwrite, or reformat unrelated changes.
- Stage, commit, push, tag, or open pull requests only on explicit user request; never push by default.
- Do not run destructive Git, filesystem, Docker, database, or deployment actions without explicit approval for that exact action.
- For external audit, use only the commands defined by `docs/release-management/audit-package.md`: `audit package`, `audit package verify`, and `audit submit --browser-backend codex-chrome`.
- Manual ZIP assembly, manual submit, manual page verdict reading, copied session material, and manual verdicts are forbidden.
- Use subagents only when the current harness and user request explicitly allow them; keep them scoped to read-heavy work. The main agent owns final edits, verification, and commit composition.

## Done Means

- Requested behavior is implemented and aligned with the relevant domain document.
- Relevant tests and verifiers passed in the current session.
- Generated docs, task notes, and diary are synchronized when required.
- `git diff --check` and required source, docs, and license checks passed, or the blocker is named explicitly.
- The final response names changed files, exact checks, residual risk, and the commit when one was created.
