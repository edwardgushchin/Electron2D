# Universal Agent Instructions

These instructions are global defaults for Codex agents across projects. They apply when a repository does not provide a stricter local rule.

## Precedence
- A project-local `AGENTS.md` is authoritative for project-specific structure, commands, safety constraints, naming, and documentation rules.
- When this file and a local `AGENTS.md` conflict, follow the stricter safe rule. Prefer the project-specific rule when it narrows scope or adds domain constraints.
- The user's explicit current request may override workflow preferences, but it must not override safety, security, data-protection, or destructive-action constraints.
- If a repository has multiple nested instruction files, follow the most specific file for the files being changed.

## Default Human Language
- Keep this global instruction file in English.
- Write human-facing project Markdown in clear Russian by default unless the user or a project-local rule explicitly requires another language.
- Human-facing Markdown includes domain documents, task files, completed-task archives, development diaries, README files, agent notes, check summaries, acceptance explanations, and prose descriptions of UI labels or workflow states.
- Keep English for exact technical identifiers, paths, commands, code symbols, API fields, package names, official product names, protocol names, and values that must match source contracts.
- When editing existing prose, preserve the repository's terminology and language conventions. Avoid mixed Russian-English prose labels when a normal Russian phrase works.
- In human-facing Markdown, including tasks, completed task archives, domain documents, release notes, changelogs, README files, and diary entries created during the current session, explain internal engineering terms in plain language instead of leaving shorthand such as "internal surface", "test host", "runtime pass", "host context", or similar jargon unexplained. If an English technical term is required because it names a code concept, define it nearby in clear Russian the first time it appears. Prefer concrete descriptions such as "внутренний механизм, доступный только тестам и будущему редактору" over vague hybrid phrases.

## Public Documentation And API Comments
- Outside `README.md`, public documentation must not explain Electron2D through SDL, SDL3, SDL_GPU, SDL_Renderer, SDL_ttf, SDL_mixer, SDL_shadercross, or companion library comparisons. In domain documents, task files, completed-task archives, release notes, changelogs, and GitHub Wiki sources, describe backend choices as internal platform, rendering, audio, text, or export backends without naming SDL-family projects unless the exact identifier is unavoidable in code, paths, package names, or a user-requested reference.
- Outside `README.md`, documentation must not call Electron2D, its runtime, or its public API "Godot-like", "Godot-подобный", or equivalent marketing language. If an internal task or domain document must reference upstream API compatibility, use precise wording such as "совместимость с выбранным API-подмножеством Godot" and keep it technical, not promotional.
- Every public API type, constructor, method, field, property, event, delegate, enum, and enum value must have full SDL-like C# XML documentation. Follow the completeness level of SDL3-CS public API comments such as `PInvoke.cs`: use `<summary>` with `<para>` blocks when the description has more than one sentence, `<remarks>` for behavior and limits, `<param name="...">`, `<typeparam name="...">`, `<returns>`, `<exception cref="...">`, `<threadsafety>`, `<since>`, and `<seealso cref="..."/>` where applicable. Use inline XML tags such as `<see cref="..."/>`, `<paramref name="..."/>`, and `<c>...</c>` instead of plain text code names when a symbol or literal is referenced. Do not leave placeholder comments, bare `<inheritdoc />`, or one-line summaries for public API unless the member is a trivial override whose inherited documentation is exactly correct.

## Repository-First Workflow
- Inspect the repository before changing it. Start with local instructions, `README` or documentation indexes, local `TASKS.md` when present, and nearby source or tests relevant to the request.
- Use fast search tools such as `rg --files` and `rg` when available.
- Let the existing structure, patterns, helper APIs, naming, and test style guide the implementation.
- Keep edits scoped to the requested behavior. Avoid unrelated refactors, formatting churn, dependency changes, and metadata churn unless they are necessary to complete the task safely.
- Prefer structured parsers and project APIs over ad hoc text manipulation when the repository or standard toolchain provides them.

## License Policy
- Electron2D is distributed under the MIT License. Do not replace it with SDL's zlib license text: "SDL-like" for this repository means a full per-file license block at the top of source files, using Electron2D's MIT terms.
- Every tracked hand-written C# source file (`*.cs`) in `src/`, `tests/`, and `data/templates/` must start with the project MIT header in `/* ... */` form.
- Every tracked PowerShell source file (`*.ps1`) in `tools/` must start with the project MIT header in `<# ... #>` form.
- Preserve existing license headers when editing source files. When creating a new source file, add the header before code or declarations.
- Run `powershell -ExecutionPolicy Bypass -File tools\Verify-SourceLicenseHeaders.ps1` before staging or committing source changes.

## Worktree And Git Hygiene
- In a Git repository, check worktree status before editing and again before finalizing when files changed.
- Treat unknown changes as user work. Never revert, overwrite, or reformat unrelated changes unless the user explicitly asks.
- If user changes overlap with the requested area, read them carefully and work with them. Ask only when the overlap makes the task impossible or ambiguous.
- Do not run destructive commands such as hard resets, forced checkouts, recursive deletes, database wipes, or volume cleanup without explicit approval for that exact action.
- Stage, commit, push, tag, or open pull requests only when the user explicitly requests it. Stage only files related to the requested task.
- Commit messages must be written in English. Conventional technical prefixes are allowed, but the human-readable description after the prefix must also be English.

## Feature Gate
- Feature or runtime work means adding or changing product behavior, UI/API flows, domain rules, integrations, data flow, configuration behavior, startup behavior, production code paths, or user-facing behavior.
- Before implementing feature or runtime work, find or create a concrete domain document under `docs/<domain>/`. A domain document is the single verbal description of a specific thing: it includes the expected contract, the current implemented state, known limits, and verification rules in one file.
- If the repository lacks a suitable domain folder, create the minimal `docs/<domain>/` structure and a concise document before implementation when the requested behavior is clear. If the behavior is unclear, stop and ask or propose the missing document instead of writing production code.
- Write or update automated tests that encode the domain document's acceptance criteria before changing production code. When practical, run the focused tests first and confirm they fail against the current implementation.
- After tests are in place, implement the production change with the smallest scope that satisfies the document and the tests.
- After implementation and green tests, update the same domain document if the actual behavior, limitations, commands, screenshots, or verification evidence changed. Do not create a second "implementation documentation" file for the same thing.
- A feature or runtime change is not complete until the domain document, automated tests, and code are aligned. If the project has no viable test toolchain, add and document one or mark implementation blocked before writing production code.

## Task Workflow
- Active tasks live in local `TASKS.md`. This file is intentionally ignored by Git and must not be staged or pushed. Every substantive project change should still be represented there before implementation when the local file is present.
- If local `TASKS.md` is missing, create it with a minimal generic task template in the repository's default human language before making substantive changes, but keep it local-only.
- `CHANGELOG*` and `RELEASE-NOTES*` are local release draft files. They must stay ignored by Git and must not be staged, committed, pushed, or treated as canonical remote documentation.
- Task IDs should be stable and sequential, for example `T-0001`. Use local ISO 8601 timestamps with timezone offsets.
- Use clear statuses such as open, in progress, blocked, and accepted/closed. Mark only the task being worked on as in progress.
- Each task should be zero-context for another agent: include priority, dependencies, linked specs/docs/source files, a detailed brief, acceptance criteria, subtasks when useful, and agent notes.
- Acceptance criteria for code changes must explicitly require the domain document, automated tests, and final document update described in the Feature Gate.
- Do not close or archive a task just because implementation is finished. Close/archive only after the user explicitly accepts it.
- Completed tasks belong in local `completed-tasks/YYYY/MM Месяц.md` monthly archives, not in the active task list and not as a future-work backlog. These archives are intentionally ignored by Git and must not be staged or pushed. Do not create one-file-per-task archives such as `completed-tasks/T-0001.md`; append the completed task entry to the file for its completion month instead.

## Release Sequencing
- Do not start `Electron2D.Editor` implementation tasks until the UI public API gate is closed: all UI-related public API rows in GitHub Wiki `API-Compatibility.md` must be implemented, tested, documented, and marked `Supported`, not `Partial`. It is not enough to change compatibility labels; the underlying runtime API, XML comments, generated Wiki pages, domain documents, and automated checks must already match the `Supported` status.

## Development Diary
- Every agent session that works in this repository must keep a local development diary entry under `dev-diary/`. Diary notes are working logs for continuity between agents: they do not replace `TASKS.md`, are not product domain documents, and must not be used as a future-work backlog. `dev-diary/` is intentionally ignored by Git and must not be staged or pushed.
- Use the local date for the file path: `dev-diary/YYYY/MM Месяц/DD-MM-YYYY.md`, for example `dev-diary/2026/06 Июнь/21-06-2026.md`. `YYYY` is the local four-digit year. Month directory names use Russian month names.
- New daily diary files must start with `# Дневник разработки: DD-MM-YYYY`. If a historical daily file already exists without this heading or in an older format, keep it append-only and add new entries strictly at the tail; do not rewrite old entries only to migrate formatting.
- Add or update a diary entry when starting work in the repository, after important decisions, discoveries, file changes, checks, commits, pushes, blockers, cleanup, scope changes, and before sending the final response.
- Every diary entry for repository work must include a `- Действия:` section. Keep it as a chronological action log with nested bullets in the form `  - HH:MM - ...`. Record each meaningful action phase there as it happens; do not rely only on the final `Изменения` or `Проверки` summaries to reconstruct the work after the fact.
- Existing daily diary files are append-only. If the daily file already exists, add every new entry strictly after the last existing entry in that file. Never insert new entries at the beginning or in the middle of the file, never sort entries, and never rewrite, move, or "repair" older entries unless the user explicitly asks for that cleanup.
- Before sending the final response, inspect the tail of the daily diary file and confirm that the newest entry you added is the last entry in the file.
- Use this compact Markdown format. The labels are Russian because diary entries are human-readable Russian notes:

```markdown
# Дневник разработки: 21-06-2026

## 14:30 +03:00 - Agent: Codex

- Задача: T-0001 / ad-hoc / запрос пользователя
- Контекст: кратко, что агент собирался сделать.
- Действия:
  - 14:30 - изучены `AGENTS.md`, состояние рабочей копии и связанная документация.
  - 14:40 - изменены конкретные файлы.
  - 14:50 - выполнены проверки и зафиксированы результаты.
- Изменения: какие файлы или документы были изменены.
- Решения: важные решения и почему.
- Проверки: какие команды проверки запускались и результат.
- Далее: что осталось сделать или что должен знать следующий агент.
```

- If the work is not tied to a task from `TASKS.md`, write `Задача: ad-hoc`.

## Testing And Verification
- Run the narrowest useful checks first, then broader checks when risk or blast radius justifies them.
- Use the repository's documented commands for linting, formatting, tests, builds, contract checks, and generated artifact verification.
- Do not claim a check passed unless it was run in the current work session. If a check cannot run, state the blocker and residual risk.
- For frontend changes, run the relevant lint/build/test command and visually verify the affected local experience when a local server or static file makes that practical.
- For generated artifacts, run the project's generation or consistency check and include resulting files only when they are part of the expected change.

## Docker, Deployment, And Heavy Checks
- Do not deploy, redeploy, rebuild, or replace a local or remote runtime stack unless the user explicitly asks for that action in the current conversation.
- Treat `docker compose up`, rebuilds, service recreation, deployment smoke runs, and equivalent stack-changing commands as deployment actions.
- After any Docker deploy, rebuild, one-off container run, or smoke run, clean up temporary Docker artifacts before the final response unless the user asks to keep them.
- Prefer project-scoped cleanup: inspect Docker usage before and after, remove stopped one-off containers for the current project, prune dangling build cache with the safe non-aggressive option, and prune dangling images when reported reclaimable.
- Never prune Docker volumes, database volumes, named Compose volumes, or run volume-destructive cleanup unless the user explicitly confirms data removal.
- Avoid running resource-heavy checks in parallel on a developer workstation. Run Docker builds, deployments, full test suites, frontend builds, browser automation, and contract generation sequentially unless the user explicitly asks for parallel stress testing.
- After browser automation or local servers started by the agent, close processes that are no longer needed unless the user asked to keep them running.

## Security And External Systems
- Never commit or expose real tokens, passwords, session secrets, private keys, production secret files, customer data, account exports, or other sensitive material.
- Keep secret configuration out of frontend bundles and public examples. Use non-secret example config files when examples are needed.
- Do not enable approval bypasses, destructive production actions, live external transactions, or irreversible side effects unless the user explicitly asks in the current conversation and the repository's safety rules allow it.
- Fail closed when credentials are missing, external payloads are invalid, permissions are unclear, or safety checks cannot be completed.
- Redact sensitive values in logs, summaries, screenshots, and final responses.

## Final Response
- Keep the final response concise and concrete: state what changed, which files matter, and which checks passed or could not run.
- For feature or runtime work, name the domain document used or updated, the tests added or changed, and the exact test command result or blocker.
- If a local server was started for the user, provide the URL and note whether it is still running.
- If work is blocked, state the blocker, what remains safe to do next, and any files already changed.
- Do not present unverified assumptions as completed work.
