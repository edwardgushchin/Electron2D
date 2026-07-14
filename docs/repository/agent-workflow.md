# Electron2D Agent Workflow

This document contains the detailed agent workflow that used to make root `AGENTS.md` too large. Root `AGENTS.md` stays a compact project map and guardrail index; read this document before substantive repository work.

## Instruction Priority

- System and developer instructions from the execution environment outrank repository rules.
- The closest applicable `AGENTS.md` or `AGENTS.override.md` narrows the root contract for the files being changed.
- The current user request may change workflow preferences, but it cannot override safety, data protection, destructive-action limits, or verifiability.
- When instructions conflict, choose the stricter safe rule and record the decision briefly in the diary.

## Language And Terms

- `AGENTS.md` and this workflow document are English by explicit user request.
- The repository human language is declared by the closest applicable `AGENTS.md`; root `AGENTS.md` currently declares Russian (`ru`). Changing the repository language requires updating that instruction first.
- Project-facing Markdown uses the repository language unless the user or a more specific local rule explicitly requires another language. This includes domain documents, the diary, README files, agent notes, release notes, changelogs, and UI prose.
- Every new or changed human-readable taskboard value must use the repository language. This includes task and subtask titles and descriptions, acceptance-criterion descriptions, comments, activity and evidence notes, blocker and closure explanations, human-readable group names and descriptions, and prose in the execution contract such as readiness rules, stop conditions, allowed or forbidden changes, required outputs, and external-audit instructions.
- Do not copy source material written in another language into a human-readable task field as ordinary prose. State its meaning in the repository language and retain the original only when it is an exact quotation, external evidence, or a technical literal.
- Exact identifiers remain unchanged: paths, commands, API fields, package names, code symbols, file formats, official product names, and diagnostic codes.
- Task IDs, criterion IDs, schema and enum values, machine labels, literal command lines, and other contract values also remain unchanged. Translate only the human-readable explanation around them.
- Migrated `legacySourceFragments`, historical task bytes, external quotations, and attached evidence remain verbatim. This language rule applies prospectively to task creation and updates; any normalization of existing task prose must use `e2d tasks` and must not rewrite preserved legacy content.
- In Russian prose, do not use English words as shorthand when a normal Russian term exists. If an exact term is required, put it in backticks or explain it nearby.
- Avoid English-heavy comma lists inside Russian sentences. Give the Russian meaning first, then the exact identifier when needed.

### Human Writing Style For Tasks

Task text is written for a teammate, not for a parser or another language model. Titles, descriptions, criteria, comments, evidence, blocker explanations and execution-contract prose must sound like normal human communication in the repository language.

- Start with what needs to happen, why it matters and what result is expected. Do not start with an inventory of schema fields, storage objects or internal subsystems.
- Use complete, natural sentences. Prefer active verbs and concrete nouns over compressed noun chains, bureaucratic wording and strings of technical labels.
- Do not turn prose into a machine summary. Dense slash-separated lists, arrows that stand in for an explanation, unexplained abbreviations and English-heavy term sequences are not acceptable task writing.
- Mention an internal field, schema name, enum value, command or path only when the person doing the work needs that exact detail. Explain its meaning in ordinary words before or alongside the literal identifier.
- Do not list every implementation detail merely because it exists. Keep the task focused on the decision, constraint or observable result that affects the work.
- Write acceptance criteria as outcomes a person can verify. A criterion should say what must be true, not recite the names of internal structures involved in making it true.
- Before saving task text, read it as if the reader had not opened the schema and did not know the current agent's train of thought. If the reader must decode the sentence, rewrite it.

Unacceptable machine-like wording:

> Task schema v2 хранит immutable UID, человекочитаемый ID, execution contract, criteria, activity, audit fields, attachment metadata и legacy fragments. Board schema v2 хранит placements/ranks и иерархию **Epoch -> Milestone -> Task**; readiness вычисляется из dependency DAG. **Done** требует человеческой приёмки, а перенос в **.taskboard/completed** выполняется отдельным **archive**.

The same meaning written for a person:

> У каждой задачи есть обычный номер и постоянный внутренний идентификатор. В задаче хранятся требования, критерии проверки, история изменений и сведения о вложениях. На доске задачи можно объединять в эпохи и вехи и располагать в нужном порядке. Если задача зависит от других, начать её можно только после их завершения. Задача считается завершённой только после приёмки человеком. После этого её можно отдельно перенести в архив `.taskboard/completed` командой `archive`.

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

- The tracked canonical store is `.taskboard`; its board document is `.taskboard/board.e2tasks`, active tasks live under `.taskboard/tasks/`, and archived tasks live under `.taskboard/completed/`.
- The CLI is the only public writer for task state, and `e2d tasks` is the canonical read interface. Agents, repository automation, VS Code, Tooling and MCP must not edit `.e2tasks` or `.e2task` JSON directly.
- Before substantive work, use `e2d tasks get <task-id> --project . --format json` and `e2d tasks board --project . --format json` to read the task, its current revision, dependencies and group placement.
- Every substantive change should have a task created through `e2d tasks create`. The task must remain zero-context: priority, dependencies, linked docs/source/tests, a detailed brief, acceptance criteria, subtasks, activity and an execution contract.
- A newly created task must be semantically complete in the canonical schema before implementation starts. Read it back through `e2d tasks get` and fill every applicable structured field instead of hiding the same information only in `description`: task type, ready-to-start rules, stop conditions, allowed and forbidden changes, required outputs and commands, parent/dependencies/subtasks, board placement, linked documentation/source/tests/artifacts, acceptance criteria, priority and deadline when one is actually known. Fields may stay empty only when they are genuinely not applicable or are historical runtime data that does not exist yet, such as activity, attachments, diagnostics, transactions, jobs, acceptance timestamps or cancellation data.
- Every task must carry the relevant tags from the board-owned tag catalog. Reuse existing tags by stable ID, create a missing generally reusable tag through `e2d tasks tag create`, and avoid encoding status, priority, epoch or milestone as tags because those already have canonical fields. Read the task back and verify that the expected tag IDs resolve to the intended board tag definitions.
- Task creation fails closed when the public CLI cannot express a required structured field. Do not leave a partial task, edit `.e2task` directly or treat prose in `description` as a substitute for missing schema data. Extend and verify the revision-aware `e2d tasks` mutation contract first, then complete the task and only after that move it to `InProgress`.
- Mutations use the task and board revisions returned by the preceding read. Use the relevant `e2d tasks update`, `set-status`, `dependency`, `parent`, `group`, `comment`, `attachment`, `submit`, `accept`, `request-changes`, `cancel`, `reopen`, `archive` or `unarchive` command; never bypass optimistic concurrency by editing files.
- Acceptance criteria and subtasks remain target contract until explicit human acceptance. Current evidence, audit verdicts, blocker reports and remaining gaps belong in task activity, normally through `e2d tasks comment add`, rather than by rewriting the target contract.
- `Done` requires human acceptance. Do not invoke acceptance or archive a task without explicit user acceptance. Only `Done` or `Cancelled` tasks may be moved to `.taskboard/completed/` through `e2d tasks archive`.
- Run `e2d tasks verify --project . --format json` after task graph or task-state mutations. Missing dependency targets, dependency cycles, parent cycles, invalid placements, revision mismatches and dual legacy/canonical stores fail closed.
- `CHANGELOG*` and `RELEASE-NOTES*` are local release drafts. They stay ignored and are not canonical documentation.

### Ready-To-Start Gate

When a user request, goal prompt, or automation names a concrete task ID, run this gate through `e2d tasks get` before changing production code, tests, generated artifacts, docs, or task state for that task.

You may start implementation work only when all of these are true:

- the task state is `open` or an explicitly defined ready state;
- every task dependency is accepted, closed, or has a saved `ACCEPT` verdict when audit-managed;
- every external blocker has an explicit current user override recorded in the task notes and diary.

Stop conditions:

- If the task state is `blocked`, do not implement production code, do not add tests for that task, do not package audit artifacts, and do not commit.
- If the task state is `tracking`, use it only as context; pick an unblocked child task instead of implementing the tracking task directly.
- If the task has `Execution class: final-gate`, treat it as verify-only: run the gate commands, gather evidence, produce the gate report, and record blockers with owning tasks. Do not fix leaf implementations, subsystem backends, platform runtime behavior, or class-specific docs inside the final gate; only missing or stale gate tooling/evidence plumbing may be changed there.
- If dependencies are not accepted or an external blocker has no explicit current user override, add a blocker report through `e2d tasks comment add`, set the task to `Blocked` through `e2d tasks set-status` when appropriate, update the diary, and then stop without commit.
- A goal prompt that says to reach `ACCEPT` and commit does not bypass this gate. The gate must pass first, or the correct result is a blocker report.

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
- Before adding a new test, decide whether it protects a stable product, API, tooling, or process contract for the full project lifecycle. Do not create tests for one-off bookkeeping, wording cleanup, task-board moves, transient audit packaging details, or other low-risk changes when a direct inspection, existing verifier, or documented note is enough.
- For repeated integration tests, build once and then use focused `--filter`, `--no-build`, and `--no-restore` runs.
- Do not run the full `RepositoryBuildToolTests` suite until the local docs index has been rebuilt for the current tree with `update docs`.
- `audit package` must not be the first expensive runner. Build and run focused tests first, then package.
- After source changes, run the license verifier.
- For docs or generated artifacts, run the repository generation and check commands.
- Do not claim a check passed unless it ran in the current session.

### Mandatory Windows UI Capture Through FFmpeg

Every UI task must use the tracked `eng/tools/capture_window.py` for visual capture of the real Windows application. It uses the installed `ffmpeg` `gdigrab` input and has no third-party Python dependencies. This is the only approved screenshot path for Electron2D repository work: agents must not use the Windows-control or `computer-use` screenshot backend, even when that backend is available.

Typical commands:

```powershell
python eng/tools/capture_window.py --list
python eng/tools/capture_window.py --title "Visual Studio Code" --output .temp/taskboard-window.png
python eng/tools/capture_window.py --output .temp/active-window.png
python -m unittest discover -s eng/tools/tests -p "test_capture_window.py" -v
```

Rules:

- Run the helper against the real application after every UI implementation or fix. Unit tests, Extension Host checks and compilation are complementary evidence, not replacements for the required PNG.
- Do not use Windows-control or `computer-use` for screenshots in this repository. Do not describe the FFmpeg path as a fallback; it is the canonical capture path.
- Keep captures under `.temp/` unless the task explicitly requires a tracked visual artifact.
- Use `--title <fragment>` for a known application, no selector for the foreground window, `--hwnd <value>` for an exact native window, or `--desktop` for the virtual desktop.
- Inspect the resulting PNG before claiming visual verification; a successful process exit alone is not visual evidence.
- Record the command, PNG path and what was visibly verified in task activity and the diary.
- The helper may restore and activate a minimized or covered target. If the Windows foreground lock still rejects activation, it temporarily applies `TOPMOST`, captures the visible desktop rectangle and removes `TOPMOST` in `finally`; it must never leave the target permanently above other windows.
- Visible desktop crop is the primary window strategy because `gdigrab` can return a successful but fully black `HWND`/title frame for GPU-composited Electron or Chromium windows. Exact `HWND` and title capture remain fallbacks when desktop crop itself fails.
- A desktop crop is evidence of pixels actually visible after target activation. It can still include foreign overlays, native popup windows or monitor-edge clipping, so inspect the PNG and describe any such limitation.
- A fixture, headless render or user-provided pre-change screenshot may be diagnostic context, but it never replaces the mandatory post-change capture through this helper.
- If Python, `ffmpeg`, window discovery, or every capture strategy fails, report the exact command and diagnostic and leave visual verification explicitly blocked. Do not silently substitute another screenshot backend, fixture or headless render.

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
