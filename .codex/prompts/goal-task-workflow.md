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
6. PACKAGE/VERIFY/SUBMIT: when external acceptance is needed, follow `docs/release-management/audit-package.md` exactly. Repeated external blockers move through its stabilization model, including `audit-loop-stabilization`, `previousVerdictChain` and `blockerClosureList`.
7. VERDICT: only saved reports count. Primary `VERDICT: ACCEPT` starts the control audit path; task acceptance needs primary and control `VERDICT: ACCEPT`.
8. ACCEPT/DONE: after accepted reports and closed actionable notes, sync task state, diary, archives/release notes as required, remove temporary audit artifacts, run final checks, inspect `git status`, then create the requested commit without push.
