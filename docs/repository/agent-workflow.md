# Electron2D Agent Workflow

This document contains the detailed agent workflow that used to make root `AGENTS.md` too large. Root `AGENTS.md` stays a compact project map and guardrail index; read this document before substantive repository work.

## Instruction Priority

- System and developer instructions from the execution environment outrank repository rules.
- The closest applicable `AGENTS.md` or `AGENTS.override.md` narrows the root contract for the files being changed.
- The current user request may change workflow preferences, but it cannot override safety, data protection, destructive-action limits, or verifiability.
- When instructions conflict, choose the stricter safe rule and record the decision briefly in the diary.

## Language And Terms

- `AGENTS.md` and this workflow document are English by explicit user request.
- Project-facing Markdown is Russian by default unless the user or a more specific local rule explicitly requires another language. This includes domain documents, task files, completed-task archives, the diary, README files, agent notes, release notes, changelogs, and UI prose.
- Exact identifiers remain unchanged: paths, commands, API fields, package names, code symbols, file formats, official product names, and diagnostic codes.
- In Russian prose, do not use English words as shorthand when a normal Russian term exists. If an exact term is required, put it in backticks or explain it nearby.
- Avoid English-heavy comma lists inside Russian sentences. Give the Russian meaning first, then the exact identifier when needed.

## Documentation And Public API

- Public documentation outside `README.md` must not explain Electron2D through SDL-family backend names, companion libraries, or marketing comparisons. Describe backends as internal platform, rendering, audio, text, or export backends.
- Public documentation outside `README.md` must not describe Electron2D as "Godot-like" or equivalent marketing wording. For technical compatibility, use precise API-subset wording.
- Every public C# type, member, enum, delegate, and event needs complete XML documentation: `summary`, `remarks` where useful, `param`, `typeparam`, `returns`, `exception`, `threadsafety`, `since`, and `seealso` where applicable.
- Do not leave placeholder comments, bare `<inheritdoc />`, or one-line summaries for non-trivial public API.

## C# Type Naming

- Namespace, assembly, and folder context already carry product context. Do not add `Editor` or `Electron2D` to every type name mechanically.
- In `src/Electron2D.Editor/`, prefer role names such as `ProjectManager`, `ProjectCreateOptions`, `RunController`, `OutputConsole`, and `SceneTreeDock`.
- Add a prefix only when a real collision remains visible at the call site after namespace or folder narrowing.
- Test, smoke, harness, and result types follow the same rule.

## Feature Gate

Feature or runtime work means changing product behavior, UI or API flow, domain rules, integrations, data flow, configuration behavior, startup behavior, production paths, or user-facing behavior.

Required order:

1. Find or update the concrete domain document under `docs/<domain>/`.
2. Add or update an automated test that encodes the acceptance criteria.
3. When practical, run the focused test before implementation and confirm the expected RED.
4. Implement the smallest production change that satisfies the document and test.
5. Get GREEN.
6. Update the same domain document if actual behavior, limits, commands, or evidence changed.

Do not create a second implementation-documentation file for the same behavior when a domain document already exists.

## Task Workflow

- Active tasks live in tracked `TASKS.md`.
- Every substantive change should be represented in `TASKS.md` when the file exists.
- Each task entry must be zero-context: priority, dependencies, linked docs/source/tests, brief, acceptance criteria, subtasks, and agent notes.
- Do not close or archive a task without explicit user acceptance.
- Completed tasks are appended to monthly archives under `data/completed-tasks/YYYY/MM <Russian month name>.md`, not to one-file-per-task archives.
- `TASKS.md` must end with `## ROADMAP`. The roadmap must not create new task IDs and is not an acceptance state.
- `CHANGELOG*` and `RELEASE-NOTES*` are local release drafts. They stay ignored and are not canonical documentation.

## Development Diary

Every repository-work session updates the tracked diary under `data/dev-diary/`.

Path format:

```text
data/dev-diary/YYYY/MM <Russian month name>/DD-MM-YYYY.md
```

Rules:

- Create new daily files with the repository's current diary heading convention.
- Add every new entry strictly to the end of the daily file.
- Every entry must include a chronological actions section with nested local-time bullets.
- Do not sort, rewrite, move, or repair old diary entries without a direct user request.
- Before the final response, inspect the diary tail and confirm the newest entry is last.

## Worktree And Commit Hygiene

- Check `git status --short` before edits and before the final response when files changed.
- Treat unrelated worktree changes as user work.
- Do not use `git reset --hard`, forced checkout, recursive delete, database wipe, or volume cleanup without explicit approval.
- Stage, commit, push, tag, or open pull requests only on explicit request.
- Commit messages use Conventional Commits with an English imperative summary.
- Do not stage the diary unless the task explicitly requires the working log in the commit.
- Keep commits atomic: one reviewable logical change per commit.

## Checks

- Run the narrowest useful checks first, then broader checks when risk justifies them.
- For repeated integration tests, build once and then use focused `--filter`, `--no-build`, and `--no-restore` runs.
- Do not run the full `RepositoryBuildToolTests` suite until the local docs index has been rebuilt for the current tree with `update docs`.
- `audit package` must not be the first expensive runner. Build and run focused tests first, then package.
- After source changes, run the license verifier.
- For docs or generated artifacts, run the repository generation and check commands.
- Do not claim a check passed unless it ran in the current session.

## External Audit Workflow

The full contract lives in `docs/release-management/audit-package.md`; `docs/release-management/AUDIT-REQUEST.md` controls the prompt sent to the external auditor.

Key invariants:

- External audit is an acceptance gate, not a debugging loop.
- `ACCEPT` is a valid verdict. Do not prompt reviewers to "find blockers".
- A blocker requires a current-scope criterion, package evidence, an exact file/command/artifact, material impact, and failed disproof.
- Missing proof means `unsupported concern`, follow-up, note, or accepted risk.
- Previous blocker closure must be tied to configured package checks.
- More than two saved external reports in `metadata.previousVerdictChain` requires the configured `audit-loop-stabilization` check.
- Control audit after primary `ACCEPT` uses a clean-control ZIP in a new ChatGPT project chat, with no previous context and no saved verdict files.
- Saved reports count only when the first non-empty line is exact `VERDICT: ACCEPT` or `VERDICT: NEEDS_FIXES`; prompt text, chat preview, and manual page reading do not count.

Supported commands only:

```bash
dotnet run --project eng/Electron2D.Build -- audit package ...
dotnet run --project eng/Electron2D.Build -- audit package verify ...
dotnet run --project eng/Electron2D.Build -- audit submit ... --browser-backend codex-chrome
```

Forbidden:

- manual ZIP assembly, editing, or repair;
- manual submit;
- manual page verdict reading;
- copied cookies or session files;
- arbitrary clipboard verdicts;
- alternative browser paths unless the domain contract changes first.

## Subagents

Use subagents when parallel read-heavy work will reduce context pollution:

- classify saved verdicts;
- inspect independent source areas;
- summarize large logs;
- compare docs against tests;
- search for repeated blocker classes.

Do not let multiple agents edit the same files in parallel. The main agent owns final edits, final verification, and commit composition.

Subagent prompt must include:

- exact scope and files;
- read/write permission;
- forbidden actions;
- expected summary format;
- whether to wait for all agents before acting.

## Docker, Deployment, And External Systems

- Do not deploy, redeploy, rebuild, or replace runtime stacks unless explicitly asked.
- Treat `docker compose up`, service recreation, and deployment smoke as deployment actions.
- Cleanup after Docker work must be project-scoped and non-destructive.
- Never prune volumes, named Compose volumes, or database volumes without explicit data-removal confirmation.
- Do not run resource-heavy checks in parallel on a developer workstation unless explicitly asked.
- Do not expose secrets, private keys, customer data, production configs, session material, or account exports.
- Redact sensitive values in logs, summaries, screenshots, and final responses.

## Final Response

Keep the final response concise and concrete:

- what changed;
- important files;
- checks and exact results;
- checks that could not run and residual risk;
- local server URL if one was started;
- commit hash if a commit was created.

For feature or runtime work, name the domain document, tests, and command results.
