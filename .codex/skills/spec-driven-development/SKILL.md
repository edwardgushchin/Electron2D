---
name: spec-driven-development
description: This skill should be used when users want guidance on Spec-Driven Development methodology using GitHub's Spec-Kit. Guide users through executable specification workflows for both new projects (greenfield) and existing codebases (brownfield). After any SDD command generates artifacts, automatically provide structured 10-point summaries with feature status tracking, enabling natural language feature management and keeping users engaged throughout the process.
license: MIT
metadata:
  version: 2.1.0
  author: Based on GitHub Spec-Kit by Den Delimarsky and John Lam
  triggers:
    - spec-driven development
    - spec kit
    - speckit
    - sdd
    - specify cli
    - specification driven
    - github spec-kit
    - /speckit
    - constitution
    - specify init
    - executable specifications
    - intent-driven development
    - brownfield
    - existing codebase
    - legacy code
    - legacy system
    - add features to existing
    - modernize
    - modernization
    - existing project
    - reverse engineer
    - codebase analysis
    - iterative enhancement
    - feature status
    - track features
    - add feature
    - move feature
    - reorder features
    - feature progress
    - feature dependencies
  tags:
    - development-methodology
    - ai-native-development
    - spec-driven
    - github
    - project-management
    - workflow
    - requirements
    - planning
---

# Spec-Driven Development Skill

Guide users through GitHub's Spec-Kit for Spec-Driven Development, a methodology that flips traditional software development by making specifications and planning artifacts drive implementation intent.

## Core Philosophy

Spec-Driven Development emphasizes:
- **Intent-driven development**: Define the "what" before the "how"
- **Rich specification creation**: Use guardrails and organizational principles
- **Multi-step refinement**: Not one-shot code generation
- **AI-native**: Heavy reliance on advanced AI capabilities

Remember: This is **AI-native development**. In StellarDown, Spec-Kit outputs are planning/spec-generation artifacts that shape implementation intent, but they do **not** bypass the repository's canonical implementation pipeline (`test-driven-development`).

## StellarDown Repository Integration (Required)

For this repository, `spec-driven-development` is a planning/spec-generation workflow, not a replacement for the repo task, diary, test, and documentation rules.

- It complements `test-driven-development`; it does not compete with or replace the implementation pipeline.
- Active project work is tracked in `TASKS.md`.
- Development context is recorded in `dev-diary/`.
- Accepted/closed history is archived in `completed-tasks/YYYY/MM Месяц.md`, grouped by completion month.
- `/speckit.*` names in this skill are upstream Spec-Kit workflow labels or host-provided aliases, not repository-local custom commands by default.
- If the current runtime does not expose `/speckit.*`, use the equivalent `specify` CLI steps or describe the same workflow phase in natural language.

## StellarDown Document Storage (Required)

Canonical domain documents must be saved under `docs/<domain>/`.

There is no separate canonical `specifications` tree and no separate canonical `documentation` tree. A domain document is the single durable source for one concrete thing: expected contract, current implemented state, limits, and verification notes live together.

## StellarDown Semantics and Context Order (Required)

- A **domain document** describes what an artifact should do, what it currently does, known limits, and how it is verified.
- Spec-Kit artifacts are planning aids. They do not become a second source of truth unless their decisions are folded into the domain document.

Before implementing any task:

1. Read `AGENTS.md`, `TASKS.md`, the relevant domain document under `docs/<domain>/`, and the latest relevant `dev-diary/` or `completed-tasks/` context.
2. Update or create the domain document before implementation.
3. Only after the document is clear, move through SDD / Spec-Kit planning and then the TDD implementation path.

When something is ambiguous:

- First cross-check the domain document again.
- If the answer is not in the document, ask the user an explicit question.
- After the answer, update the document so that the decision becomes part of the contract and current context.

### Brownfield Rule: Reverse Engineering != Canonical Document

- If brownfield analysis mainly describes current architecture, files, types, APIs, dependencies, and constraints, fold only the durable conclusions into the relevant domain document.
- A domain document must stay a verbal description of a concrete thing, not a raw reverse-engineered inventory.
- If Spec-Kit creates technical planning artifacts, keep them as working methodology artifacts and summarize their decisions into `docs/<domain>/...` before implementation.

### Handoff Back To Implementation

When planning/spec-generation artifacts are ready, continue through the integrated `test-driven-development` pipeline: domain document -> failing tests -> implementation -> green tests -> final domain document update when needed -> diary/task update.

Recommended layout for Spec-Kit artifacts:
- Feature folders: `.temp/spec-kit/<###-feature-name>/`

Compatibility note:
- If some external tool expects repository-root `specs/`, treat it only as an optional temporary mirror of `.temp/spec-kit/`, not as a second editable canonical location.

## Quick Decision Tree

### Is this a new project (greenfield)?
-> See [Greenfield Workflow](references/greenfield.md) for the complete process

### Is this an existing codebase (brownfield)?
-> See [Brownfield Workflow](references/brownfield.md) for reverse-engineering and integration guidance

### Need installation help?
-> See [Installation Guide](references/sdd_install.md) for setup and troubleshooting

## How to Use This Skill

### When a user asks about SDD / Spec Kit

1. Explain the core philosophy (intent-driven, executable specs, iterative refinement).
2. Verify prerequisites (uv, Python 3.11+, Git, agent integration).
3. Identify context:
   - Greenfield (new project / new feature) -> [Greenfield Workflow](references/greenfield.md)
   - Brownfield (existing codebase / modernization) -> [Brownfield Workflow](references/brownfield.md)
4. After each Spec-Kit step, for example a `/speckit.*` step when the host provides it, run the artifact summarization loop (10-point summary + feature status).

### When a user wants to start a new project

1. Install Specify CLI (see [Installation Guide](references/sdd_install.md)).
2. Initialize:
   ```bash
   specify init my-project --ai opencode
   ```
3. Follow the greenfield flow (constitution -> specify -> plan -> tasks -> handoff to the repository implementation pipeline).

### When a user has an existing codebase

1. Check whether Spec Kit scaffolding exists (`.specify/` plus temporary `.temp/spec-kit/` storage or its optional `specs/` mirror).
2. If missing, initialize in place (`specify init --here`).
3. Create or adjust the constitution to reflect brownfield constraints.
4. Add features using the same specify -> plan -> tasks -> handoff-to-implementation loop.

## Installation Quick Start

**Recommended (Persistent):**
```bash
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git
```

**One-time Usage:**
```bash
uvx --from git+https://github.com/github/spec-kit.git specify init <PROJECT_NAME>
```

**Verify:**
```bash
specify check
```

For detailed installation options, troubleshooting, and environment variables, see [Installation Guide](references/sdd_install.md).

## Artifact Summaries and Feature Status

After any SDD step that creates or changes artifacts, summarize the result before moving on.

- Detect which artifact changed (`constitution.md`, `spec.md`, `plan.md`, `tasks.md`, or brownfield analysis notes).
- Read the changed artifact and extract the main decisions, risks, and next-step implications.
- Present the standard 10-point summary plus a brief feature-status line.
- Offer concise next actions: proceed, revise, regenerate, or explain.
- Skip this loop only when the user explicitly asks to skip summaries, nothing changed, or the command failed.

Detailed template, examples, status rules, and dashboard patterns:
- [Summarization Guide](references/summarization.md)
- [Feature Management Guide](references/feature_management.md)

## Operational References

Use the detailed reference docs when the condensed workflow above is not enough:

- [Installation Guide](references/sdd_install.md)
- [Greenfield Workflow](references/greenfield.md)
- [Brownfield Workflow](references/brownfield.md)
- [Operational Reference](references/operational_reference.md)

## Integration with Other Skills

- Use `test-driven-development` when implementing tasks (tests first).
