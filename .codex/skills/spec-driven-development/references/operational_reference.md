# Operational Reference

Use this document for detailed command references, workflow shapes, tooling helpers, and upstream metadata that do not need to stay in the main `SKILL.md`.

## Supported AI Agents

Works with:

- Claude Code
- GitHub Copilot
- Gemini CLI
- Cursor
- Qwen Code
- opencode
- Windsurf
- Kilo Code
- Auggie CLI
- CodeBuddy CLI
- Roo Code
- Codex CLI
- Amp
- Amazon Q Developer CLI (limited: no custom args for slash commands)

## Key Commands Reference

### Specify CLI

```bash
specify init <project-name>
specify init --here --ai opencode --script ps
specify check
```

### Upstream Spec-Kit Step Commands

```text
/speckit.constitution
/speckit.specify
/speckit.clarify
/speckit.plan
/speckit.tasks
/speckit.analyze
/speckit.checklist
/speckit.implement
```

For StellarDown, `/speckit.implement` is not the canonical implementation path. Treat it as an upstream Spec-Kit capability only. The repository's real delivery flow after planning is domain document under `docs/<domain>/`, then `test-driven-development`, then a final update of the same document when behavior, limits, or verification notes changed.

### Brownfield Extras

If a user references workflow labels like `/speckit.reverse-engineer` or `/speckit.integration-plan`, treat them as conventions, not core Spec Kit commands and not repository-local custom commands by default. Execute them as: analyze the codebase -> capture as-is behavior -> write integration notes under `.temp/spec-kit/<feature>/` (optionally mirrored under `specs/<feature>/`) -> summarize.

## Workflow Overview

### Greenfield

```text
specify init -> /speckit.constitution -> [SUMMARIZE] ->
/speckit.specify -> [SUMMARIZE] -> /speckit.plan -> [SUMMARIZE] ->
/speckit.tasks -> [SUMMARIZE] -> [HANDOFF TO REPO IMPLEMENTATION PIPELINE]
```

Full details: [Greenfield Workflow](greenfield.md)

### Brownfield

Spec Kit does not ship a single brownfield command; treat brownfield as a workflow:

```text
specify init --here -> [ANALYZE CODEBASE] -> [SUMMARIZE] ->
/speckit.constitution -> [SUMMARIZE] ->
/speckit.specify -> [SUMMARIZE] -> /speckit.plan -> [SUMMARIZE] ->
/speckit.tasks -> [SUMMARIZE] -> [HANDOFF TO REPO IMPLEMENTATION PIPELINE]
```

Full details: [Brownfield Workflow](brownfield.md)

## Analysis Scripts

The SDD skill includes optional helpers for validation and progress checks.

### `scripts/phase_summary.sh`

Generates a progress report across phases in a `tasks.md` file.

On Windows, use Git Bash/WSL or compute the same summary through equivalent local tooling.

```bash
.codex/skills/spec-driven-development/scripts/phase_summary.sh specs/003-keyboard-shortcuts/tasks.md
```

### `scripts/analyze-requirements.py`

Analyzes FR coverage across `spec.md` and `tasks.md`.

```bash
python .codex/skills/spec-driven-development/scripts/analyze-requirements.py
```

### `scripts/analyze-success-criteria.py`

Analyzes SC verification coverage.

```bash
python .codex/skills/spec-driven-development/scripts/analyze-success-criteria.py
```

### `scripts/analyze-edge-cases.py`

Analyzes edge case coverage across specifications.

```bash
python .codex/skills/spec-driven-development/scripts/analyze-edge-cases.py
```

## Upstream References

- GitHub Spec-Kit Repository: https://github.com/github/spec-kit
- Issues/Support: https://github.com/github/spec-kit/issues
- License: MIT

## Maintainers

- Den Delimarsky (@localden)
- John Lam (@jflam)
