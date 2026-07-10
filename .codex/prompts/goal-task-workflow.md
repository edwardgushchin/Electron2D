# Goal Task Workflow

Этот файл описывает только порядок goal-цикла. Он не заменяет и не дублирует root rules из `AGENTS.md`, подробный рабочий протокол из `docs/repository/agent-workflow.md` и точный audit-контракт из `docs/release-management/audit-package.md`.

Короткий active goal prompt живёт в `.codex/prompts/goal-prompt.md`, имеет лимит 3500 символов и остаётся одной компактной постановкой цели.

## Sources

- Repository rules: `AGENTS.md`.
- Detailed agent workflow: `docs/repository/agent-workflow.md`.
- Task state: `TASKS.md`.
- Domain behavior: relevant `docs/<domain>/`.
- Audit package and submit rules: `docs/release-management/audit-package.md`.
- External auditor text: `docs/release-management/AUDIT-REQUEST.md`.

## Flow

1. PREFLIGHT: load the sources above for `<task-id>`, inspect relevant saved verdict reports and diary notes, then identify current HEAD, baseline, scope, acceptance criteria and planned checks.
2. READY-TO-START: continue to work only if task state is `open` or an explicitly defined ready state, all dependencies are accepted/closed or have saved `ACCEPT`, and each external blocker has an explicit current user override. If state is `blocked` or `tracking`, dependencies are not accepted, or an external blocker has no override, write a blocker report to `TASKS.md` and the diary, then stop without production work, audit packaging or commit.
3. WORK: execute the repository feature gate from `docs/repository/agent-workflow.md`. If subagents are useful, keep them scoped and let the main agent own final edits and decisions.
4. CHECKS: use the narrowest useful checks first. For audit-contract changes, `verify audit-contracts` is the fast path. Escalate to focused integration tests, docs checks, license checks and `git diff --check` according to risk and changed files.
5. LOCAL LOOP: treat local failures as local work, not external audit iterations. Record numerical evidence by level, for example `Fast: 3 теста / 1m13s; Medium: 4 теста / 3m18s; Heavy: не запускался`.
6. PACKAGE/VERIFY/SUBMIT: when external acceptance is needed, invoke only the deterministic C# commands defined by `docs/release-management/audit-package.md`. The agent does not reproduce browser steps, choose chats manually, assemble ZIP files, read verdicts manually or repeat a ZIP whose submit iteration has already been reserved; routing and the complete browser sequence belong to the tool, as does global `rNN` reservation. Repeated external blockers must first pass the audit-loop root-cause gate below, then move through the stabilization model, including `audit-loop-stabilization`, `previousVerdictChain` and `blockerClosureList`.
7. VERDICT: only saved reports count. Primary `VERDICT: ACCEPT` starts the control audit path; task acceptance needs primary and control `VERDICT: ACCEPT`.
8. ACCEPT/DONE: after accepted reports and closed actionable notes, sync task state, diary, archives/release notes as required, remove temporary audit artifacts, run final checks, inspect `git status`, then create the requested commit without push.

## Audit-Loop Root-Cause Gate

After every saved `VERDICT: NEEDS_FIXES`, classify each blocker by failure class, not only by the concrete example. A failure class is the underlying invariant that failed, for example boundary detection, token parsing, platform path semantics, JSON/raw-text ambiguity, browser extraction, package restore model or report validation.

Before preparing the next audit package, write a blocker synthesis to `TASKS.md` and the diary:

- saved report path and blocker id;
- concrete failing example;
- underlying failure class;
- whether this is a new class or a repeat/variant of an existing class;
- why the proposed fix closes the whole class, not only the external auditor's example;
- focused tests or verifier cases that cover the concrete example and at least one neighboring variant.

If two saved reports in the same task hit the same component and the same failure class, narrow example-by-example fixes are forbidden. Stop packaging and either implement a class-level design fix or record a blocker explaining why such a fix is not currently possible. The next package is forbidden until the relevant domain document states the generalized invariant and focused tests prove it.

For repeated sanitizer, parser, path, boundary or token failures, closure must include a token-level, parser-level or equivalent full-invariant proof. Local delimiter, adjacent-character or one-off guard fixes are insufficient unless the blocker synthesis proves that the full token cannot be ambiguous on every relevant platform.

For repeated browser, package, restore or report-extraction failures, closure must include a stable driver, harness test or verifier for the workflow class. Source-text assertions and retries alone are insufficient closure.

A new package after repeated `NEEDS_FIXES` must include `audit-loop-stabilization` evidence that exercises the generalized failure class, not only the latest external example.

Broadening a fix within the same failure class is required closure work for the existing audit scope. It must remain inside `metadata.scopeTaskIds`, `metadata.scopeSummary`, package evidence, domain documentation and `blockerClosureList`; if the fix changes behavior outside the failed invariant, stop and record a blocker or create a separate task instead of expanding the package silently. Do not classify same-class closure as scope creep unless it changes behavior outside the failed invariant.
