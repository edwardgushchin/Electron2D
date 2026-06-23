# Summarization Guide

After any SDD command generates or modifies artifacts, automatically summarize the result so the user can review intent before the next step.

## After Each Command Completes

1. Detect which artifacts changed:
   - Constitution: `.specify/memory/constitution.md` (or `memory/constitution.md`, depending on template)
   - Spec: `.temp/spec-kit/<###-feature-name>/spec.md` (or a compatibility mirror under `specs/`)
   - Plan: `.temp/spec-kit/<###-feature-name>/plan.md`
   - Supporting planning artifacts when present: `research.md`, `data-model.md`, `contracts/`, `quickstart.md`
   - Tasks: `.temp/spec-kit/<###-feature-name>/tasks.md`
   - Any brownfield analysis reports you created
2. Read the changed artifacts and extract:
   - Core decisions and rationale
   - Requirements, success criteria, and edge cases
   - Architecture choices, milestones, and dependencies
   - Risk areas, gaps, and review points
3. Present the structured summary.
4. Include a brief feature-status line.
5. Offer next-step choices: proceed, modify, regenerate, or explain.

## 10-Point Summary Template

Use this format after any SDD command completes:

```text
## [Command Name] Completed - Here's What Just Happened

### Key Decisions Made (Top 3)
1. [Decision] - Rationale: [Why this was chosen]
2. [Decision] - Rationale: [Why this was chosen]
3. [Decision] - Rationale: [Why this was chosen]

### What Was Generated
- [Artifact 1]: [Brief description]
- [Artifact 2]: [Brief description]

### Important Items to Review (Top 3)
1. [Critical review point]
2. [Important verification item]
3. [Edge case or scope boundary]

### Watch Out For (Top 2)
- [Potential issue] - How to avoid: [Guidance]
- [Common mistake] - How to avoid: [Guidance]

### What This Enables Next (2 Options)
- Option 1: [Next step] - Best if: [Condition]
- Option 2: [Alternative] - Best if: [Condition]

Feature Status: [Current Feature] ([Stage]) -> Next: [Next Feature]
Progress: [X]% | Completed: [N] of [Total] features | Dependencies: [Status]

Your options: [A] Proceed [B] Modify [C] Explain more [D] Show full status
```

## Example After `/speckit.specify`

```text
## Specify Completed - Here's What Just Happened

### Key Decisions Made
1. Authentication uses JWT tokens - Rationale: stateless and horizontally scalable
2. Password minimum is 12 characters - Rationale: balances security and usability
3. Session timeout is 24 hours - Rationale: reasonable default unless security posture says otherwise

### What Was Generated
- `specs/001-user-authentication/spec.md`: requirements, user stories, FR/SC IDs, edge cases

### Important Items to Review
1. Password reset flow - verify notification and infra assumptions
2. MFA scope - confirm whether it is MVP or later
3. Rate limiting - align thresholds with security policy

### Watch Out For
- External dependencies not specified - How to avoid: add them to `plan.md` and `research.md`
- Compliance or retention gaps - How to avoid: add explicit FR/SC items and follow-up tasks

### What This Enables Next
- Option 1: Run `/speckit.plan` - Best if: the requirements look correct
- Option 2: Refine `spec.md` - Best if: requirements or edge cases are still unclear

Feature Status: user-authentication (Specified) -> Next: profile-management
Progress: 20% | Completed: 0 of 5 features | Dependencies: database-setup complete

Your options: [A] Proceed [B] Modify [C] Explain JWT choice [D] Show full status
```

## When to Skip Summaries

Skip the summarization loop only when:

- The user explicitly requests "skip summaries" or "run all steps automatically"
- You reran a command and no artifacts changed
- The command failed and you need to troubleshoot instead

## Why This Exists

- It removes the black-box feel of generated artifacts.
- It creates fast feedback loops before implementation starts.
- It keeps the user oriented through feature-status tracking.

## Feature Status Rules

Always include a brief status line after an SDD command. Show a detailed dashboard only when the user asks.

Stage mapping:

| Stage | Progress | Evidence |
|------:|---------:|----------|
| Specified | 20% | `spec.md` exists |
| Planned | 40% | `plan.md` exists |
| Tasked | 60% | `tasks.md` exists |
| In Progress | 80% | non-spec implementation work started |
| Complete | 100% | implementation finished, tests pass, review loop closed |

`Complete` inside SDD means the feature-delivery pipeline is complete, not that branch integration or repository task closure already happened.

For detailed dashboards and natural-language feature operations, see [Feature Management Guide](feature_management.md).
