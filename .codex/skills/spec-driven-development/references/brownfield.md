# Brownfield Workflow (Existing Codebases)

Use this when integrating new features into an existing codebase, modernizing, or reverse-engineering legacy behavior.

## Goals

- Preserve existing behavior (unless explicitly changing it)
- Make new intent explicit via specs and plans
- Reduce risk via incremental delivery and verification
- `/speckit.*` below means upstream Spec-Kit slash commands or equivalent workflow labels; if your host does not expose them, use the same step through CLI/manual workflow.

## Step-by-Step

### 1) Initialize Spec Kit (in-place)

From the repo root:

```bash
specify init --here --ai opencode --script ps
```

If you must avoid agent/tool checks:

```bash
specify init --here --ai opencode --ignore-agent-tools --script ps
```

### 2) Establish a brownfield-aware constitution

Run:

```text
/speckit.constitution <principles>
```

Brownfield-specific principles to include:
- Backward compatibility expectations
- Migration strategy (feature flags, dual-write, deprecations)
- Test strategy (characterization tests before refactor)
- Observability/logging requirements

### 3) Codebase analysis (manual workflow)

Do a quick architecture survey before planning any new feature:
- Build/test commands
- Major modules and boundaries
- Data stores and schemas
- External integrations
- Current technical debt hotspots

Create or update the relevant domain document under `docs/<domain>/` if you need to persist current-state findings.

### 4) Reverse engineer (optional but recommended)

If the existing system lacks current-state docs, write an as-is document for critical flows:
- User-visible scenarios
- Inputs/outputs and error behavior
- Implicit constraints and edge cases
- Success criteria as currently measured

This is not a rewrite - it's documentation that becomes a safety net.

### 5) Specify the new feature

```text
/speckit.specify <what + why + integration constraints>
```

In brownfield, always include:
- Integration points (APIs, modules, UI surfaces)
- Compatibility requirements
- Migration and rollout constraints
- Explicit "non-goals" to avoid scope creep

### 6) Plan for integration

```text
/speckit.plan <tech choices + integration approach>
```

Your plan should call out:
- Where code changes will land
- What contracts will change
- Data migration steps
- Risk mitigation and verification strategy

### 7) Task + handoff to repository implementation pipeline

```text
/speckit.tasks
```

Then hand off to the repository's canonical delivery flow:

- current-state findings and desired contract in the same domain document under `docs/<domain>/`;
- characterization/new failing tests + implementation via `test-driven-development`;
- then verification, diary update, and task tracker update under the repo `AGENTS.md`.

## Brownfield Pitfalls

- Trying to "spec everything" before shipping anything → spec only what you need for the next increment.
- Planning without reading existing integration points → always trace real code paths.
- Refactoring before characterization tests → lock behavior first.
