# Greenfield Workflow (New Projects)

Use this when you're starting from scratch (or when you're willing to treat the codebase as greenfield for a new feature).

## Preconditions

- Specify CLI installed and working (`specify check`)
- You are in the project directory (Git repo recommended)
- If you are working in StellarDown: ensure specs land under `.temp/spec-kit/` (see `spec-driven-development` SKILL.md)
- `/speckit.*` below means upstream Spec-Kit slash commands or equivalent workflow labels; if your host does not expose them, use the same step through CLI/manual workflow.

## Step-by-Step

### 1) Bootstrap Spec Kit

Initialize templates in the repo:

```bash
specify init --here --ai opencode --script ps
```

Notes:
- Use `--script ps` on Windows; use `--script sh` on macOS/Linux.
- Use `--force` only when you understand the overwrite/merge risk.

### 2) Create the constitution

Run in your AI agent:

```text
/speckit.constitution <your governing principles>
```

The constitution should include:
- Testing standards (TDD or not)
- Complexity limits (projects/packages limit, layering rules)
- Performance and determinism constraints (if relevant)
- Security/compliance constraints

After generation, do the 10-point summary and include the current feature status.

### 3) Specify the feature (requirements)

```text
/speckit.specify <what you want + why>
```

Rules:
- Do NOT describe the tech stack here.
- Require FR/SC IDs (FR-001, SC-001, ...).
- Demand explicit edge cases.

After generation:
- Read `.temp/spec-kit/<feature>/spec.md` (or the compatibility mirror under `specs/`) and summarize.
- Remove or resolve `[NEEDS CLARIFICATION: ...]` markers using `/speckit.clarify`.

### 4) Clarify ambiguities

```text
/speckit.clarify <focus areas>
```

Repeat until the spec is stable enough to plan.

### 5) Plan (technical design)

```text
/speckit.plan <tech stack + constraints + architecture preferences>
```

After generation:
- Summarize `plan.md` plus any supporting docs (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`).
- Call out any over-engineering or constitution violations early.

### 6) Tasks (executable breakdown)

```text
/speckit.tasks
```

After generation:
- Summarize `tasks.md`.
- Verify tasks reference requirements (FR/SC) and cover edge cases.

Optional quality checks:

```bash
python .codex/skills/spec-driven-development/scripts/analyze-requirements.py
python .codex/skills/spec-driven-development/scripts/analyze-success-criteria.py
python .codex/skills/spec-driven-development/scripts/analyze-edge-cases.py
```

### 7) Analyze (optional, recommended)

```text
/speckit.analyze
```

Use this as a gate before implementation.

### 8) Handoff to repository implementation pipeline

After planning is ready, switch back to the repository's canonical delivery flow:

- update or create the canonical domain document under `docs/<domain>/` if needed;
- `test-driven-development` for failing tests + implementation;
- final update of the same domain document when implementation changes behavior, limits, or verification notes;
- verification, diary update, and task tracker update under the repo `AGENTS.md`.

## Common Greenfield Failure Modes

- Spec includes tech stack too early в†’ move stack decisions to `/speckit.plan`.
- FR/SC missing IDs в†’ add IDs; it enables traceability.
- Tasks are vague в†’ require file paths and verification steps.
- Over-engineering в†’ enforce constitution gates and simplify.
